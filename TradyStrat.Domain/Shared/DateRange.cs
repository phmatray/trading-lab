namespace TradyStrat.Domain.Shared;

public sealed record DateRange
{
    public DateOnly From { get; }
    public DateOnly To   { get; }

    public DateRange(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new ArgumentException($"DateRange From ({from}) must be ≤ To ({to}).");
        From = from;
        To   = to;
    }

    public IEnumerable<DateOnly> Days
    {
        get
        {
            for (var d = From; d <= To; d = d.AddDays(1))
                yield return d;
        }
    }

    public bool Contains(DateOnly d) => d >= From && d <= To;

    public override string ToString() => $"{From}..{To}";
}
