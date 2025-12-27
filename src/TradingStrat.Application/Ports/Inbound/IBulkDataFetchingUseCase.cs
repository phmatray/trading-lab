using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
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
    /// <returns>Result containing successful and failed ticker counts with details, or errors if the operation failed.</returns>
    Task<Result<BulkFetchResult>> ExecuteAsync(
        BulkFetchDataCommand command,
        IProgress<BulkFetchProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Command for bulk data fetching operation.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record BulkFetchDataCommand
{
    public List<string> Tickers { get; init; }
    public TimeFrame TimeFrame { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool SkipExisting { get; init; }

    public BulkFetchDataCommand(
        List<string> Tickers,
        TimeFrame TimeFrame,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        bool SkipExisting = false)
    {
        // Validate parameters
        ValidationGuard.Require(Tickers).NotNull();
        ValidationGuard.Require(Tickers.Count > 0, "Tickers list cannot be empty", nameof(Tickers));

        // Validate each ticker is not empty
        foreach (string ticker in Tickers)
        {
            ValidationGuard.Require(ticker).NotNullOrWhiteSpace();
        }

        // Validate date range if both are provided
        if (StartDate.HasValue && EndDate.HasValue)
        {
            ValidationGuard.Require(StartDate.Value <= EndDate.Value,
                "Start date must be before or equal to end date",
                nameof(StartDate));
        }

        // Validate end date is not in the future
        if (EndDate.HasValue)
        {
            ValidationGuard.Require(EndDate.Value <= DateTime.Today,
                "End date cannot be in the future",
                nameof(EndDate));
        }

        // Validate start date is not in the future
        if (StartDate.HasValue)
        {
            ValidationGuard.Require(StartDate.Value <= DateTime.Today,
                "Start date cannot be in the future",
                nameof(StartDate));
        }

        // Assign validated values (normalize tickers)
        this.Tickers = Tickers.Select(t => t.ToUpperInvariant().Trim()).ToList();
        this.TimeFrame = TimeFrame;
        this.StartDate = StartDate;
        this.EndDate = EndDate;
        this.SkipExisting = SkipExisting;
    }
}

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
