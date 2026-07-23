// <copyright file="IPortfolioService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Services;

using TradingBot.Core.Models.Trading;

/// <summary>
/// Service for managing portfolio operations including positions and trade history.
/// </summary>
public interface IPortfolioService
{
    /// <summary>
    /// Retrieves trade history with optional filtering.
    /// </summary>
    /// <param name="startDate">Optional start date filter (inclusive).</param>
    /// <param name="endDate">Optional end date filter (inclusive).</param>
    /// <param name="symbol">Optional symbol filter (e.g., "AAPL").</param>
    /// <param name="strategy">Optional strategy name filter (e.g., "MomentumStrategy").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of trades matching the filters.</returns>
    Task<IEnumerable<Trade>> GetTradeHistoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? symbol = null,
        string? strategy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all currently open positions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of open positions.</returns>
    Task<IEnumerable<Position>> GetOpenPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes an open position by executing a market order.
    /// </summary>
    /// <param name="positionId">The unique identifier of the position to close.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if position was closed successfully, false if position not found or close failed.</returns>
    /// <remarks>
    /// This method:
    /// 1. Validates the position exists and is open
    /// 2. Creates a market order to exit the position
    /// 3. Executes the order via IOrderExecutionService
    /// 4. Updates the position status to closed
    /// 5. Creates a Trade record with realized P&amp;L
    /// 6. Publishes SignalR event OnPositionClosed
    /// </remarks>
    Task<bool> ClosePositionAsync(Guid positionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports trade history to CSV format.
    /// </summary>
    /// <param name="startDate">Optional start date filter (inclusive).</param>
    /// <param name="endDate">Optional end date filter (inclusive).</param>
    /// <param name="symbol">Optional symbol filter.</param>
    /// <param name="strategy">Optional strategy name filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CSV-formatted string with headers: Symbol, Side, Quantity, Entry Price, Exit Price, Entry Time, Exit Time, Duration (days), Realized P&amp;L, P&amp;L %, Commission, Strategy.</returns>
    Task<string> ExportTradeHistoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? symbol = null,
        string? strategy = null,
        CancellationToken cancellationToken = default);
}
