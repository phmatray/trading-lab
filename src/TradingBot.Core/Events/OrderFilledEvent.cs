// <copyright file="OrderFilledEvent.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Events;

/// <summary>
/// Domain event raised when an order is filled.
/// </summary>
public sealed class OrderFilledEvent : DomainEventBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderFilledEvent"/> class.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <param name="symbol">The symbol.</param>
    /// <param name="quantity">The quantity filled.</param>
    /// <param name="averageFillPrice">The average fill price.</param>
    /// <param name="commission">The commission charged.</param>
    public OrderFilledEvent(Guid orderId, string symbol, decimal quantity, decimal averageFillPrice, decimal commission)
    {
        OrderId = orderId;
        Symbol = symbol;
        Quantity = quantity;
        AverageFillPrice = averageFillPrice;
        Commission = commission;
    }

    /// <summary>
    /// Gets the order identifier.
    /// </summary>
    public Guid OrderId { get; }

    /// <summary>
    /// Gets the symbol.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the quantity filled.
    /// </summary>
    public decimal Quantity { get; }

    /// <summary>
    /// Gets the average fill price.
    /// </summary>
    public decimal AverageFillPrice { get; }

    /// <summary>
    /// Gets the commission charged.
    /// </summary>
    public decimal Commission { get; }
}
