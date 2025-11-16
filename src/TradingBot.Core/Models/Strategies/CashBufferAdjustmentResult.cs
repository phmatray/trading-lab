// <copyright file="CashBufferAdjustmentResult.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Strategies;

/// <summary>
/// Result object returned by cash buffer adjustment execution.
/// Contains adjustment details and before/after ratios.
/// </summary>
public sealed class CashBufferAdjustmentResult
{
    /// <summary>
    /// Gets or sets a value indicating whether an adjustment was made.
    /// </summary>
    public bool Adjusted { get; set; }

    /// <summary>
    /// Gets or sets the action taken (Buy, Sell, or None).
    /// </summary>
    public string Action { get; set; } = "None";

    /// <summary>
    /// Gets or sets the order ID if an adjustment order was placed.
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Gets or sets the cash ratio before adjustment.
    /// </summary>
    public decimal CashRatioBefore { get; set; }

    /// <summary>
    /// Gets or sets the cash ratio after adjustment.
    /// </summary>
    public decimal CashRatioAfter { get; set; }

    /// <summary>
    /// Gets or sets the adjustment amount (positive for buy, negative for sell).
    /// </summary>
    public decimal AdjustmentAmount { get; set; }

    /// <summary>
    /// Gets or sets the reason for adjustment or why no adjustment was made.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
