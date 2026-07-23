using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Strategies;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for AI-powered strategy analysis and recommendations.
/// Runs backtests, analyzes performance metrics, and generates structured insights using LLM.
/// </summary>
public interface IAnalyzeStrategyUseCase
{
    /// <summary>
    /// Analyzes a trading strategy using AI and returns structured recommendations.
    /// Combines backtest results with technical indicators to provide actionable insights.
    /// </summary>
    /// <param name="command">Command containing ticker, strategy type, and parameters.</param>
    /// <param name="cancellationToken">Cancellation token for request cancellation.</param>
    /// <returns>Result containing structured recommendation with summary, confidence scores, and action items, or errors if analysis fails.</returns>
    Task<Result<StrategyRecommendation>> ExecuteAsync(
        AnalyzeStrategyCommand command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Command object for requesting AI strategy analysis.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record AnalyzeStrategyCommand
{
    public string Ticker { get; init; }
    public StrategyType StrategyType { get; init; }
    public Dictionary<string, object>? StrategyParameters { get; init; }

    public AnalyzeStrategyCommand(
        string Ticker,
        StrategyType StrategyType,
        Dictionary<string, object>? StrategyParameters = null)
    {
        // Validate parameters
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();

        // Assign validated values
        this.Ticker = Ticker.ToUpperInvariant().Trim();
        this.StrategyType = StrategyType;
        this.StrategyParameters = StrategyParameters;
    }
}
