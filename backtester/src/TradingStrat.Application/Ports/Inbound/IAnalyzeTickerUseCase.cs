using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Inbound port (use case interface) for analyzing a ticker using AI.
/// Generates market analysis with summary, recommendations, and actionable insights.
/// </summary>
public interface IAnalyzeTickerUseCase
{
    /// <summary>
    /// Analyzes a ticker and generates AI-powered insights.
    /// </summary>
    /// <param name="ticker">The ticker symbol to analyze.</param>
    /// <param name="progress">Optional progress reporter for data fetching.</param>
    /// <returns>Result containing the ticker analysis, or errors if operation failed.</returns>
    Task<Result<TickerAnalysis>> ExecuteAsync(
        string ticker,
        IProgress<string>? progress = null);
}
