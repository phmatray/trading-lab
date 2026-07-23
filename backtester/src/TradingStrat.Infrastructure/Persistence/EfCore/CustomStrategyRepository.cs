using Microsoft.EntityFrameworkCore;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Infrastructure.Persistence.EfCore;

/// <summary>
/// EF Core implementation of ICustomStrategyPort.
/// Persists custom strategies to SQLite database.
/// </summary>
public class CustomStrategyRepository : ICustomStrategyPort
{
    private readonly TradingContext _context;

    public CustomStrategyRepository(TradingContext context)
    {
        _context = context;
    }

    public async Task<CustomStrategy> CreateAsync(CustomStrategy strategy)
    {
        strategy.CreatedAt = DateTime.UtcNow;
        strategy.LastUpdatedAt = DateTime.UtcNow;

        _context.CustomStrategies.Add(strategy);
        await _context.SaveChangesAsync();

        return strategy;
    }

    public async Task<CustomStrategy> UpdateAsync(CustomStrategy strategy)
    {
        strategy.LastUpdatedAt = DateTime.UtcNow;

        _context.CustomStrategies.Update(strategy);
        await _context.SaveChangesAsync();

        return strategy;
    }

    public async Task DeleteAsync(int strategyId)
    {
        CustomStrategy? strategy = await _context.CustomStrategies.FindAsync(strategyId);
        if (strategy is not null)
        {
            _context.CustomStrategies.Remove(strategy);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<CustomStrategy?> GetByIdAsync(int strategyId)
    {
        return await _context.CustomStrategies.FindAsync(strategyId);
    }

    public async Task<List<CustomStrategy>> GetAllAsync(string? category = null)
    {
        IQueryable<CustomStrategy> query = _context.CustomStrategies;

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.Category == category);
        }

        return await query
            .OrderByDescending(s => s.LastUpdatedAt)
            .ToListAsync();
    }

    public async Task IncrementUsageCountAsync(int strategyId)
    {
        CustomStrategy? strategy = await _context.CustomStrategies.FindAsync(strategyId);
        if (strategy is not null)
        {
            strategy.TimesUsed++;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateBacktestStatsAsync(int strategyId, decimal returnPercentage, DateTime backtestDate)
    {
        CustomStrategy? strategy = await _context.CustomStrategies.FindAsync(strategyId);
        if (strategy is not null)
        {
            strategy.LastBacktestReturn = returnPercentage;
            strategy.LastBacktestDate = backtestDate;
            await _context.SaveChangesAsync();
        }
    }
}
