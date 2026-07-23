// <copyright file="IOrderExecutionService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service for executing and managing trading orders.
/// </summary>
public interface IOrderExecutionService
{
    /// <summary>
    /// Event raised when an order is filled.
    /// </summary>
    event EventHandler<Order>? OrderFilled;

    /// <summary>
    /// Event raised when an order is cancelled.
    /// </summary>
    event EventHandler<Order>? OrderCancelled;

    /// <summary>
    /// Event raised when an order is rejected.
    /// </summary>
    event EventHandler<Order>? OrderRejected;

    /// <summary>
    /// Submits a new order for execution.
    /// </summary>
    /// <param name="order">Order to submit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The submitted order with updated status.</returns>
    Task<Order> SubmitOrderAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an existing order.
    /// </summary>
    /// <param name="orderId">Order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if order was cancelled, false if not found or already filled.</returns>
    Task<bool> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order by its identifier.
    /// </summary>
    /// <param name="orderId">Order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Order if found, null otherwise.</returns>
    Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders with optional filters.
    /// </summary>
    /// <param name="symbol">Symbol filter (optional).</param>
    /// <param name="status">Status filter (optional).</param>
    /// <param name="startDate">Start date filter (optional).</param>
    /// <param name="endDate">End date filter (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of orders matching the filters.</returns>
    Task<IReadOnlyList<Order>> GetOrdersAsync(
        string? symbol = null,
        OrderStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open orders (pending, submitted, partially filled).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of open orders.</returns>
    Task<IReadOnlyList<Order>> GetOpenOrdersAsync(CancellationToken cancellationToken = default);
}
