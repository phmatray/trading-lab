// <copyright file="IOrderRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Repository interface for Order entity operations.
/// </summary>
public interface IOrderRepository : IRepository<Order>
{
    /// <summary>
    /// Gets orders by symbol.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of orders for the symbol.</returns>
    Task<IReadOnlyList<Order>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders by status.
    /// </summary>
    /// <param name="status">Order status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of orders with the specified status.</returns>
    Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders within a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of orders within the date range.</returns>
    Task<IReadOnlyList<Order>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open orders (Pending, Submitted, PartiallyFilled).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of open orders.</returns>
    Task<IReadOnlyList<Order>> GetOpenOrdersAsync(CancellationToken cancellationToken = default);
}
