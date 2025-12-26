using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;

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

    private const int BUILT_IN_STRATEGY_COUNT = 4; // RSI, MACD, MA Crossover, ML FastTree

    public GetDashboardStatsUseCase(
        ICustomStrategyPort customStrategyPort,
        IBacktestArchivePort backtestArchivePort,
        IPortfolioPort portfolioPort)
    {
        _customStrategyPort = customStrategyPort;
        _backtestArchivePort = backtestArchivePort;
        _portfolioPort = portfolioPort;
    }

    public async Task<DashboardStatsResult> ExecuteAsync()
    {
        // Run all queries in parallel for better performance
        var customStrategiesTask = _customStrategyPort.GetAllAsync();
        var backtestCountTask = _backtestArchivePort.GetBacktestRunCountAsync();
        var portfoliosTask = _portfolioPort.GetAllPortfoliosAsync();
        var lastBacktestDateTask = _backtestArchivePort.GetLastBacktestDateAsync();

        await Task.WhenAll(customStrategiesTask, backtestCountTask, portfoliosTask, lastBacktestDateTask);

        List<Domain.Entities.CustomStrategy> customStrategies = await customStrategiesTask;
        int backtestCount = await backtestCountTask;
        List<Domain.Entities.Portfolio> portfolios = await portfoliosTask;
        DateTime? lastBacktestDate = await lastBacktestDateTask;

        int totalStrategies = BUILT_IN_STRATEGY_COUNT + customStrategies.Count;
        int totalPortfolios = portfolios.Count;

        // TODO: Implement proper securities count and data coverage calculation
        // For now, return placeholder values
        int totalSecurities = 0;
        decimal dataCoveragePercentage = 0m;
        DateTime? lastDataUpdate = null;

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
