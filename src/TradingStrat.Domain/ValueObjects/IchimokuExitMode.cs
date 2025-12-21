namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Configurable exit modes for Ichimoku strategy.
/// </summary>
public enum IchimokuExitMode
{
    /// <summary>
    /// Exit when price closes below Kijun-sen (base line).
    /// Conservative - allows some breathing room above Kijun.
    /// </summary>
    CloseBelowKijun,

    /// <summary>
    /// Exit when price enters the Kumo (cloud).
    /// More aggressive - exits at first sign of cloud penetration.
    /// </summary>
    PriceIntoKumo,

    /// <summary>
    /// Exit when Tenkan crosses below Kijun (bearish crossover).
    /// Very aggressive - exits on momentum shift, not price action.
    /// </summary>
    TenkanKijunBearishCross
}
