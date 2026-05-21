using TradyStrat.Application.Time;
using Ardalis.Specification;
using TradyStrat.Domain;
using TradyStrat.Application.UseCases;
using TradyStrat.Application.AiSuggestion.Backfill;
using TradyStrat.Application.AiSuggestion.Specifications;
using TradyStrat.Application.Dashboard.Navigation;
using TradyStrat.Application.Fx;
using TradyStrat.Application.Fx.Specifications;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.PriceFeed.Specifications;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.Trades.Specifications;

namespace TradyStrat.Application.Dashboard.UseCases;

public sealed class LoadDashboardUseCase(
    IIndicatorEngine indicators,
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
    BuildFocusDerivedSliceUseCase buildFocusSlice,
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

        // Historical mode reads existing suggestion rows; live mode does NOT call
        // the AI from this use case (the page streams Pending → Ready per ticker).
        IReadOnlyList<Suggestion> historicalRows = Array.Empty<Suggestion>();
        Dictionary<int, SuggestionState?> historicalStates = new();
        if (input.IsHistorical)
        {
            var heldIds = ordered.Where(i => i.Kind == InstrumentKind.Held).Select(i => i.Id).ToList();
            var rows = new List<Suggestion>();
            foreach (var id in heldIds)
            {
                var row = await suggestionRepo.FirstOrDefaultAsync(
                    new SuggestionForDateSpec(target, id), ct);
                if (row is not null) rows.Add(row);
                historicalStates[id] = row is null ? null : new SuggestionState.Ready(row);
            }
            historicalRows = rows;
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

            SuggestionState? callState;
            if (inst.Kind != InstrumentKind.Held)
            {
                callState = null;                                            // watchlist
            }
            else if (input.IsHistorical)
            {
                callState = historicalStates[inst.Id];                       // Ready or null
            }
            else
            {
                callState = new SuggestionState.Pending();                   // live skeleton
            }

            tickers.Add(new TickerView(
                InstrumentId: inst.Id,
                Ticker:       inst.Ticker,
                Currency:     inst.Currency,
                Price:        reading.Price,
                PriceEur:     eur,
                DeltaPct:     deltaPct,
                Zone:         reading.Zone,
                Spark:        spark,
                CallState:    callState));

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

        // Resolve the focus instrument. Live mode initializes Pending; historical
        // mode looks up the row (if any) and computes the focus-derived slice.
        var focusInstrument = ordered.SingleOrDefault(i => i.Ticker == focusTicker)
            ?? throw new InvalidOperationException(
                $"Focus ticker '{focusTicker}' is not in the Instruments table.");

        SuggestionState? focusState;
        FocusDerivedSlice focusDerived;
        if (input.IsHistorical)
        {
            var focusRow = historicalRows.SingleOrDefault(s => s.InstrumentId == focusInstrument.Id);
            focusState = focusRow is null ? null : new SuggestionState.Ready(focusRow);
            focusDerived = focusRow is null
                ? FocusDerivedSlice.Empty
                : await buildFocusSlice.BuildAsync(focusRow, target, ct);
        }
        else
        {
            focusState = new SuggestionState.Pending();
            focusDerived = FocusDerivedSlice.Empty;
        }

        var entryNum  = await tradeRepo.CountAsync(new TradesAsOfSpec(target), ct);
        var latestBar = await priceRepo.FirstOrDefaultAsync(new LatestPriceBarSpec(focusTicker), ct);

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
        var fxAsOf   = fxLatest is { } fxr
            ? RelativeTimeFormatter.Format(fxr.FetchedAt, nowUtc)
            : "";

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
            FocusCallState: focusState,
            Tickers: tickers,
            Positions: snap.Positions,
            FocusTicker: focusTicker,
            Growth: growthSeries,
            LatestPriceDate: latestBar?.Date,
            GoalPace: goalPace,
            CallDiff: focusDerived.CallDiff,
            BackfillStatus: backfillCoord.Status,
            PriceAsOfRelative: priceAsOf,
            CallAsOfRelative: focusState is SuggestionState.Ready r
                ? RelativeTimeFormatter.Format(r.Suggestion.CreatedAt, nowUtc)
                : "",
            FxAsOfRelative: fxAsOf,
            IndicatorHistories: focusDerived.IndicatorHistories,
            CapitalEvents: SeedCapitalEvents(),
            IsHistorical: input.IsHistorical,
            EarliestTradingDay: earliest,
            LatestTradingDay: latest,
            PrevTradingDay: prev,
            NextTradingDay: next)
        {
            MarketSnapshot = focusDerived.MarketSnapshot,
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

