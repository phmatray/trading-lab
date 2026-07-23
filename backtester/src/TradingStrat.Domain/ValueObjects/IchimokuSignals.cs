using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Internal helper record containing all Ichimoku indicator states at a specific bar.
/// Used to encapsulate signal generation logic.
/// </summary>
internal sealed class IchimokuSignals : ValueObject
{
    public decimal Price { get; init; }
    public decimal ConversionLine { get; init; }
    public decimal BaseLine { get; init; }
    public decimal LeadingSpanA { get; init; }
    public decimal LeadingSpanB { get; init; }
    public decimal LaggingSpan { get; init; }
    public decimal PriceAtLaggingSpanPosition { get; init; } // Price 26 bars ago (for LaggingSpan comparison)
    public decimal KumoTop { get; init; }
    public decimal KumoBottom { get; init; }
    public bool PriceAboveKumo { get; init; }
    public bool PriceBelowKumo { get; init; }
    public bool PriceInKumo { get; init; }
    public bool ConversionLineAboveBaseLine { get; init; }
    public bool LaggingSpanAbovePriceHistory { get; init; }
    public TrendState WeeklyTrend { get; init; }

    public IchimokuSignals(
        decimal price,
        decimal conversionLine,
        decimal baseLine,
        decimal leadingSpanA,
        decimal leadingSpanB,
        decimal laggingSpan,
        decimal priceAtLaggingSpanPosition,
        decimal kumoTop,
        decimal kumoBottom,
        bool priceAboveKumo,
        bool priceBelowKumo,
        bool priceInKumo,
        bool conversionLineAboveBaseLine,
        bool laggingSpanAbovePriceHistory,
        TrendState weeklyTrend)
    {
        Price = price;
        ConversionLine = conversionLine;
        BaseLine = baseLine;
        LeadingSpanA = leadingSpanA;
        LeadingSpanB = leadingSpanB;
        LaggingSpan = laggingSpan;
        PriceAtLaggingSpanPosition = priceAtLaggingSpanPosition;
        KumoTop = kumoTop;
        KumoBottom = kumoBottom;
        PriceAboveKumo = priceAboveKumo;
        PriceBelowKumo = priceBelowKumo;
        PriceInKumo = priceInKumo;
        ConversionLineAboveBaseLine = conversionLineAboveBaseLine;
        LaggingSpanAbovePriceHistory = laggingSpanAbovePriceHistory;
        WeeklyTrend = weeklyTrend;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Price;
        yield return ConversionLine;
        yield return BaseLine;
        yield return LeadingSpanA;
        yield return LeadingSpanB;
        yield return LaggingSpan;
        yield return PriceAtLaggingSpanPosition;
        yield return KumoTop;
        yield return KumoBottom;
        yield return PriceAboveKumo;
        yield return PriceBelowKumo;
        yield return PriceInKumo;
        yield return ConversionLineAboveBaseLine;
        yield return LaggingSpanAbovePriceHistory;
        yield return WeeklyTrend;
    }
}
