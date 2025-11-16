// <copyright file="ICashBufferManager.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Strategy;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service interface for managing cash buffer adjustments (rebalancing to target ratio).
/// </summary>
public interface ICashBufferManager
{
    /// <summary>
    /// Adjusts cash buffer to bring cash ratio within target range (MIN to MAX).
    /// </summary>
    /// <param name="strategy">The strategy to adjust cash buffer for.</param>
    /// <param name="currentCashRatio">Current cash ratio (cash / total equity).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The adjustment order ID (null if no adjustment needed).</returns>
    Task<Guid?> AdjustCashBufferAsync(
        WeeklyCashManagedStrategy strategy,
        decimal currentCashRatio,
        CancellationToken cancellationToken = default);
}
