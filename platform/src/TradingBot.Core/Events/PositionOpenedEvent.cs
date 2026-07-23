// <copyright file="PositionOpenedEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Enums;
using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when a position is opened.
/// </summary>
public sealed class PositionOpenedEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PositionOpenedEvent"/> class.
    /// </summary>
    /// <param name="positionId">The position identifier.</param>
    /// <param name="symbol">The symbol.</param>
    /// <param name="side">The order side (Buy/Sell).</param>
    /// <param name="quantity">The quantity.</param>
    /// <param name="entryPrice">The entry price.</param>
    public PositionOpenedEvent(Guid positionId, string symbol, OrderSide side, decimal quantity, decimal entryPrice)
    {
        PositionId = positionId;
        Symbol = symbol;
        Side = side;
        Quantity = quantity;
        EntryPrice = entryPrice;
    }

    /// <summary>
    /// Gets the position identifier.
    /// </summary>
    public Guid PositionId { get; }

    /// <summary>
    /// Gets the symbol.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the order side.
    /// </summary>
    public OrderSide Side { get; }

    /// <summary>
    /// Gets the quantity.
    /// </summary>
    public decimal Quantity { get; }

    /// <summary>
    /// Gets the entry price.
    /// </summary>
    public decimal EntryPrice { get; }
}
