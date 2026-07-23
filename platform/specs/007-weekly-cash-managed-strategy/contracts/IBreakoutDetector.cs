// <copyright file="IBreakoutDetector.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service interface for detecting breakout conditions using price momentum and volume.
/// Used by optional breakout rule to accelerate buying during strong trends.
/// </summary>
public interface IBreakoutDetector
{
    /// <summary>
    /// Evaluates all breakout conditions for a symbol.
    /// Conditions: price > MA20, weekly price increase > threshold, volume > average × multiplier.
    /// </summary>
    /// <param name="symbol">The symbol to evaluate.</param>
    /// <param name="priceIncreaseThreshold">Minimum weekly price increase percentage (default 0.10 for 10%).</param>
    /// <param name="volumeMultiplier">Minimum volume as multiplier of 20-day average (default 1.5x).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all breakout conditions are met, false otherwise.</returns>
    Task<bool> IsBreakoutConditionMetAsync(
        string symbol,
        decimal priceIncreaseThreshold = 0.10m,
        decimal volumeMultiplier = 1.5m,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates weekly price change percentage for a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Price change as decimal (e.g., 0.12 for 12% increase), or null if insufficient data.</returns>
    Task<decimal?> CalculateWeeklyPriceChangeAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates 20-day average volume for a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Average daily volume over 20 days, or null if insufficient data.</returns>
    Task<decimal?> CalculateAverageVolumeAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current volume for a symbol (today's or latest available).
    /// </summary>
    /// <param name="symbol">The symbol to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current volume, or null if unavailable.</returns>
    Task<decimal?> GetCurrentVolumeAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if current price is above MA20 (bullish condition).
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if price > MA20, false otherwise.</returns>
    Task<bool> IsPriceAboveMA20Async(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that sufficient historical data exists for breakout analysis (minimum 20 trading days).
    /// </summary>
    /// <param name="symbol">The symbol to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if sufficient data exists, false otherwise.</returns>
    Task<bool> HasSufficientDataForBreakoutAsync(string symbol, CancellationToken cancellationToken = default);
}
