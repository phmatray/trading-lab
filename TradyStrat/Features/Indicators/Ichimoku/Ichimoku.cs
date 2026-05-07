using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Indicators.Ichimoku;

public static class Ichimoku
{
    public static IchimokuReading? LatestFor(IReadOnlyList<PriceBar> bars)
    {
        const int min = 52 + 26;
        if (bars.Count < min) return null;

        decimal MidOver(int n)
        {
            decimal high = decimal.MinValue, low = decimal.MaxValue;
            for (var i = bars.Count - n; i < bars.Count; i++)
            {
                if (bars[i].High > high) high = bars[i].High;
                if (bars[i].Low  < low)  low  = bars[i].Low;
            }
            return (high + low) / 2m;
        }

        var tenkan  = MidOver(9);
        var kijun   = MidOver(26);
        var senkouA = (tenkan + kijun) / 2m;
        var senkouB = MidOver(52);
        var chikou  = bars[^27].Close;
        var price   = bars[^1].Close;

        var top    = Math.Max(senkouA, senkouB);
        var bottom = Math.Min(senkouA, senkouB);
        var signal = price > top
            ? IchimokuSignal.AboveCloud
            : price < bottom
                ? IchimokuSignal.BelowCloud
                : IchimokuSignal.InCloud;

        return new IchimokuReading(tenkan, kijun, senkouA, senkouB, chikou, signal);
    }
}
