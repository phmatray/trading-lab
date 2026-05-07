using TechnicalAnalysis.Common;
using TechnicalAnalysis.Functions;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.Indicators;

public static class Rsi
{
    public static decimal? LatestFor(IReadOnlyList<PriceBar> bars, int period = 14)
    {
        if (bars.Count < period + 1) return null;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var output = new double[closes.Length];
        int begIdx = 0, nb = 0;

        var rc = TAFunc.Rsi(
            0, closes.Length - 1, in closes,
            in period, ref begIdx, ref nb, ref output);

        if (rc != RetCode.Success || nb == 0)
            throw new IndicatorComputationException($"RSI failed: {rc}");

        return (decimal)output[nb - 1];
    }
}
