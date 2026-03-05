// <copyright file="Trade.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Enums;
using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Models.Trading;

/// <summary>
/// Represents a completed trade (closed position).
/// </summary>
public sealed class Trade : EntityBase<Guid>, IAggregateRoot
{
    /// <summary>
    /// Gets or sets the trading symbol.
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Gets or sets the trade side (Buy for long, Sell for short).
    /// </summary>
    public required OrderSide Side { get; set; }

    /// <summary>
    /// Gets or sets the trade quantity.
    /// </summary>
    public required decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the entry price.
    /// </summary>
    public required decimal EntryPrice { get; set; }

    /// <summary>
    /// Gets or sets the exit price.
    /// </summary>
    public required decimal ExitPrice { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the position was opened (UTC).
    /// </summary>
    public required DateTime EntryTime { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the position was closed (UTC).
    /// </summary>
    public required DateTime ExitTime { get; set; }

    /// <summary>
    /// Gets or sets the total commission paid (entry + exit).
    /// </summary>
    public decimal Commission { get; set; }

    /// <summary>
    /// Gets or sets the name of the strategy that executed this trade.
    /// </summary>
    public required string StrategyName { get; set; }

    /// <summary>
    /// Gets the realized profit/loss.
    /// </summary>
    public decimal RealizedPnL =>
        Side == OrderSide.Buy
            ? ((ExitPrice - EntryPrice) * Quantity) - Commission
            : ((EntryPrice - ExitPrice) * Quantity) - Commission;

    /// <summary>
    /// Gets the realized profit/loss percentage.
    /// </summary>
    public decimal RealizedPnLPercent =>
        Side == OrderSide.Buy
            ? ((ExitPrice - EntryPrice) / EntryPrice) * 100m
            : ((EntryPrice - ExitPrice) / EntryPrice) * 100m;

    /// <summary>
    /// Gets the trade duration.
    /// </summary>
    public TimeSpan Duration => ExitTime - EntryTime;

    /// <summary>
    /// Gets a value indicating whether the trade was profitable.
    /// </summary>
    public bool IsWinner => RealizedPnL > 0;
}
