using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Tests.TestDoubles;

/// <summary>
/// In-memory implementation of ICustomStrategyPort for testing.
/// Provides fast, isolated testing without database dependencies.
/// </summary>
public class InMemoryCustomStrategyRepository : ICustomStrategyPort
{
    private readonly Dictionary<int, CustomStrategy> _strategies = new();
    private int _nextId = 1;

    public Task<CustomStrategy> CreateAsync(CustomStrategy strategy)
    {
        strategy.Id = _nextId++;
        strategy.CreatedAt = DateTime.UtcNow;
        strategy.LastUpdatedAt = DateTime.UtcNow;
        _strategies[strategy.Id] = strategy;
        return Task.FromResult(strategy);
    }

    public Task<CustomStrategy> UpdateAsync(CustomStrategy strategy)
    {
        if (!_strategies.ContainsKey(strategy.Id))
        {
            throw new InvalidOperationException($"Strategy {strategy.Id} not found");
        }

        strategy.LastUpdatedAt = DateTime.UtcNow;
        _strategies[strategy.Id] = strategy;
        return Task.FromResult(strategy);
    }

    public Task DeleteAsync(int strategyId)
    {
        _strategies.Remove(strategyId);
        return Task.CompletedTask;
    }

    public Task<CustomStrategy?> GetByIdAsync(int strategyId)
    {
        _strategies.TryGetValue(strategyId, out CustomStrategy? strategy);
        return Task.FromResult(strategy);
    }

    public Task<List<CustomStrategy>> GetAllAsync(string? category = null)
    {
        IEnumerable<CustomStrategy> query = _strategies.Values;

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.Category == category);
        }

        return Task.FromResult(query.OrderByDescending(s => s.LastUpdatedAt).ToList());
    }

    public Task IncrementUsageCountAsync(int strategyId)
    {
        if (_strategies.TryGetValue(strategyId, out CustomStrategy? strategy))
        {
            strategy.TimesUsed++;
        }

        return Task.CompletedTask;
    }

    public Task UpdateBacktestStatsAsync(int strategyId, decimal returnPercentage, DateTime backtestDate)
    {
        if (_strategies.TryGetValue(strategyId, out CustomStrategy? strategy))
        {
            strategy.LastBacktestReturn = returnPercentage;
            strategy.LastBacktestDate = backtestDate;
        }

        return Task.CompletedTask;
    }

    // Helper methods for testing
    public void Clear()
    {
        _strategies.Clear();
        _nextId = 1;
    }

    public int Count => _strategies.Count;
}
