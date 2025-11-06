// <copyright file="TransactionCostModel.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Backtest;

/// <summary>
/// Model for transaction cost configuration.
/// </summary>
public sealed class TransactionCostModel
{
    /// <summary>
    /// Gets or sets the commission charged per trade.
    /// </summary>
    public decimal CommissionPerTrade { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets the commission charged per share/unit.
    /// </summary>
    public decimal CommissionPerShare { get; set; } = 0.0m;

    /// <summary>
    /// Gets or sets the slippage as a percentage (e.g., 0.1 for 0.1%).
    /// </summary>
    public decimal SlippagePercent { get; set; } = 0.1m;

    /// <summary>
    /// Gets or sets the bid-ask spread as a percentage (e.g., 0.05 for 0.05%).
    /// </summary>
    public decimal SpreadPercent { get; set; } = 0.05m;

    /// <summary>
    /// Gets or sets a value indicating whether to apply transaction costs.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
