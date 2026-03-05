// <copyright file="IMA20IndicatorService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.MarketData;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service interface for calculating the 20-day moving average (MA20) indicator.
/// </summary>
public interface IMA20IndicatorService
{
    /// <summary>
    /// Calculates the 20-day simple moving average for a given symbol.
    /// </summary>
    /// <param name="symbol">The symbol to calculate MA20 for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The MA20 value, or null if insufficient data (less than 20 candles).</returns>
    Task<decimal?> CalculateMA20Async(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the 20-day simple moving average from a list of candles.
    /// </summary>
    /// <param name="candles">List of candles (must be at least 20 candles, ordered by date ascending).</param>
    /// <returns>The MA20 value, or null if insufficient data.</returns>
    decimal? CalculateMA20(IReadOnlyList<Candle> candles);

    /// <summary>
    /// Updates the MA20 calculation with a new candle (sliding window optimization).
    /// </summary>
    /// <param name="previousMA20">Previous MA20 value.</param>
    /// <param name="oldestCandle">The oldest candle being removed from the window.</param>
    /// <param name="newestCandle">The newest candle being added to the window.</param>
    /// <returns>The updated MA20 value.</returns>
    decimal UpdateMA20(decimal previousMA20, Candle oldestCandle, Candle newestCandle);
}
