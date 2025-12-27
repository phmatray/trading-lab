using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for retrieving dashboard statistics.
/// Aggregates data from multiple ports to provide overview metrics.
/// </summary>
public class GetDashboardStatsUseCase : IGetDashboardStatsUseCase
{
    private readonly ICustomStrategyPort _customStrategyPort;
    private readonly IBacktestArchivePort _backtestArchivePort;
    private readonly IPortfolioPort _portfolioPort;
    private readonly IHistoricalDataPort _historicalDataPort;

    private const int BUILT_IN_STRATEGY_COUNT = 4; // RSI, MACD, MA Crossover, ML FastTree

    public GetDashboardStatsUseCase(
        ICustomStrategyPort customStrategyPort,
        IBacktestArchivePort backtestArchivePort,
        IPortfolioPort portfolioPort,
        IHistoricalDataPort historicalDataPort)
    {
        _customStrategyPort = customStrategyPort;
        _backtestArchivePort = backtestArchivePort;
        _portfolioPort = portfolioPort;
        _historicalDataPort = historicalDataPort;
    }

    public async Task<Result<DashboardStatsResult>> ExecuteAsync()
    {
        try
        {
            // Run all queries in parallel for better performance
            var customStrategiesTask = _customStrategyPort.GetAllAsync();
            var backtestCountTask = _backtestArchivePort.GetBacktestRunCountAsync();
            var portfoliosTask = _portfolioPort.GetAllPortfoliosAsync();
            var lastBacktestDateTask = _backtestArchivePort.GetLastBacktestDateAsync();
            var allTickersTask = _historicalDataPort.GetAllTickersAsync();
            var tickerSummariesTask = _historicalDataPort.GetAllTickerSummariesAsync(Domain.ValueObjects.TimeFrame.D1);
            var lastDataUpdateTask = _historicalDataPort.GetDatabaseLastModifiedAsync();

            await Task.WhenAll(
                customStrategiesTask,
                backtestCountTask,
                portfoliosTask,
                lastBacktestDateTask,
                allTickersTask,
                tickerSummariesTask,
                lastDataUpdateTask);

            List<Domain.Entities.CustomStrategy> customStrategies = await customStrategiesTask;
            int backtestCount = await backtestCountTask;
            List<Domain.Entities.Portfolio> portfolios = await portfoliosTask;
            DateTime? lastBacktestDate = await lastBacktestDateTask;
            List<string> allTickers = await allTickersTask;
            List<TickerSummary> tickerSummaries = await tickerSummariesTask;
            DateTime? lastDataUpdate = await lastDataUpdateTask;

            int totalStrategies = BUILT_IN_STRATEGY_COUNT + customStrategies.Count;
            int totalPortfolios = portfolios.Count;
            int totalSecurities = allTickers.Count;

            // Calculate data coverage: % of tickers with data in the last 7 days
            decimal dataCoveragePercentage = CalculateDataCoverage(tickerSummaries);

            return Result<DashboardStatsResult>.Success(new DashboardStatsResult(
                TotalStrategies: totalStrategies,
                TotalBacktests: backtestCount,
                TotalPortfolios: totalPortfolios,
                TotalSecurities: totalSecurities,
                DataCoveragePercentage: dataCoveragePercentage,
                LastBacktestDate: lastBacktestDate,
                LastDataUpdate: lastDataUpdate
            ));
        }
        catch (Exception ex)
        {
            return Result<DashboardStatsResult>.Failure(
                Error.BusinessRule($"Failed to retrieve dashboard statistics: {ex.Message}", "DASHBOARD_STATS_FAILED"));
        }
    }

    private static decimal CalculateDataCoverage(List<TickerSummary> tickerSummaries)
    {
        if (tickerSummaries.Count == 0)
        {
            return 0m;
        }

        DateTime sevenDaysAgo = DateTime.Today.AddDays(-7);
        int tickersWithRecentData = tickerSummaries.Count(t =>
            t.LatestDate.HasValue && t.LatestDate.Value >= sevenDaysAgo);

        return (decimal)tickersWithRecentData / tickerSummaries.Count * 100m;
    }
}
