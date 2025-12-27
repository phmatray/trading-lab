using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case implementation for retrieving backtest runs from the archive.
/// </summary>
public class GetBacktestArchiveUseCase : IGetBacktestArchiveUseCase
{
    private readonly IBacktestArchivePort _backtestArchivePort;

    public GetBacktestArchiveUseCase(IBacktestArchivePort backtestArchivePort)
    {
        _backtestArchivePort = backtestArchivePort;
    }

    public async Task<Result<BacktestArchiveResult>> ExecuteAsync(GetBacktestArchiveQuery query)
    {
        try
        {
        // Get filtered backtest runs
        var backtestRuns = await _backtestArchivePort.GetBacktestRunsAsync(
            ticker: query.Ticker,
            strategyType: query.StrategyType,
            limit: query.Limit
        );

        // Apply date range filter if specified
        if (query.StartDate.HasValue || query.EndDate.HasValue)
        {
            backtestRuns = backtestRuns.Where(b =>
            {
                if (query.StartDate.HasValue && b.ExecutedAt < query.StartDate.Value)
                {
                    return false;
                }
                if (query.EndDate.HasValue && b.ExecutedAt > query.EndDate.Value)
                {
                    return false;
                }
                return true;
            }).ToList();
        }

        // Convert to summaries
        var summaries = backtestRuns
            .Select(BacktestRunSummary.FromBacktestRun)
            .ToList();

        // Apply sorting
        summaries = query.SortBy?.ToLowerInvariant() switch
        {
            "totalreturn" => query.SortDescending
                ? summaries.OrderByDescending(s => s.TotalReturnPercentage ?? decimal.MinValue).ToList()
                : summaries.OrderBy(s => s.TotalReturnPercentage ?? decimal.MinValue).ToList(),

            "sharperatio" => query.SortDescending
                ? summaries.OrderByDescending(s => s.SharpeRatio ?? decimal.MinValue).ToList()
                : summaries.OrderBy(s => s.SharpeRatio ?? decimal.MinValue).ToList(),

            "totaltrades" => query.SortDescending
                ? summaries.OrderByDescending(s => s.TotalTrades ?? 0).ToList()
                : summaries.OrderBy(s => s.TotalTrades ?? 0).ToList(),

            "winrate" => query.SortDescending
                ? summaries.OrderByDescending(s => s.WinRate ?? 0).ToList()
                : summaries.OrderBy(s => s.WinRate ?? 0).ToList(),

            _ => query.SortDescending
                ? summaries.OrderByDescending(s => s.ExecutedAt).ToList()
                : summaries.OrderBy(s => s.ExecutedAt).ToList()
        };

        // Get archive statistics
        int totalCount = await _backtestArchivePort.GetBacktestRunCountAsync();
        DateTime? mostRecentDate = await _backtestArchivePort.GetLastBacktestDateAsync();

        // Get top performer
        List<BacktestRun> topBacktests = await _backtestArchivePort.GetTopBacktestRunsAsync(limit: 1);
        BacktestRunSummary? topPerformer = topBacktests.Any()
            ? BacktestRunSummary.FromBacktestRun(topBacktests.First())
            : null;

            return Result<BacktestArchiveResult>.Success(new BacktestArchiveResult(
                BacktestRuns: summaries,
                TotalCount: totalCount,
                MostRecentDate: mostRecentDate,
                TopPerformer: topPerformer
            ));
        }
        catch (Exception ex)
        {
            return Result<BacktestArchiveResult>.Failure(
                Error.BusinessRule($"Failed to retrieve backtest archive: {ex.Message}", "BACKTEST_ARCHIVE_FAILED"));
        }
    }
}
