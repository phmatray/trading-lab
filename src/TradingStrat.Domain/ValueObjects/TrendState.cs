namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Represents the weekly trend state for multi-timeframe Ichimoku analysis.
/// </summary>
public enum TrendState
{
    /// <summary>
    /// Bullish trend: Price above Kumo AND Tenkan above Kijun on weekly timeframe.
    /// </summary>
    Bullish,

    /// <summary>
    /// Bearish trend: Price below Kumo OR Tenkan below Kijun on weekly timeframe.
    /// </summary>
    Bearish,

    /// <summary>
    /// Neutral/Unclear: Price inside Kumo or conflicting signals.
    /// </summary>
    Neutral
}
