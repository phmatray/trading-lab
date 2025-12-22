using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Strategies;

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
        List<HistoricalPrice> allData = await _historicalDataPort.GetHistoricalDataAsync(command.Ticker);

        if (allData.Count == 0)
        {
            throw new InvalidOperationException(
                $"No historical data found for {command.Ticker}. " +
                "Please run the data fetcher first to download historical data.");
        }

        // Determine date range
        DateTime? latestDate = await _historicalDataPort.GetLatestDataDateAsync(command.Ticker);
        DateTime endDate = command.EndDate ?? latestDate ?? DateTime.Today;
        DateTime startDate = command.StartDate ?? endDate.AddYears(-2);

        // Create strategy (custom or built-in)
        IStrategy strategy = command.CustomStrategyId.HasValue
            ? await _strategyFactory.CreateCustomStrategyFromIdAsync(command.CustomStrategyId.Value)
            : _strategyFactory.CreateStrategy(command.StrategyType, command.StrategyParameters);

        // Create backtest configuration
        var config = new BacktestConfiguration(
            Ticker: command.Ticker,
            StartDate: startDate,
            EndDate: endDate,
            InitialCapital: command.InitialCapital,
            CommissionPercentage: command.CommissionPercentage,
            MinimumCommission: command.MinimumCommission);

        // Run backtest
        Progress<(int current, int total, int trades)>? internalProgress = progress != null
            ? new Progress<(int current, int total, int trades)>(p =>
                progress.Report(new BacktestProgress(p.current, p.total, p.trades)))
            : null;

        BacktestResult result = await _backtestEngine.RunBacktestAsync(strategy, config, internalProgress);

        return result;
    }
}
