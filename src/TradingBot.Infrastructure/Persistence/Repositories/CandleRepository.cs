// <copyright file="CandleRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.MarketData;

namespace TradingBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Candle entity operations.
/// </summary>
public class CandleRepository : Repository<Candle>, ICandleRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CandleRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CandleRepository(TradingBotDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Candle>> GetBySymbolAndTimeframeAsync(
        string symbol,
        string timeframe,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Symbol == symbol && c.Timeframe == timeframe)
            .OrderBy(c => c.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Candle>> GetByDateRangeAsync(
        string symbol,
        string timeframe,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Symbol == symbol &&
                       c.Timeframe == timeframe &&
                       c.Timestamp >= startDate &&
                       c.Timestamp <= endDate)
            .OrderBy(c => c.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Candle>> GetRecentAsync(
        string symbol,
        string timeframe,
        int count,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Symbol == symbol && c.Timeframe == timeframe)
            .OrderByDescending(c => c.Timestamp)
            .Take(count)
            .OrderBy(c => c.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var candlesToDelete = await DbSet
            .Where(c => c.Timestamp < cutoffDate)
            .ToListAsync(cancellationToken);

        DbSet.RemoveRange(candlesToDelete);
        return await Context.SaveChangesAsync(cancellationToken);
    }
}
