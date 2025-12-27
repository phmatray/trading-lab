using TradingStrat.Application.Common;
using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Common;
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

    public async Task<Result<BacktestResult>> ExecuteAsync(
        BacktestCommand command,
        IProgress<BacktestProgress>? progress = null)
    {
        // Command validation happens in constructor - command is guaranteed to be valid here

        try
        {
            // Default to D1 (daily) if no timeframe specified
            var timeFrame = command.TimeFrame ?? Domain.ValueObjects.TimeFrame.D1;

            // Check if data exists
            List<HistoricalPrice> allData = await _historicalDataPort.GetHistoricalDataAsync(
                command.Ticker,
                timeFrame);

            if (allData.Count == 0)
            {
                return Result<BacktestResult>.Failure(
                    Error.InsufficientData(
                        $"No historical data found for {command.Ticker} ({timeFrame}). Please run the data fetcher first to download historical data.",
                        ErrorCodes.Data.NoHistoricalData));
            }

            // Determine date range
            DateTime? latestDate = await _historicalDataPort.GetLatestDataDateAsync(command.Ticker, timeFrame);
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
                MinimumCommission: command.MinimumCommission,
                TimeFrame: timeFrame,
                TradingStyle: command.TradingStyle);

            // Run backtest
            Progress<(int current, int total, int trades)>? internalProgress = progress != null
                ? new Progress<(int current, int total, int trades)>(p =>
                    progress.Report(new BacktestProgress(p.current, p.total, p.trades)))
                : null;

            BacktestResult result = await _backtestEngine.RunBacktestAsync(strategy, config, internalProgress);

            return Result<BacktestResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<BacktestResult>.Failure(
                Error.BusinessRule($"Failed to execute backtest: {ex.Message}", ErrorCodes.Backtest.ExecutionFailed));
        }
    }
}
