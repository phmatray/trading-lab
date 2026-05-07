using Shouldly;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Fx;             // TestRepo<T>
using TradyStrat.Tests.Indicators;     // SeriesLoader
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;
using Xunit;

namespace TradyStrat.Tests.AiSuggestion;

public class SnapshotBuilderTests
{
    [Fact]
    public async Task Builds_snapshot_with_all_three_tickers_and_eur_conversion()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        // CON3.L — full 250-bar series so Bollinger/RSI/SMA can compute
        foreach (var b in SeriesLoader.LoadCloses("CON3.L")) db.PriceBars.Add(b);
        // COIN, BTC-USD — single recent bar so the engine returns a price (indicators may be null)
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=new(2026,5,6),
                Open=200, High=200, Low=200, Close=200, Volume=1 });
        // FX rate
        db.FxRates.Add(new FxRate { Id=0, Pair="EURUSD", Date=new(2026,5,6),
            UsdPerEur = 1.08m, FetchedAt = DateTime.UtcNow });
        // Goal
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        await db.SaveChangesAsync(ct);

        var classifier = new ZoneClassifier(new IZoneRule[]
        {
            new BollingerZoneRule(), new RsiZoneRule(),
            new MovingAverageZoneRule(), new IchimokuZoneRule()
        });
        var engine    = new IndicatorEngine(new TestRepo<PriceBar>(db), classifier);
        var portfolio = new PortfolioService(new TestRepo<Trade>(db));
        var fx        = new FxConverter(new TestRepo<FxRate>(db));
        var clock     = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));

        var sb = new SnapshotBuilder(engine, portfolio, fx,
            new TestRepo<GoalConfig>(db), new TestRepo<Trade>(db), clock);

        var snap = await sb.BuildAsync(ct);

        snap.Today.ShouldBe(new DateOnly(2026,5,6));
        snap.Goal.TargetEur.ShouldBe(1_000_000m);
        snap.Tickers.Count.ShouldBe(3);
        snap.Tickers.Single(t => t.Ticker == "COIN").PriceEur!.Value.ShouldBe(200m / 1.08m, tolerance: 0.01m);
        snap.Tickers.Single(t => t.Ticker == "CON3.L").Currency.ShouldBe("USD");
        snap.PromptHash.ShouldNotBeNullOrEmpty();
    }
}
