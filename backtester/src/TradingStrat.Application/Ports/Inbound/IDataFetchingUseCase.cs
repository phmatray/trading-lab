using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for fetching and updating historical market data from external sources.
/// Handles ticker resolution from ISIN, incremental updates, and duplicate detection.
/// Extracted from ProgramMenu.RunDataFetcherAsync in original architecture.
/// </summary>
public interface IDataFetchingUseCase
{
    /// <summary>
    /// Executes the data fetching workflow: resolves ticker, fetches new data, and saves to storage.
    /// </summary>
    /// <param name="command">Command containing ticker, ISIN, and date range parameters.</param>
    /// <param name="progress">Optional progress reporter for UI updates.</param>
    /// <returns>Result containing summary of the fetched data including record counts and date ranges, or errors if the operation failed.</returns>
    Task<Result<DataSummaryResult>> ExecuteAsync(
        FetchDataCommand command,
        IProgress<string>? progress = null);
}

/// <summary>
/// Command object for fetching historical market data.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record FetchDataCommand
{
    public string Ticker { get; init; }
    public string? Isin { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public TimeFrame? TimeFrame { get; init; }

    public FetchDataCommand(
        string Ticker,
        string? Isin = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        TimeFrame? TimeFrame = null)
    {
        // Validate parameters
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();

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

        // Assign validated values
        this.Ticker = Ticker.ToUpperInvariant().Trim();
        this.Isin = Isin?.ToUpperInvariant().Trim();
        this.StartDate = StartDate;
        this.EndDate = EndDate;
        this.TimeFrame = TimeFrame;
    }
}
