using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Indicators.Ichimoku;

public sealed class IchimokuHistoryProvider : IIndicatorHistoryProvider
{
    public IndicatorKind Kind => IndicatorKind.Ichimoku;

    public IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN)
    {
        if (bars.Count == 0) return IndicatorSeries.Empty;

        // Spec: sparkline shows the close-price line (the underlying the cloud overlays).
        // Thresholds null because Ichimoku has no single hi/lo threshold pair to draw.
        var take = Math.Min(lastN, bars.Count);
        var slice = new decimal[take];
        for (int i = 0; i < take; i++) slice[i] = bars[bars.Count - take + i].Close;
        return new IndicatorSeries(slice, ThresholdHi: null, ThresholdLo: null);
    }
}
