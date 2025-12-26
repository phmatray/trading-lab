namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for retrieving top performing strategies based on backtest results.
/// </summary>
public interface IGetTopStrategiesUseCase
{
    /// <summary>
    /// Executes the use case to retrieve top performing strategies.
    /// </summary>
    /// <param name="limit">Maximum number of strategies to return (default 5).</param>
    /// <returns>List of top strategies ordered by performance metric (Sharpe ratio).</returns>
    Task<List<TopStrategyResult>> ExecuteAsync(int limit = 5);
}

/// <summary>
/// Result object containing top strategy performance data.
/// </summary>
public sealed record TopStrategyResult(
    string StrategyName,
    string Ticker,
    decimal TotalReturn,
    decimal SharpeRatio,
    decimal MaxDrawdown,
    int TotalTrades,
    DateTime LastBacktestDate
);
