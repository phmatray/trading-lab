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
        decimal Price,
        decimal ConversionLine,
        decimal BaseLine,
        decimal LeadingSpanA,
        decimal LeadingSpanB,
        decimal LaggingSpan,
        decimal PriceAtLaggingSpanPosition,
        decimal KumoTop,
        decimal KumoBottom,
        bool PriceAboveKumo,
        bool PriceBelowKumo,
        bool PriceInKumo,
        bool ConversionLineAboveBaseLine,
        bool LaggingSpanAbovePriceHistory,
        TrendState WeeklyTrend)
    {
        this.Price = Price;
        this.ConversionLine = ConversionLine;
        this.BaseLine = BaseLine;
        this.LeadingSpanA = LeadingSpanA;
        this.LeadingSpanB = LeadingSpanB;
        this.LaggingSpan = LaggingSpan;
        this.PriceAtLaggingSpanPosition = PriceAtLaggingSpanPosition;
        this.KumoTop = KumoTop;
        this.KumoBottom = KumoBottom;
        this.PriceAboveKumo = PriceAboveKumo;
        this.PriceBelowKumo = PriceBelowKumo;
        this.PriceInKumo = PriceInKumo;
        this.ConversionLineAboveBaseLine = ConversionLineAboveBaseLine;
        this.LaggingSpanAbovePriceHistory = LaggingSpanAbovePriceHistory;
        this.WeeklyTrend = WeeklyTrend;
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
