using TechnicalAnalysis.Common;
using TechnicalAnalysis.Functions;
using TradyStrat.Domain;

namespace TradyStrat.Domain.Indicators;

public static class Bollinger
{
    public static BollingerReading? LatestFor(
        IReadOnlyList<PriceBar> bars,
        int period = 20, double devUp = 2.0, double devDown = 2.0)
    {
        if (bars.Count < period) return null;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var upper  = new double[closes.Length];
        var middle = new double[closes.Length];
        var lower  = new double[closes.Length];
        int begIdx = 0, nb = 0;
        var maType = MAType.Sma;

        var rc = TAFunc.BollingerBands(
            0, closes.Length - 1, in closes,
            in period, in devUp, in devDown, in maType,
            ref begIdx, ref nb,
            ref upper, ref middle, ref lower);

        if (rc != RetCode.Success || nb == 0)
            throw new IndicatorComputationException($"Bollinger failed: {rc}");

        var last  = nb - 1;
        var sigma = (decimal)((upper[last] - middle[last]) / 2.0);
        return new BollingerReading(
            (decimal)upper[last], (decimal)middle[last], (decimal)lower[last], sigma);
    }
}
