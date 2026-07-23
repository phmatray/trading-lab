using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Services;

/// <summary>
/// Service for refreshing stale historical data across all tickers.
/// Used by background service for scheduled refresh operations.
/// </summary>
public interface IDataRefreshService
{
    /// <summary>
    /// Refreshes all stale data for the specified timeframe.
    /// </summary>
    /// <param name="timeFrame">Timeframe to refresh.</param>
    /// <param name="staleThresholdHours">Number of hours before data is considered stale.</param>
    /// <param name="progress">Optional progress reporting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing refresh statistics.</returns>
    Task<RefreshResult> RefreshAllStaleDataAsync(
        TimeFrame timeFrame,
        int staleThresholdHours,
        IProgress<RefreshProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of tickers with stale data for the specified timeframe.
    /// </summary>
    /// <param name="timeFrame">Timeframe to check.</param>
    /// <param name="staleThresholdHours">Number of hours before data is considered stale.</param>
    /// <returns>Count of tickers with stale data.</returns>
    Task<int> GetStaleTickerCountAsync(TimeFrame timeFrame, int staleThresholdHours);
}

/// <summary>
/// Progress report for data refresh operations.
/// </summary>
public sealed record RefreshProgress(
    int TotalTickers,
    int CompletedTickers,
    int SuccessfulTickers,
    int FailedTickers,
    string CurrentTicker,
    int ProgressPercentage);

/// <summary>
/// Result of a data refresh operation.
/// </summary>
public sealed record RefreshResult(
    TimeFrame TimeFrame,
    int TotalTickersProcessed,
    int SuccessfulRefreshes,
    int FailedRefreshes,
    int SkippedTickers,
    Dictionary<string, string> Failures,
    DateTime CompletedAt);
