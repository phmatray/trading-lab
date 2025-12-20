using TradingStrat.Domain.Entities;

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
    /// <returns>Structured recommendation with summary, confidence scores, and action items.</returns>
    Task<StrategyRecommendation> ExecuteAsync(
        AnalyzeStrategyCommand command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Command object for requesting AI strategy analysis.
/// </summary>
/// <param name="Ticker">Stock ticker symbol to analyze.</param>
/// <param name="StrategyType">Strategy identifier (e.g., "ma", "rsi", "macd", "ml").</param>
/// <param name="StrategyParameters">Optional strategy-specific parameters (e.g., periods, thresholds).</param>
public record AnalyzeStrategyCommand(
    string Ticker,
    string StrategyType,
    Dictionary<string, object>? StrategyParameters = null);
