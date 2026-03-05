// <copyright file="IWeeklyRoutineExecutor.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service interface for orchestrating weekly cash-managed strategy routine execution.
/// Coordinates buy/sell logic, cash buffer management, and state updates.
/// </summary>
public interface IWeeklyRoutineExecutor
{
    /// <summary>
    /// Executes the weekly routine for a specific strategy.
    /// Performs: buy logic, sell logic, cash buffer adjustment, and state persistence.
    /// </summary>
    /// <param name="strategyId">The strategy identifier to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result object containing execution details and any orders placed.</returns>
    Task<WeeklyRoutineResult> ExecuteWeeklyRoutineAsync(Guid strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the daily routine for all active strategies.
    /// Updates: current prices, MA20 values, days_below_ma20 counters.
    /// Does NOT execute buy/sell orders (only state updates).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of strategies updated.</returns>
    Task<int> ExecuteDailyRoutineAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates buy conditions for a strategy (does not execute order).
    /// Conditions: COIN > MA20, cash ratio > MIN_CASH_RATIO, available cash > 0.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if buy conditions are met, false otherwise.</returns>
    Task<bool> ShouldExecuteBuyAsync(Guid strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates sell conditions for a strategy (does not execute order).
    /// Conditions: days_below_ma20 >= 2, ETP shares held > 0.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if sell conditions are met, false otherwise.</returns>
    Task<bool> ShouldExecuteSellAsync(Guid strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the buy order amount based on strategy configuration.
    /// Formula: min(WEEKLY_BUY_RATIO × total_equity, available_cash).
    /// Applies breakout rule multiplier if conditions are met.
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Buy amount in dollars, or 0 if no buy should occur.</returns>
    Task<decimal> CalculateBuyAmountAsync(Guid strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the sell order quantity based on strategy configuration.
    /// Formula: WEEKLY_SELL_RATIO × ETP_shares_held (rounded down).
    /// </summary>
    /// <param name="strategyId">The strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sell quantity in shares, or 0 if no sell should occur.</returns>
    Task<decimal> CalculateSellQuantityAsync(Guid strategyId, CancellationToken cancellationToken = default);
}
