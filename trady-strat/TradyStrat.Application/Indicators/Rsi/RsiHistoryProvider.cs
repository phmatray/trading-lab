using TechnicalAnalysis.Common;
using TradyStrat.Domain.Indicators;
using TradyStrat.Domain.Indicators.Services;
using TechnicalAnalysis.Functions;
using TradyStrat.Domain;

namespace TradyStrat.Application.Indicators.Rsi;

public sealed class RsiHistoryProvider : IIndicatorHistoryProvider
{
    private const int Period = 14;

    public IndicatorKind Kind => IndicatorKind.Rsi;

    public IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN)
    {
        if (bars.Count < Period + 1) return IndicatorSeries.Empty;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var output = new double[closes.Length];
        int begIdx = 0, nb = 0;
        var period = Period;

        var rc = TAFunc.Rsi(
            0, closes.Length - 1, in closes,
            in period, ref begIdx, ref nb, ref output);

        if (rc != RetCode.Success || nb == 0) return IndicatorSeries.Empty;

        var take = Math.Min(lastN, nb);
        var slice = new decimal[take];
        for (int i = 0; i < take; i++) slice[i] = (decimal)output[nb - take + i];

        return new IndicatorSeries(slice, ThresholdHi: 70m, ThresholdLo: 30m);
    }
}
