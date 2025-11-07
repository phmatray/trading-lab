// <copyright file="IPortfolioService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Web.Models;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for managing portfolio history and positions.
/// </summary>
public interface IPortfolioService
{
    /// <summary>
    /// Gets filtered and paginated trade history.
    /// </summary>
    /// <param name="filter">Filter criteria for trade history.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated trade history result.</returns>
    Task<PortfolioHistoryResult> GetTradeHistoryAsync(
        PortfolioHistoryFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes an open position by symbol.
    /// </summary>
    /// <param name="symbol">Trading symbol of the position to close.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if position was closed successfully, false if position not found.</returns>
    Task<bool> ClosePositionAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports trade history to CSV format.
    /// </summary>
    /// <param name="filter">Filter criteria for trades to export.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CSV file content as byte array.</returns>
    Task<byte[]> ExportTradeHistoryAsync(
        PortfolioHistoryFilter filter,
        CancellationToken cancellationToken = default);
}
