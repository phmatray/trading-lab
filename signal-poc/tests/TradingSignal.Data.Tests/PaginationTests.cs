using Shouldly;
using TradingSignal.Core;
using TradingSignal.Data.Caching;

namespace TradingSignal.Data.Tests;

public sealed class PaginationTests
{
    private static List<Candle> Synthetic(DateTime start, TimeSpan interval, int count)
    {
        var list = new List<Candle>(count);
        for (var i = 0; i < count; i++)
        {
            var t = start + TimeSpan.FromTicks(interval.Ticks * i);
            decimal p = 100m + i;
            list.Add(new Candle(t, p, p + 1, p - 1, p, 10m));
        }
        return list;
    }

    [Fact]
    public async Task Pagination_Assembles_Contiguous_Series_Across_Multiple_Pages()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(1);
        var expected = Synthetic(start, interval, count: 2500);
        var end = expected[^1].OpenTimeUtc + interval;

        var fetcher = new FakeKlineFetcher(expected);
        var sut = new BinanceMarketDataSource(
            fetcher,
            NullCandleCache.Instance,
            logger: null,
            interPageDelay: TimeSpan.Zero,
            pageLimit: 1000);

        var result = await sut.GetCandlesAsync("BTCUSDT", interval, start, end, CancellationToken.None);

        result.Count.ShouldBe(expected.Count);
        result.Select(c => c.OpenTimeUtc).ShouldBe(expected.Select(c => c.OpenTimeUtc));
        fetcher.CallCount.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Pagination_Strips_Overlap_When_Pages_Share_Boundary_Candle()
    {
        // Pathological server: each page repeats the last candle of the previous page.
        var start = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(1);
        var all = Synthetic(start, interval, count: 1500);
        var end = all[^1].OpenTimeUtc + interval;

        var fetcher = new OverlappingFetcher(all, pageSize: 500);
        var sut = new BinanceMarketDataSource(
            fetcher,
            NullCandleCache.Instance,
            logger: null,
            interPageDelay: TimeSpan.Zero,
            pageLimit: 500);

        var result = await sut.GetCandlesAsync("BTCUSDT", interval, start, end, CancellationToken.None);

        result.Count.ShouldBe(all.Count);
        for (var i = 1; i < result.Count; i++)
            result[i].OpenTimeUtc.ShouldBeGreaterThan(result[i - 1].OpenTimeUtc);
    }

    [Fact]
    public async Task Stops_When_Page_Smaller_Than_Limit()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var interval = TimeSpan.FromHours(1);
        var all = Synthetic(start, interval, count: 750);
        var end = start + TimeSpan.FromDays(365);

        var fetcher = new FakeKlineFetcher(all);
        var sut = new BinanceMarketDataSource(
            fetcher,
            NullCandleCache.Instance,
            logger: null,
            interPageDelay: TimeSpan.Zero,
            pageLimit: 1000);

        var result = await sut.GetCandlesAsync("BTCUSDT", interval, start, end, CancellationToken.None);

        result.Count.ShouldBe(750);
        fetcher.CallCount.ShouldBe(1);
    }

    private sealed class OverlappingFetcher : TradingSignal.Data.Binance.IKlineFetcher
    {
        private readonly IReadOnlyList<Candle> _all;
        private readonly int _pageSize;

        public OverlappingFetcher(IReadOnlyList<Candle> all, int pageSize)
        {
            _all = all;
            _pageSize = pageSize;
        }

        public Task<IReadOnlyList<Candle>> FetchPageAsync(
            string symbol, TimeSpan interval, DateTime startUtc, DateTime endUtc, int limit, CancellationToken ct)
        {
            var window = _all
                .Where(c => c.OpenTimeUtc >= startUtc && c.OpenTimeUtc < endUtc)
                .Take(_pageSize)
                .ToList();
            return Task.FromResult<IReadOnlyList<Candle>>(window);
        }
    }
}
