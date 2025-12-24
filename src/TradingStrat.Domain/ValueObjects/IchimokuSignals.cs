namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Internal helper record containing all Ichimoku indicator states at a specific bar.
/// Used to encapsulate signal generation logic.
/// </summary>
internal record IchimokuSignals(
    decimal Price,
    decimal ConversionLine,
    decimal BaseLine,
    decimal LeadingSpanA,
    decimal LeadingSpanB,
    decimal LaggingSpan,
    decimal PriceAtLaggingSpanPosition, // Price 26 bars ago (for LaggingSpan comparison)
    decimal KumoTop,
    decimal KumoBottom,
    bool PriceAboveKumo,
    bool PriceBelowKumo,
    bool PriceInKumo,
    bool ConversionLineAboveBaseLine,
    bool LaggingSpanAbovePriceHistory,
    TrendState WeeklyTrend
);
