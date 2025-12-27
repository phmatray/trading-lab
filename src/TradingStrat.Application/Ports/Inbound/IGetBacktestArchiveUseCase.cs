using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for retrieving backtest runs from the archive with filtering and sorting.
/// </summary>
public interface IGetBacktestArchiveUseCase
{
    /// <summary>
    /// Executes the use case to retrieve backtest runs.
    /// </summary>
    /// <param name="query">The query with filtering criteria.</param>
    /// <returns>Result containing archive result with backtest runs and metadata, or errors if retrieval fails.</returns>
    Task<Result<BacktestArchiveResult>> ExecuteAsync(GetBacktestArchiveQuery query);
}

/// <summary>
/// Query for retrieving backtest runs with filters.
/// </summary>
public sealed record GetBacktestArchiveQuery(
    string? Ticker = null,
    string? StrategyType = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? SortBy = "ExecutedAt", // ExecutedAt, TotalReturn, SharpeRatio
    bool SortDescending = true,
    int Limit = 100
);

/// <summary>
/// Result containing backtest runs and archive statistics.
/// </summary>
public sealed record BacktestArchiveResult(
    List<BacktestRunSummary> BacktestRuns,
    int TotalCount,
    DateTime? MostRecentDate,
    BacktestRunSummary? TopPerformer
);

/// <summary>
/// Summary view of a backtest run for display in the archive.
/// </summary>
public sealed record BacktestRunSummary(
    int Id,
    string Ticker,
    string StrategyType,
    string StrategyName,
    DateTime ExecutedAt,
    int ExecutionTimeMs,
    string Status,
    string? ErrorMessage,
    decimal? TotalReturnPercentage,
    decimal? SharpeRatio,
    int? TotalTrades,
    decimal? WinRate,
    string? Tags
)
{
    /// <summary>
    /// Creates a summary from a BacktestRun entity.
    /// </summary>
    public static BacktestRunSummary FromBacktestRun(BacktestRun backtestRun)
    {
        // Parse results JSON to extract metrics
        decimal? totalReturn = null;
        decimal? sharpeRatio = null;
        int? totalTrades = null;
        decimal? winRate = null;

        if (backtestRun.Status == "Success" && !string.IsNullOrEmpty(backtestRun.ResultsJson))
        {
            try
            {
                BacktestResult? result = System.Text.Json.JsonSerializer.Deserialize<BacktestResult>(backtestRun.ResultsJson);
                if (result != null)
                {
                    totalReturn = result.Metrics.TotalReturnPercentage;
                    sharpeRatio = result.Metrics.SharpeRatio;
                    totalTrades = result.Trades.Count;
                    winRate = result.Metrics.WinRate;
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }
        }

        return new BacktestRunSummary(
            Id: backtestRun.Id,
            Ticker: backtestRun.Ticker,
            StrategyType: backtestRun.StrategyType,
            StrategyName: backtestRun.StrategyName,
            ExecutedAt: backtestRun.ExecutedAt,
            ExecutionTimeMs: backtestRun.ExecutionTimeMs,
            Status: backtestRun.Status,
            ErrorMessage: backtestRun.ErrorMessage,
            TotalReturnPercentage: totalReturn,
            SharpeRatio: sharpeRatio,
            TotalTrades: totalTrades,
            WinRate: winRate,
            Tags: backtestRun.Tags
        );
    }
}
