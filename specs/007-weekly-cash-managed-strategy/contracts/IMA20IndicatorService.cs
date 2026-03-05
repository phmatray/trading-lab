// <copyright file="IMA20IndicatorService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.MarketData;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service interface for calculating 20-day simple moving average (MA20) indicator.
/// Uses sliding window algorithm for O(1) amortized performance.
/// </summary>
public interface IMA20IndicatorService
{
    /// <summary>
    /// Calculates the MA20 value for a symbol using the latest available data.
    /// </summary>
    /// <param name="symbol">The symbol to calculate MA20 for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The MA20 value, or null if insufficient data (minimum 20 days required).</returns>
    Task<decimal?> CalculateMA20Async(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the MA20 value from a provided set of candles.
    /// </summary>
    /// <param name="candles">Historical candle data (must be ordered by date ascending, minimum 20 candles).</param>
    /// <returns>The MA20 value, or null if insufficient data.</returns>
    decimal? CalculateMA20FromCandles(IReadOnlyList<Candle> candles);

    /// <summary>
    /// Updates MA20 value incrementally using sliding window (O(1) operation).
    /// </summary>
    /// <param name="currentMA20">Current MA20 value.</param>
    /// <param name="oldestPrice">The oldest price dropping out of the 20-day window.</param>
    /// <param name="newestPrice">The newest price entering the 20-day window.</param>
    /// <returns>The updated MA20 value.</returns>
    decimal UpdateMA20Incrementally(decimal currentMA20, decimal oldestPrice, decimal newestPrice);

    /// <summary>
    /// Validates that a symbol has sufficient historical data to calculate MA20.
    /// </summary>
    /// <param name="symbol">The symbol to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if at least 20 trading days of data exist, false otherwise.</returns>
    Task<bool> HasSufficientDataAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last 20 candles for a symbol (required for MA20 calculation).
    /// </summary>
    /// <param name="symbol">The symbol to retrieve candles for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of up to 20 most recent daily candles, ordered by date ascending.</returns>
    Task<IReadOnlyList<Candle>> GetLast20CandlesAsync(string symbol, CancellationToken cancellationToken = default);
}
