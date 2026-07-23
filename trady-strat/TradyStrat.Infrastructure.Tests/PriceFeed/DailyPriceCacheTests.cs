using TradyStrat.Infrastructure.PriceFeed;
using Microsoft.EntityFrameworkCore;
using TradyStrat.TestKit;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.PriceFeed;

public class DailyPriceCacheTests
{
    private static PriceBar Bar(DateOnly d) => new()
    {
        Id = 0, Ticker = "CON3.DE", Date = d,
        Open = 1, High = 1, Low = 1, Close = 1, Volume = 1
    };

    [Fact]
    public async Task First_call_fetches_two_years_back_and_persists()
    {
        await using var db = InMemoryDb.Create();
        var clock = new FakeClock(new DateTime(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc));
        var feed  = new StubPriceFeed([Bar(new(2024,5,7)), Bar(new(2026,5,5)), Bar(new(2026,5,6))]);
        var cache = new DailyPriceCache(feed, db, clock, NullLogger<DailyPriceCache>.Instance);

        await cache.EnsureFreshAsync("CON3.DE", TestContext.Current.CancellationToken);

        feed.CallCount.ShouldBe(1);
        feed.Ranges[0].From.ShouldBe(new DateOnly(2024,5,6));
        (await db.PriceBars.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(3);
    }

    [Fact]
    public async Task No_fetch_when_today_already_in_db()
    {
        await using var db = InMemoryDb.Create();
        var clock = new FakeClock(new DateTime(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc));
        db.PriceBars.Add(Bar(new(2026,5,6)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var feed  = new StubPriceFeed([]);
        var cache = new DailyPriceCache(feed, db, clock, NullLogger<DailyPriceCache>.Instance);

        await cache.EnsureFreshAsync("CON3.DE", TestContext.Current.CancellationToken);

        feed.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Subsequent_call_fetches_only_missing_days()
    {
        await using var db = InMemoryDb.Create();
        db.PriceBars.Add(Bar(new(2026,5,4)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var clock = new FakeClock(new DateTime(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc));
        var feed  = new StubPriceFeed([Bar(new(2026,5,5)), Bar(new(2026,5,6))]);
        var cache = new DailyPriceCache(feed, db, clock, NullLogger<DailyPriceCache>.Instance);

        await cache.EnsureFreshAsync("CON3.DE", TestContext.Current.CancellationToken);

        feed.Ranges[0].From.ShouldBe(new DateOnly(2026,5,5));
        (await db.PriceBars.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(3);
    }
}
