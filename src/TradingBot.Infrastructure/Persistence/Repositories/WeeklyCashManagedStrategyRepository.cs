// <copyright file="WeeklyCashManagedStrategyRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Strategy;

namespace TradingBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for WeeklyCashManagedStrategy.
/// </summary>
internal sealed class WeeklyCashManagedStrategyRepository : Repository<WeeklyCashManagedStrategy>, IWeeklyCashManagedStrategyRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeeklyCashManagedStrategyRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public WeeklyCashManagedStrategyRepository(TradingBotDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<WeeklyCashManagedStrategy?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WeeklyCashManagedStrategy>> GetEnabledStrategiesAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WeeklyCashManagedStrategy>> GetStrategiesDueForExecutionAsync(
        int currentDayOfWeek,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.IsEnabled && s.ExecutionDayOfWeek == currentDayOfWeek)
            .ToListAsync(cancellationToken);
    }
}
