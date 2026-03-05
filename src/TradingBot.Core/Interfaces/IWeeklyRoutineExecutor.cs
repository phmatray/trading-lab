// <copyright file="IWeeklyRoutineExecutor.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Strategy;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service interface for executing the weekly routine (buy/sell logic orchestration).
/// </summary>
public interface IWeeklyRoutineExecutor
{
    /// <summary>
    /// Executes the weekly routine for a strategy (buy, sell, cash buffer adjustment).
    /// </summary>
    /// <param name="strategy">The strategy to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the weekly routine execution.</returns>
    Task<WeeklyRoutineResult> ExecuteWeeklyRoutineAsync(
        WeeklyCashManagedStrategy strategy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the daily routine for a strategy (update MA20, prices, days_below_ma20 counter).
    /// </summary>
    /// <param name="strategy">The strategy to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteDailyRoutineAsync(
        WeeklyCashManagedStrategy strategy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether buy conditions are met for a strategy.
    /// </summary>
    /// <param name="strategyId">Strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if buy should execute.</returns>
    Task<bool> ShouldExecuteBuyAsync(Guid strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the buy amount based on strategy configuration.
    /// </summary>
    /// <param name="strategyId">Strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Buy amount in dollars.</returns>
    Task<decimal> CalculateBuyAmountAsync(Guid strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether sell conditions are met for a strategy.
    /// </summary>
    /// <param name="strategyId">Strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if sell should execute.</returns>
    Task<bool> ShouldExecuteSellAsync(Guid strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the sell quantity based on strategy configuration.
    /// </summary>
    /// <param name="strategyId">Strategy identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sell quantity in shares.</returns>
    Task<decimal> CalculateSellQuantityAsync(Guid strategyId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of a weekly routine execution.
/// </summary>
public sealed class WeeklyRoutineResult
{
    /// <summary>
    /// Gets or sets the buy order ID (null if no buy executed).
    /// </summary>
    public Guid? BuyOrderId { get; set; }

    /// <summary>
    /// Gets or sets the sell order ID (null if no sell executed).
    /// </summary>
    public Guid? SellOrderId { get; set; }

    /// <summary>
    /// Gets or sets the cash buffer adjustment order ID (null if no adjustment).
    /// </summary>
    public Guid? CashBufferOrderId { get; set; }

    /// <summary>
    /// Gets or sets the cash ratio after execution.
    /// </summary>
    public decimal CashRatioAfter { get; set; }

    /// <summary>
    /// Gets or sets execution notes (for logging/diagnostics).
    /// </summary>
    public required string Notes { get; set; }
}
