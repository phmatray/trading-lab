// <copyright file="ICandleRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.MarketData;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Repository interface for Candle entity operations.
/// </summary>
public interface ICandleRepository : IRepository<Candle>
{
    /// <summary>
    /// Gets candles by symbol and timeframe.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="timeframe">Timeframe (e.g., "1d", "1h", "5m").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of candles.</returns>
    Task<IReadOnlyList<Candle>> GetBySymbolAndTimeframeAsync(
        string symbol,
        string timeframe,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets candles within a date range.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="timeframe">Timeframe (e.g., "1d", "1h", "5m").</param>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of candles within the date range.</returns>
    Task<IReadOnlyList<Candle>> GetByDateRangeAsync(
        string symbol,
        string timeframe,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent N candles for a symbol.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="timeframe">Timeframe (e.g., "1d", "1h", "5m").</param>
    /// <param name="count">Number of candles to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent candles.</returns>
    Task<IReadOnlyList<Candle>> GetRecentAsync(
        string symbol,
        string timeframe,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes candles older than the specified date.
    /// </summary>
    /// <param name="cutoffDate">Delete candles before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of candles deleted.</returns>
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}
