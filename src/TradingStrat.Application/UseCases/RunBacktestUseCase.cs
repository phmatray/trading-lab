using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

public class RunBacktestUseCase : IBacktestUseCase
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly BacktestEngine _backtestEngine;
    private readonly IStrategyFactory _strategyFactory;

    public RunBacktestUseCase(
        IHistoricalDataPort historicalDataPort,
        BacktestEngine backtestEngine,
        IStrategyFactory strategyFactory)
    {
        _historicalDataPort = historicalDataPort;
        _backtestEngine = backtestEngine;
        _strategyFactory = strategyFactory;
    }

    public async Task<BacktestResult> ExecuteAsync(
        BacktestCommand command,
        IProgress<BacktestProgress>? progress = null)
    {
        // Check if data exists
        var allData = await _historicalDataPort.GetHistoricalDataAsync(command.Ticker);

        if (allData.Count == 0)
        {
            throw new InvalidOperationException(
                $"No historical data found for {command.Ticker}. " +
                "Please run the data fetcher first to download historical data.");
        }

        // Determine date range
        var latestDate = await _historicalDataPort.GetLatestDataDateAsync(command.Ticker);
        var endDate = command.EndDate ?? latestDate ?? DateTime.Today;
        var startDate = command.StartDate ?? endDate.AddYears(-2);

        // Create strategy
        var strategy = _strategyFactory.CreateStrategy(
            command.StrategyType,
            command.StrategyParameters);

        // Create backtest configuration
        var config = new BacktestConfiguration(
            Ticker: command.Ticker,
            StartDate: startDate,
            EndDate: endDate,
            InitialCapital: command.InitialCapital,
            CommissionPercentage: command.CommissionPercentage,
            MinimumCommission: command.MinimumCommission);

        // Run backtest
        var internalProgress = progress != null
            ? new Progress<(int current, int total, int trades)>(p =>
                progress.Report(new BacktestProgress(p.current, p.total, p.trades)))
            : null;

        var result = await _backtestEngine.RunBacktestAsync(strategy, config, internalProgress);

        return result;
    }
}
