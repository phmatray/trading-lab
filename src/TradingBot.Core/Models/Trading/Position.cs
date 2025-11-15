// <copyright file="Position.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.SharedKernel;
using TradingBot.Core.Enums;
using TradingBot.Core.Events;

namespace TradingBot.Core.Models.Trading;

/// <summary>
/// Represents an open trading position aggregate root.
/// </summary>
public sealed class Position : EntityBase<Guid>, IAggregateRoot
{
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

    /// <summary>
    /// Updates the current price of the position.
    /// </summary>
    /// <param name="newPrice">The new current price.</param>
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
        {
            throw new ArgumentException("Price must be positive", nameof(newPrice));
        }

        CurrentPrice = newPrice;

        RegisterDomainEvent(new PositionPriceUpdatedEvent(Id, Symbol, newPrice, UnrealizedPnL));
    }

    /// <summary>
    /// Closes the position at the specified exit price.
    /// </summary>
    /// <param name="exitPrice">The exit price.</param>
    /// <param name="exitTime">The exit time.</param>
    /// <returns>The realized profit and loss.</returns>
    public decimal Close(decimal exitPrice, DateTime exitTime)
    {
        if (exitPrice <= 0)
        {
            throw new ArgumentException("Exit price must be positive", nameof(exitPrice));
        }

        var realizedPnL = CalculateRealizedPnL(exitPrice);

        RegisterDomainEvent(new PositionClosedEvent(Id, Symbol, realizedPnL));

        return realizedPnL;
    }

    /// <summary>
    /// Increases the quantity of the position (averaging in).
    /// </summary>
    /// <param name="additionalQuantity">The additional quantity to add.</param>
    /// <param name="newAveragePrice">The new average entry price after adding quantity.</param>
    public void IncreaseQuantity(decimal additionalQuantity, decimal newAveragePrice)
    {
        if (additionalQuantity <= 0)
        {
            throw new ArgumentException("Additional quantity must be positive", nameof(additionalQuantity));
        }

        if (newAveragePrice <= 0)
        {
            throw new ArgumentException("New average price must be positive", nameof(newAveragePrice));
        }

        Quantity += additionalQuantity;
        EntryPrice = newAveragePrice;

        // Note: No domain event for quantity increase as it's part of the same position
        // Could add PositionQuantityIncreasedEvent if needed for auditing
    }

    private decimal CalculateRealizedPnL(decimal exitPrice)
    {
        return Side == OrderSide.Buy
            ? (exitPrice - EntryPrice) * Quantity
            : (EntryPrice - exitPrice) * Quantity;
    }
}
