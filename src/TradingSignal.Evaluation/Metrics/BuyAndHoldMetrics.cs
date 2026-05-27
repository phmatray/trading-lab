using TradingSignal.Core;

namespace TradingSignal.Evaluation.Metrics;

public static class BuyAndHoldMetrics
{
    private static readonly double[] FlatEquity = { 1d };

    public static ReturnSeriesMetrics Compute(IReadOnlyList<Candle> candles, int periodsPerYear, double feeBps)
    {
        if (candles.Count < 2)
            return new ReturnSeriesMetrics(0, 0d, 0d, 0d, FlatEquity);

        var returns = new double[candles.Count - 1];
        for (var i = 1; i < candles.Count; i++)
        {
            var prev = (double)candles[i - 1].Close;
            var curr = (double)candles[i].Close;
            returns[i - 1] = prev == 0d ? 0d : (curr - prev) / prev;
        }

        var raw = ReturnMetrics.Compute(returns, periodsPerYear);
        var feeRoundTrip = 2d * feeBps / 10_000d;
        var cumretNet = (1d + raw.CumulativeReturnPct) * (1d - feeRoundTrip) - 1d;

        return raw with { CumulativeReturnPct = cumretNet };
    }
}
