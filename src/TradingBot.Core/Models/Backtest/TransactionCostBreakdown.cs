// <copyright file="TransactionCostBreakdown.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Backtest;

/// <summary>
/// Breakdown of transaction costs.
/// </summary>
public sealed class TransactionCostBreakdown
{
    /// <summary>
    /// Gets or sets the commission cost.
    /// </summary>
    public decimal Commission { get; set; }

    /// <summary>
    /// Gets or sets the slippage cost.
    /// </summary>
    public decimal Slippage { get; set; }

    /// <summary>
    /// Gets or sets the spread cost.
    /// </summary>
    public decimal Spread { get; set; }

    /// <summary>
    /// Gets or sets the total cost.
    /// </summary>
    public decimal TotalCost { get; set; }
}
