using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for retrieving comprehensive data status for all tickers.
/// Supports filtering, sorting, and pagination.
/// </summary>
public interface IGetAllDataStatusUseCase
{
    /// <summary>
    /// Executes the use case to retrieve data status for all tickers.
    /// </summary>
    /// <param name="query">Optional query parameters for filtering, sorting, and pagination.</param>
    /// <returns>Result containing comprehensive data status with coverage information, or errors if the operation failed.</returns>
    Task<Result<AllDataStatusResult>> ExecuteAsync(DataStatusQuery? query = null);
}

/// <summary>
/// Query parameters for filtering, sorting, and paginating data status results.
/// </summary>
/// <param name="TimeFrame">Timeframe to query (defaults to D1).</param>
/// <param name="SearchTicker">Optional search term to filter tickers by name.</param>
/// <param name="StatusFilter">Optional filter by status (Complete/Partial/Gaps).</param>
/// <param name="MinCoverage">Optional minimum coverage percentage filter.</param>
/// <param name="MaxCoverage">Optional maximum coverage percentage filter.</param>
/// <param name="SortBy">Column to sort by (defaults to Ticker).</param>
/// <param name="SortDirection">Sort direction (defaults to Ascending).</param>
/// <param name="PageNumber">Page number for pagination (1-based, defaults to 1).</param>
/// <param name="PageSize">Number of items per page (defaults to 25).</param>
public record DataStatusQuery(
    TimeFrame? TimeFrame = null,
    string? SearchTicker = null,
    DataStatusFilter? StatusFilter = null,
    decimal? MinCoverage = null,
    decimal? MaxCoverage = null,
    SortColumn SortBy = SortColumn.Ticker,
    SortDirection SortDirection = SortDirection.Ascending,
    int PageNumber = 1,
    int PageSize = 25);

/// <summary>
/// Status filter options for data coverage.
/// </summary>
public enum DataStatusFilter
{
    /// <summary>
    /// Show all tickers regardless of status.
    /// </summary>
    All,

    /// <summary>
    /// Show only tickers with >= 95% coverage.
    /// </summary>
    Complete,

    /// <summary>
    /// Show only tickers with 80-95% coverage.
    /// </summary>
    Partial,

    /// <summary>
    /// Show only tickers with gaps (&lt; 80% coverage).
    /// </summary>
    WithGaps
}

/// <summary>
/// Columns available for sorting.
/// </summary>
public enum SortColumn
{
    Ticker,
    RecordCount,
    Coverage,
    OldestDate,
    LatestDate
}

/// <summary>
/// Sort direction options.
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}

/// <summary>
/// Result object containing data status for all tickers with pagination info.
/// </summary>
public sealed record AllDataStatusResult(
    int TotalTickers,
    int TotalRecords,
    decimal AverageCoveragePercentage,
    List<TickerDataStatus> TickerStatuses,
    int TotalPages,
    int CurrentPage,
    int PageSize
);

/// <summary>
/// Data status for a single ticker.
/// </summary>
public sealed record TickerDataStatus(
    string Ticker,
    string? ISIN,
    int RecordCount,
    DateTime? OldestDate,
    DateTime? LatestDate,
    int DaysCovered,
    decimal CoveragePercentage,
    List<DateGap> Gaps
);

/// <summary>
/// Represents a gap in historical data.
/// </summary>
public sealed record DateGap(
    DateTime StartDate,
    DateTime EndDate,
    int DaysMissing
);
