using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for comparing multiple trading strategies side-by-side.
/// </summary>
public interface IMultiStrategyComparisonUseCase
{
    /// <summary>
    /// Executes the use case to compare multiple strategies.
    /// </summary>
    /// <param name="command">The command containing strategies to compare.</param>
    /// <param name="progress">Optional progress reporting.</param>
    /// <returns>Comparison result with metrics and equity curves for all strategies.</returns>
    Task<MultiStrategyComparisonResult> ExecuteAsync(
        MultiStrategyComparisonCommand command,
        IProgress<string>? progress = null);
}

/// <summary>
/// Command for comparing multiple trading strategies.
/// </summary>
public sealed record MultiStrategyComparisonCommand(
    string Ticker,
    List<StrategyConfiguration> Strategies,
    DateTime StartDate,
    DateTime EndDate,
    decimal InitialCapital,
    decimal CommissionPercentage,
    decimal MinimumCommission
);

/// <summary>
/// Configuration for a single strategy in the comparison.
/// </summary>
public sealed record StrategyConfiguration(
    string StrategyType,
    Dictionary<string, object> Parameters,
    int? CustomStrategyId = null
);

/// <summary>
/// Result containing comparison data for all strategies.
/// </summary>
public sealed record MultiStrategyComparisonResult(
    string Ticker,
    DateTime StartDate,
    DateTime EndDate,
    List<StrategyComparisonItem> Strategies,
    StrategyComparisonItem? BestByReturn,
    StrategyComparisonItem? BestBySharpe,
    StrategyComparisonItem? BestByDrawdown
);

/// <summary>
/// Comparison data for a single strategy.
/// </summary>
public sealed record StrategyComparisonItem(
    string StrategyName,
    string StrategyType,
    Dictionary<string, object> Parameters,
    PerformanceMetrics Metrics,
    List<EquityPoint> EquityCurve,
    int TotalTrades,
    int WinningTrades,
    int LosingTrades
);
