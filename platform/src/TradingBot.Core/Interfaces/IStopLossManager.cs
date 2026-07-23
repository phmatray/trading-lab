// <copyright file="IStopLossManager.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service for managing stop-loss and trailing stop orders.
/// </summary>
public interface IStopLossManager
{
    /// <summary>
    /// Creates a fixed stop-loss order for a position.
    /// </summary>
    /// <param name="positionId">Position ID.</param>
    /// <param name="stopPrice">Stop price level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created stop-loss order ID.</returns>
    Task<Guid> CreateStopLossAsync(
        Guid positionId,
        decimal stopPrice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing stop-loss price.
    /// </summary>
    /// <param name="stopLossOrderId">Stop-loss order ID.</param>
    /// <param name="newStopPrice">New stop price level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateStopLossAsync(
        Guid stopLossOrderId,
        decimal newStopPrice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a trailing stop-loss order for a position.
    /// </summary>
    /// <param name="positionId">Position ID.</param>
    /// <param name="trailingPercent">Trailing distance as percentage (e.g., 5.0 for 5%).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created trailing stop order ID.</returns>
    Task<Guid> CreateTrailingStopAsync(
        Guid positionId,
        decimal trailingPercent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates trailing stop-loss levels based on current price.
    /// </summary>
    /// <param name="positionId">Position ID.</param>
    /// <param name="currentPrice">Current market price.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if stop level was updated, false otherwise.</returns>
    Task<bool> UpdateTrailingStopAsync(
        Guid positionId,
        decimal currentPrice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a stop-loss order.
    /// </summary>
    /// <param name="stopLossOrderId">Stop-loss order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveStopLossAsync(
        Guid stopLossOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any stop-loss orders have been triggered.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of triggered stop-loss order IDs.</returns>
    Task<IReadOnlyList<Guid>> CheckTriggeredStopsAsync(
        CancellationToken cancellationToken = default);
}
