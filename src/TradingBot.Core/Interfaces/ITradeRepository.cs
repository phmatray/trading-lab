// <copyright file="ITradeRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Repository interface for Trade entity operations.
/// </summary>
public interface ITradeRepository : IRepository<Trade>
{
    /// <summary>
    /// Gets trades by symbol.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of trades for the symbol.</returns>
    Task<IReadOnlyList<Trade>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trades within a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of trades within the date range.</returns>
    Task<IReadOnlyList<Trade>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trades by strategy name.
    /// </summary>
    /// <param name="strategyName">Strategy name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of trades for the strategy.</returns>
    Task<IReadOnlyList<Trade>> GetByStrategyAsync(
        string strategyName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets winning trades (RealizedPnL greater than 0).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of winning trades.</returns>
    Task<IReadOnlyList<Trade>> GetWinningTradesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets losing trades (RealizedPnL less than 0).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of losing trades.</returns>
    Task<IReadOnlyList<Trade>> GetLosingTradesAsync(CancellationToken cancellationToken = default);
}
