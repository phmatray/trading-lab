namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for retrieving comprehensive data status for all tickers.
/// </summary>
public interface IGetAllDataStatusUseCase
{
    /// <summary>
    /// Executes the use case to retrieve data status for all tickers.
    /// </summary>
    /// <returns>Comprehensive data status with coverage information.</returns>
    Task<AllDataStatusResult> ExecuteAsync();
}

/// <summary>
/// Result object containing data status for all tickers.
/// </summary>
public sealed record AllDataStatusResult(
    int TotalTickers,
    int TotalRecords,
    decimal AverageCoveragePercentage,
    List<TickerDataStatus> TickerStatuses
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
