using TradingStrat.Domain.Common;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for retrieving dashboard statistics.
/// </summary>
public interface IGetDashboardStatsUseCase
{
    /// <summary>
    /// Executes the use case to retrieve dashboard statistics.
    /// </summary>
    /// <returns>Result containing dashboard statistics including strategy count, backtest count, portfolio count, and data coverage, or errors if retrieval fails.</returns>
    Task<Result<DashboardStatsResult>> ExecuteAsync();
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
