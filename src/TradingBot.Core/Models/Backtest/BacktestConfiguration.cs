// <copyright file="BacktestConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Backtest;

/// <summary>
/// Configuration for running a backtest.
/// </summary>
public sealed class BacktestConfiguration
{
    /// <summary>
    /// Gets or sets the backtest identifier.
    /// </summary>
    public required string BacktestId { get; set; }

    /// <summary>
    /// Gets or sets the strategy name to backtest.
    /// </summary>
    public required string StrategyName { get; set; }

    /// <summary>
    /// Gets or sets the symbol to backtest.
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Gets or sets the start date for the backtest.
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for the backtest.
    /// </summary>
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the initial capital amount.
    /// </summary>
    public required decimal InitialCapital { get; set; }

    /// <summary>
    /// Gets or sets the commission per trade.
    /// </summary>
    public decimal CommissionPerTrade { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets the slippage percentage.
    /// </summary>
    public decimal SlippagePercent { get; set; } = 0.1m;

    /// <summary>
    /// Gets or sets a value indicating whether to enable transaction costs.
    /// </summary>
    public bool EnableTransactionCosts { get; set; } = true;
}
