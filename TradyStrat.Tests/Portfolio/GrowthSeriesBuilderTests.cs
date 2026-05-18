using Shouldly;
using TradyStrat.Features.Portfolio;
using TradyStrat.Domain;
using TradyStrat.Tests.Fx;             // TestRepo<T>
using TradyStrat.Tests.Specifications; // InMemoryDb
using Xunit;

namespace TradyStrat.Tests.Portfolio;

public class GrowthSeriesBuilderTests
{
    [Fact]
    public async Task Builds_one_point_per_day_with_running_value()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        // 10 sh on day 1 at €4, 5 sh on day 3 at €5
        db.Trades.AddRange(
            new Trade { Id = 0, InstrumentId = 1, ExecutedOn = new(2026,1,1), Side = TradeSide.Buy,
                        Quantity = 10m, PricePerShare = 4m, FeesEur = 0m, Note = null,
                        CreatedAt = DateTime.UtcNow },
            new Trade { Id = 0, InstrumentId = 1, ExecutedOn = new(2026,1,3), Side = TradeSide.Buy,
                        Quantity = 5m, PricePerShare = 5m, FeesEur = 0m, Note = null,
                        CreatedAt = DateTime.UtcNow });

        // Daily CON3 prices days 1..4
        db.PriceBars.AddRange(
            new PriceBar { Id=0, Ticker="CON3.DE", Date=new(2026,1,1), Open=4, High=4, Low=4, Close=4.0m, Volume=1 },
            new PriceBar { Id=0, Ticker="CON3.DE", Date=new(2026,1,2), Open=4, High=4, Low=4, Close=4.5m, Volume=1 },
            new PriceBar { Id=0, Ticker="CON3.DE", Date=new(2026,1,3), Open=5, High=5, Low=5, Close=5.0m, Volume=1 },
            new PriceBar { Id=0, Ticker="CON3.DE", Date=new(2026,1,4), Open=5, High=5, Low=5, Close=5.2m, Volume=1 });

        await db.SaveChangesAsync(ct);

        var b = new GrowthSeriesBuilder(new TestRepo<Trade>(db), new TestRepo<PriceBar>(db));
        var pts = await b.BuildAsync("CON3.DE", ct);

        // 1 synthetic leading-zero point + 4 daily bars
        pts.Count.ShouldBe(5);
        pts[0].Date.ShouldBe(new DateOnly(2025,12,31));
        pts[0].ValueEur.ShouldBe(0m);
        pts[1].ValueEur.ShouldBe(10m * 4.0m);
        pts[2].ValueEur.ShouldBe(10m * 4.5m);
        pts[3].ValueEur.ShouldBe(15m * 5.0m);
        pts[4].ValueEur.ShouldBe(15m * 5.2m);
    }

    [Fact]
    public async Task Returns_empty_when_no_trades()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var b = new GrowthSeriesBuilder(new TestRepo<Trade>(db), new TestRepo<PriceBar>(db));
        (await b.BuildAsync("CON3.DE", ct)).ShouldBeEmpty();
    }
}
