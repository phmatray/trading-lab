namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Represents a date range with start and end dates.
/// Encapsulates validation logic and common date range operations.
/// </summary>
public readonly record struct DateRange
{
    public DateTime Start { get; init; }
    public DateTime End { get; init; }

    public DateRange(DateTime start, DateTime end)
    {
        if (start > end)
        {
            throw new ArgumentException(
                $"Start date ({start:yyyy-MM-dd}) cannot be after end date ({end:yyyy-MM-dd}).",
                nameof(start));
        }

        Start = start.Date; // Normalize to date only (remove time component)
        End = end.Date;
    }

    // Factory methods
    public static DateRange LastDays(int days)
    {
        if (days <= 0)
        {
            throw new ArgumentException("Days must be positive.", nameof(days));
        }

        DateTime end = DateTime.Today;
        DateTime start = end.AddDays(-days);
        return new DateRange(start, end);
    }

    public static DateRange LastYears(int years)
    {
        if (years <= 0)
        {
            throw new ArgumentException("Years must be positive.", nameof(years));
        }

        DateTime end = DateTime.Today;
        DateTime start = end.AddYears(-years);
        return new DateRange(start, end);
    }

    public static DateRange YearToDate()
    {
        DateTime today = DateTime.Today;
        DateTime startOfYear = new(today.Year, 1, 1);
        return new DateRange(startOfYear, today);
    }

    public static DateRange LastMonth()
    {
        DateTime today = DateTime.Today;
        DateTime startOfLastMonth = today.AddMonths(-1);
        startOfLastMonth = new DateTime(startOfLastMonth.Year, startOfLastMonth.Month, 1);
        DateTime endOfLastMonth = startOfLastMonth.AddMonths(1).AddDays(-1);
        return new DateRange(startOfLastMonth, endOfLastMonth);
    }

    // Properties
    public int TotalDays => (End - Start).Days + 1; // Inclusive of both start and end
    public int TotalWeeks => TotalDays / 7;
    public int TotalMonths => ((End.Year - Start.Year) * 12) + End.Month - Start.Month;

    // Query methods
    public bool Contains(DateTime date)
    {
        DateTime normalizedDate = date.Date;
        return normalizedDate >= Start && normalizedDate <= End;
    }

    public bool Overlaps(DateRange other)
    {
        return Start <= other.End && End >= other.Start;
    }

    public bool IsInFuture()
    {
        return Start > DateTime.Today;
    }

    public bool IsInPast()
    {
        return End < DateTime.Today;
    }

    public bool IsCurrent()
    {
        DateTime today = DateTime.Today;
        return today >= Start && today <= End;
    }

    // Split range into smaller periods
    public IEnumerable<DateRange> SplitByMonths()
    {
        DateTime current = Start;

        while (current <= End)
        {
            DateTime monthEnd = new DateTime(current.Year, current.Month, DateTime.DaysInMonth(current.Year, current.Month));

            if (monthEnd > End)
            {
                monthEnd = End;
            }

            yield return new DateRange(current, monthEnd);

            current = monthEnd.AddDays(1);
        }
    }

    public override string ToString() => $"{Start:yyyy-MM-dd} to {End:yyyy-MM-dd}";

    public string ToString(string format) => $"{Start.ToString(format)} to {End.ToString(format)}";
}
