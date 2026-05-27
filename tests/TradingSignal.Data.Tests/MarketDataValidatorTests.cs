using Shouldly;
using TradingSignal.Core;
using TradingSignal.Data.Validation;

namespace TradingSignal.Data.Tests;

public sealed class MarketDataValidatorTests
{
    private static Candle C(DateTime t) => new(t, 1m, 1m, 1m, 1m, 1m);

    [Fact]
    public void Detects_NonMonotonic()
    {
        var t = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var candles = new[]
        {
            C(t),
            C(t + TimeSpan.FromHours(2)),
            C(t + TimeSpan.FromHours(1)), // back in time
        };

        Should.Throw<InvalidOperationException>(() =>
            MarketDataValidator.Validate(candles, TimeSpan.FromHours(1), logger: null));
    }

    [Fact]
    public void Detects_Duplicate_Timestamps()
    {
        var t = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var candles = new[] { C(t), C(t) };

        Should.Throw<InvalidOperationException>(() =>
            MarketDataValidator.Validate(candles, TimeSpan.FromHours(1), logger: null));
    }

    [Fact]
    public void Passes_On_Contiguous_Stream()
    {
        var t = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var candles = Enumerable.Range(0, 50)
            .Select(i => C(t + TimeSpan.FromHours(i)))
            .ToArray();

        Should.NotThrow(() =>
            MarketDataValidator.Validate(candles, TimeSpan.FromHours(1), logger: null));
    }
}
