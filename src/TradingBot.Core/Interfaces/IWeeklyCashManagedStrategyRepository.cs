// <copyright file="IWeeklyCashManagedStrategyRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Strategy;
using TradingBot.Core.SharedKernel;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Repository interface for WeeklyCashManagedStrategy aggregate root.
/// Extends base repository with strategy-specific queries.
/// </summary>
public interface IWeeklyCashManagedStrategyRepository : IRepository<WeeklyCashManagedStrategy>
{
    /// <summary>
    /// Gets a strategy by its unique name.
    /// </summary>
    /// <param name="name">The strategy name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The strategy or null if not found.</returns>
    Task<WeeklyCashManagedStrategy?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled strategies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of enabled strategies.</returns>
    Task<IReadOnlyList<WeeklyCashManagedStrategy>> GetEnabledStrategiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets strategies that are due for execution based on the current day of week.
    /// </summary>
    /// <param name="currentDayOfWeek">Current day of week (0=Sunday, 6=Saturday).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of strategies due for execution.</returns>
    Task<IReadOnlyList<WeeklyCashManagedStrategy>> GetStrategiesDueForExecutionAsync(
        int currentDayOfWeek,
        CancellationToken cancellationToken = default);
}
