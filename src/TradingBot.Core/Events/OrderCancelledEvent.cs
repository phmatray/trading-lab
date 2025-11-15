// <copyright file="OrderCancelledEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when an order is cancelled.
/// </summary>
public sealed class OrderCancelledEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderCancelledEvent"/> class.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="symbol">The symbol.</param>
    public OrderCancelledEvent(Guid orderId, string symbol)
    {
        OrderId = orderId;
        Symbol = symbol;
    }

    /// <summary>
    /// Gets the order identifier.
    /// </summary>
    public Guid OrderId { get; }

    /// <summary>
    /// Gets the symbol.
    /// </summary>
    public string Symbol { get; }
}
