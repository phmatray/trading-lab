using TradingStrat.Application.Ports.Outbound;

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
    /// <returns>Summary of the fetched data including record counts and date ranges.</returns>
    Task<DataSummaryResult> ExecuteAsync(
        FetchDataCommand command,
        IProgress<string>? progress = null);
}

/// <summary>
/// Command object for fetching historical market data.
/// </summary>
/// <param name="Ticker">Stock ticker symbol (e.g., "CON3.L").</param>
/// <param name="Isin">Optional ISIN code for ticker resolution (e.g., "XS2399367254").</param>
/// <param name="StartDate">Optional start date for data fetch (defaults to 2 years ago if not specified).</param>
/// <param name="EndDate">Optional end date for data fetch (defaults to today if not specified).</param>
public record FetchDataCommand(
    string Ticker,
    string? Isin = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null);
