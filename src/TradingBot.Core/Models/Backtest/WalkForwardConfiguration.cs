// <copyright file="WalkForwardConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Backtest;

/// <summary>
/// Configuration for walk-forward optimization.
/// </summary>
public sealed class WalkForwardConfiguration
{
    /// <summary>
    /// Gets or sets the strategy name.
    /// </summary>
    public required string StrategyName { get; set; }

    /// <summary>
    /// Gets or sets the symbol to optimize.
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Gets or sets the optimization start date.
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the optimization end date.
    /// </summary>
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the number of walk-forward windows.
    /// </summary>
    public int NumberOfWindows { get; set; } = 5;

    /// <summary>
    /// Gets or sets the training period percentage (0.0 to 1.0).
    /// </summary>
    public decimal TrainingPercentage { get; set; } = 0.7m;

    /// <summary>
    /// Gets or sets a value indicating whether to use rolling windows.
    /// </summary>
    public bool UseRollingWindow { get; set; } = true;

    /// <summary>
    /// Gets or sets the parameter ranges to optimize.
    /// </summary>
    public required Dictionary<string, ParameterRange> ParameterRanges { get; set; }

    /// <summary>
    /// Gets or sets the optimization metric (Sharpe, Sortino, Return, etc.).
    /// </summary>
    public string OptimizationMetric { get; set; } = "Sharpe";

    /// <summary>
    /// Gets or sets the initial capital.
    /// </summary>
    public decimal InitialCapital { get; set; } = 10000m;

    /// <summary>
    /// Gets or sets a value indicating whether transaction costs are enabled.
    /// </summary>
    public bool EnableTransactionCosts { get; set; } = true;

    /// <summary>
    /// Gets or sets the commission per trade.
    /// </summary>
    public decimal CommissionPerTrade { get; set; } = 1m;

    /// <summary>
    /// Gets or sets the slippage percentage.
    /// </summary>
    public decimal SlippagePercent { get; set; } = 0.1m;
}
