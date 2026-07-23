// <copyright file="PositionRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Position entity operations.
/// </summary>
public class PositionRepository : Repository<Position>, IPositionRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PositionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public PositionRepository(TradingBotDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<Position?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.Symbol == symbol, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Position>> GetOpenPositionsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.Quantity != 0)
            .OrderByDescending(p => p.OpenedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Position>> GetByStrategyAsync(
        string strategyName,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.StrategyName == strategyName)
            .OrderByDescending(p => p.OpenedAt)
            .ToListAsync(cancellationToken);
    }
}
