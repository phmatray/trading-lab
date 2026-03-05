// <copyright file="IBreakoutDetector.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Strategy;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service interface for detecting breakout conditions (price increase + volume).
/// </summary>
public interface IBreakoutDetector
{
    /// <summary>
    /// Detects if a breakout condition is met for the strategy's underlying asset.
    /// </summary>
    /// <param name="strategy">The strategy with breakout rule configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The buy ratio multiplier (e.g., 2.0 for double buy amount), or 1.0 if no breakout detected.</returns>
    Task<decimal> DetectBreakoutAsync(
        WeeklyCashManagedStrategy strategy,
        CancellationToken cancellationToken = default);
}
