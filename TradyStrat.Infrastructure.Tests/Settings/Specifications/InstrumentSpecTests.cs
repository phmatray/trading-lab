using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Application.Settings.Specifications;
using TradyStrat.TestKit;             // TestRepo<T>
using TradyStrat.TestKit.Specifications; // InMemoryDb
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings.Specifications;

public class InstrumentSpecTests
{
    private static readonly string[] WatchlistTickersOrdered = ["BTC-USD", "COIN"];

    private static Instrument Make(string ticker, InstrumentKind kind = InstrumentKind.Held) =>
        new()
        {
            Id = 0, Ticker = ticker, Name = ticker, Currency = "USD",
            Exchange = "NMS", TimezoneId = "America/New_York",
            Kind = kind, AddedAt = DateTime.UtcNow,
        };

    [Fact]
    public async Task InstrumentByTickerSpec_finds_match()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        db.Instruments.Add(Make("CON3.L"));
        db.Instruments.Add(Make("COIN", InstrumentKind.Watchlist));
        await db.SaveChangesAsync(ct);

        var repo = new TestRepo<Instrument>(db);
        var hit = await repo.FirstOrDefaultAsync(new InstrumentByTickerSpec("COIN"), ct);

        hit.ShouldNotBeNull();
        hit!.Ticker.ShouldBe("COIN");
    }

    [Fact]
    public async Task InstrumentsByKindSpec_filters_to_kind()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        db.Instruments.Add(Make("CON3.L", InstrumentKind.Held));
        db.Instruments.Add(Make("COIN",   InstrumentKind.Watchlist));
        db.Instruments.Add(Make("BTC-USD",InstrumentKind.Watchlist));
        await db.SaveChangesAsync(ct);

        var repo = new TestRepo<Instrument>(db);
        var watch = await repo.ListAsync(new InstrumentsByKindSpec(InstrumentKind.Watchlist), ct);

        watch.Count.ShouldBe(2);
        watch.Select(i => i.Ticker).ShouldBe(WatchlistTickersOrdered);
    }
}
