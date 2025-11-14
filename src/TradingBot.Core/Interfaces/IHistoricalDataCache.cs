// <copyright file="IHistoricalDataCache.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.MarketData;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service for caching historical market data.
/// </summary>
public interface IHistoricalDataCache
{
    /// <summary>
    /// Gets cached historical data for a symbol.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="timeframe">Timeframe (e.g., "1d", "1h", "5m").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached candles if available, null otherwise.</returns>
    Task<IReadOnlyList<Candle>?> GetAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string timeframe,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores historical data in the cache.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="timeframe">Timeframe (e.g., "1d", "1h", "5m").</param>
    /// <param name="candles">Candles to cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string timeframe,
        IReadOnlyList<Candle> candles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached data for a symbol.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvalidateAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
