namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for retrieving dashboard statistics.
/// </summary>
public interface IGetDashboardStatsUseCase
{
    /// <summary>
    /// Executes the use case to retrieve dashboard statistics.
    /// </summary>
    /// <returns>Dashboard statistics including strategy count, backtest count, portfolio count, and data coverage.</returns>
    Task<DashboardStatsResult> ExecuteAsync();
}

/// <summary>
/// Result object containing dashboard statistics.
/// </summary>
public sealed record DashboardStatsResult(
    int TotalStrategies,
    int TotalBacktests,
    int TotalPortfolios,
    int TotalSecurities,
    decimal DataCoveragePercentage,
    DateTime? LastBacktestDate,
    DateTime? LastDataUpdate
);
