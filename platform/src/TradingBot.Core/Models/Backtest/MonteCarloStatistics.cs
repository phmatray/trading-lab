// <copyright file="MonteCarloStatistics.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Backtest;

/// <summary>
/// Monte Carlo simulation statistics.
/// </summary>
public sealed class MonteCarloStatistics
{
    /// <summary>
    /// Gets or sets the mean return across all simulations.
    /// </summary>
    public required decimal MeanReturn { get; set; }

    /// <summary>
    /// Gets or sets the median return.
    /// </summary>
    public required decimal MedianReturn { get; set; }

    /// <summary>
    /// Gets or sets the standard deviation of returns.
    /// </summary>
    public required decimal StdDevReturn { get; set; }

    /// <summary>
    /// Gets or sets the minimum return observed.
    /// </summary>
    public required decimal MinReturn { get; set; }

    /// <summary>
    /// Gets or sets the maximum return observed.
    /// </summary>
    public required decimal MaxReturn { get; set; }

    /// <summary>
    /// Gets or sets the 5th percentile return (5% worst case).
    /// </summary>
    public required decimal Percentile5 { get; set; }

    /// <summary>
    /// Gets or sets the 25th percentile return.
    /// </summary>
    public required decimal Percentile25 { get; set; }

    /// <summary>
    /// Gets or sets the 75th percentile return.
    /// </summary>
    public required decimal Percentile75 { get; set; }

    /// <summary>
    /// Gets or sets the 95th percentile return (5% best case).
    /// </summary>
    public required decimal Percentile95 { get; set; }

    /// <summary>
    /// Gets or sets the mean maximum drawdown.
    /// </summary>
    public required decimal MeanDrawdown { get; set; }

    /// <summary>
    /// Gets or sets the worst drawdown observed across all simulations.
    /// </summary>
    public required decimal MaxDrawdownObserved { get; set; }

    /// <summary>
    /// Gets or sets the mean Sharpe ratio.
    /// </summary>
    public required decimal MeanSharpeRatio { get; set; }

    /// <summary>
    /// Gets or sets the probability of profit (percentage of simulations with positive return).
    /// </summary>
    public required decimal ProbabilityOfProfit { get; set; }
}
