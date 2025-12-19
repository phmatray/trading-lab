namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Configurable entry modes for Ichimoku strategy.
/// </summary>
public enum IchimokuEntryMode
{
    /// <summary>
    /// Enter when all Ichimoku conditions are aligned (no recent cross required).
    /// Suitable for trending markets where Tenkan > Kijun persists.
    /// </summary>
    AllConditionsOnly,

    /// <summary>
    /// Require a Tenkan/Kijun bullish cross within last N days AND all other conditions.
    /// More selective - waits for fresh momentum signal.
    /// </summary>
    RequireRecentCross
}
