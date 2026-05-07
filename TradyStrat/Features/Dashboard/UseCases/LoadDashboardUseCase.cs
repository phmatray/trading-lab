using Ardalis.Specification;
using TradyStrat.Application.Abstractions;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.Dashboard;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;
using TradyStrat.Features.Fx.Specifications;
using TradyStrat.Features.PriceFeed.Specifications;
using TradyStrat.Features.AiSuggestion.Specifications;
using TradyStrat.Features.Trades.Specifications;

namespace TradyStrat.Features.Dashboard.UseCases;

public sealed class LoadDashboardUseCase(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    GrowthSeriesBuilder growth,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    IReadRepositoryBase<PriceBar> priceRepo,
    IReadRepositoryBase<Suggestion> suggestionRepo,
    IReadRepositoryBase<FxRate> fxRepo,
    GetTodaysSuggestionUseCase todaysSuggestion,
    ISuggestionBackfillCoordinator backfillCoord,
    IClock clock,
    ILogger<LoadDashboardUseCase> log)
    : UseCaseBase<Unit, DashboardViewModel>(log)
{
    private const string FocusTicker = "CON3.L";
    private const string FxPair      = "EURUSD";
    private const int    SparklineWindow = 30;

    private static readonly (string Ticker, string Currency)[] Catalog =
    [
        (FocusTicker, "USD"),
        ("COIN",      "USD"),
        ("BTC-USD",   "USD"),
    ];

    protected override async Task<DashboardViewModel> ExecuteCore(Unit unit, CancellationToken ct)
    {
        var today = clock.TodayInExchangeTzFor(FocusTicker);
        var goal  = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(clock.UtcNow());

        var tickers = new List<TickerView>();
        decimal? focusPriceEur = null;

        foreach (var (ticker, currency) in Catalog)
        {
            var reading = await indicators.ComputeFor(ticker, ct);
            decimal? eur = null;
            if (currency == "USD")
                eur = await fx.UsdToEurAsync(reading.Price, today, ct);
            if (ticker == FocusTicker) focusPriceEur = eur ?? reading.Price;

            var deltaPct = await ComputeDeltaPctAsync(ticker, ct);
            tickers.Add(new TickerView(
                ticker, currency, reading.Price, eur, deltaPct, reading.Zone));
        }

        var snap = await portfolio.SnapshotAsync(focusPriceEur ?? 0m, goal.TargetEur, ct);
        var growthSeries = await growth.BuildAsync(FocusTicker, ct);

        // The growth series is computed from raw bar.Close values (no FX).
        // Pin its trailing point to the hero's EUR-valued snapshot so the chart's
        // "today" label and the big hero number agree on the same page.
        // Followup-B: thread FxConverter through GrowthSeriesBuilder for a fully
        // EUR-correct curve across all dates.
        if (growthSeries.Count > 0)
        {
            var pinned = growthSeries.ToList();
            pinned[^1] = pinned[^1] with
            {
                Date = today,
                ValueEur = snap.CurrentValueEur
            };
            growthSeries = pinned;
        }

        var todays    = await todaysSuggestion.ExecuteAsync(Unit.Value, ct);
        var entryNum  = await tradeRepo.CountAsync(new AllTradesSpec(), ct);
        var latestBar = await priceRepo.FirstOrDefaultAsync(new LatestPriceBarSpec(FocusTicker), ct);

        // ---- new: prior suggestion + call diff
        var prior = await suggestionRepo.FirstOrDefaultAsync(new PriorSuggestionSpec(today), ct);
        var callDiff = new CallDiffBuilder()
            .WithToday(todays)
            .WithPrior(prior)
            .Build();

        // ---- new: indicator histories per citation
        var histories = new Dictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries>();
        foreach (var c in todays.Citations)
        {
            var kind = IndicatorKindParser.From(c.Indicator);
            if (kind is null) continue;
            var key = (c.Ticker, kind.Value);
            if (histories.ContainsKey(key)) continue;
            histories[key] = await indicators.HistoryFor(c.Ticker, kind.Value, SparklineWindow, ct);
        }

        // ---- new: goal pace
        var firstTrade = await tradeRepo.FirstOrDefaultAsync(new EarliestTradeSpec(), ct);
        var goalPace = GoalPaceCalculator.Compute(
            currentValueEur: snap.CurrentValueEur,
            goal: goal,
            today: today,
            firstTradeDate: firstTrade?.ExecutedOn);

        // ---- new: freshness pills
        var nowUtc = clock.UtcNow();
        var fxLatest = await fxRepo.FirstOrDefaultAsync(new LatestFxRateSpec(FxPair, today), ct);
        var priceAsOf = latestBar is { } lb
            ? RelativeTimeFormatter.Format(lb.Date.ToDateTime(TimeOnly.MinValue), nowUtc)
            : "";
        var callAsOf = RelativeTimeFormatter.Format(todays.CreatedAt, nowUtc);
        var fxAsOf   = fxLatest is { } fxr
            ? RelativeTimeFormatter.Format(fxr.FetchedAt, nowUtc)
            : "";

        // ---- new: enqueue backfill (fire-and-forget) & snapshot status
        if (prior is { ForDate: var lastDate } && today.AddDays(-1) > lastDate)
        {
            _ = backfillCoord
                .EnsureBackfilledAsync(lastDate, today.AddDays(-1), CancellationToken.None)
                .ContinueWith(
                    t => LoadDashboardLog.BackfillCrashed(log, t.Exception),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }
        var backfillStatus = backfillCoord.Status;

        return new DashboardViewModel(
            Today: today,
            EntryNumber: entryNum,
            Portfolio: snap,
            Goal: goal,
            TodaysCall: todays,
            Tickers: tickers,
            Growth: growthSeries,
            LatestPriceDate: latestBar?.Date,
            GoalPace: goalPace,
            CallDiff: callDiff,
            BackfillStatus: backfillStatus,
            PriceAsOfRelative: priceAsOf,
            CallAsOfRelative: callAsOf,
            FxAsOfRelative: fxAsOf,
            IndicatorHistories: histories);
    }

    private async Task<decimal?> ComputeDeltaPctAsync(string ticker, CancellationToken ct)
    {
        var bars = await priceRepo.ListAsync(new PriceBarsForTickerSpec(ticker), ct);
        if (bars.Count < 2) return null;
        var prev = bars[^2].Close;
        var curr = bars[^1].Close;
        if (prev == 0m) return null;
        return (curr - prev) / prev * 100m;
    }
}

internal static partial class LoadDashboardLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Backfill chain crashed unobserved")]
    public static partial void BackfillCrashed(ILogger logger, Exception? ex);
}
