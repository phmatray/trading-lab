using System.Text.Json;
using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for analyzing a ticker using AI.
/// Fetches market data, builds context with technical indicators, and generates AI-powered insights.
/// </summary>
public class AnalyzeTickerUseCase : IAnalyzeTickerUseCase
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly IAssistantPort _assistantPort;
    private readonly PortfolioContextBuilder _contextBuilder;

    public AnalyzeTickerUseCase(
        IHistoricalDataPort historicalDataPort,
        IAssistantPort assistantPort,
        PortfolioContextBuilder contextBuilder)
    {
        _historicalDataPort = historicalDataPort ?? throw new ArgumentNullException(nameof(historicalDataPort));
        _assistantPort = assistantPort ?? throw new ArgumentNullException(nameof(assistantPort));
        _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
    }

    /// <inheritdoc />
    public async Task<Result<TickerAnalysis>> ExecuteAsync(
        string ticker,
        IProgress<string>? progress = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return Result<TickerAnalysis>.Failure(
                    Error.Validation("Ticker is required", ErrorCodes.Data.TickerRequired));
            }

            progress?.Report($"Fetching market data for {ticker}...");

            // Fetch 30 days of historical data
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddDays(-30);

            List<HistoricalPrice> historicalPrices = await _historicalDataPort
                .GetHistoricalDataAsync(ticker, TimeFrame.D1, startDate, endDate);

            if (historicalPrices.Count == 0)
            {
                return Result<TickerAnalysis>.Failure(
                    Error.InsufficientData(
                        $"No historical data available for {ticker}. Please fetch data first.",
                        ErrorCodes.Data.NoHistoricalData));
            }

            progress?.Report("Calculating technical indicators...");

            // Build market context with technical indicators
            string marketContext = await _contextBuilder.BuildContextForTicker(ticker, daysBack: 30);

            // Get current price and daily change
            HistoricalPrice latestPrice = historicalPrices[^1];
            decimal currentPrice = latestPrice.Close ?? 0m;
            decimal dailyChangePercent = 0m;

            if (historicalPrices.Count > 1)
            {
                decimal previousClose = historicalPrices[^2].Close ?? 0m;
                if (previousClose > 0)
                {
                    dailyChangePercent = ((currentPrice - previousClose) / previousClose) * 100m;
                }
            }

            progress?.Report("Generating AI analysis...");

            // Call AI for structured analysis (no conversation history needed for ticker analysis)
            string aiResponse = await _assistantPort.GetChatResponseAsync(
                systemPrompt: PromptTemplates.TickerAnalysisSystemPrompt,
                conversationHistory: new List<ChatMessage>(),
                userMessage: marketContext);

            progress?.Report("Parsing analysis results...");

            // Parse JSON response
            TickerAnalysisDto? dto = JsonSerializer.Deserialize<TickerAnalysisDto>(
                aiResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dto is null)
            {
                return Result<TickerAnalysis>.Failure(
                    Error.BusinessRule(
                        "Failed to parse AI analysis response",
                        ErrorCodes.Analysis.AnalysisFailed));
            }

            // Create TickerAnalysis value object
            TickerAnalysis analysis = new(
                ticker: ticker,
                currentPrice: currentPrice,
                dailyChangePercent: dailyChangePercent,
                summary: dto.Summary ?? "No summary available",
                recommendation: dto.Recommendation ?? "HOLD",
                actionableInsights: dto.ActionableInsights ?? new List<string>(),
                confidence: dto.Confidence,
                generatedAt: DateTime.UtcNow);

            progress?.Report("Analysis complete");

            return Result<TickerAnalysis>.Success(analysis);
        }
        catch (JsonException ex)
        {
            return Result<TickerAnalysis>.Failure(
                Error.BusinessRule(
                    $"Failed to parse AI response as JSON: {ex.Message}",
                    ErrorCodes.Analysis.AnalysisFailed));
        }
        catch (Exception ex)
        {
            return Result<TickerAnalysis>.Failure(
                Error.BusinessRule(
                    $"Failed to analyze ticker: {ex.Message}",
                    ErrorCodes.Analysis.AnalysisFailed));
        }
    }

    /// <summary>
    /// DTO for deserializing AI analysis JSON response.
    /// </summary>
    private class TickerAnalysisDto
    {
        public string? Summary { get; set; }
        public string? Recommendation { get; set; }
        public List<string>? ActionableInsights { get; set; }
        public int Confidence { get; set; }
    }
}
