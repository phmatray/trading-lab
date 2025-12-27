namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Represents a gap in historical data time series.
/// Value object capturing the start date, end date, and number of missing days.
/// </summary>
public sealed record DateGap(
    DateTime StartDate,
    DateTime EndDate,
    int DaysMissing
);
