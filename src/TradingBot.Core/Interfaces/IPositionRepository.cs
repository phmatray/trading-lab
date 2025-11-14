// <copyright file="IPositionRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Repository interface for Position entity operations.
/// </summary>
public interface IPositionRepository : IRepository<Position>
{
    /// <summary>
    /// Gets a position by symbol.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Position if found, null otherwise.</returns>
    Task<Position?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all open positions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of open positions.</returns>
    Task<IReadOnlyList<Position>> GetOpenPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets positions by strategy name.
    /// </summary>
    /// <param name="strategyName">Strategy name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of positions for the strategy.</returns>
    Task<IReadOnlyList<Position>> GetByStrategyAsync(
        string strategyName,
        CancellationToken cancellationToken = default);
}
