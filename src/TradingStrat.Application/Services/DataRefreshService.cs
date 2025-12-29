using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Services;

/// <summary>
/// Service for refreshing stale historical data.
/// Identifies tickers with outdated data and fetches latest prices.
/// </summary>
public class DataRefreshService : IDataRefreshService
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly IMarketDataPort _marketDataPort;

    public DataRefreshService(
        IHistoricalDataPort historicalDataPort,
        IMarketDataPort marketDataPort)
    {
        _historicalDataPort = historicalDataPort;
        _marketDataPort = marketDataPort;
    }

    public async Task<RefreshResult> RefreshAllStaleDataAsync(
        TimeFrame timeFrame,
        int staleThresholdHours,
        IProgress<RefreshProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        DateTime staleThreshold = DateTime.UtcNow.AddHours(-staleThresholdHours);
        Dictionary<string, string> failures = new();
        int successfulRefreshes = 0;
        int failedRefreshes = 0;
        int completedTickers = 0;

        // Get all ticker summaries
        List<TickerSummary> summaries = await _historicalDataPort.GetAllTickerSummariesAsync(timeFrame);

        if (!summaries.Any())
        {
            return new RefreshResult(
                timeFrame,
                TotalTickersProcessed: 0,
                SuccessfulRefreshes: 0,
                FailedRefreshes: 0,
                SkippedTickers: 0,
                Failures: failures,
                CompletedAt: DateTime.UtcNow);
        }

        // Filter to only stale tickers
        List<TickerSummary> staleTickers = summaries
            .Where(s => IsStale(s, staleThreshold))
            .ToList();

        int totalTickers = staleTickers.Count;

        if (totalTickers == 0)
        {
            // All tickers are up to date
            return new RefreshResult(
                timeFrame,
                TotalTickersProcessed: 0,
                SuccessfulRefreshes: 0,
                FailedRefreshes: 0,
                SkippedTickers: summaries.Count,
                Failures: failures,
                CompletedAt: DateTime.UtcNow);
        }

        // Refresh each stale ticker sequentially to avoid API rate limits
        foreach (TickerSummary summary in staleTickers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int progressPercentage = totalTickers > 0 ? (completedTickers * 100) / totalTickers : 0;

            progress?.Report(new RefreshProgress(
                TotalTickers: totalTickers,
                CompletedTickers: completedTickers,
                SuccessfulTickers: successfulRefreshes,
                FailedTickers: failedRefreshes,
                CurrentTicker: summary.Ticker,
                ProgressPercentage: progressPercentage));

            try
            {
                await RefreshSingleTickerAsync(summary, timeFrame, cancellationToken);
                successfulRefreshes++;
            }
            catch (Exception ex)
            {
                failures[summary.Ticker] = ex.Message;
                failedRefreshes++;
                // Continue processing remaining tickers (graceful failure)
            }

            completedTickers++;
        }

        // Report final progress
        progress?.Report(new RefreshProgress(
            TotalTickers: totalTickers,
            CompletedTickers: completedTickers,
            SuccessfulTickers: successfulRefreshes,
            FailedTickers: failedRefreshes,
            CurrentTicker: string.Empty,
            ProgressPercentage: 100));

        return new RefreshResult(
            TimeFrame: timeFrame,
            TotalTickersProcessed: totalTickers,
            SuccessfulRefreshes: successfulRefreshes,
            FailedRefreshes: failedRefreshes,
            SkippedTickers: summaries.Count - totalTickers,
            Failures: failures,
            CompletedAt: DateTime.UtcNow);
    }

    public async Task<int> GetStaleTickerCountAsync(TimeFrame timeFrame, int staleThresholdHours)
    {
        DateTime staleThreshold = DateTime.UtcNow.AddHours(-staleThresholdHours);
        List<TickerSummary> summaries = await _historicalDataPort.GetAllTickerSummariesAsync(timeFrame);

        return summaries.Count(s => IsStale(s, staleThreshold));
    }

    private bool IsStale(TickerSummary summary, DateTime staleThreshold)
    {
        // Ticker is stale if:
        // 1. No data exists (LatestDate is null)
        // 2. Latest date is before the stale threshold
        if (!summary.LatestDate.HasValue)
        {
            return true; // No data exists
        }

        return summary.LatestDate.Value < staleThreshold;
    }

    private async Task RefreshSingleTickerAsync(
        TickerSummary summary,
        TimeFrame timeFrame,
        CancellationToken cancellationToken)
    {
        // Determine start date for refresh
        DateTime startDate = summary.LatestDate?.AddDays(1) ?? DateTime.UtcNow.AddYears(-2);
        DateTime endDate = DateTime.UtcNow;

        // Skip if no new data needed
        if (startDate >= endDate)
        {
            return;
        }

        // Fetch new data from market data provider
        IReadOnlyList<HistoricalPrice> prices = await _marketDataPort.FetchHistoricalDataAsync(
            summary.Ticker,
            timeFrame,
            startDate,
            endDate,
            cancellationToken);

        if (prices.Any())
        {
            // Save new data to repository
            await _historicalDataPort.SaveHistoricalDataAsync(
                summary.Ticker,
                summary.ISIN,
                timeFrame,
                prices);
        }
    }
}
