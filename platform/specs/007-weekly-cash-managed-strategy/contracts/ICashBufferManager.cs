// <copyright file="ICashBufferManager.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service interface for managing cash buffer adjustments to maintain target ratio range.
/// Ensures portfolio maintains healthy liquidity between MIN_CASH_RATIO and MAX_CASH_RATIO.
/// </summary>
public interface ICashBufferManager
{
    /// <summary>
    /// Calculates current cash ratio for a strategy's portfolio.
    /// Formula: cash / (cash + ETP_shares_held × current_ETP_price).
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current cash ratio as decimal (0.0 to 1.0).</returns>
    Task<decimal> CalculateCurrentCashRatioAsync(Guid strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates total portfolio equity for a strategy.
    /// Formula: cash + (ETP_shares_held × current_ETP_price).
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total equity in dollars.</returns>
    Task<decimal> CalculateTotalEquityAsync(Guid strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if cash buffer adjustment is needed after primary buy/sell logic.
    /// Returns true if cash ratio is outside MIN_CASH_RATIO to MAX_CASH_RATIO range.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if adjustment needed, false if within acceptable range.</returns>
    Task<bool> NeedsAdjustmentAsync(Guid strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes cash buffer adjustment by placing buy or sell order.
    /// If cash ratio < MIN: sells WEEKLY_SELL_RATIO × ETP_shares_held.
    /// If cash ratio > MAX and COIN > MA20: buys WEEKLY_BUY_RATIO × total_equity.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Order ID if adjustment order was placed, null if no adjustment needed.</returns>
    Task<Guid?> ExecuteAdjustmentAsync(Guid strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the amount needed to bring cash ratio back to target (midpoint of MIN/MAX).
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dollar amount to buy (positive) or sell (negative), or 0 if no adjustment needed.</returns>
    Task<decimal> CalculateAdjustmentAmountAsync(Guid strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates cash ratio is within configured MIN/MAX bounds.
    /// </summary>
    /// <param name="cashRatio">The cash ratio to validate.</param>
    /// <param name="minCashRatio">Minimum allowed ratio.</param>
    /// <param name="maxCashRatio">Maximum allowed ratio.</param>
    /// <returns>True if within bounds, false otherwise.</returns>
    bool IsWithinBounds(decimal cashRatio, decimal minCashRatio, decimal maxCashRatio);
}
