namespace TradingStrat.Domain.Services;

/// <summary>
/// Direction of a crossover between two series.
/// </summary>
public enum CrossDirection
{
    /// <summary>
    /// First series crosses above second series.
    /// </summary>
    Above,

    /// <summary>
    /// First series crosses below second series.
    /// </summary>
    Below
}

/// <summary>
/// Standalone service for detecting crossovers in price series and indicators.
/// Eliminates duplication across strategies (MA Crossover, MACD, CustomRuleBasedStrategy).
/// </summary>
public static class CrossoverDetector
{
    /// <summary>
    /// Detects if a value series crosses above a threshold at the current index.
    /// </summary>
    /// <param name="values">The value series to check.</param>
    /// <param name="currentIndex">The current index (must be at least 1 for comparison with previous value).</param>
    /// <param name="threshold">The threshold value to cross.</param>
    /// <returns>True if current value is above threshold and previous value was at or below threshold.</returns>
    public static bool DetectCrossAbove(decimal[] values, int currentIndex, decimal threshold)
    {
        if (currentIndex < 1)
        {
            return false;
        }

        return values[currentIndex] > threshold
            && values[currentIndex - 1] <= threshold;
    }

    /// <summary>
    /// Detects if a value series crosses below a threshold at the current index.
    /// </summary>
    /// <param name="values">The value series to check.</param>
    /// <param name="currentIndex">The current index (must be at least 1 for comparison with previous value).</param>
    /// <param name="threshold">The threshold value to cross.</param>
    /// <returns>True if current value is below threshold and previous value was at or above threshold.</returns>
    public static bool DetectCrossBelow(decimal[] values, int currentIndex, decimal threshold)
    {
        if (currentIndex < 1)
        {
            return false;
        }

        return values[currentIndex] < threshold
            && values[currentIndex - 1] >= threshold;
    }

    /// <summary>
    /// Detects if one series crosses another series in the specified direction at the current index.
    /// </summary>
    /// <param name="series1">The first series (typically the faster moving indicator).</param>
    /// <param name="series2">The second series (typically the slower moving indicator).</param>
    /// <param name="currentIndex">The current index (must be >= 1 for comparison with previous values).</param>
    /// <param name="direction">The direction of the cross (Above or Below).</param>
    /// <returns>True if series1 crosses series2 in the specified direction at currentIndex.</returns>
    public static bool DetectCrossBetween(
        decimal[] series1,
        decimal[] series2,
        int currentIndex,
        CrossDirection direction)
    {
        if (currentIndex < 1)
        {
            return false;
        }

        bool currentAbove = series1[currentIndex] > series2[currentIndex];
        bool previousAbove = series1[currentIndex - 1] > series2[currentIndex - 1];

        return direction == CrossDirection.Above
            ? currentAbove && !previousAbove
            : !currentAbove && previousAbove;
    }
}
