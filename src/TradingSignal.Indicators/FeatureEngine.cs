using Skender.Stock.Indicators;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;

namespace TradingSignal.Indicators;

public sealed class FeatureEngine(string symbol) : IFeatureEngine
{
    // Longest lookback among configured indicators is EMA50; MACD signal adds ~9.
    // Use 60 as a conservative floor — below this, indicators are still warming up.
    public int WarmupPeriods => 60;

    public FeatureSet Compute(IReadOnlyList<Candle> candles, int upToIndex)
    {
        if (upToIndex < 0 || upToIndex >= candles.Count)
            throw new ArgumentOutOfRangeException(nameof(upToIndex),
                $"upToIndex {upToIndex} out of range [0,{candles.Count})");
        if (upToIndex < WarmupPeriods)
            throw new ArgumentException(
                $"upToIndex {upToIndex} below warmup floor {WarmupPeriods}", nameof(upToIndex));

        // Slice BEFORE computing. This is the load-bearing invariant — any code path
        // that hands Skender the unsliced candle list leaks future data.
        var quotes = new List<Quote>(upToIndex + 1);
        for (var i = 0; i <= upToIndex; i++)
        {
            var c = candles[i];
            quotes.Add(new Quote
            {
                Date = c.OpenTimeUtc,
                Open = c.Open,
                High = c.High,
                Low = c.Low,
                Close = c.Close,
                Volume = c.Volume,
            });
        }

        var rsi = quotes.GetRsi(14).Last();
        var macd = quotes.GetMacd(12, 26, 9).Last();
        var ema20 = quotes.GetEma(20).Last();
        var ema50 = quotes.GetEma(50).Last();
        var atr = quotes.GetAtr(14).Last();

        var closes = new double[quotes.Count];
        for (var i = 0; i < quotes.Count; i++) closes[i] = (double)quotes[i].Close;

        var return1 = ComputeReturn(closes, lookback: 1);
        var return5 = ComputeReturn(closes, lookback: 5);
        var volatilityPct = ComputeVolatilityPct(closes, window: 20);

        var current = candles[upToIndex];
        return new FeatureSet(
            AsOfUtc: current.OpenTimeUtc,
            Symbol: symbol,
            Close: current.Close,
            Rsi14: rsi.Rsi ?? 0d,
            MacdLine: macd.Macd ?? 0d,
            MacdSignal: macd.Signal ?? 0d,
            MacdHistogram: macd.Histogram ?? 0d,
            Ema20: ema20.Ema ?? 0d,
            Ema50: ema50.Ema ?? 0d,
            Atr14: atr.Atr ?? 0d,
            Return1: return1,
            Return5: return5,
            VolatilityPct: volatilityPct);
    }

    private static double ComputeReturn(double[] closes, int lookback)
    {
        var n = closes.Length;
        if (n <= lookback) return 0d;
        var prev = closes[n - 1 - lookback];
        if (prev == 0d) return 0d;
        return (closes[n - 1] - prev) / prev;
    }

    private static double ComputeVolatilityPct(double[] closes, int window)
    {
        var n = closes.Length;
        if (n < window + 1) return 0d;

        Span<double> rets = stackalloc double[window];
        for (var i = 0; i < window; i++)
        {
            var idx = n - window + i;
            var prev = closes[idx - 1];
            rets[i] = prev == 0d ? 0d : (closes[idx] - prev) / prev;
        }

        var mean = 0d;
        for (var i = 0; i < window; i++) mean += rets[i];
        mean /= window;

        var sumSq = 0d;
        for (var i = 0; i < window; i++)
        {
            var d = rets[i] - mean;
            sumSq += d * d;
        }

        return Math.Sqrt(sumSq / window) * 100d;
    }
}
