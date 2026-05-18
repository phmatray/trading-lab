using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.AiSuggestion.Backfill;
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Application.Dashboard;
using TradyStrat.Application.Dashboard.UseCases;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Cli.Mcp.Tools;
using TradyStrat.Domain;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Tools;

public sealed class DashboardToolTests
{
    private static readonly DateOnly FixedToday = new(2026, 5, 18);
    private const string FocusTicker = "CON3.L";
    private const string OtherTicker = "COIN";

    // ─── Fake use case ───────────────────────────────────────────────────────────

    private sealed class FakeDashboardUseCase(DashboardViewModel vm)
        : IUseCase<LoadDashboardInput, DashboardViewModel>
    {
        public LoadDashboardInput? LastInput { get; private set; }

        public Task<DashboardViewModel> ExecuteAsync(LoadDashboardInput input, CancellationToken ct)
        {
            LastInput = input;
            return Task.FromResult(vm);
        }
    }

    // ─── Fake indicator engine ────────────────────────────────────────────────

    private sealed class FakeIndicatorEngine(IndicatorReading reading) : IIndicatorEngine
    {
        public Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct)
            => Task.FromResult(reading);

        public Task<IndicatorReading> ComputeFor(string ticker, DateOnly asOf, CancellationToken ct)
            => Task.FromResult(reading);

        public Task<IndicatorSeries> HistoryFor(
            string ticker, IndicatorKind kind, int lastN, CancellationToken ct)
            => Task.FromResult(IndicatorSeries.Empty);

        public Task<IndicatorSeries> HistoryFor(
            string ticker, IndicatorKind kind, int lastN, DateOnly asOf, CancellationToken ct)
            => Task.FromResult(IndicatorSeries.Empty);
    }

    // ─── Fixture helpers ──────────────────────────────────────────────────────

    private static Instrument MakeInstrument(int id, string ticker) => new()
    {
        Id = id,
        Ticker = ticker,
        Name = ticker,
        Currency = "GBP",
        Exchange = "LSE",
        TimezoneId = "Europe/London",
        Kind = InstrumentKind.Held,
        AddedAt = DateTime.UtcNow,
    };

    private static IndicatorReading MakeReading(string ticker) => new(
        Ticker: ticker,
        Price: 100m,
        Bollinger: new BollingerReading(Upper: 110m, Middle: 100m, Lower: 90m, Sigma: 5m),
        Rsi: 45m,
        Sma50: 98m,
        Sma200: 92m,
        Ichimoku: new IchimokuReading(
            Tenkan: 99m, Kijun: 97m,
            SenkouA: 96m, SenkouB: 94m,
            Chikou: 102m,
            Signal: IchimokuSignal.AboveCloud),
        Zone: Zone.Accumulate,
        Reasons: ["RSI in zone"]);

    private static DashboardViewModel MakeViewModel(string focusTicker = FocusTicker)
    {
        var position = new PositionRow(
            InstrumentId: 1,
            Ticker: focusTicker,
            Currency: "GBP",
            Quantity: 10m,
            CostBasisEur: 1000m,
            MarketValueEur: 1100m,
            UnrealizedPnLEur: 100m,
            RealizedPnLEur: 0m);

        var snap = new PortfolioSnapshot(
            Positions: [position],
            CurrentValueEur: 1100m,
            CostBasisEur: 1000m,
            UnrealizedPnLEur: 100m,
            RealizedPnLEur: 0m,
            ProgressPct: 11m,
            Shares: 10m,
            AvgCostEur: 100m);

        var tickerView = new TickerView(
            Ticker: focusTicker,
            Currency: "GBP",
            Price: 110m,
            PriceEur: 100m,
            DeltaPct: 0.5m,
            Zone: Zone.Accumulate,
            Spark: [100m, 105m, 110m],
            TodaysCall: null);

        var goal = new GoalConfig
        {
            Id = 1,
            TargetEur = 10_000m,
            TargetDate = new DateOnly(2027, 12, 31),
            UpdatedAt = DateTime.UtcNow,
        };

        return new DashboardViewModel(
            Today: FixedToday,
            EntryNumber: 1,
            Portfolio: snap,
            Goal: goal,
            TodaysCall: null,
            Tickers: [tickerView],
            Positions: [position],
            FocusTicker: focusTicker,
            Growth: [],
            LatestPriceDate: FixedToday,
            GoalPace: GoalPaceVm.Zero,
            CallDiff: CallDiff.None,
            BackfillStatus: BackfillStatus.Idle.Instance,
            PriceAsOfRelative: "today",
            CallAsOfRelative: "",
            FxAsOfRelative: "",
            IndicatorHistories: new Dictionary<(string, IndicatorKind), IndicatorSeries>(),
            CapitalEvents: [],
            IsHistorical: false,
            EarliestTradingDay: new DateOnly(2025, 12, 7),
            LatestTradingDay: FixedToday,
            PrevTradingDay: FixedToday.AddDays(-1),
            NextTradingDay: null);
    }

    private static async Task<(DashboardTool tool, FakeDashboardUseCase fakeUseCase)> BuildAsync(
        string focus,
        string[] knownTickers,
        DashboardViewModel? vm = null,
        DateOnly? clockDate = null,
        CancellationToken ct = default)
    {
        var db = InMemoryDb.Create();
        for (var i = 0; i < knownTickers.Length; i++)
            db.Instruments.Add(MakeInstrument(i + 1, knownTickers[i]));
        await db.SaveChangesAsync(ct);

        var repo = new TestRepo<Instrument>(db);
        var listInstruments = new ListInstrumentsUseCase(repo, NullLogger<ListInstrumentsUseCase>.Instance);
        var guards = new Guards(listInstruments);

        var fakeUseCase = new FakeDashboardUseCase(vm ?? MakeViewModel(focus));
        var fakeIndicators = new FakeIndicatorEngine(MakeReading(focus));
        var clock = new FakeClock(clockDate.HasValue
            ? clockDate.Value.ToDateTime(TimeOnly.MinValue)
            : FixedToday.ToDateTime(TimeOnly.MinValue));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Tickers:Focus"] = focus })
            .Build();

        var tool = new DashboardTool(fakeUseCase, guards, fakeIndicators, clock, config);
        return (tool, fakeUseCase);
    }

    // ─── Tests ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_snapshot_for_explicit_instrument_and_asOf()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, _) = await BuildAsync(FocusTicker, [FocusTicker, OtherTicker], ct: ct);

        var result = await tool.GetDashboard(
            instrument: FocusTicker,
            asOf: "2026-05-18",
            ct: ct);

        result.ShouldNotBeNull();
        result.Ticker.ShouldBe(FocusTicker);
    }

    [Fact]
    public async Task Defaults_instrument_to_Tickers_Focus_when_omitted()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, _) = await BuildAsync(FocusTicker, [FocusTicker, OtherTicker], ct: ct);

        var result = await tool.GetDashboard(ct: ct);

        result.ShouldNotBeNull();
        result.Ticker.ShouldBe(FocusTicker);
    }

    [Fact]
    public async Task Defaults_asOf_to_clock_today_when_omitted()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, useCase) = await BuildAsync(FocusTicker, [FocusTicker], clockDate: FixedToday, ct: ct);

        await tool.GetDashboard(ct: ct);

        useCase.LastInput.ShouldNotBeNull();
        useCase.LastInput!.TargetDate.ShouldBe(FixedToday);
        useCase.LastInput.IsHistorical.ShouldBeFalse();
    }

    [Fact]
    public async Task Unknown_instrument_throws_ArgumentException()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, _) = await BuildAsync(FocusTicker, [FocusTicker], ct: ct);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => tool.GetDashboard(instrument: "UNKNOWN", ct: ct));

        ex.Message.ShouldContain("UNKNOWN");
        ex.Message.ShouldContain("list_instruments");
    }
}
