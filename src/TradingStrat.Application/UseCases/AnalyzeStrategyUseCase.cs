using System.Text.Json;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Application.Strategies;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for AI-powered strategy analysis and recommendations.
/// Combines backtest results with technical analysis to provide structured insights.
/// </summary>
public class AnalyzeStrategyUseCase : IAnalyzeStrategyUseCase
{
    private readonly IAssistantPort _assistantPort;
    private readonly PortfolioContextBuilder _contextBuilder;
    private readonly IBacktestUseCase _backtestUseCase;
    private readonly IStrategyRegistry _strategyRegistry;

    public AnalyzeStrategyUseCase(
        IAssistantPort assistantPort,
        PortfolioContextBuilder contextBuilder,
        IBacktestUseCase backtestUseCase,
        IStrategyRegistry strategyRegistry)
    {
        _assistantPort = assistantPort;
        _contextBuilder = contextBuilder;
        _backtestUseCase = backtestUseCase;
        _strategyRegistry = strategyRegistry;
    }

    public async Task<Result<StrategyRecommendation>> ExecuteAsync(
        AnalyzeStrategyCommand command,
        CancellationToken cancellationToken = default)
    {
        // Run quick backtest to get recent performance data (last 3 months)
        var backtestCommand = new BacktestCommand(
            Ticker: command.Ticker,
            StrategyType: command.StrategyType,
            StrategyParameters: command.StrategyParameters,
            StartDate: DateTime.Today.AddMonths(-3),
            EndDate: DateTime.Today
        );

        Result<BacktestResult> backtestResultWrapper = await _backtestUseCase.ExecuteAsync(backtestCommand);

        if (backtestResultWrapper.IsFailure)
        {
            // If backtest fails, return error
            string errorMessage = string.Join(", ", backtestResultWrapper.Errors.Select(e => e.Message));
            return Result<StrategyRecommendation>.Failure(
                Error.BusinessRule($"Unable to run backtest: {errorMessage}", "BACKTEST_FAILED"));
        }

        BacktestResult backtestResult = backtestResultWrapper.Value;

        // Build comprehensive market context
        string marketContext = await _contextBuilder.BuildContextForTicker(command.Ticker);

        // Calculate average profit/loss
        decimal avgProfitLoss = 0;
        if (backtestResult.Trades.Count > 0)
        {
            avgProfitLoss = backtestResult.Trades.Where(t => t.ProfitLoss.HasValue).Average(t => t.ProfitLoss!.Value);
        }

        // Build analysis prompt with backtest results and market data
        string analysisPrompt = $@"
{marketContext}

STRATEGY ANALYSIS REQUEST:
Strategy: {command.StrategyType}
Parameters: {JsonSerializer.Serialize(command.StrategyParameters ?? new Dictionary<string, object>())}

RECENT BACKTEST RESULTS (Last 3 Months):
- Total Return: {backtestResult.Metrics.TotalReturn:P2}
- Sharpe Ratio: {backtestResult.Metrics.SharpeRatio:F2}
- Win Rate: {backtestResult.Metrics.WinRate:P1}
- Max Drawdown: {backtestResult.Metrics.MaxDrawdown:P2}
- Total Trades: {backtestResult.Trades.Count}
- Winning Trades: {backtestResult.Trades.Count(t => t.ProfitLoss > 0)}
- Losing Trades: {backtestResult.Trades.Count(t => t.ProfitLoss < 0)}
- Average Profit/Loss per Trade: ${avgProfitLoss:F2}

Provide your analysis in the specified JSON format with actionable recommendations.
";

        // Get AI analysis (non-streaming for structured output)
        string response = await _assistantPort.GetChatResponseAsync(
            PromptTemplates.StrategyAnalysisSystemPrompt,
            new List<ChatMessage>(), // No conversation history for analysis
            analysisPrompt,
            cancellationToken);

        // Parse JSON response
        StrategyRecommendation? recommendation;

        // Extract JSON from response (in case LLM adds extra text)
        int jsonStart = response.IndexOf('{');
        int jsonEnd = response.LastIndexOf('}') + 1;
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            string jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart);

            try
            {
                recommendation = JsonSerializer.Deserialize<StrategyRecommendation>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                return Result<StrategyRecommendation>.Failure(
                    Error.BusinessRule($"Failed to parse AI response: {ex.Message}", "AI_RESPONSE_PARSE_ERROR"));
            }
        }
        else
        {
            return Result<StrategyRecommendation>.Failure(
                Error.BusinessRule("No JSON object found in AI response", "AI_RESPONSE_PARSE_ERROR"));
        }

        if (recommendation == null)
        {
            return Result<StrategyRecommendation>.Failure(
                Error.BusinessRule("Failed to deserialize AI recommendation", "AI_RESPONSE_PARSE_ERROR"));
        }

        // Set metadata
        recommendation.Ticker = command.Ticker;
        recommendation.StrategyType = _strategyRegistry.GetDescriptor(command.StrategyType).Key;
        recommendation.CreatedAt = DateTime.UtcNow;

        return Result<StrategyRecommendation>.Success(recommendation);
    }
}
