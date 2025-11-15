// <copyright file="PositionPriceUpdatedEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when a position's price is updated.
/// </summary>
public sealed class PositionPriceUpdatedEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PositionPriceUpdatedEvent"/> class.
    /// </summary>
    /// <param name="positionId">The position identifier.</param>
    /// <param name="symbol">The symbol.</param>
    /// <param name="newPrice">The new current price.</param>
    /// <param name="unrealizedPnL">The unrealized profit and loss.</param>
    public PositionPriceUpdatedEvent(Guid positionId, string symbol, decimal newPrice, decimal unrealizedPnL)
    {
        PositionId = positionId;
        Symbol = symbol;
        NewPrice = newPrice;
        UnrealizedPnL = unrealizedPnL;
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
    /// Gets the new current price.
    /// </summary>
    public decimal NewPrice { get; }

    /// <summary>
    /// Gets the unrealized profit and loss.
    /// </summary>
    public decimal UnrealizedPnL { get; }
}
