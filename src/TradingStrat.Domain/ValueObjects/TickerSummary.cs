namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Lightweight summary of a ticker's data for status display and coverage analysis.
/// Value object representing metadata about available historical data for a ticker.
/// </summary>
/// <param name="Ticker">Stock ticker symbol.</param>
/// <param name="ISIN">ISIN code for the security (if available).</param>
/// <param name="RecordCount">Number of historical records.</param>
/// <param name="OldestDate">Date of the oldest record.</param>
/// <param name="LatestDate">Date of the most recent record.</param>
public sealed record TickerSummary(
    string Ticker,
    string? ISIN,
    int RecordCount,
    DateTime? OldestDate,
    DateTime? LatestDate);
