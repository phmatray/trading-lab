using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for fetching historical data for multiple tickers in a single bulk operation.
/// Provides progress reporting and graceful failure handling (continues on error).
/// </summary>
public interface IBulkDataFetchingUseCase
{
    /// <summary>
    /// Executes bulk data fetching for multiple tickers with progress reporting.
    /// </summary>
    /// <param name="command">Command containing tickers, timeframe, and date range.</param>
    /// <param name="progress">Optional progress reporter for tracking bulk operation.</param>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <returns>Result containing successful and failed ticker counts with details.</returns>
    Task<BulkFetchResult> ExecuteAsync(
        BulkFetchDataCommand command,
        IProgress<BulkFetchProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Command for bulk data fetching operation.
/// </summary>
/// <param name="Tickers">List of ticker symbols to fetch data for.</param>
/// <param name="TimeFrame">Timeframe for historical data (M1, M5, H1, D1, W1, MN1).</param>
/// <param name="StartDate">Optional start date. If null, uses incremental update from latest data.</param>
/// <param name="EndDate">Optional end date. If null, uses today.</param>
/// <param name="SkipExisting">If true, skips tickers that already have up-to-date data.</param>
public record BulkFetchDataCommand(
    List<string> Tickers,
    TimeFrame TimeFrame,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    bool SkipExisting = false);

/// <summary>
/// Result of bulk data fetching operation.
/// </summary>
/// <param name="TotalTickers">Total number of tickers in the request.</param>
/// <param name="SuccessfulTickers">Number of tickers successfully fetched.</param>
/// <param name="FailedTickers">Number of tickers that failed to fetch.</param>
/// <param name="SkippedTickers">Number of tickers skipped (already up-to-date).</param>
/// <param name="SuccessfulResults">Dictionary of successful results by ticker.</param>
/// <param name="FailedResults">Dictionary of error messages by ticker.</param>
/// <param name="SkippedResults">List of tickers that were skipped.</param>
public record BulkFetchResult(
    int TotalTickers,
    int SuccessfulTickers,
    int FailedTickers,
    int SkippedTickers,
    Dictionary<string, DataSummaryResult> SuccessfulResults,
    Dictionary<string, string> FailedResults,
    List<string> SkippedResults);

/// <summary>
/// Progress reporting for bulk fetch operations.
/// </summary>
/// <param name="TotalTickers">Total number of tickers to process.</param>
/// <param name="CompletedTickers">Number of tickers completed so far.</param>
/// <param name="CurrentTicker">Ticker currently being processed.</param>
/// <param name="CurrentStatus">Status message for current ticker (e.g., "Fetching data...", "Saving...").</param>
/// <param name="ProgressPercentage">Overall progress percentage (0-100).</param>
public record BulkFetchProgress(
    int TotalTickers,
    int CompletedTickers,
    string CurrentTicker,
    string CurrentStatus,
    int ProgressPercentage);
