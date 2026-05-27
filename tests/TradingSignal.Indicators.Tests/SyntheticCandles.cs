using TradingSignal.Core;

namespace TradingSignal.Indicators.Tests;

internal static class SyntheticCandles
{
    public static IReadOnlyList<Candle> Generate(int count, int seed = 42)
    {
        var rng = new Random(seed);
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(1);
        var candles = new List<Candle>(count);
        decimal price = 50_000m;

        for (var i = 0; i < count; i++)
        {
            var drift = (decimal)(rng.NextDouble() * 100.0 - 50.0); // +/- $50 per step
            var open = price;
            var close = price + drift;
            var high = Math.Max(open, close) + (decimal)(rng.NextDouble() * 20.0);
            var low = Math.Min(open, close) - (decimal)(rng.NextDouble() * 20.0);
            var vol = (decimal)(rng.NextDouble() * 100.0 + 1.0);
            candles.Add(new Candle(start + TimeSpan.FromTicks(interval.Ticks * i), open, high, low, close, vol));
            price = close;
        }
        return candles;
    }
}
