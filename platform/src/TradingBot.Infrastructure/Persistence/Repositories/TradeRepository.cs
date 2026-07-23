// <copyright file="TradeRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Trade entity operations.
/// </summary>
public class TradeRepository : Repository<Trade>, ITradeRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TradeRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public TradeRepository(TradingBotDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Trade>> GetBySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.Symbol == symbol)
            .OrderByDescending(t => t.ExitTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Trade>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.EntryTime >= startDate && t.ExitTime <= endDate)
            .OrderByDescending(t => t.ExitTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Trade>> GetByStrategyAsync(
        string strategyName,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.StrategyName == strategyName)
            .OrderByDescending(t => t.ExitTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Trade>> GetWinningTradesAsync(CancellationToken cancellationToken = default)
    {
        // Get all trades and filter in memory since RealizedPnL is a computed property
        var allTrades = await DbSet.ToListAsync(cancellationToken);
        return allTrades
            .Where(t => t.RealizedPnL > 0)
            .OrderByDescending(t => t.RealizedPnL)
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Trade>> GetLosingTradesAsync(CancellationToken cancellationToken = default)
    {
        // Get all trades and filter in memory since RealizedPnL is a computed property
        var allTrades = await DbSet.ToListAsync(cancellationToken);
        return allTrades
            .Where(t => t.RealizedPnL < 0)
            .OrderBy(t => t.RealizedPnL)
            .ToList();
    }
}
