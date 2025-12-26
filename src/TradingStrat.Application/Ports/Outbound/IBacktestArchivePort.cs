using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Outbound port for accessing backtest run archive.
/// </summary>
public interface IBacktestArchivePort
{
    /// <summary>
    /// Saves a backtest run to the archive.
    /// </summary>
    /// <param name="backtestRun">The backtest run to save.</param>
    /// <returns>The saved backtest run with assigned ID.</returns>
    Task<BacktestRun> SaveBacktestRunAsync(BacktestRun backtestRun);

    /// <summary>
    /// Gets all backtest runs, optionally filtered by ticker or strategy type.
    /// </summary>
    /// <param name="ticker">Optional ticker filter.</param>
    /// <param name="strategyType">Optional strategy type filter.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <returns>List of backtest runs ordered by execution date descending.</returns>
    Task<List<BacktestRun>> GetBacktestRunsAsync(string? ticker = null, string? strategyType = null, int limit = 100);

    /// <summary>
    /// Gets a backtest run by ID.
    /// </summary>
    /// <param name="id">The backtest run ID.</param>
    /// <returns>The backtest run, or null if not found.</returns>
    Task<BacktestRun?> GetBacktestRunByIdAsync(int id);

    /// <summary>
    /// Gets the total count of backtest runs.
    /// </summary>
    /// <returns>Total number of backtest runs.</returns>
    Task<int> GetBacktestRunCountAsync();

    /// <summary>
    /// Gets the date of the most recent backtest run.
    /// </summary>
    /// <returns>The most recent backtest execution date, or null if no backtests exist.</returns>
    Task<DateTime?> GetLastBacktestDateAsync();

    /// <summary>
    /// Deletes a backtest run by ID.
    /// </summary>
    /// <param name="id">The backtest run ID to delete.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteBacktestRunAsync(int id);

    /// <summary>
    /// Gets top performing backtest runs ordered by performance metric.
    /// </summary>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <returns>List of top backtest runs ordered by Sharpe ratio or total return.</returns>
    Task<List<BacktestRun>> GetTopBacktestRunsAsync(int limit = 5);
}
