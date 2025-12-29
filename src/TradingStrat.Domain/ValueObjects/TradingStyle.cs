using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Enumeration of trading styles based on holding period and timeframe.
/// Each style has different characteristics for risk management, position sizing, and parameters.
/// </summary>
public enum TradingStyleType
{
    /// <summary>
    /// Scalping - Very short-term trading (M1-M15), high frequency, quick in-and-out trades.
    /// </summary>
    Scalping,

    /// <summary>
    /// Day Trading - Intraday trading (M15-H1), no overnight positions, close all by end of day.
    /// </summary>
    DayTrading,

    /// <summary>
    /// Swing Trading - Multi-day positions (H1-D1), holding for days to weeks to capture trends.
    /// </summary>
    SwingTrading,

    /// <summary>
    /// Long Term - Position trading (D1+), holding for weeks to months for major market moves.
    /// </summary>
    LongTerm
}

/// <summary>
/// Value object representing a trading style with default configuration.
/// Immutable record containing style-specific defaults for backtesting and live trading.
/// Each trading style has appropriate timeframe ranges, position sizing, and commission structures.
/// </summary>
public sealed class TradingStyle : ValueObject
{
    /// <summary>
    /// The type of trading style.
    /// </summary>
    public required TradingStyleType Type { get; init; }

    /// <summary>
    /// Default timeframe recommended for this trading style.
    /// </summary>
    public required TimeFrame DefaultTimeFrame { get; init; }

    /// <summary>
    /// Minimum valid timeframe for this trading style.
    /// </summary>
    public required TimeFrame MinTimeFrame { get; init; }

    /// <summary>
    /// Maximum valid timeframe for this trading style.
    /// </summary>
    public required TimeFrame MaxTimeFrame { get; init; }

    /// <summary>
    /// Default position size as a percentage of total capital (0.0 to 1.0).
    /// </summary>
    public decimal DefaultPositionSizePercent { get; init; }

    /// <summary>
    /// Maximum recommended position size as a percentage of total capital (0.0 to 1.0).
    /// </summary>
    public decimal MaxPositionSizePercent { get; init; }

    /// <summary>
    /// Default commission percentage per trade (e.g., 0.001 = 0.1%).
    /// Higher frequency strategies typically have higher commission rates.
    /// </summary>
    public decimal DefaultCommissionPercentage { get; init; }

    /// <summary>
    /// Default minimum commission per trade in currency units.
    /// </summary>
    public decimal DefaultMinimumCommission { get; init; }

    /// <summary>
    /// Base period multiplier for strategy parameters.
    /// Used as a reference for parameter adjustments across timeframes.
    /// </summary>
    public int PeriodMultiplier { get; init; }

    /// <summary>
    /// Scalping trading style - M1 to M15 timeframes.
    /// Characteristics: Very high frequency, small positions, higher commissions, quick profits.
    /// </summary>
    public static readonly TradingStyle Scalping = new()
    {
        Type = TradingStyleType.Scalping,
        DefaultTimeFrame = TimeFrame.M5,
        MinTimeFrame = TimeFrame.M1,
        MaxTimeFrame = TimeFrame.M15,
        DefaultPositionSizePercent = 0.05m,  // 5% - smaller positions for higher frequency
        MaxPositionSizePercent = 0.20m,      // 20% max
        DefaultCommissionPercentage = 0.002m, // 0.2% - higher due to high frequency
        DefaultMinimumCommission = 0.50m,     // $0.50 minimum
        PeriodMultiplier = 1
    };

    /// <summary>
    /// Day Trading style - M15 to H1 timeframes.
    /// Characteristics: Intraday only, moderate frequency, no overnight risk.
    /// </summary>
    public static readonly TradingStyle DayTrading = new()
    {
        Type = TradingStyleType.DayTrading,
        DefaultTimeFrame = TimeFrame.M15,
        MinTimeFrame = TimeFrame.M15,
        MaxTimeFrame = TimeFrame.H1,
        DefaultPositionSizePercent = 0.10m,  // 10%
        MaxPositionSizePercent = 0.30m,      // 30% max
        DefaultCommissionPercentage = 0.001m, // 0.1%
        DefaultMinimumCommission = 1.0m,      // $1.00 minimum
        PeriodMultiplier = 1
    };

    /// <summary>
    /// Swing Trading style - H1 to D1 timeframes.
    /// Characteristics: Multi-day holds, captures intermediate trends, moderate frequency.
    /// </summary>
    public static readonly TradingStyle SwingTrading = new()
    {
        Type = TradingStyleType.SwingTrading,
        DefaultTimeFrame = TimeFrame.H4,
        MinTimeFrame = TimeFrame.H1,
        MaxTimeFrame = TimeFrame.D1,
        DefaultPositionSizePercent = 0.20m,  // 20%
        MaxPositionSizePercent = 0.50m,      // 50% max
        DefaultCommissionPercentage = 0.001m, // 0.1%
        DefaultMinimumCommission = 1.0m,      // $1.00 minimum
        PeriodMultiplier = 1
    };

    /// <summary>
    /// Long Term trading style - D1 and higher timeframes.
    /// Characteristics: Long holding periods, lower frequency, larger positions, captures major trends.
    /// </summary>
    public static readonly TradingStyle LongTerm = new()
    {
        Type = TradingStyleType.LongTerm,
        DefaultTimeFrame = TimeFrame.D1,
        MinTimeFrame = TimeFrame.D1,
        MaxTimeFrame = TimeFrame.Mn1,
        DefaultPositionSizePercent = 0.25m,  // 25% - larger positions, lower frequency
        MaxPositionSizePercent = 1.0m,       // 100% - can go all-in for long-term positions
        DefaultCommissionPercentage = 0.001m, // 0.1%
        DefaultMinimumCommission = 1.0m,      // $1.00 minimum
        PeriodMultiplier = 1
    };

    /// <summary>
    /// Validates if a timeframe is appropriate for this trading style.
    /// </summary>
    /// <param name="timeFrame">The timeframe to validate.</param>
    /// <returns>True if the timeframe falls within the valid range for this style.</returns>
    public bool IsTimeFrameValid(TimeFrame timeFrame)
    {
        int minutes = timeFrame.ToMinutes();
        return minutes >= MinTimeFrame.ToMinutes() && minutes <= MaxTimeFrame.ToMinutes();
    }

    /// <summary>
    /// Adjusts a strategy parameter period based on the current timeframe relative to the style's default.
    /// This maintains similar time coverage when using the same strategy across different timeframes.
    /// </summary>
    /// <param name="basePeriod">The original period parameter (e.g., 14 for RSI).</param>
    /// <param name="currentTimeFrame">The timeframe being used for the backtest/analysis.</param>
    /// <param name="adjustForTimeFrame">Whether to apply timeframe adjustment (default: true).</param>
    /// <returns>The adjusted period that maintains similar time coverage.</returns>
    /// <example>
    /// For a Swing Trading style (default H4):
    /// - 14-period RSI on H4 (default) = 14 bars (~2.3 days)
    /// - 14-period RSI on H1 (4x smaller) = 14 * 4 = 56 bars (~2.3 days)
    /// - 14-period RSI on D1 (6x larger) = 14 / 6 = 2 bars (~2 days, minimum enforced)
    /// </example>
    public int AdjustPeriod(int basePeriod, TimeFrame currentTimeFrame, bool adjustForTimeFrame = true)
    {
        if (!adjustForTimeFrame)
        {
            return basePeriod;
        }

        int currentMinutes = currentTimeFrame.ToMinutes();
        int defaultMinutes = DefaultTimeFrame.ToMinutes();

        if (currentMinutes == defaultMinutes)
        {
            return basePeriod; // No adjustment needed
        }

        // Calculate multiplier to maintain similar time coverage
        // If current TF is smaller (e.g., M15 vs H1), multiplier < 1, so we need more bars
        // If current TF is larger (e.g., D1 vs H1), multiplier > 1, so we need fewer bars
        double multiplier = (double)defaultMinutes / currentMinutes;
        int adjustedPeriod = (int)Math.Round(basePeriod * multiplier);

        // Enforce minimum period of 2 for technical indicators
        return Math.Max(adjustedPeriod, 2);
    }

    /// <summary>
    /// Creates a TradingStyle instance from a TradingStyleType enum value.
    /// </summary>
    /// <param name="type">The trading style type.</param>
    /// <returns>The corresponding TradingStyle instance with all default values.</returns>
    /// <exception cref="ArgumentException">Thrown when the type is not recognized.</exception>
    public static TradingStyle FromType(TradingStyleType type)
    {
        return type switch
        {
            TradingStyleType.Scalping => Scalping,
            TradingStyleType.DayTrading => DayTrading,
            TradingStyleType.SwingTrading => SwingTrading,
            TradingStyleType.LongTerm => LongTerm,
            _ => throw new ArgumentException($"Invalid trading style type: {type}", nameof(type))
        };
    }

    /// <summary>
    /// Parses a string representation of a trading style to a TradingStyle instance.
    /// </summary>
    /// <param name="value">String representation (e.g., "Scalping", "DayTrading").</param>
    /// <returns>The corresponding TradingStyle instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid trading style.</exception>
    public static TradingStyle FromString(string value)
    {
        if (Enum.TryParse(value, ignoreCase: true, out TradingStyleType type))
        {
            return FromType(type);
        }

        throw new ArgumentException(
            $"Invalid trading style: {value}. Valid values are: Scalping, DayTrading, SwingTrading, LongTerm",
            nameof(value));
    }

    /// <summary>
    /// Returns the string representation of this trading style type.
    /// </summary>
    /// <returns>The trading style type as a string.</returns>
    public override string ToString() => Type.ToString();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;
        yield return DefaultTimeFrame;
        yield return MinTimeFrame;
        yield return MaxTimeFrame;
        yield return DefaultPositionSizePercent;
        yield return MaxPositionSizePercent;
        yield return DefaultCommissionPercentage;
        yield return DefaultMinimumCommission;
        yield return PeriodMultiplier;
    }
}
