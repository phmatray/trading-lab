namespace TradyStrat.Domain.Shared;

public readonly record struct TimezoneId
{
    public string Value { get; }

    private TimezoneId(string value) => Value = value;

    public static TimezoneId Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TimezoneId must not be empty.", nameof(value));

        var trimmed = value.Trim();
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(trimmed);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new ArgumentException(
                $"'{trimmed}' is not a known IANA timezone id.", nameof(value), ex);
        }

        return new TimezoneId(trimmed);
    }

    public override string ToString() => Value;
}
