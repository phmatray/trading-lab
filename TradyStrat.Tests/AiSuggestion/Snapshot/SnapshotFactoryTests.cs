using Shouldly;
using TradyStrat.Features.Indicators.Zones;
using TradyStrat.Features.Indicators.History;
using TradyStrat.Data;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Common.Domain;
using TradyStrat.Features.Indicators.Bollinger;
using TradyStrat.Features.Indicators.Ichimoku;
using TradyStrat.Features.Indicators.MovingAverage;
using TradyStrat.Features.Indicators.Rsi;
using TradyStrat.Tests.Fx;             // TestRepo<T>
using TradyStrat.Tests.Indicators;     // SeriesLoader
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;
using Xunit;

namespace TradyStrat.Tests.AiSuggestion.Snapshot;

public class SnapshotFactoryTests
{
    private static SnapshotFactory BuildSut(AppDbContext db)
    {
        var classifier = new ZoneClassifier(new IZoneRule[]
        {
            new BollingerZoneRule(), new RsiZoneRule(),
            new MovingAverageZoneRule(), new IchimokuZoneRule()
        });
        var engine    = new IndicatorEngine(new TestRepo<PriceBar>(db), classifier, new IndicatorHistoryProviderFactory([]));
        var portfolio = new PortfolioService(new TestRepo<Trade>(db));
        var fx        = new FxConverter(new TestRepo<FxRate>(db));
        var clock     = new FakeClock(new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc));
        return new SnapshotFactory(engine, portfolio, fx,
            new TestRepo<GoalConfig>(db), new TestRepo<Trade>(db), clock);
    }

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

        var sut  = BuildSut(db);
        var today = new DateOnly(2026, 5, 6);
        var snap = await sut.CreateAsync(today, ct);

        snap.Today.ShouldBe(new DateOnly(2026,5,6));
        snap.Goal.TargetEur.ShouldBe(1_000_000m);
        snap.Tickers.Count.ShouldBe(3);
        snap.Tickers.Single(t => t.Ticker == "COIN").PriceEur!.Value.ShouldBe(200m / 1.08m, tolerance: 0.01m);
        snap.Tickers.Single(t => t.Ticker == "CON3.L").Currency.ShouldBe("USD");
        snap.PromptHash.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAsync_sets_snapshot_Today_to_asOf()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        // CON3.L — full 250-bar series; CSV ends at 2025-12-08, which is our asOf
        foreach (var b in SeriesLoader.LoadCloses("CON3.L")) db.PriceBars.Add(b);
        // COIN, BTC-USD — single bar at asOf
        var asOf = new DateOnly(2025, 12, 8);
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=asOf,
                Open=300, High=300, Low=300, Close=300, Volume=1 });
        // FX rate for asOf
        db.FxRates.Add(new FxRate { Id=0, Pair="EURUSD", Date=asOf,
            UsdPerEur = 1.10m, FetchedAt = DateTime.UtcNow });
        // Goal
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        await db.SaveChangesAsync(ct);

        var sut = BuildSut(db);
        var snapshot = await sut.CreateAsync(asOf, ct);

        snapshot.Today.ShouldBe(asOf);
    }
}
