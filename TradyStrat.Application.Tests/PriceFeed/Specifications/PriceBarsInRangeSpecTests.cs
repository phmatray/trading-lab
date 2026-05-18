using Shouldly;
using TradyStrat.Application.PriceFeed.Specifications;
using TradyStrat.Domain;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.PriceFeed.Specifications;

public class PriceBarsInRangeSpecTests
{
    private static readonly DateOnly From = new(2026, 1, 5);
    private static readonly DateOnly To   = new(2026, 1, 10);
    private const string Ticker  = "AAPL";
    private const string Other   = "MSFT";

    [Fact]
    public async Task Returns_bars_in_range_ascending_for_ticker()
    {
        await using var db = InMemoryDb.Create();

        // Seed bars: one before range, three inside, one after
        SeedBar(db, Ticker, new DateOnly(2026, 1, 4),  100m);  // before
        SeedBar(db, Ticker, new DateOnly(2026, 1, 5),  101m);  // from (inclusive)
        SeedBar(db, Ticker, new DateOnly(2026, 1, 7),  102m);  // inside
        SeedBar(db, Ticker, new DateOnly(2026, 1, 10), 103m);  // to (inclusive)
        SeedBar(db, Ticker, new DateOnly(2026, 1, 11), 104m);  // after
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new TestRepo<PriceBar>(db);
        var spec = new PriceBarsInRangeSpec(Ticker, From, To);
        var results = await repo.ListAsync(spec, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(3);
        results.Select(b => b.Close).ShouldBe([101m, 102m, 103m]);
        results.Select(b => b.Date).ShouldBeInOrder();
    }

    [Fact]
    public async Task Excludes_bars_for_other_tickers()
    {
        await using var db = InMemoryDb.Create();

        SeedBar(db, Ticker, new DateOnly(2026, 1, 7), 200m);
        SeedBar(db, Other,  new DateOnly(2026, 1, 7), 300m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new TestRepo<PriceBar>(db);
        var spec = new PriceBarsInRangeSpec(Ticker, From, To);
        var results = await repo.ListAsync(spec, TestContext.Current.CancellationToken);

        results.ShouldHaveSingleItem().Ticker.ShouldBe(Ticker);
    }

    [Fact]
    public async Task Empty_range_returns_empty()
    {
        await using var db = InMemoryDb.Create();

        SeedBar(db, Ticker, new DateOnly(2026, 1, 7), 100m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo = new TestRepo<PriceBar>(db);
        // from > to: logically empty range
        var spec = new PriceBarsInRangeSpec(Ticker, new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1));
        var results = await repo.ListAsync(spec, TestContext.Current.CancellationToken);

        results.ShouldBeEmpty();
    }

    private static void SeedBar(TradyStrat.Infrastructure.Data.AppDbContext db, string ticker, DateOnly date, decimal close)
        => db.PriceBars.Add(new PriceBar
        {
            Id = 0, Ticker = ticker, Date = date,
            Open = close, High = close, Low = close, Close = close, Volume = 1,
        });
}
