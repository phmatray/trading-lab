using TradingSignal.Core;

namespace TradingSignal.Backtest.Tests;

internal static class Synthetic
{
    public static IReadOnlyList<Candle> Candles(int count, int seed = 42)
    {
        Random rng = new(seed);
        DateTime start = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan interval = TimeSpan.FromHours(1);
        List<Candle> candles = new(count);
        decimal price = 50_000m;

        for (int i = 0; i < count; i++)
        {
            decimal drift = (decimal)(rng.NextDouble() * 100.0 - 50.0);
            decimal open = price;
            decimal close = price + drift;
            decimal high = Math.Max(open, close) + (decimal)(rng.NextDouble() * 20.0);
            decimal low = Math.Min(open, close) - (decimal)(rng.NextDouble() * 20.0);
            decimal vol = (decimal)(rng.NextDouble() * 100.0 + 1.0);
            candles.Add(new Candle(start + TimeSpan.FromTicks(interval.Ticks * i), open, high, low, close, vol));
            price = close;
        }
        return candles;
    }
}
