using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.Dashboard.UseCases;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Common.Domain;
using TradyStrat.Features.Indicators.Bollinger;
using TradyStrat.Features.Indicators.Ichimoku;
using TradyStrat.Features.Indicators.MovingAverage;
using TradyStrat.Features.Indicators.Rsi;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Indicators;        // SeriesLoader
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;
using TradyStrat.Tests.AiSuggestion.UseCases;  // StubSnapshotBuilder, StubAiClient
using Xunit;

namespace TradyStrat.Tests.Dashboard.UseCases;

public class LoadDashboardUseCaseTests
{
    private sealed class NullCoordinator : ISuggestionBackfillCoordinator
    {
        public BackfillStatus Status => BackfillStatus.Idle.Instance;
#pragma warning disable CS0067 // event never raised in stub
        public event Action<BackfillStatus>? StatusChanged;
#pragma warning restore CS0067
        public Task EnsureBackfilledAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct)
            => Task.CompletedTask;
    }

    [Fact]
    public async Task Composes_view_model_with_three_tickers_and_growth_series()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        // CON3.L — 250-bar series so all indicators compute
        foreach (var b in SeriesLoader.LoadCloses("CON3.L")) db.PriceBars.Add(b);
        // COIN, BTC-USD — minimal recent bar each
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=new(2026,5,6),
                Open=200, High=200, Low=200, Close=200, Volume=1 });
        db.FxRates.Add(new FxRate { Id=0, Pair="EURUSD", Date=new(2026,5,6),
            UsdPerEur = 1.08m, FetchedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        db.Trades.Add(new Trade {
            Id = 0, ExecutedOn = new(2025,12,7), Side = TradeSide.Buy,
            Quantity = 100m, PricePerShare = 4m, FeesEur = 0m, Note = null,
            CreatedAt = DateTime.UtcNow });
        db.Suggestions.Add(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "stable", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var classifier = new ZoneClassifier(new IZoneRule[] {
            new BollingerZoneRule(), new RsiZoneRule(),
            new MovingAverageZoneRule(), new IchimokuZoneRule() });
        var indicators = new IndicatorEngine(new TestRepo<PriceBar>(db), classifier, new IndicatorHistoryProviderFactory([]));
        var portfolio  = new PortfolioService(new TestRepo<Trade>(db));
        var growth     = new GrowthSeriesBuilder(new TestRepo<Trade>(db), new TestRepo<PriceBar>(db));
        var fx         = new FxConverter(new TestRepo<FxRate>(db));
        var clock      = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));

        // Use the existing AI stubs from T32 to plug GetTodaysSuggestionUseCase
        var snapStub = new StubSnapshotFactory(new AiSnapshot(
            new(2026,5,6), GoalConfig.Default(DateTime.UtcNow),
            new(0,0,0,0,0,0), [], [], 1.08m, "h"));
        var aiStub = new StubAiClient(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "x", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        var todays = new GetTodaysSuggestionUseCase(
            new TestRepo<Suggestion>(db), snapStub, aiStub, clock,
            NullLogger<GetTodaysSuggestionUseCase>.Instance);

        var uc = new LoadDashboardUseCase(
            indicators, portfolio, growth, fx,
            new TestRepo<GoalConfig>(db),
            new TestRepo<Trade>(db),
            new TestRepo<PriceBar>(db),
            new TestRepo<Suggestion>(db),
            new TestRepo<FxRate>(db),
            todays,
            new NullCoordinator(),
            clock,
            NullLogger<LoadDashboardUseCase>.Instance);

        var vm = await uc.ExecuteAsync(Unit.Value, ct);

        vm.Today.ShouldBe(new DateOnly(2026,5,6));
        vm.Tickers.Count.ShouldBe(3);
        vm.Growth.Count.ShouldBeGreaterThan(0);
        vm.Goal.TargetEur.ShouldBe(1_000_000m);
        vm.TodaysCall.Rationale.ShouldBe("stable");
    }

    [Fact]
    public async Task Last_growth_point_matches_hero_current_value_eur()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        // CON3.L — 250-bar series so all indicators compute
        foreach (var b in SeriesLoader.LoadCloses("CON3.L")) db.PriceBars.Add(b);
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=new(2026,5,6),
                Open=200, High=200, Low=200, Close=200, Volume=1 });
        // EURUSD = 1.08 means EUR ≠ USD numerically, surfacing the bug if not pinned.
        db.FxRates.Add(new FxRate { Id=0, Pair="EURUSD", Date=new(2026,5,6),
            UsdPerEur = 1.08m, FetchedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        db.Trades.Add(new Trade {
            Id = 0, ExecutedOn = new(2025,12,7), Side = TradeSide.Buy,
            Quantity = 100m, PricePerShare = 4m, FeesEur = 0m, Note = null,
            CreatedAt = DateTime.UtcNow });
        db.Suggestions.Add(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "stable", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var classifier = new ZoneClassifier(new IZoneRule[] {
            new BollingerZoneRule(), new RsiZoneRule(),
            new MovingAverageZoneRule(), new IchimokuZoneRule() });
        var indicators = new IndicatorEngine(new TestRepo<PriceBar>(db), classifier, new IndicatorHistoryProviderFactory([]));
        var portfolio  = new PortfolioService(new TestRepo<Trade>(db));
        var growth     = new GrowthSeriesBuilder(new TestRepo<Trade>(db), new TestRepo<PriceBar>(db));
        var fx         = new FxConverter(new TestRepo<FxRate>(db));
        var clock      = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));

        var snapStub = new StubSnapshotFactory(new AiSnapshot(
            new(2026,5,6), GoalConfig.Default(DateTime.UtcNow),
            new(0,0,0,0,0,0), [], [], 1.08m, "h"));
        var aiStub = new StubAiClient(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "x", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        var todays = new GetTodaysSuggestionUseCase(
            new TestRepo<Suggestion>(db), snapStub, aiStub, clock,
            NullLogger<GetTodaysSuggestionUseCase>.Instance);

        var uc = new LoadDashboardUseCase(
            indicators, portfolio, growth, fx,
            new TestRepo<GoalConfig>(db),
            new TestRepo<Trade>(db),
            new TestRepo<PriceBar>(db),
            new TestRepo<Suggestion>(db),
            new TestRepo<FxRate>(db),
            todays,
            new NullCoordinator(),
            clock,
            NullLogger<LoadDashboardUseCase>.Instance);

        var vm = await uc.ExecuteAsync(Unit.Value, ct);

        vm.Growth.Count.ShouldBeGreaterThan(0);
        var lastPoint = vm.Growth[^1];
        lastPoint.ValueEur.ShouldBe(vm.Portfolio.CurrentValueEur);
        lastPoint.Date.ShouldBe(vm.Today);
    }
}
