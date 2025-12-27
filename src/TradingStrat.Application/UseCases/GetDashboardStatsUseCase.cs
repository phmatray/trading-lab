using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for retrieving dashboard statistics.
/// Aggregates data from multiple ports to provide overview metrics.
/// Uses DataCoverageService for calculating data freshness.
/// Uses BaseUseCase to eliminate try-catch boilerplate.
/// </summary>
public class GetDashboardStatsUseCase : BaseUseCase<Unit, DashboardStatsResult>, IGetDashboardStatsUseCase
{
    private readonly ICustomStrategyPort _customStrategyPort;
    private readonly IBacktestArchivePort _backtestArchivePort;
    private readonly IPortfolioPort _portfolioPort;
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly DataCoverageService _dataCoverageService;

    private const int BUILT_IN_STRATEGY_COUNT = 4; // RSI, MACD, MA Crossover, ML FastTree

    public GetDashboardStatsUseCase(
        ICustomStrategyPort customStrategyPort,
        IBacktestArchivePort backtestArchivePort,
        IPortfolioPort portfolioPort,
        IHistoricalDataPort historicalDataPort,
        DataCoverageService dataCoverageService)
    {
        _customStrategyPort = customStrategyPort;
        _backtestArchivePort = backtestArchivePort;
        _portfolioPort = portfolioPort;
        _historicalDataPort = historicalDataPort;
        _dataCoverageService = dataCoverageService;
    }

    public Task<Result<DashboardStatsResult>> ExecuteAsync()
        => base.ExecuteAsync(Unit.Value, ExecuteCoreAsync, ErrorCodes.Dashboard.StatsFailed);

    private async Task<DashboardStatsResult> ExecuteCoreAsync(Unit _)
    {
        // Run all queries in parallel for better performance
        Task<List<CustomStrategy>> customStrategiesTask = _customStrategyPort.GetAllAsync();
        Task<int> backtestCountTask = _backtestArchivePort.GetBacktestRunCountAsync();
        Task<List<Portfolio>> portfoliosTask = _portfolioPort.GetAllPortfoliosAsync();
        Task<DateTime?> lastBacktestDateTask = _backtestArchivePort.GetLastBacktestDateAsync();
        Task<List<string>> allTickersTask = _historicalDataPort.GetAllTickersAsync();
        Task<List<TickerSummary>> tickerSummariesTask = _historicalDataPort.GetAllTickerSummariesAsync(TimeFrame.D1);
        Task<DateTime?> lastDataUpdateTask = _historicalDataPort.GetDatabaseLastModifiedAsync();

        await Task.WhenAll(
            customStrategiesTask,
            backtestCountTask,
            portfoliosTask,
            lastBacktestDateTask,
            allTickersTask,
            tickerSummariesTask,
            lastDataUpdateTask);

        List<CustomStrategy> customStrategies = await customStrategiesTask;
        int backtestCount = await backtestCountTask;
        List<Portfolio> portfolios = await portfoliosTask;
        DateTime? lastBacktestDate = await lastBacktestDateTask;
        List<string> allTickers = await allTickersTask;
        List<TickerSummary> tickerSummaries = await tickerSummariesTask;
        DateTime? lastDataUpdate = await lastDataUpdateTask;

        int totalStrategies = BUILT_IN_STRATEGY_COUNT + customStrategies.Count;
        int totalPortfolios = portfolios.Count;
        int totalSecurities = allTickers.Count;

        // Calculate data coverage using domain service
        decimal dataCoveragePercentage = _dataCoverageService.CalculateDataCoveragePercentage(tickerSummaries);

        return new DashboardStatsResult(
            TotalStrategies: totalStrategies,
            TotalBacktests: backtestCount,
            TotalPortfolios: totalPortfolios,
            TotalSecurities: totalSecurities,
            DataCoveragePercentage: dataCoveragePercentage,
            LastBacktestDate: lastBacktestDate,
            LastDataUpdate: lastDataUpdate
        );
    }
}
