using System.Diagnostics;
using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Orchestrates A/B parameter optimization by running two backtests and comparing results.
/// Follows hexagonal architecture pattern with dependency injection.
/// </summary>
public class RunParameterOptimizationUseCase : IParameterOptimizationUseCase
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly BacktestEngine _backtestEngine;
    private readonly IStrategyFactory _strategyFactory;

    public RunParameterOptimizationUseCase(
        IHistoricalDataPort historicalDataPort,
        BacktestEngine backtestEngine,
        IStrategyFactory strategyFactory)
    {
        _historicalDataPort = historicalDataPort;
        _backtestEngine = backtestEngine;
        _strategyFactory = strategyFactory;
    }

    public async Task<Result<ParameterOptimizationResult>> ExecuteAsync(
        ParameterOptimizationCommand command,
        IProgress<Application.Ports.Inbound.OptimizationProgress>? progress = null)
    {
        try
        {
            // Default to D1 (daily) if no timeframe specified
            TimeFrame timeFrame = command.TimeFrame ?? Domain.ValueObjects.TimeFrame.D1;

            Stopwatch stopwatch = Stopwatch.StartNew();

            // Validate data exists
            List<HistoricalPrice> data = await _historicalDataPort.GetHistoricalDataAsync(command.Ticker, timeFrame);
            if (data.Count == 0)
            {
                return Result<ParameterOptimizationResult>.Failure(
                    Error.InsufficientData(
                        $"No historical data found for {command.Ticker} ({timeFrame}). Please run the data fetcher first to download historical data.",
                        "NO_HISTORICAL_DATA"));
            }

        // Determine date range
        DateTime endDate = command.EndDate ?? await GetLatestDateAsync(command.Ticker, timeFrame);
        DateTime startDate = command.StartDate ?? endDate.AddYears(-2);

        // Run backtest for Variant A
        progress?.Report(new Application.Ports.Inbound.OptimizationProgress(
            command.VariantA.Label, 0, 0, 0));

        BacktestResult resultA = await RunSingleBacktest(
            command.VariantA,
            command.Ticker,
            timeFrame,
            startDate,
            endDate,
            command.InitialCapital,
            command.CommissionPercentage,
            command.MinimumCommission,
            variantLabel: command.VariantA.Label,
            progress);

        // Run backtest for Variant B
        progress?.Report(new Application.Ports.Inbound.OptimizationProgress(
            command.VariantB.Label, 0, 0, 0));

        BacktestResult resultB = await RunSingleBacktest(
            command.VariantB,
            command.Ticker,
            timeFrame,
            startDate,
            endDate,
            command.InitialCapital,
            command.CommissionPercentage,
            command.MinimumCommission,
            variantLabel: command.VariantB.Label,
            progress);

        // Calculate ranking
        ComparisonRanking ranking = ComparisonRanking.CalculateRanking(
            resultA.Metrics,
            resultB.Metrics);

        // Create comparison
        StrategyComparison comparison = new StrategyComparison(
            command.VariantA,
            resultA,
            command.VariantB,
            resultB,
            ranking,
            command.Ticker,
            DateTime.Now);

            stopwatch.Stop();

            return Result<ParameterOptimizationResult>.Success(
                new ParameterOptimizationResult(comparison, stopwatch.Elapsed));
        }
        catch (Exception ex)
        {
            return Result<ParameterOptimizationResult>.Failure(
                Error.BusinessRule($"Failed to execute parameter optimization: {ex.Message}", "PARAMETER_OPTIMIZATION_FAILED"));
        }
    }

    private async Task<DateTime> GetLatestDateAsync(string ticker, TimeFrame timeFrame)
    {
        DateTime? latestDate = await _historicalDataPort.GetLatestDataDateAsync(ticker, timeFrame);
        return latestDate ?? DateTime.Today;
    }

    private async Task<BacktestResult> RunSingleBacktest(
        StrategyVariant variant,
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate,
        decimal initialCapital,
        decimal commissionPercentage,
        decimal minimumCommission,
        string variantLabel,
        IProgress<Application.Ports.Inbound.OptimizationProgress>? progress)
    {
        // Create strategy using factory
        IStrategy strategy = _strategyFactory.CreateStrategy(
            variant.StrategyType,
            variant.Parameters);

        // Create backtest configuration
        BacktestConfiguration config = new BacktestConfiguration(
            Ticker: ticker,
            StartDate: startDate,
            EndDate: endDate,
            InitialCapital: initialCapital,
            CommissionPercentage: commissionPercentage,
            MinimumCommission: minimumCommission,
            TimeFrame: timeFrame);

        // Wrap progress reporter to include variant label
        Progress<(int current, int total, int trades)>? wrappedProgress = progress != null
            ? new Progress<(int current, int total, int trades)>(p =>
                progress.Report(new Application.Ports.Inbound.OptimizationProgress(
                    variantLabel, p.current, p.total, p.trades)))
            : null;

        // Run backtest
        BacktestResult result = await _backtestEngine.RunBacktestAsync(
            strategy,
            config,
            wrappedProgress);

        return result;
    }
}
