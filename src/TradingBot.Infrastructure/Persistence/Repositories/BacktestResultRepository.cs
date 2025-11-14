// <copyright file="BacktestResultRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Backtest;

namespace TradingBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing backtest results persistence.
/// </summary>
public sealed class BacktestResultRepository : IBacktestResultRepository
{
    private readonly TradingBotDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="BacktestResultRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public BacktestResultRepository(TradingBotDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<BacktestResult>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BacktestResults
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BacktestResult?> GetByIdAsync(string backtestId, CancellationToken cancellationToken = default)
    {
        return await _context.BacktestResults
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BacktestId == backtestId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SaveAsync(BacktestResult result, CancellationToken cancellationToken = default)
    {
        await _context.BacktestResults.AddAsync(result, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string backtestId, CancellationToken cancellationToken = default)
    {
        var result = await _context.BacktestResults
            .FirstOrDefaultAsync(b => b.BacktestId == backtestId, cancellationToken);

        if (result == null)
        {
            return false;
        }

        _context.BacktestResults.Remove(result);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
