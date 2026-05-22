using TechnicalAnalysis.Common;
using TechnicalAnalysis.Functions;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Indicators;

public static class MovingAverage
{
    public static decimal? LatestFor(IReadOnlyList<PriceBar> bars, int period)
    {
        if (bars.Count < period) return null;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var output = new double[closes.Length];
        int begIdx = 0, nb = 0;

        var rc = TAFunc.Sma(
            0, closes.Length - 1, in closes,
            in period, ref begIdx, ref nb, ref output);

        if (rc != RetCode.Success || nb == 0)
            throw new IndicatorComputationException($"SMA({period}) failed: {rc}");

        return (decimal)output[nb - 1];
    }
}
