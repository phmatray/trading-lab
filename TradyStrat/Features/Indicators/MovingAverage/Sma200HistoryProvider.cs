using TechnicalAnalysis.Common;
using TechnicalAnalysis.Functions;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators;

public sealed class Sma200HistoryProvider : IIndicatorHistoryProvider
{
    private const int Period = 200;

    public IndicatorKind Kind => IndicatorKind.Sma200;

    public IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN)
    {
        if (bars.Count < Period) return IndicatorSeries.Empty;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var output = new double[closes.Length];
        int begIdx = 0, nb = 0;
        var period = Period;

        var rc = TAFunc.Sma(
            0, closes.Length - 1, in closes,
            in period, ref begIdx, ref nb, ref output);

        if (rc != RetCode.Success || nb == 0) return IndicatorSeries.Empty;

        var take = Math.Min(lastN, nb);
        var slice = new decimal[take];
        for (int i = 0; i < take; i++) slice[i] = (decimal)output[nb - take + i];

        return new IndicatorSeries(slice, ThresholdHi: slice[^1], ThresholdLo: null);
    }
}
