using TradyStrat.Application.Time;
using System.Text.Json;
using Ardalis.Specification;
using TradyStrat.Domain;
using TradyStrat.Application.UseCases;
using TradyStrat.Application.AiSuggestion;        // JsonOpts
using TradyStrat.Application.AiSuggestion.Backfill;
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Application.AiSuggestion.Specifications;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Dashboard.Navigation;
using TradyStrat.Application.Fx;
using TradyStrat.Application.Fx.Specifications;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Application.PriceFeed.Specifications;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.Trades.Specifications;

namespace TradyStrat.Application.Dashboard.UseCases;

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
    ListInstrumentsUseCase listInstruments,
    ISettingsReader settings,
    GetAllTodaysSuggestionsUseCase getAllTodaysSuggestions,
    ISuggestionBackfillCoordinator backfillCoord,
    IEntryNavigationService nav,
    ILogger<LoadDashboardUseCase> log)
    : UseCaseBase<LoadDashboardInput, DashboardViewModel>(log)
{
    private const int SparklineWindow = 30;

    protected override async Task<DashboardViewModel> ExecuteCore(LoadDashboardInput input, CancellationToken ct)
    {
        var target = input.TargetDate;
        var goal   = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(DateTime.UtcNow);

        var focusTicker = await settings.FocusTickerAsync(ct);

        // Catalog order: focus first, then Held alphabetically (excluding focus),
        // then Watchlist alphabetically. Stable ordering keeps zone-card layout
        // predictable as new instruments are added.
        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var ordered = instruments
            .OrderBy(i => i.Ticker == focusTicker ? 0 : i.Kind == InstrumentKind.Held ? 1 : 2)
            .ThenBy(i => i.Ticker, StringComparer.Ordinal)
            .ToList();

        // Today's-call batch — read-only in historical mode, no AI invocation.
        // Computed before the per-ticker loop so each TickerView can pull its
        // own call from the batch (priceMap is still built by the loop
        // downstream). Focus-instrument resolution is intentionally deferred
        // until after the per-ticker loop so an empty-Instruments DB hits the
        // indicator path first (which throws TradyStratException, surfaced as
        // the "Could not load dashboard" fallback) rather than escaping with
        // an uncaught InvalidOperationException.
        IReadOnlyList<Suggestion> allTodays;
        if (input.IsHistorical)
        {
            // Historical mode — read-only across all held instruments.
            var heldIds = ordered.Where(i => i.Kind == InstrumentKind.Held).Select(i => i.Id).ToList();
            var historicalRows = new List<Suggestion>();
            foreach (var id in heldIds)
            {
                var row = await suggestionRepo.FirstOrDefaultAsync(
                    new SuggestionForDateSpec(target, id), ct);
                if (row is not null) historicalRows.Add(row);
            }
            allTodays = historicalRows;
        }
        else
        {
            // Live mode — Saga aggregator fans out per held instrument.
            allTodays = await getAllTodaysSuggestions.ExecuteAsync(Unit.Value, ct);
        }

        // Iterate Held + Watchlist for zone analysis. Held instruments contribute
        // to the priceMap (used by PortfolioService.SnapshotAsync); Watchlist do not.
        var tickers = new List<TickerView>();
        var priceMap = new Dictionary<int, (decimal PriceEur, string Ticker, string Currency)>();

        foreach (var inst in ordered)
        {
            var reading = await indicators.ComputeFor(inst.Ticker, target, ct);
            decimal? eur = string.Equals(inst.Currency, "EUR", StringComparison.OrdinalIgnoreCase)
                ? reading.Price
                : await fx.ToEurAsync(reading.Price, inst.Currency, target, ct);

            var deltaPct = await ComputeDeltaPctAsync(inst.Ticker, target, ct);
            var spark    = await ComputeSparkAsync(inst.Ticker, target, ct);

            // Held tickers surface their own call; Watchlist stays null.
            var todaysCall = inst.Kind == InstrumentKind.Held
                ? allTodays.FirstOrDefault(s => s.InstrumentId == inst.Id)
                : null;

            tickers.Add(new TickerView(
                inst.Ticker, inst.Currency, reading.Price, eur, deltaPct, reading.Zone, spark, todaysCall));

            if (inst.Kind == InstrumentKind.Held && eur is { } e)
                priceMap[inst.Id] = (e, inst.Ticker, inst.Currency);
        }

        var snap = await portfolio.SnapshotAsync(target, priceMap, goal.TargetEur, ct);
        var growthSeries = await growth.BuildAsync(focusTicker, ct);

        // Pin trailing growth point to the EUR-valued snapshot so chart and hero agree.
        // (Historical mode: this still anchors at the latest stored bar; documented limitation.)
        if (growthSeries.Count > 0)
        {
            var pinned = growthSeries.ToList();
            pinned[^1] = new GrowthPoint(target, snap.CurrentValueEur);
            growthSeries = pinned;
        }

        // Resolve the focus instrument id once — needed for the focus's TodaysCall
        // pick from the batch and for the prior-suggestion lookup below.
        var focusInstrument = ordered.SingleOrDefault(i => i.Ticker == focusTicker)
            ?? throw new InvalidOperationException(
                $"Focus ticker '{focusTicker}' is not in the Instruments table.");

        var todays = allTodays.SingleOrDefault(s => s.InstrumentId == focusInstrument.Id);

        // Prediction-market snapshot — Empty by default; deserialize if column present.
        var marketSnap = MarketSnapshot.Empty;
        if (todays?.MarketSnapshotJson is { Length: > 0 } marketJson)
        {
            try
            {
                marketSnap = JsonSerializer.Deserialize<MarketSnapshot>(marketJson, JsonOpts.Strict)
                             ?? MarketSnapshot.Empty;
            }
            catch (JsonException ex)
            {
                LoadDashboardLog.MarketSnapshotMalformed(log, ex);
                // marketSnap stays Empty — rail will be absent for this entry.
            }
        }

        var entryNum  = await tradeRepo.CountAsync(new TradesAsOfSpec(target), ct);
        var latestBar = await priceRepo.FirstOrDefaultAsync(new LatestPriceBarSpec(focusTicker), ct);

        // Prior suggestion + call diff. Skip when today's call is null (historical-empty).
        Suggestion? prior = null;
        var callDiff = CallDiff.None;
        if (todays is not null)
        {
            prior = await suggestionRepo.FirstOrDefaultAsync(
                new PriorSuggestionSpec(target, focusInstrument.Id), ct);
            callDiff = new CallDiffBuilder()
                .WithToday(todays)
                .WithPrior(prior)
                .Build();
        }

        // Indicator histories per citation.
        var histories = new Dictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries>();
        if (todays is not null)
        {
            foreach (var c in todays.Citations)
            {
                var kind = IndicatorKindParser.From(c.Indicator);
                if (kind is null) continue;
                var key = (c.Ticker, kind.Value);
                if (histories.ContainsKey(key)) continue;
                histories[key] = await indicators.HistoryFor(c.Ticker, kind.Value, SparklineWindow, target, ct);
            }
        }

        // Goal pace.
        var firstTrade = await tradeRepo.FirstOrDefaultAsync(new EarliestTradeSpec(), ct);
        var goalPace = GoalPaceCalculator.Compute(
            currentValueEur: snap.CurrentValueEur,
            goal: goal,
            today: target,
            firstTradeDate: firstTrade?.ExecutedOn);

        // Freshness pills.
        var nowUtc = DateTime.UtcNow;
        var fxLatest = await fxRepo.FirstOrDefaultAsync(new LatestFxRateSpec("EUR", "USD", target), ct);
        var priceAsOf = latestBar is { } lb
            ? RelativeTimeFormatter.Format(lb.Date.ToDateTime(TimeOnly.MinValue), nowUtc)
            : "";
        var callAsOf = todays is null ? "" : RelativeTimeFormatter.Format(todays.CreatedAt, nowUtc);
        var fxAsOf   = fxLatest is { } fxr
            ? RelativeTimeFormatter.Format(fxr.FetchedAt, nowUtc)
            : "";

        // Backfill chain — live mode only.
        if (!input.IsHistorical && prior is { ForDate: var lastDate } && target.AddDays(-1) > lastDate)
        {
            _ = backfillCoord
                .EnsureBackfilledAsync(lastDate, target.AddDays(-1), CancellationToken.None)
                .ContinueWith(
                    t => LoadDashboardLog.BackfillCrashed(log, t.Exception),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }
        var backfillStatus = backfillCoord.Status;

        // Navigation fields.
        var earliest = await nav.EarliestAsync(ct);
        var latest   = await nav.LatestAsync(ct);
        var prev     = await nav.PreviousAsync(target, ct);
        var next     = target >= latest ? null : await nav.NextAsync(target, ct);

        return new DashboardViewModel(
            Today: target,
            EntryNumber: entryNum,
            Portfolio: snap,
            Goal: goal,
            TodaysCall: todays,
            Tickers: tickers,
            Positions: snap.Positions,
            FocusTicker: focusTicker,
            Growth: growthSeries,
            LatestPriceDate: latestBar?.Date,
            GoalPace: goalPace,
            CallDiff: callDiff,
            BackfillStatus: backfillStatus,
            PriceAsOfRelative: priceAsOf,
            CallAsOfRelative: callAsOf,
            FxAsOfRelative: fxAsOf,
            IndicatorHistories: histories,
            CapitalEvents: SeedCapitalEvents(),
            IsHistorical: input.IsHistorical,
            EarliestTradingDay: earliest,
            LatestTradingDay: latest,
            PrevTradingDay: prev,
            NextTradingDay: next)
        {
            MarketSnapshot = marketSnap,
        };
    }

    private async Task<decimal?> ComputeDeltaPctAsync(string ticker, DateOnly asOf, CancellationToken ct)
    {
        var bars = await priceRepo.ListAsync(new PriceBarsAsOfSpec(ticker, asOf), ct);
        if (bars.Count < 2) return null;
        var prev = bars[^2].Close;
        var curr = bars[^1].Close;
        if (prev == 0m) return null;
        return (curr - prev) / prev * 100m;
    }

    private async Task<IReadOnlyList<decimal>> ComputeSparkAsync(string ticker, DateOnly asOf, CancellationToken ct)
    {
        var bars = await priceRepo.ListAsync(new PriceBarsAsOfSpec(ticker, asOf), ct);
        if (bars.Count == 0) return [];
        var window = Math.Min(bars.Count, SparklineWindow);
        var slice = new decimal[window];
        for (var i = 0; i < window; i++)
            slice[i] = bars[bars.Count - window + i].Close;
        return slice;
    }

    /// <summary>
    /// Editorial event annotations plotted on the growth chart and elaborated
    /// in the footnote rail beneath it. Hardcoded placeholder list — to be
    /// replaced with a real persistence layer (or trade-derived inference)
    /// once the design is locked. Dates align loosely with the seeded trade
    /// history so the numerals visually anchor on the line during dev runs.
    /// </summary>
    private static IReadOnlyList<CapitalEvent> SeedCapitalEvents() =>
    [
        new(new DateOnly(2025, 12, 7), "i",
            "Initial position.",
            "CON3.L entry — UK construction sentiment turning, AI rationale cited macro-housing tailwind."),
        new(new DateOnly(2026, 1, 21), "ii",
            "Doubled the position.",
            "Post-budget signal lifted the focus ticker; conviction added on confirmation, not anticipation."),
        new(new DateOnly(2026, 3, 12), "iii",
            "Trimmed 30%.",
            "RSI 78, parabolic; reasoning explicitly flagged overbought and recommended partial exit."),
        new(new DateOnly(2026, 4, 24), "iv",
            "Re-entered after correction.",
            "Position rebuilt at lower cost basis; capital +18% YTD when written."),
    ];
}

internal static partial class LoadDashboardLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Backfill chain crashed unobserved")]
    public static partial void BackfillCrashed(ILogger logger, Exception? ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "MarketSnapshotJson malformed; rail will not render")]
    public static partial void MarketSnapshotMalformed(ILogger logger, Exception ex);
}
