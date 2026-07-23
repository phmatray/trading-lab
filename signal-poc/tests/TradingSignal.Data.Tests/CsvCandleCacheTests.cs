using Shouldly;
using TradingSignal.Core;
using TradingSignal.Data.Caching;

namespace TradingSignal.Data.Tests;

public sealed class CsvCandleCacheTests : IDisposable
{
    private readonly string _root;

    public CsvCandleCacheTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "tsig-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* test cleanup */ }
    }

    [Fact]
    public async Task Round_Trips_Candles_Through_Csv()
    {
        var cache = new CsvCandleCache(_root);
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(1);
        var candles = Enumerable.Range(0, 24)
            .Select(i => new Candle(start + TimeSpan.FromHours(i),
                100m + i, 101m + i, 99m + i, 100.5m + i, 1m))
            .ToList();

        await cache.WriteAsync("BTCUSDT", interval, candles, CancellationToken.None);
        var read = await cache.TryReadAsync("BTCUSDT", interval,
            start, start + TimeSpan.FromHours(24), CancellationToken.None);

        read.ShouldNotBeNull();
        read.Count.ShouldBe(24);
        read[0].OpenTimeUtc.ShouldBe(start);
        read[^1].OpenTimeUtc.ShouldBe(start + TimeSpan.FromHours(23));
    }

    [Fact]
    public async Task Returns_Null_When_Range_Not_Fully_Covered()
    {
        var cache = new CsvCandleCache(_root);
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(1);
        var candles = Enumerable.Range(0, 10)
            .Select(i => new Candle(start + TimeSpan.FromHours(i), 1m, 1m, 1m, 1m, 1m))
            .ToList();

        await cache.WriteAsync("BTCUSDT", interval, candles, CancellationToken.None);
        var read = await cache.TryReadAsync("BTCUSDT", interval,
            start, start + TimeSpan.FromHours(20), CancellationToken.None);

        read.ShouldBeNull();
    }

    [Fact]
    public async Task Write_Merges_Without_Duplicates()
    {
        var cache = new CsvCandleCache(_root);
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(1);

        var first = Enumerable.Range(0, 10)
            .Select(i => new Candle(start + TimeSpan.FromHours(i), 1m, 1m, 1m, 1m, 1m))
            .ToList();
        var second = Enumerable.Range(5, 10) // overlaps 5..9
            .Select(i => new Candle(start + TimeSpan.FromHours(i), 2m, 2m, 2m, 2m, 2m))
            .ToList();

        await cache.WriteAsync("BTCUSDT", interval, first, CancellationToken.None);
        await cache.WriteAsync("BTCUSDT", interval, second, CancellationToken.None);

        var read = await cache.TryReadAsync("BTCUSDT", interval,
            start, start + TimeSpan.FromHours(15), CancellationToken.None);

        read.ShouldNotBeNull();
        read.Count.ShouldBe(15);
        read[7].Open.ShouldBe(2m); // overlap region overwritten by 'second'
    }
}
