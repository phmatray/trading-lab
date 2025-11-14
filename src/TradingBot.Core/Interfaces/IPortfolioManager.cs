// <copyright file="IPortfolioManager.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service for managing portfolio positions and account information.
/// </summary>
public interface IPortfolioManager
{
    /// <summary>
    /// Gets the current account information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Account information.</returns>
    Task<Account> GetAccountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open positions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of open positions.</returns>
    Task<IReadOnlyList<Position>> GetPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific position by symbol.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Position or null if not found.</returns>
    Task<Position?> GetPositionAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trade history.
    /// </summary>
    /// <param name="startDate">Start date filter (optional).</param>
    /// <param name="endDate">End date filter (optional).</param>
    /// <param name="symbol">Symbol filter (optional).</param>
    /// <param name="strategyName">Strategy filter (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of trades.</returns>
    Task<IReadOnlyList<Trade>> GetTradeHistoryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? symbol = null,
        string? strategyName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a position by symbol.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if position was closed, false if not found.</returns>
    Task<bool> ClosePositionAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes all open positions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of positions closed.</returns>
    Task<int> CloseAllPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for the portfolio.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Performance metrics.</returns>
    Task<PerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);
}
