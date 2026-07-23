// <copyright file="MonteCarloSimulator.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

#pragma warning disable SA1204 // Static members should appear before non-static members

using Microsoft.Extensions.Logging;
using TradingBot.Core.Models.Backtest;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Analytics;

/// <summary>
/// Monte Carlo simulator for analyzing strategy robustness and risk.
/// </summary>
/// <remarks>
/// Performs Monte Carlo analysis by randomly resampling trade sequences
/// to understand the distribution of possible outcomes and assess risk.
/// </remarks>
public sealed class MonteCarloSimulator
{
    private readonly ILogger<MonteCarloSimulator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonteCarloSimulator"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public MonteCarloSimulator(ILogger<MonteCarloSimulator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs Monte Carlo simulation on backtest results.
    /// </summary>
    /// <param name="backtestResult">Original backtest result.</param>
    /// <param name="numberOfSimulations">Number of Monte Carlo simulations to run.</param>
    /// <param name="seed">Optional random seed for reproducibility.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Monte Carlo simulation results.</returns>
    public async Task<MonteCarloResult> SimulateAsync(
        BacktestResult backtestResult,
        int numberOfSimulations = 1000,
        int? seed = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting Monte Carlo simulation with {Simulations} runs for {Strategy}",
            numberOfSimulations,
            backtestResult.StrategyName);

        if (backtestResult.Trades.Count == 0)
        {
            throw new InvalidOperationException("Cannot run Monte Carlo simulation on backtest with no trades");
        }

        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        var simulations = new List<MonteCarloSimulation>();
        var calculator = new PerformanceCalculator();

        for (int i = 0; i < numberOfSimulations; i++)
        {
            // Randomly resample trades with replacement
            var resampledTrades = ResampleTrades(backtestResult.Trades.ToList(), random);

            // Calculate equity curve from resampled trades
            var equityCurve = CalculateEquityCurve(backtestResult.InitialCapital, resampledTrades);
            var finalEquity = equityCurve.Last().Equity;

            // Calculate performance metrics
            var performance = calculator.CalculateMetrics(
                resampledTrades,
                backtestResult.InitialCapital,
                finalEquity,
                equityCurve);

            simulations.Add(new MonteCarloSimulation
            {
                SimulationNumber = i + 1,
                FinalEquity = finalEquity,
                TotalReturn = performance.TotalReturn,
                MaxDrawdown = performance.MaxDrawdown,
                SharpeRatio = performance.SharpeRatio,
                ProfitFactor = performance.ProfitFactor,
            });

            // Progress reporting
            if ((i + 1) % 100 == 0 || i == numberOfSimulations - 1)
            {
                _logger.LogDebug("Completed {Count}/{Total} simulations", i + 1, numberOfSimulations);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        // Calculate statistics
        var returns = simulations.Select(s => s.TotalReturn).ToList();
        var drawdowns = simulations.Select(s => s.MaxDrawdown).ToList();
        var sharpeRatios = simulations.Select(s => s.SharpeRatio).ToList();

        var result = new MonteCarloResult
        {
            StrategyName = backtestResult.StrategyName,
            Symbol = backtestResult.Symbol,
            NumberOfSimulations = numberOfSimulations,
            OriginalReturn = backtestResult.TotalReturn,
            OriginalDrawdown = backtestResult.Performance.MaxDrawdown,
            Simulations = simulations,
            Statistics = new MonteCarloStatistics
            {
                MeanReturn = returns.Average(),
                MedianReturn = CalculateMedian(returns),
                StdDevReturn = CalculateStdDev(returns),
                MinReturn = returns.Min(),
                MaxReturn = returns.Max(),
                Percentile5 = CalculatePercentile(returns, 0.05m),
                Percentile25 = CalculatePercentile(returns, 0.25m),
                Percentile75 = CalculatePercentile(returns, 0.75m),
                Percentile95 = CalculatePercentile(returns, 0.95m),
                MeanDrawdown = drawdowns.Average(),
                MaxDrawdownObserved = drawdowns.Max(),
                MeanSharpeRatio = sharpeRatios.Average(),
                ProbabilityOfProfit = (decimal)simulations.Count(s => s.TotalReturn > 0) / numberOfSimulations,
            },
        };

        _logger.LogInformation(
            "Monte Carlo simulation completed. Mean return: {MeanReturn:F2}%, Probability of profit: {ProbProfit:P1}",
            result.Statistics.MeanReturn,
            result.Statistics.ProbabilityOfProfit);

        await Task.CompletedTask;
        return result;
    }

    private List<Trade> ResampleTrades(List<Trade> originalTrades, Random random)
    {
        var resampled = new List<Trade>();
        int count = originalTrades.Count;

        for (int i = 0; i < count; i++)
        {
            // Random sampling with replacement
            int index = random.Next(count);
            resampled.Add(originalTrades[index]);
        }

        return resampled;
    }

    private List<(DateTime Date, decimal Equity)> CalculateEquityCurve(
        decimal initialCapital,
        List<Trade> trades)
    {
        var curve = new List<(DateTime, decimal)>();
        var equity = initialCapital;
        var currentDate = DateTime.UtcNow;

        curve.Add((currentDate, equity));

        foreach (var trade in trades)
        {
            equity += trade.RealizedPnL;
            currentDate = currentDate.AddDays(1);
            curve.Add((currentDate, equity));
        }

        return curve;
    }

    private static decimal CalculateMedian(List<decimal> values)
    {
        if (values.Count == 0)
        {
            return 0m;
        }

        var sorted = values.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;

        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2m
            : sorted[mid];
    }

    private static decimal CalculateStdDev(List<decimal> values)
    {
        if (values.Count < 2)
        {
            return 0m;
        }

        var mean = values.Average();
        var sumOfSquares = values.Sum(v => (v - mean) * (v - mean));
        var variance = sumOfSquares / (values.Count - 1);

        return (decimal)Math.Sqrt((double)variance);
    }

    private static decimal CalculatePercentile(List<decimal> values, decimal percentile)
    {
        if (values.Count == 0)
        {
            return 0m;
        }

        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling((double)(percentile * sorted.Count)) - 1;
        index = Math.Max(0, Math.Min(sorted.Count - 1, index));

        return sorted[index];
    }
}
