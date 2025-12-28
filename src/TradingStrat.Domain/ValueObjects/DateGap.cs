using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Represents a gap in historical data time series.
/// Value object capturing the start date, end date, and number of missing days.
/// </summary>
public sealed class DateGap : ValueObject
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int DaysMissing { get; init; }

    public DateGap(DateTime StartDate, DateTime EndDate, int DaysMissing)
    {
        this.StartDate = StartDate;
        this.EndDate = EndDate;
        this.DaysMissing = DaysMissing;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
        yield return DaysMissing;
    }
}
