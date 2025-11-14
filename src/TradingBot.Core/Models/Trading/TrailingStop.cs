// <copyright file="TrailingStop.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Models.Trading;

/// <summary>
/// Represents a trailing stop-loss configuration.
/// </summary>
public sealed class TrailingStop
{
    /// <summary>
    /// Gets or sets the position ID this trailing stop is attached to.
    /// </summary>
    public required Guid PositionId { get; set; }

    /// <summary>
    /// Gets or sets the stop-loss order ID.
    /// </summary>
    public required Guid StopOrderId { get; set; }

    /// <summary>
    /// Gets or sets the trailing distance as percentage (e.g., 5.0 for 5%).
    /// </summary>
    public required decimal TrailingPercent { get; set; }

    /// <summary>
    /// Gets or sets the current stop price level.
    /// </summary>
    public required decimal CurrentStopPrice { get; set; }

    /// <summary>
    /// Gets or sets the highest price reached (for long positions).
    /// </summary>
    public decimal HighestPrice { get; set; }

    /// <summary>
    /// Gets or sets the lowest price reached (for short positions).
    /// </summary>
    public decimal LowestPrice { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is for a long position.
    /// </summary>
    public bool IsLong { get; set; } = true;

    /// <summary>
    /// Gets or sets when this trailing stop was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the stop level was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
