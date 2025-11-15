// <copyright file="Order.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Ardalis.SharedKernel;
using TradingBot.Core.Enums;
using TradingBot.Core.Events;

namespace TradingBot.Core.Models.Trading;

/// <summary>
/// Represents a trading order aggregate root.
/// </summary>
public sealed class Order : EntityBase<Guid>, IAggregateRoot
{
    /// <summary>
    /// Gets or sets the trading symbol.
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Gets or sets the order type.
    /// </summary>
    public required OrderType Type { get; set; }

    /// <summary>
    /// Gets or sets the order side (Buy or Sell).
    /// </summary>
    public required OrderSide Side { get; set; }

    /// <summary>
    /// Gets or sets the order quantity.
    /// </summary>
    public required decimal Quantity { get; set; }

    /// <summary>
    /// Gets or sets the limit price (for limit orders).
    /// </summary>
    public decimal? LimitPrice { get; set; }

    /// <summary>
    /// Gets or sets the stop price (for stop-loss/trailing-stop orders).
    /// </summary>
    public decimal? StopPrice { get; set; }

    /// <summary>
    /// Gets or sets the order status.
    /// </summary>
    public required OrderStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the order was created (UTC).
    /// </summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the order was submitted (UTC).
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the order was filled (UTC).
    /// </summary>
    public DateTime? FilledAt { get; set; }

    /// <summary>
    /// Gets or sets the quantity that has been filled.
    /// </summary>
    public decimal FilledQuantity { get; set; }

    /// <summary>
    /// Gets or sets the average fill price.
    /// </summary>
    public decimal AverageFillPrice { get; set; }

    /// <summary>
    /// Gets or sets the commission paid for this order.
    /// </summary>
    public decimal Commission { get; set; }

    /// <summary>
    /// Gets or sets the name of the strategy that created this order.
    /// </summary>
    public required string StrategyName { get; set; }

    /// <summary>
    /// Gets or sets the signal ID that triggered this order.
    /// </summary>
    public Guid? SignalId { get; set; }

    /// <summary>
    /// Marks the order as filled with the specified details.
    /// </summary>
    /// <param name="fillPrice">The fill price.</param>
    /// <param name="commission">The commission charged.</param>
    /// <param name="filledAt">The timestamp when filled.</param>
    public void MarkAsFilled(decimal fillPrice, decimal commission, DateTime filledAt)
    {
        if (Status == OrderStatus.Filled)
        {
            throw new InvalidOperationException("Order is already filled");
        }

        if (fillPrice <= 0)
        {
            throw new ArgumentException("Fill price must be positive", nameof(fillPrice));
        }

        Status = OrderStatus.Filled;
        FilledQuantity = Quantity;
        AverageFillPrice = fillPrice;
        Commission = commission;
        FilledAt = filledAt;

        RegisterDomainEvent(new OrderFilledEvent(Id, Symbol, Quantity, fillPrice, commission));
    }

    /// <summary>
    /// Cancels the order.
    /// </summary>
    public void Cancel()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot cancel order in {Status} status");
        }

        Status = OrderStatus.Cancelled;

        RegisterDomainEvent(new OrderCancelledEvent(Id, Symbol));
    }
}
