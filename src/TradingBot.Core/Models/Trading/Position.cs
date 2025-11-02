// <copyright file="Position.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Enums;

namespace TradingBot.Core.Models.Trading;

/// <summary>
/// Represents an open trading position.
/// </summary>
public sealed class Position
{
    /// <summary>
    /// Gets or sets the unique identifier for this position.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the trading symbol.
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Gets or sets the position side (Buy for long, Sell for short).
    /// </summary>
    public required OrderSide Side { get; set; }

    /// <summary>
    /// Gets or sets the position quantity.
    /// </summary>
    public required decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the average entry price.
    /// </summary>
    public required decimal EntryPrice { get; set; }

    /// <summary>
    /// Gets or sets the current market price.
    /// </summary>
    public required decimal CurrentPrice { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the position was opened (UTC).
    /// </summary>
    public required DateTime OpenedAt { get; set; }

    /// <summary>
    /// Gets or sets the stop-loss price.
    /// </summary>
    public decimal? StopLoss { get; set; }

    /// <summary>
    /// Gets or sets the take-profit price.
    /// </summary>
    public decimal? TakeProfit { get; set; }

    /// <summary>
    /// Gets or sets the name of the strategy that opened this position.
    /// </summary>
    public required string StrategyName { get; set; }

    /// <summary>
    /// Gets the unrealized profit/loss.
    /// </summary>
    public decimal UnrealizedPnL =>
        Side == OrderSide.Buy
            ? (CurrentPrice - EntryPrice) * Quantity
            : (EntryPrice - CurrentPrice) * Quantity;

    /// <summary>
    /// Gets the unrealized profit/loss percentage.
    /// </summary>
    public decimal UnrealizedPnLPercent =>
        Side == OrderSide.Buy
            ? ((CurrentPrice - EntryPrice) / EntryPrice) * 100m
            : ((EntryPrice - CurrentPrice) / EntryPrice) * 100m;

    /// <summary>
    /// Gets the position value.
    /// </summary>
    public decimal PositionValue => Quantity * CurrentPrice;
}
