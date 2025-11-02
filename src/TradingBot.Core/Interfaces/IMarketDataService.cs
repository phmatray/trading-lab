// <copyright file="IMarketDataService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.MarketData;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Provides market data operations with Yahoo Finance integration.
/// </summary>
public interface IMarketDataService
{
    /// <summary>
    /// Gets a real-time quote for the specified symbol.
    /// </summary>
    /// <param name="symbol">The trading symbol.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current quote.</returns>
    Task<Quote> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets real-time quotes for multiple symbols.
    /// </summary>
    /// <param name="symbols">The trading symbols.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of quotes.</returns>
    Task<IReadOnlyList<Quote>> GetQuotesAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical OHLCV data for the specified symbol and time range.
    /// </summary>
    /// <param name="symbol">The trading symbol.</param>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="timeframe">Candle timeframe (e.g., "1d", "1h").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of candles.</returns>
    Task<IReadOnlyList<Candle>> GetHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string timeframe,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to real-time quote updates for a symbol.
    /// </summary>
    /// <param name="symbol">The trading symbol.</param>
    /// <param name="callback">Callback invoked on each quote update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeToQuotesAsync(
        string symbol,
        Action<Quote> callback,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from quote updates for a symbol.
    /// </summary>
    /// <param name="symbol">The trading symbol.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribeFromQuotesAsync(string symbol);
}
