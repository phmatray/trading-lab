namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Enumeration of supported chart timeframes for historical price data.
/// Follows trading platform conventions (M=Minute, H=Hour, D=Day, W=Week, MN=Month).
/// Values represent the timeframe duration in minutes for easy comparison and conversion.
/// </summary>
public enum TimeFrameUnit
{
    /// <summary>1 minute timeframe.</summary>
    M1 = 1,

    /// <summary>5 minute timeframe.</summary>
    M5 = 5,

    /// <summary>15 minute timeframe.</summary>
    M15 = 15,

    /// <summary>30 minute timeframe.</summary>
    M30 = 30,

    /// <summary>1 hour timeframe (60 minutes).</summary>
    H1 = 60,

    /// <summary>4 hour timeframe (240 minutes).</summary>
    H4 = 240,

    /// <summary>1 day timeframe (1440 minutes).</summary>
    D1 = 1440,

    /// <summary>1 week timeframe (10080 minutes).</summary>
    W1 = 10080,

    /// <summary>1 month timeframe (approximately 30 days in minutes).</summary>
    MN1 = 43200
}

/// <summary>
/// Value object representing a chart timeframe with conversion utilities.
/// Immutable record containing domain logic for timeframe operations.
/// </summary>
public sealed record TimeFrame
{
    /// <summary>
    /// The timeframe unit represented by this instance.
    /// </summary>
    public required TimeFrameUnit Unit { get; init; }

    // Static instances for common timeframes (flyweight pattern)
    public static readonly TimeFrame M1 = new() { Unit = TimeFrameUnit.M1 };
    public static readonly TimeFrame M5 = new() { Unit = TimeFrameUnit.M5 };
    public static readonly TimeFrame M15 = new() { Unit = TimeFrameUnit.M15 };
    public static readonly TimeFrame M30 = new() { Unit = TimeFrameUnit.M30 };
    public static readonly TimeFrame H1 = new() { Unit = TimeFrameUnit.H1 };
    public static readonly TimeFrame H4 = new() { Unit = TimeFrameUnit.H4 };
    public static readonly TimeFrame D1 = new() { Unit = TimeFrameUnit.D1 };
    public static readonly TimeFrame W1 = new() { Unit = TimeFrameUnit.W1 };
    public static readonly TimeFrame MN1 = new() { Unit = TimeFrameUnit.MN1 };

    /// <summary>
    /// Converts the timeframe to minutes.
    /// </summary>
    /// <returns>The number of minutes this timeframe represents.</returns>
    public int ToMinutes() => (int)Unit;

    /// <summary>
    /// Converts the timeframe to a TimeSpan.
    /// </summary>
    /// <returns>TimeSpan representation of this timeframe.</returns>
    public TimeSpan ToTimeSpan() => TimeSpan.FromMinutes(ToMinutes());

    /// <summary>
    /// Determines if this is an intraday timeframe (less than daily).
    /// </summary>
    /// <returns>True if the timeframe is less than one day (M1, M5, M15, M30, H1, H4).</returns>
    public bool IsIntraday() => Unit < TimeFrameUnit.D1;

    /// <summary>
    /// Determines if this is a daily timeframe.
    /// </summary>
    /// <returns>True if the timeframe is exactly one day (D1).</returns>
    public bool IsDaily() => Unit == TimeFrameUnit.D1;

    /// <summary>
    /// Determines if this timeframe is higher than daily (weekly or monthly).
    /// </summary>
    /// <returns>True if the timeframe is greater than one day (W1, MN1).</returns>
    public bool IsHigherThanDaily() => Unit > TimeFrameUnit.D1;

    /// <summary>
    /// Calculates the appropriate lookback period multiplier for this timeframe
    /// relative to a reference timeframe.
    /// Used to adjust indicator periods across different timeframes while maintaining
    /// similar time coverage.
    /// </summary>
    /// <param name="referenceTimeFrame">The reference timeframe to compare against.</param>
    /// <returns>The multiplier to apply to periods (e.g., 4 when comparing H1 to M15).</returns>
    /// <example>
    /// 14-period RSI on D1 = ~14 days
    /// 14-period RSI on H1 with D1 reference = 14 * 24 = 336 bars (~14 days)
    /// </example>
    public int GetPeriodMultiplier(TimeFrame referenceTimeFrame)
    {
        if (referenceTimeFrame.ToMinutes() == 0)
        {
            throw new ArgumentException("Reference timeframe cannot have zero duration", nameof(referenceTimeFrame));
        }

        return ToMinutes() / referenceTimeFrame.ToMinutes();
    }

    /// <summary>
    /// Parses a string representation of a timeframe to a TimeFrame instance.
    /// </summary>
    /// <param name="value">String representation (e.g., "M1", "H1", "D1").</param>
    /// <returns>The corresponding TimeFrame instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid timeframe.</exception>
    public static TimeFrame FromString(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return value.ToUpperInvariant() switch
        {
            "M1" => M1,
            "M5" => M5,
            "M15" => M15,
            "M30" => M30,
            "H1" => H1,
            "H4" => H4,
            "D1" => D1,
            "W1" => W1,
            "MN1" => MN1,
            _ => throw new ArgumentException($"Invalid timeframe: {value}. Valid values are: M1, M5, M15, M30, H1, H4, D1, W1, MN1", nameof(value))
        };
    }

    /// <summary>
    /// Returns the string representation of this timeframe (e.g., "M1", "H1", "D1").
    /// </summary>
    /// <returns>The timeframe code as a string.</returns>
    public override string ToString() => Unit.ToString();
}
