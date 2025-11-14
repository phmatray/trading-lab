// <copyright file="WalkForwardOptimizer.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

#pragma warning disable SA1204 // Static members should appear before non-static members

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Backtest;

namespace TradingBot.Analytics;

/// <summary>
/// Implements walk-forward optimization for strategy parameter tuning.
/// </summary>
/// <remarks>
/// Walk-forward optimization divides historical data into training and testing windows,
/// optimizes parameters on training data, and validates on out-of-sample testing data.
/// This helps prevent overfitting and provides more robust parameter selection.
/// </remarks>
public sealed class WalkForwardOptimizer
{
    private readonly ILogger<WalkForwardOptimizer> _logger;
    private readonly IBacktestingEngine _backtestingEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="WalkForwardOptimizer"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="backtestingEngine">Backtesting engine.</param>
    public WalkForwardOptimizer(
        ILogger<WalkForwardOptimizer> logger,
        IBacktestingEngine backtestingEngine)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _backtestingEngine = backtestingEngine ?? throw new ArgumentNullException(nameof(backtestingEngine));
    }

    /// <summary>
    /// Performs walk-forward optimization on a strategy.
    /// </summary>
    /// <param name="config">Walk-forward optimization configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Walk-forward optimization result.</returns>
    public async Task<WalkForwardResult> OptimizeAsync(
        WalkForwardConfiguration config,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting walk-forward optimization for {Strategy} with {Windows} windows",
            config.StrategyName,
            config.NumberOfWindows);

        var windows = CreateWindows(config);
        var windowResults = new List<WalkForwardWindowResult>();

        for (int i = 0; i < windows.Count; i++)
        {
            var window = windows[i];
            _logger.LogInformation(
                "Processing window {Index}/{Total}: Training {TrainStart:yyyy-MM-dd} to {TrainEnd:yyyy-MM-dd}, " +
                "Testing {TestStart:yyyy-MM-dd} to {TestEnd:yyyy-MM-dd}",
                i + 1,
                windows.Count,
                window.TrainingStart,
                window.TrainingEnd,
                window.TestingStart,
                window.TestingEnd);

            // Optimize on training period
            var bestParameters = await OptimizeParametersAsync(
                config,
                window.TrainingStart,
                window.TrainingEnd,
                cancellationToken);

            // Validate on testing period
            var testResult = await ValidateParametersAsync(
                config,
                bestParameters,
                window.TestingStart,
                window.TestingEnd,
                cancellationToken);

            windowResults.Add(new WalkForwardWindowResult
            {
                WindowNumber = i + 1,
                TrainingStart = window.TrainingStart,
                TrainingEnd = window.TrainingEnd,
                TestingStart = window.TestingStart,
                TestingEnd = window.TestingEnd,
                BestParameters = bestParameters,
                TestingResult = testResult,
            });

            _logger.LogInformation(
                "Window {Index} completed. Best parameters: {Parameters}, Test return: {Return:F2}%",
                i + 1,
                string.Join(", ", bestParameters.Select(p => $"{p.Key}={p.Value}")),
                testResult.TotalReturn);
        }

        // Calculate overall statistics
        var result = new WalkForwardResult
        {
            StrategyName = config.StrategyName,
            Symbol = config.Symbol,
            StartDate = config.StartDate,
            EndDate = config.EndDate,
            NumberOfWindows = windows.Count,
            WindowResults = windowResults,
            AverageReturn = windowResults.Average(w => w.TestingResult.TotalReturn),
            MedianReturn = CalculateMedian(windowResults.Select(w => w.TestingResult.TotalReturn).ToList()),
            WinningWindows = windowResults.Count(w => w.TestingResult.TotalReturn > 0),
            TotalWindows = windowResults.Count,
        };

        _logger.LogInformation(
            "Walk-forward optimization completed. Average return: {AvgReturn:F2}%, Win rate: {WinRate:F1}%",
            result.AverageReturn,
            (decimal)result.WinningWindows / result.TotalWindows * 100m);

        return result;
    }

    private List<Window> CreateWindows(WalkForwardConfiguration config)
    {
        var windows = new List<Window>();
        var totalDays = (config.EndDate - config.StartDate).Days;
        var windowDays = totalDays / config.NumberOfWindows;
        var trainingDays = (int)(windowDays * config.TrainingPercentage);
        var testingDays = windowDays - trainingDays;

        var currentDate = config.StartDate;

        for (int i = 0; i < config.NumberOfWindows; i++)
        {
            var trainingStart = currentDate;
            var trainingEnd = trainingStart.AddDays(trainingDays);
            var testingStart = trainingEnd;
            var testingEnd = testingStart.AddDays(testingDays);

            // Ensure we don't exceed the end date
            if (testingEnd > config.EndDate)
            {
                testingEnd = config.EndDate;
            }

            windows.Add(new Window
            {
                TrainingStart = trainingStart,
                TrainingEnd = trainingEnd,
                TestingStart = testingStart,
                TestingEnd = testingEnd,
            });

            // Move to next window (with optional overlap)
            currentDate = config.UseRollingWindow
                ? trainingStart.AddDays(testingDays)
                : testingEnd;
        }

        return windows;
    }

    private async Task<Dictionary<string, decimal>> OptimizeParametersAsync(
        WalkForwardConfiguration config,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Optimizing parameters on training period");

        // Generate parameter combinations
        var parameterSets = GenerateParameterCombinations(config.ParameterRanges);

        BacktestResult? bestResult = null;
        Dictionary<string, decimal>? bestParameters = null;
        var bestMetric = decimal.MinValue;

        foreach (var parameters in parameterSets)
        {
            var backtestConfig = new BacktestConfiguration
            {
                BacktestId = Guid.NewGuid().ToString(),
                StrategyName = config.StrategyName,
                Symbol = config.Symbol,
                StartDate = startDate,
                EndDate = endDate,
                InitialCapital = config.InitialCapital,
                EnableTransactionCosts = config.EnableTransactionCosts,
                CommissionPerTrade = config.CommissionPerTrade,
                SlippagePercent = config.SlippagePercent,
            };

            var result = await _backtestingEngine.RunBacktestAsync(backtestConfig, cancellationToken);

            // Evaluate using optimization metric
            var metric = EvaluateResult(result, config.OptimizationMetric);

            if (metric > bestMetric)
            {
                bestMetric = metric;
                bestResult = result;
                bestParameters = parameters;
            }
        }

        _logger.LogDebug(
            "Optimization complete. Best metric value: {Metric:F2}",
            bestMetric);

        return bestParameters ?? new Dictionary<string, decimal>();
    }

    private async Task<BacktestResult> ValidateParametersAsync(
        WalkForwardConfiguration config,
        Dictionary<string, decimal> parameters,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Validating parameters on testing period");

        var backtestConfig = new BacktestConfiguration
        {
            BacktestId = Guid.NewGuid().ToString(),
            StrategyName = config.StrategyName,
            Symbol = config.Symbol,
            StartDate = startDate,
            EndDate = endDate,
            InitialCapital = config.InitialCapital,
            EnableTransactionCosts = config.EnableTransactionCosts,
            CommissionPerTrade = config.CommissionPerTrade,
            SlippagePercent = config.SlippagePercent,
        };

        return await _backtestingEngine.RunBacktestAsync(backtestConfig, cancellationToken);
    }

    private List<Dictionary<string, decimal>> GenerateParameterCombinations(
        Dictionary<string, ParameterRange> parameterRanges)
    {
        // Generate all combinations using Cartesian product
        var combinations = new List<Dictionary<string, decimal>>();

        // Start with empty combination
        var current = new Dictionary<string, decimal>();
        GenerateCombinationsRecursive(
            parameterRanges.ToList(),
            0,
            current,
            combinations);

        _logger.LogDebug("Generated {Count} parameter combinations", combinations.Count);

        return combinations;
    }

    private void GenerateCombinationsRecursive(
        List<KeyValuePair<string, ParameterRange>> parameters,
        int index,
        Dictionary<string, decimal> current,
        List<Dictionary<string, decimal>> result)
    {
        if (index >= parameters.Count)
        {
            result.Add(new Dictionary<string, decimal>(current));
            return;
        }

        var param = parameters[index];
        var range = param.Value;

        for (decimal value = range.Min; value <= range.Max; value += range.Step)
        {
            current[param.Key] = value;
            GenerateCombinationsRecursive(parameters, index + 1, current, result);
        }
    }

    private decimal EvaluateResult(BacktestResult result, string optimizationMetric)
    {
        return optimizationMetric.ToLowerInvariant() switch
        {
            "sharpe" or "sharperatio" => result.Performance.SharpeRatio,
            "sortino" or "sortinoratio" => result.Performance.SortinoRatio,
            "calmar" or "calmarratio" => result.Performance.CalmarRatio,
            "return" or "totalreturn" => result.Performance.TotalReturn,
            "profitfactor" => result.Performance.ProfitFactor,
            _ => result.Performance.TotalReturn,
        };
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

    private sealed class Window
    {
        public DateTime TrainingStart { get; set; }

        public DateTime TrainingEnd { get; set; }

        public DateTime TestingStart { get; set; }

        public DateTime TestingEnd { get; set; }
    }
}
