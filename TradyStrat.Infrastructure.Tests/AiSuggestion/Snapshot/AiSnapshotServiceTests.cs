using TradyStrat.Application.AiSuggestion.Snapshot.Sections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.Fx;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.Indicators.Bollinger;
using TradyStrat.Application.Indicators.History;
using TradyStrat.Application.Indicators.Ichimoku;
using TradyStrat.Application.Indicators.MovingAverage;
using TradyStrat.Application.Indicators.Rsi;
using TradyStrat.Application.Indicators.Zones;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.TestKit.Time;
using TradyStrat.TestKit;             // TestRepo<T>
using TradyStrat.TestKit.Indicators;     // SeriesLoader
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.AiSuggestion.Snapshot;

public class AiSnapshotServiceTests
{
    // Catalog order: focus first, then watchlist preserved in legacy order so the
    // day-one PromptHash stays byte-identical against the pre-multi-ticker fixture.
    private static readonly string[] ExpectedCatalogOrder = ["CON3.L", "COIN", "BTC-USD"];

    private static AiSnapshotService BuildSut(
        AppDbContext db,
        IReadOnlyList<PredictionMarket>? predictionMarkets = null,
        bool predictionMarketsThrow = false)
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
        var listInstruments = new ListInstrumentsUseCase(
            new TestRepo<Instrument>(db),
            NullLogger<ListInstrumentsUseCase>.Instance);
        var provider = new StubPredictionMarketProvider(
            predictionMarkets ?? [],
            shouldThrow: predictionMarketsThrow);
        ISnapshotSectionProvider[] sections =
        [
            new GoalSection(new TestRepo<GoalConfig>(db), clock),
            new TickersSection(engine, fx, listInstruments),
            new PortfolioSection(portfolio, listInstruments),
            new RecentTradesSection(new TestRepo<Trade>(db)),
            new MarketsSection(provider, NullLogger<MarketsSection>.Instance),
            new UsdPerEurSection(fx),
        ];
        return new AiSnapshotService(sections);
    }

    private sealed class StubPredictionMarketProvider(
        IReadOnlyList<PredictionMarket> markets, bool shouldThrow) : IPredictionMarketProvider
    {
        public Task<IReadOnlyList<PredictionMarket>> GetMarketsAsync(CancellationToken ct) =>
            shouldThrow
                ? Task.FromException<IReadOnlyList<PredictionMarket>>(
                    new PolymarketUnavailableException("stub failure"))
                : Task.FromResult(markets);
    }

    private static void SeedInstruments(AppDbContext db)
    {
        var seedAt = DateTime.UtcNow;
        db.Instruments.Add(new Instrument {
            Id = 0, Ticker = "CON3.L", Name = "Leverage Shares 3x Long Coinbase",
            Currency = "USD", Exchange = "LSE", TimezoneId = "Europe/London",
            Kind = InstrumentKind.Held, AddedAt = seedAt });
        db.Instruments.Add(new Instrument {
            Id = 0, Ticker = "COIN", Name = "Coinbase Global, Inc.",
            Currency = "USD", Exchange = "NMS", TimezoneId = "America/New_York",
            Kind = InstrumentKind.Watchlist, AddedAt = seedAt });
        db.Instruments.Add(new Instrument {
            Id = 0, Ticker = "BTC-USD", Name = "Bitcoin USD",
            Currency = "USD", Exchange = "CCC", TimezoneId = "UTC",
            Kind = InstrumentKind.Watchlist, AddedAt = seedAt });
    }

    [Fact]
    public async Task Builds_snapshot_with_all_three_tickers_and_eur_conversion()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        SeedInstruments(db);
        // CON3.L — full 250-bar series so Bollinger/RSI/SMA can compute
        foreach (var b in SeriesLoader.LoadCloses("CON3.L")) db.PriceBars.Add(b);
        // COIN, BTC-USD — single recent bar so the engine returns a price (indicators may be null)
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=new(2026,5,6),
                Open=200, High=200, Low=200, Close=200, Volume=1 });
        // FX rate
        db.FxRates.Add(new FxRate { Id=0, Base="EUR", Quote="USD", Date=new(2026,5,6),
            Rate = 1.08m, FetchedAt = DateTime.UtcNow });
        // Goal
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        await db.SaveChangesAsync(ct);

        var focusId = (await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct)).Id;
        var sut  = BuildSut(db);
        var today = new DateOnly(2026, 5, 6);
        var snap = await sut.CreateAsync(focusId, today, ct);

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

        SeedInstruments(db);
        // CON3.L — full 250-bar series; CSV ends at 2025-12-08, which is our asOf
        foreach (var b in SeriesLoader.LoadCloses("CON3.L")) db.PriceBars.Add(b);
        // COIN, BTC-USD — single bar at asOf
        var asOf = new DateOnly(2025, 12, 8);
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=asOf,
                Open=300, High=300, Low=300, Close=300, Volume=1 });
        // FX rate for asOf
        db.FxRates.Add(new FxRate { Id=0, Base="EUR", Quote="USD", Date=asOf,
            Rate = 1.10m, FetchedAt = DateTime.UtcNow });
        // Goal
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        await db.SaveChangesAsync(ct);

        var focusId = (await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct)).Id;
        var sut = BuildSut(db);
        var snapshot = await sut.CreateAsync(focusId, asOf, ct);

        snapshot.Today.ShouldBe(asOf);
    }

    [Fact]
    public async Task Catalog_iterates_focus_then_legacy_watchlist_order()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        // Seed instruments in alphabetical (BTC-USD, COIN, CON3.L) order to prove
        // the catalog ignores DB order and applies the legacy-order trick.
        var seedAt = DateTime.UtcNow;
        db.Instruments.Add(new Instrument {
            Id = 0, Ticker = "BTC-USD", Name = "Bitcoin USD",
            Currency = "USD", Exchange = "CCC", TimezoneId = "UTC",
            Kind = InstrumentKind.Watchlist, AddedAt = seedAt });
        db.Instruments.Add(new Instrument {
            Id = 0, Ticker = "COIN", Name = "Coinbase Global, Inc.",
            Currency = "USD", Exchange = "NMS", TimezoneId = "America/New_York",
            Kind = InstrumentKind.Watchlist, AddedAt = seedAt });
        db.Instruments.Add(new Instrument {
            Id = 0, Ticker = "CON3.L", Name = "Leverage Shares 3x Long Coinbase",
            Currency = "USD", Exchange = "LSE", TimezoneId = "Europe/London",
            Kind = InstrumentKind.Held, AddedAt = seedAt });

        // One-bar price series per instrument so IndicatorEngine returns a reading.
        var asOf = new DateOnly(2026, 5, 6);
        foreach (var t in new[] { "CON3.L", "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=asOf,
                Open=100, High=100, Low=100, Close=100, Volume=1 });
        db.FxRates.Add(new FxRate { Id=0, Base="EUR", Quote="USD", Date=asOf,
            Rate = 1.08m, FetchedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        await db.SaveChangesAsync(ct);

        var focusId = (await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct)).Id;
        var sut  = BuildSut(db);
        var snap = await sut.CreateAsync(focusId, asOf, ct);

        snap.Tickers.Select(t => t.Ticker).ShouldBe(ExpectedCatalogOrder);
    }

    [Fact]
    public async Task Catalog_produces_byte_identical_PromptHash_against_seeded_set()
    {
        // Sentinel for spec §11.3: changing the prompt input shape (catalog
        // order, ticker set, currency, snapshot field shape, etc.) MUST be a
        // deliberate decision, not a silent regression. If this test fails,
        // either (a) you changed something that affects the prompt and need
        // to update the captured hash, or (b) you accidentally broke the
        // legacy-order trick.
        // Hash was updated at Task 12 (prediction-markets) because the markets list
        // was added to the payload (previously "2EB10B0275AD1282").
        // Class renamed at Task 5 of the multi-ticker AI plan (SnapshotFactory ->
        // AiSnapshotService); the hash MUST stay identical across that rename.
        const string ExpectedHash = "895EED53A280A470";

        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        SeedInstruments(db);
        // One-bar price series per instrument so IndicatorEngine returns a reading.
        var asOf = new DateOnly(2026, 5, 6);
        foreach (var t in new[] { "CON3.L", "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=asOf,
                Open=100, High=100, Low=100, Close=100, Volume=1 });
        db.FxRates.Add(new FxRate { Id=0, Base="EUR", Quote="USD", Date=asOf,
            Rate = 1.08m, FetchedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        await db.SaveChangesAsync(ct);

        var focusId = (await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct)).Id;
        var sut  = BuildSut(db);
        var snap = await sut.CreateAsync(focusId, asOf, ct);

        snap.PromptHash.ShouldBe(ExpectedHash);
    }

    [Fact]
    public async Task Includes_markets_from_provider_in_snapshot()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        SeedInstruments(db);
        // CON3.L — full 250-bar series so indicators compute
        foreach (var b in SeriesLoader.LoadCloses("CON3.L")) db.PriceBars.Add(b);
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=new(2026,5,6),
                Open=200, High=200, Low=200, Close=200, Volume=1 });
        db.FxRates.Add(new FxRate { Id=0, Base="EUR", Quote="USD", Date=new(2026,5,6),
            Rate = 1.08m, FetchedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        await db.SaveChangesAsync(ct);

        var providedMarkets = new[]
        {
            new PredictionMarket("btc-100k", "Will BTC > $100k EOY?",
                0.32m, new DateOnly(2026, 12, 31), 1_000_000m, ["bitcoin"]),
        };
        var focusId = (await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct)).Id;
        var sut = BuildSut(db, predictionMarkets: providedMarkets);
        var snap = await sut.CreateAsync(focusId, new DateOnly(2026, 5, 6), ct);

        snap.Markets.Count.ShouldBe(1);
        snap.Markets[0].Slug.ShouldBe("btc-100k");
    }

    [Fact]
    public async Task PromptHash_changes_when_markets_change()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        SeedInstruments(db);
        foreach (var b in SeriesLoader.LoadCloses("CON3.L")) db.PriceBars.Add(b);
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=new(2026,5,6),
                Open=200, High=200, Low=200, Close=200, Volume=1 });
        db.FxRates.Add(new FxRate { Id=0, Base="EUR", Quote="USD", Date=new(2026,5,6),
            Rate = 1.08m, FetchedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        await db.SaveChangesAsync(ct);

        var focusId = (await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct)).Id;
        var snap1 = await BuildSut(db, predictionMarkets: []).CreateAsync(focusId, new DateOnly(2026, 5, 6), ct);
        var snap2 = await BuildSut(db, predictionMarkets: new[]
        {
            new PredictionMarket("btc-100k", "Will BTC > $100k EOY?",
                0.32m, new DateOnly(2026, 12, 31), 1_000_000m, ["bitcoin"]),
        }).CreateAsync(focusId, new DateOnly(2026, 5, 6), ct);

        snap1.PromptHash.ShouldNotBe(snap2.PromptHash);
    }

    [Fact]
    public async Task Tolerates_provider_failure_returns_empty_markets()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        SeedInstruments(db);
        foreach (var b in SeriesLoader.LoadCloses("CON3.L")) db.PriceBars.Add(b);
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=new(2026,5,6),
                Open=200, High=200, Low=200, Close=200, Volume=1 });
        db.FxRates.Add(new FxRate { Id=0, Base="EUR", Quote="USD", Date=new(2026,5,6),
            Rate = 1.08m, FetchedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        await db.SaveChangesAsync(ct);

        var focusId = (await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct)).Id;
        var sut = BuildSut(db, predictionMarketsThrow: true);
        var snap = await sut.CreateAsync(focusId, new DateOnly(2026, 5, 6), ct);

        snap.Markets.ShouldBeEmpty();
    }
}
