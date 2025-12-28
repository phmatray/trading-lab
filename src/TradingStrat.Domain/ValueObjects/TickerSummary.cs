using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Lightweight summary of a ticker's data for status display and coverage analysis.
/// Value object representing metadata about available historical data for a ticker.
/// </summary>
public sealed class TickerSummary : ValueObject
{
    public string Ticker { get; init; }
    public string? ISIN { get; init; }
    public int RecordCount { get; init; }
    public DateTime? OldestDate { get; init; }
    public DateTime? LatestDate { get; init; }

    public TickerSummary(string ticker, string? isin, int recordCount, DateTime? oldestDate, DateTime? latestDate)
    {
        Ticker = ticker;
        ISIN = isin;
        RecordCount = recordCount;
        OldestDate = oldestDate;
        LatestDate = latestDate;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Ticker;
        yield return ISIN!;
        yield return RecordCount;
        yield return OldestDate!;
        yield return LatestDate!;
    }
}
