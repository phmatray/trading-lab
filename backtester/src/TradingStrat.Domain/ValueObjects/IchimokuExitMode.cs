namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Configurable exit modes for Ichimoku strategy.
/// </summary>
public enum IchimokuExitMode
{
    /// <summary>
    /// Exit when price closes below Base Line (Kijun-sen).
    /// Conservative - allows some breathing room above Base Line.
    /// </summary>
    CloseBelowBaseLine,

    /// <summary>
    /// Exit when price enters the Kumo (cloud).
    /// More aggressive - exits at first sign of cloud penetration.
    /// </summary>
    PriceIntoKumo,

    /// <summary>
    /// Exit when Conversion Line (Tenkan-sen) crosses below Base Line (Kijun-sen) - bearish crossover.
    /// Very aggressive - exits on momentum shift, not price action.
    /// </summary>
    ConversionBaseBearishCross
}
