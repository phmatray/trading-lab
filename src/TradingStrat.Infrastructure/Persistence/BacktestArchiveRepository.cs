using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Infrastructure.Persistence.EfCore;

namespace TradingStrat.Infrastructure.Persistence;

/// <summary>
/// Repository for persisting and retrieving backtest run history.
/// Implements IBacktestArchivePort using Entity Framework Core.
/// </summary>
public class BacktestArchiveRepository : IBacktestArchivePort
{
    private readonly TradingContext _context;

    public BacktestArchiveRepository(TradingContext context)
    {
        _context = context;
    }

    public async Task<BacktestRun> SaveBacktestRunAsync(BacktestRun backtestRun)
    {
        _context.BacktestRuns.Add(backtestRun);
        await _context.SaveChangesAsync();
        return backtestRun;
    }

    public async Task<List<BacktestRun>> GetBacktestRunsAsync(string? ticker = null, string? strategyType = null, int limit = 100)
    {
        IQueryable<BacktestRun> query = _context.BacktestRuns.AsQueryable();

        if (!string.IsNullOrEmpty(ticker))
        {
            query = query.Where(b => b.Ticker == ticker);
        }

        if (!string.IsNullOrEmpty(strategyType))
        {
            query = query.Where(b => b.StrategyType == strategyType);
        }

        return await query
            .OrderByDescending(b => b.ExecutedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<BacktestRun?> GetBacktestRunByIdAsync(int id)
    {
        return await _context.BacktestRuns.FindAsync(id);
    }

    public async Task<int> GetBacktestRunCountAsync()
    {
        return await _context.BacktestRuns.CountAsync();
    }

    public async Task<DateTime?> GetLastBacktestDateAsync()
    {
        return await _context.BacktestRuns
            .OrderByDescending(b => b.ExecutedAt)
            .Select(b => (DateTime?)b.ExecutedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteBacktestRunAsync(int id)
    {
        BacktestRun? backtestRun = await _context.BacktestRuns.FindAsync(id);
        if (backtestRun == null)
        {
            return false;
        }

        _context.BacktestRuns.Remove(backtestRun);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<BacktestRun>> GetTopBacktestRunsAsync(int limit = 5)
    {
        // Get all successful backtests
        List<BacktestRun> backtests = await _context.BacktestRuns
            .Where(b => b.Status == "Success")
            .ToListAsync();

        // Parse and sort by Sharpe ratio (or total return if Sharpe is not available)
        var sortedBacktests = backtests
            .Select(b =>
            {
                try
                {
                    BacktestResult? result = JsonSerializer.Deserialize<BacktestResult>(b.ResultsJson);
                    return new { Backtest = b, Result = result };
                }
                catch
                {
                    return new { Backtest = b, Result = (BacktestResult?)null };
                }
            })
            .Where(x => x.Result != null)
            .OrderByDescending(x => x.Result!.Metrics.SharpeRatio)
            .ThenByDescending(x => x.Result!.Metrics.TotalReturnPercentage)
            .Take(limit)
            .Select(x => x.Backtest)
            .ToList();

        return sortedBacktests;
    }
}
