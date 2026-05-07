using TechnicalAnalysis.Common;
using TechnicalAnalysis.Functions;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators;

public sealed class BollingerHistoryProvider : IIndicatorHistoryProvider
{
    private const int Period = 20;
    private const double DevUp = 2.0;
    private const double DevDown = 2.0;

    public IndicatorKind Kind => IndicatorKind.Bollinger;

    public IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN)
    {
        if (bars.Count < Period) return IndicatorSeries.Empty;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var upper  = new double[closes.Length];
        var middle = new double[closes.Length];
        var lower  = new double[closes.Length];
        int begIdx = 0, nb = 0;
        var period = Period; var devUp = DevUp; var devDown = DevDown;
        var maType = MAType.Sma;

        var rc = TAFunc.BollingerBands(
            0, closes.Length - 1, in closes,
            in period, in devUp, in devDown, in maType,
            ref begIdx, ref nb,
            ref upper, ref middle, ref lower);

        if (rc != RetCode.Success || nb == 0) return IndicatorSeries.Empty;

        var take = Math.Min(lastN, nb);
        var slice = new decimal[take];
        for (int i = 0; i < take; i++) slice[i] = (decimal)middle[nb - take + i];

        return new IndicatorSeries(
            Values: slice,
            ThresholdHi: (decimal)upper[nb - 1],
            ThresholdLo: (decimal)lower[nb - 1]);
    }
}
