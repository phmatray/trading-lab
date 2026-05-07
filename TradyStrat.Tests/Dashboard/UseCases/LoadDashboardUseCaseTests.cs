using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TradyStrat.Features.Indicators.Zones;
using TradyStrat.Features.Indicators.History;
using Shouldly;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.Dashboard.UseCases;
using TradyStrat.Features.AiSuggestion.Backfill;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Features.Dashboard.Navigation;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Features.Settings.UseCases;
using TradyStrat.Common.Domain;
using TradyStrat.Features.Indicators.Bollinger;
using TradyStrat.Features.Indicators.Ichimoku;
using TradyStrat.Features.Indicators.MovingAverage;
using TradyStrat.Features.Indicators.Rsi;
using TradyStrat.Features.AiSuggestion.CallDiff;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Indicators;        // SeriesLoader
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Common.Time;
using TradyStrat.Tests.AiSuggestion.UseCases;  // StubSnapshotFactory, StubAiClient
using Xunit;

namespace TradyStrat.Tests.Dashboard.UseCases;

public class LoadDashboardUseCaseTests
{
    private static readonly DateOnly Target = new(2026, 5, 6);

    private sealed class NullCoordinator : ISuggestionBackfillCoordinator
    {
        public BackfillStatus Status => BackfillStatus.Idle.Instance;
#pragma warning disable CS0067 // event never raised in stub
        public event Action<BackfillStatus>? StatusChanged;
#pragma warning restore CS0067
        public Task EnsureBackfilledAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct)
        {
            EnsureBackfilledCalls++;
            return Task.CompletedTask;
        }
        public int EnsureBackfilledCalls;
    }

    private sealed class FakeNav : IEntryNavigationService
    {
        public DateOnly  Earliest { get; set; } = new(2026, 1, 1);
        public DateOnly  Latest   { get; set; } = Target;
        public DateOnly? Prev     { get; set; } = new(2026, 5, 5);
        public DateOnly? Next     { get; set; }

        public Task<DateOnly>  EarliestAsync(CancellationToken ct) => Task.FromResult(Earliest);
        public Task<DateOnly>  LatestAsync(CancellationToken ct)   => Task.FromResult(Latest);
        public Task<DateOnly?> PreviousAsync(DateOnly current, CancellationToken ct) => Task.FromResult(Prev);
        public Task<DateOnly?> NextAsync(DateOnly current, CancellationToken ct)     => Task.FromResult(Next);
        public Task<DateOnly>  ResolveOrFallbackAsync(DateOnly requested, CancellationToken ct) => Task.FromResult(requested);
    }

    private static (LoadDashboardUseCase uc, NullCoordinator coord, FakeNav nav)
        BuildSut(TradyStrat.Data.AppDbContext db)
    {
        var classifier = new ZoneClassifier(new IZoneRule[] {
            new BollingerZoneRule(), new RsiZoneRule(),
            new MovingAverageZoneRule(), new IchimokuZoneRule() });
        var indicators = new IndicatorEngine(new TestRepo<PriceBar>(db), classifier, new IndicatorHistoryProviderFactory([]));
        var portfolio  = new PortfolioService(new TestRepo<Trade>(db));
        var growth     = new GrowthSeriesBuilder(new TestRepo<Trade>(db), new TestRepo<PriceBar>(db));
        var fx         = new FxConverter(new TestRepo<FxRate>(db));
        var clock      = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));

        var snapStub = new StubSnapshotFactory(new AiSnapshot(
            Target, GoalConfig.Default(DateTime.UtcNow),
            new([],0,0,0,0,0,0,0), [], [], 1.08m, "h"));
        var aiStub = new StubAiClient(new Suggestion {
            Id = 0, ForDate = Target, Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "from-ai", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        var todays = new GetTodaysSuggestionUseCase(
            new TestRepo<Suggestion>(db), snapStub, aiStub, clock,
            NullLogger<GetTodaysSuggestionUseCase>.Instance);

        var coord = new NullCoordinator();
        var nav   = new FakeNav();

        var listInstruments = new ListInstrumentsUseCase(
            new TestRepo<Instrument>(db),
            NullLogger<ListInstrumentsUseCase>.Instance);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tickers:Focus"] = "CON3.L",
            })
            .Build();

        var uc = new LoadDashboardUseCase(
            indicators, portfolio, growth, fx,
            new TestRepo<GoalConfig>(db),
            new TestRepo<Trade>(db),
            new TestRepo<PriceBar>(db),
            new TestRepo<Suggestion>(db),
            new TestRepo<FxRate>(db),
            listInstruments,
            config,
            todays,
            coord,
            nav,
            NullLogger<LoadDashboardUseCase>.Instance);

        return (uc, coord, nav);
    }

    private static async Task SeedBaseAsync(TradyStrat.Data.AppDbContext db, CancellationToken ct,
        Suggestion? seedSuggestion = null)
    {
        // Phase 1 dashboard: focus is configured as CON3.L; the use case enumerates
        // all Instruments (Held + Watchlist) from the DB. CON3.L is the lone Held
        // position; COIN and BTC-USD ride the Watchlist for zone analysis only.
        var seedAt = DateTime.UtcNow;
        db.Instruments.Add(new Instrument {
            Id = 1, Ticker = "CON3.L", Name = "Leverage Shares 3x Long Coinbase",
            Currency = "USD", Exchange = "LSE", TimezoneId = "Europe/London",
            Kind = InstrumentKind.Held, AddedAt = seedAt });
        db.Instruments.Add(new Instrument {
            Id = 2, Ticker = "COIN", Name = "Coinbase Global, Inc.",
            Currency = "USD", Exchange = "NMS", TimezoneId = "America/New_York",
            Kind = InstrumentKind.Watchlist, AddedAt = seedAt });
        db.Instruments.Add(new Instrument {
            Id = 3, Ticker = "BTC-USD", Name = "Bitcoin USD",
            Currency = "USD", Exchange = "CCC", TimezoneId = "UTC",
            Kind = InstrumentKind.Watchlist, AddedAt = seedAt });
        foreach (var b in SeriesLoader.LoadCloses("CON3.L")) db.PriceBars.Add(b);
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=Target,
                Open=200, High=200, Low=200, Close=200, Volume=1 });
        db.FxRates.Add(new FxRate { Id=0, Base="EUR", Quote="USD", Date=Target,
            Rate = 1.08m, FetchedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        db.Trades.Add(new Trade {
            Id = 0, InstrumentId = 1, ExecutedOn = new(2025,12,7), Side = TradeSide.Buy,
            Quantity = 100m, PricePerShare = 4m, FeesEur = 0m, Note = null,
            CreatedAt = DateTime.UtcNow });
        if (seedSuggestion is not null) db.Suggestions.Add(seedSuggestion);
        await db.SaveChangesAsync(ct);
    }

    [Fact]
    public async Task Live_mode_composes_view_model_with_three_tickers_and_growth_series()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        await SeedBaseAsync(db, ct, new Suggestion {
            Id = 0, ForDate = Target, Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "stable", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });

        var (uc, _, _) = BuildSut(db);
        var vm = await uc.ExecuteAsync(new LoadDashboardInput(Target, IsHistorical: false), ct);

        vm.Today.ShouldBe(Target);
        vm.Tickers.Count.ShouldBe(3);
        vm.Growth.Count.ShouldBeGreaterThan(0);
        vm.Goal.TargetEur.ShouldBe(1_000_000m);
        vm.TodaysCall.ShouldNotBeNull();
        vm.TodaysCall.Rationale.ShouldBe("stable");
        vm.IsHistorical.ShouldBeFalse();
        vm.PrevTradingDay.ShouldBe(new DateOnly(2026, 5, 5));
        vm.NextTradingDay.ShouldBeNull();
    }

    [Fact]
    public async Task Last_growth_point_matches_hero_current_value_eur()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        await SeedBaseAsync(db, ct, new Suggestion {
            Id = 0, ForDate = Target, Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "stable", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });

        var (uc, _, _) = BuildSut(db);
        var vm = await uc.ExecuteAsync(new LoadDashboardInput(Target, IsHistorical: false), ct);

        vm.Growth.Count.ShouldBeGreaterThan(0);
        var lastPoint = vm.Growth[^1];
        lastPoint.ValueEur.ShouldBe(vm.Portfolio.CurrentValueEur);
        lastPoint.Date.ShouldBe(vm.Today);
    }

    [Fact]
    public async Task Historical_mode_uses_stored_suggestion_and_skips_backfill()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        await SeedBaseAsync(db, ct, new Suggestion {
            Id = 0, ForDate = Target, Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "from-db", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });

        var (uc, coord, _) = BuildSut(db);
        var vm = await uc.ExecuteAsync(new LoadDashboardInput(Target, IsHistorical: true), ct);

        vm.IsHistorical.ShouldBeTrue();
        vm.TodaysCall.ShouldNotBeNull();
        // Came from the suggestionRepo, not the StubAiClient (which would have set "from-ai").
        vm.TodaysCall.Rationale.ShouldBe("from-db");
        coord.EnsureBackfilledCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Historical_mode_renders_empty_call_when_no_suggestion_stored()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        await SeedBaseAsync(db, ct, seedSuggestion: null); // no Suggestion row

        var (uc, _, _) = BuildSut(db);
        var vm = await uc.ExecuteAsync(new LoadDashboardInput(Target, IsHistorical: true), ct);

        vm.TodaysCall.ShouldBeNull();
        vm.CallDiff.ShouldBe(CallDiff.None);
    }

    [Fact]
    public async Task Entry_number_uses_TradesAsOfSpec_target_date()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        await SeedBaseAsync(db, ct, new Suggestion {
            Id = 0, ForDate = Target, Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "x", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });

        // One additional trade *after* the target date — should NOT be counted.
        db.Trades.Add(new Trade {
            Id = 0, InstrumentId = 1, ExecutedOn = new(2026, 6, 1), Side = TradeSide.Buy,
            Quantity = 1m, PricePerShare = 1m, FeesEur = 0m, Note = null,
            CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var (uc, _, _) = BuildSut(db);
        var vm = await uc.ExecuteAsync(new LoadDashboardInput(Target, IsHistorical: true), ct);

        vm.EntryNumber.ShouldBe(1); // only the original 2025-12-07 trade is on-or-before target
    }
}
