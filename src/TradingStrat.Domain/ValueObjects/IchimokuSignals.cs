namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Internal helper record containing all Ichimoku indicator states at a specific bar.
/// Used to encapsulate signal generation logic.
/// </summary>
internal record IchimokuSignals(
    decimal Price,
    decimal Tenkan,
    decimal Kijun,
    decimal SenkouA,
    decimal SenkouB,
    decimal Chikou,
    decimal PriceAtChikouPosition, // Price 26 bars ago (for Chikou comparison)
    decimal KumoTop,
    decimal KumoBottom,
    bool PriceAboveKumo,
    bool PriceBelowKumo,
    bool PriceInKumo,
    bool TenkanAboveKijun,
    bool ChikouAbovePriceHistory,
    TrendState WeeklyTrend
);
