// <copyright file="IPortfolioService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Trading;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for managing portfolio history and positions.
/// </summary>
public interface IPortfolioService
{
    /// <summary>
    /// Gets trade history with optional filters.
    /// </summary>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="symbol">Optional symbol filter.</param>
    /// <param name="strategy">Optional strategy name filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of trades matching the filters.</returns>
    Task<IEnumerable<Trade>> GetTradeHistoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? symbol = null,
        string? strategy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open positions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of open positions.</returns>
    Task<IEnumerable<Position>> GetOpenPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes an open position by ID.
    /// </summary>
    /// <param name="positionId">ID of the position to close.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if position was closed successfully.</returns>
    Task<bool> ClosePositionAsync(Guid positionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports trade history to CSV format.
    /// </summary>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="symbol">Optional symbol filter.</param>
    /// <param name="strategy">Optional strategy name filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CSV file content as string.</returns>
    Task<string> ExportTradeHistoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? symbol = null,
        string? strategy = null,
        CancellationToken cancellationToken = default);
}
