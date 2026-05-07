using System.Text.Json;
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Time;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.AiSuggestion;        // JsonOpts
using TradyStrat.Features.AiSuggestion.Backfill;
using TradyStrat.Features.AiSuggestion.CallDiff;
using TradyStrat.Features.AiSuggestion.Specifications;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.Dashboard.Navigation;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Fx.Specifications;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Features.PredictionMarkets;
using TradyStrat.Features.PriceFeed.Specifications;
using TradyStrat.Features.Settings.UseCases;
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
    ListInstrumentsUseCase listInstruments,
    IConfiguration config,
    GetTodaysSuggestionUseCase todaysSuggestion,
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

        var focusTicker = config["Tickers:Focus"]
            ?? throw new InvalidOperationException("Tickers:Focus is not configured.");

        // Catalog order: focus first, then Held alphabetically (excluding focus),
        // then Watchlist alphabetically. Stable ordering keeps zone-card layout
        // predictable as new instruments are added.
        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var ordered = instruments
            .OrderBy(i => i.Ticker == focusTicker ? 0 : i.Kind == InstrumentKind.Held ? 1 : 2)
            .ThenBy(i => i.Ticker, StringComparer.Ordinal)
            .ToList();

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
            tickers.Add(new TickerView(
                inst.Ticker, inst.Currency, reading.Price, eur, deltaPct, reading.Zone, spark));

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

        // Today's-call branch — read-only in historical mode, no AI invocation.
        Suggestion? todays;
        if (input.IsHistorical)
        {
            todays = await suggestionRepo.FirstOrDefaultAsync(new SuggestionForDateSpec(target), ct);
        }
        else
        {
            todays = await todaysSuggestion.ExecuteAsync(Unit.Value, ct);
        }

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
            prior = await suggestionRepo.FirstOrDefaultAsync(new PriorSuggestionSpec(target), ct);
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
