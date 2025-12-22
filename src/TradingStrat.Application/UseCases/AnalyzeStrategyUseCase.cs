using System.Text.Json;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Application.Strategies;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Strategies;

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

    public async Task<StrategyRecommendation> ExecuteAsync(
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

        BacktestResult backtestResult;
        try
        {
            backtestResult = await _backtestUseCase.ExecuteAsync(backtestCommand);
        }
        catch (Exception ex)
        {
            // If backtest fails, return error recommendation
            // Convert enum to string for database storage
            StrategyDescriptor descriptor = _strategyRegistry.GetDescriptor(command.StrategyType);

            return new StrategyRecommendation
            {
                Ticker = command.Ticker,
                StrategyType = descriptor.Key,
                Summary = $"Unable to run backtest: {ex.Message}",
                Recommendation = "Cannot provide recommendation without backtest data. Please ensure historical data is available.",
                ActionItems = new List<ActionItem>
                {
                    new ActionItem
                    {
                        Description = "Fetch historical data for this ticker using the Data Management page",
                        Priority = "High",
                        ConfidenceLevel = 1.0m
                    }
                },
                Confidence = 0,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Build comprehensive market context
        string marketContext = await _contextBuilder.BuildContextForTicker(command.Ticker, 30);

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
        string response;
        try
        {
            response = await _assistantPort.GetChatResponseAsync(
                PromptTemplates.StrategyAnalysisSystemPrompt,
                new List<ChatMessage>(), // No conversation history for analysis
                analysisPrompt,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // If AI call fails, return basic recommendation
            // Convert enum to string for database storage
            StrategyDescriptor descriptor = _strategyRegistry.GetDescriptor(command.StrategyType);

            return new StrategyRecommendation
            {
                Ticker = command.Ticker,
                StrategyType = descriptor.Key,
                Summary = $"Backtest completed with {backtestResult.Metrics.TotalReturn:P2} return over 3 months.",
                Recommendation = $"AI analysis unavailable ({ex.Message}). Based on metrics: Sharpe {backtestResult.Metrics.SharpeRatio:F2}, Win Rate {backtestResult.Metrics.WinRate:P1}.",
                ActionItems = new List<ActionItem>
                {
                    new ActionItem
                    {
                        Description = "Review backtest results manually to assess strategy performance",
                        Priority = "Medium",
                        ConfidenceLevel = 0.5m
                    }
                },
                Confidence = 50,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Parse JSON response
        StrategyRecommendation? recommendation;
        try
        {
            // Extract JSON from response (in case LLM adds extra text)
            int jsonStart = response.IndexOf('{');
            int jsonEnd = response.LastIndexOf('}') + 1;
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                string jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart);
                recommendation = JsonSerializer.Deserialize<StrategyRecommendation>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            else
            {
                throw new JsonException("No JSON object found in response");
            }
        }
        catch (JsonException ex)
        {
            // If JSON parsing fails, create fallback recommendation
            // Convert enum to string for database storage
            StrategyDescriptor descriptor = _strategyRegistry.GetDescriptor(command.StrategyType);

            return new StrategyRecommendation
            {
                Ticker = command.Ticker,
                StrategyType = descriptor.Key,
                Summary = $"Strategy shows {backtestResult.Metrics.TotalReturn:P2} return with {backtestResult.Metrics.WinRate:P1} win rate.",
                Recommendation = $"AI response formatting issue ({ex.Message}). Review backtest metrics directly.",
                ActionItems = new List<ActionItem>
                {
                    new ActionItem
                    {
                        Description = "Check backtest results on the Backtest page for detailed analysis",
                        Priority = "High",
                        ConfidenceLevel = 1.0m
                    }
                },
                Confidence = 50,
                CreatedAt = DateTime.UtcNow
            };
        }

        if (recommendation == null)
        {
            throw new InvalidOperationException("Failed to deserialize AI recommendation");
        }

        // Set metadata
        recommendation.Ticker = command.Ticker;
        recommendation.StrategyType = _strategyRegistry.GetDescriptor(command.StrategyType).Key;
        recommendation.CreatedAt = DateTime.UtcNow;

        return recommendation;
    }
}
