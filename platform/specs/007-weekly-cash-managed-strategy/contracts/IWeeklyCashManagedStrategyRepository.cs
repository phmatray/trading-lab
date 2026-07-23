// <copyright file="IWeeklyCashManagedStrategyRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Strategies;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Repository interface for WeeklyCashManagedStrategy entity operations.
/// </summary>
public interface IWeeklyCashManagedStrategyRepository : IRepository<WeeklyCashManagedStrategy>
{
    /// <summary>
    /// Gets all active (enabled) weekly cash-managed strategies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active strategies.</returns>
    Task<IReadOnlyList<WeeklyCashManagedStrategy>> GetActiveStrategiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a strategy by ETP symbol.
    /// </summary>
    /// <param name="etpSymbol">The ETP symbol (e.g., "BTCW").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The strategy if found, null otherwise.</returns>
    Task<WeeklyCashManagedStrategy?> GetByEtpSymbolAsync(string etpSymbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a strategy by underlying asset symbol.
    /// </summary>
    /// <param name="underlyingSymbol">The underlying asset symbol (e.g., "COIN").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The strategy if found, null otherwise.</returns>
    Task<WeeklyCashManagedStrategy?> GetByUnderlyingSymbolAsync(string underlyingSymbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets strategies due for weekly execution.
    /// </summary>
    /// <param name="executionDayOfWeek">The day of week for execution (e.g., Friday).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of strategies scheduled for the specified day.</returns>
    Task<IReadOnlyList<WeeklyCashManagedStrategy>> GetStrategiesDueForExecutionAsync(
        DayOfWeek executionDayOfWeek,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets strategies that need daily MA20 updates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active strategies requiring daily updates.</returns>
    Task<IReadOnlyList<WeeklyCashManagedStrategy>> GetStrategiesNeedingDailyUpdateAsync(CancellationToken cancellationToken = default);
}
