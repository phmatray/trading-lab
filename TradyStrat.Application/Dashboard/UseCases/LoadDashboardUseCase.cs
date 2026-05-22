using Ardalis.Specification;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Backfill;
using TradyStrat.Application.Dashboard.Navigation;
using TradyStrat.Application.Fx;
using TradyStrat.Application.Fx.Specifications;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.PriceFeed.Specifications;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.Time;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.Dashboard.UseCases;

public sealed class LoadDashboardUseCase(
    IIndicatorEngine indicators,
    IPortfolioRepository portfolios,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<PriceBar> priceRepo,
    ISuggestionRepository suggestionRepo,
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

        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var ordered = instruments
            .OrderBy(i => i.Ticker == focusTicker ? 0 : i.Kind == InstrumentKind.Held ? 1 : 2)
            .ThenBy(i => i.Ticker, StringComparer.Ordinal)
            .ToList();
        var instrumentById = ordered.ToDictionary(i => i.Id, i => i);

        IReadOnlyList<Suggestion> historicalRows = Array.Empty<Suggestion>();
        Dictionary<InstrumentId, SuggestionState?> historicalStates = new();
        if (input.IsHistorical)
        {
            var heldIds = ordered.Where(i => i.Kind == InstrumentKind.Held).Select(i => i.Id).ToList();
            var rows = new List<Suggestion>();
            foreach (var id in heldIds)
            {
                var row = await suggestionRepo.GetForAsync(id, target, ct);
                if (row is not null) rows.Add(row);
                historicalStates[id] = row is null ? null : new SuggestionState.Ready(row);
            }
            historicalRows = rows;
        }

        var tickers = new List<TickerView>();
        var priceMap = new Dictionary<InstrumentId, Price>();

        foreach (var inst in ordered)
        {
            var reading = await indicators.ComputeFor(inst.Ticker, target, ct);
            decimal? eur = inst.Currency == Currency.Eur
                ? reading.Price
                : await fx.ToEurAsync(reading.Price, inst.Currency.Code, target, ct);

            var deltaPct = await ComputeDeltaPctAsync(inst.Ticker, target, ct);
            var spark    = await ComputeSparkAsync(inst.Ticker, target, ct);

            SuggestionState? callState;
            if (inst.Kind != InstrumentKind.Held)
            {
                callState = null;
            }
            else if (input.IsHistorical)
            {
                callState = historicalStates[inst.Id];
            }
            else
            {
                callState = new SuggestionState.Pending();
            }

            tickers.Add(new TickerView(
                InstrumentId: inst.Id.Value,
                Ticker:       inst.Ticker,
                Currency:     inst.Currency.Code,
                Price:        reading.Price,
                PriceEur:     eur,
                DeltaPct:     deltaPct,
                Zone:         reading.Zone,
                Spark:        spark,
                CallState:    callState));

            if (inst.Kind == InstrumentKind.Held && eur is { } e)
                priceMap[inst.Id] = Price.Of(Money.Of(e, Currency.Eur));
        }

        var portfolio = await portfolios.GetAsync(ct);
        var snap = portfolio.SnapshotAsOf(
            target, instrumentById, priceMap,
            Money.Of(goal.TargetEur, Currency.Eur));

        // GrowthSeries: build per-instrument bar dictionary for the focus instrument.
        var focusBars = await priceRepo.ListAsync(new PriceBarsAsOfSpec(focusTicker, target), ct);
        var focusId   = ordered.SingleOrDefault(i => i.Ticker == focusTicker);
        var barsByInstrument = focusId is null
            ? new Dictionary<InstrumentId, IReadOnlyList<PriceBar>>()
            : new Dictionary<InstrumentId, IReadOnlyList<PriceBar>> {
                [focusId.Id] = focusBars,
            };
        var growthSeries = portfolio.GrowthSeries(barsByInstrument);

        if (growthSeries.Count > 0)
        {
            var pinned = growthSeries.ToList();
            pinned[^1] = new GrowthPoint(target, snap.CurrentValueEur.Amount);
            growthSeries = pinned;
        }

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

        // Trade-history reads now go through the Portfolio AR.
        var asOfTrades = portfolio.Positions
            .SelectMany(p => p.Trades)
            .Where(t => t.ExecutedOn <= target)
            .ToList();
        var entryNum  = asOfTrades.Count;
        var firstTrade = asOfTrades.OrderBy(t => t.ExecutedOn).FirstOrDefault();

        var latestBar = await priceRepo.FirstOrDefaultAsync(new LatestPriceBarSpec(focusTicker), ct);

        var goalPace = GoalPaceCalculator.Compute(
            currentValueEur: snap.CurrentValueEur.Amount,
            goal: goal,
            today: target,
            firstTradeDate: firstTrade?.ExecutedOn);

        var nowUtc = DateTime.UtcNow;
        var fxLatest = await fxRepo.FirstOrDefaultAsync(new LatestFxRateSpec("EUR", "USD", target), ct);
        var priceAsOf = latestBar is { } lb
            ? RelativeTimeFormatter.Format(lb.Date.ToDateTime(TimeOnly.MinValue), nowUtc)
            : "";
        var fxAsOf   = fxLatest is { } fxr
            ? RelativeTimeFormatter.Format(fxr.FetchedAt, nowUtc)
            : "";

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

    private static IReadOnlyList<CapitalEvent> SeedCapitalEvents() =>
    [
        new(new DateOnly(2025, 12, 7), RomanNumeralId.Of("i"),
            "Initial position.",
            "CON3.L entry — UK construction sentiment turning, AI rationale cited macro-housing tailwind."),
        new(new DateOnly(2026, 1, 21), RomanNumeralId.Of("ii"),
            "Doubled the position.",
            "Post-budget signal lifted the focus ticker; conviction added on confirmation, not anticipation."),
        new(new DateOnly(2026, 3, 12), RomanNumeralId.Of("iii"),
            "Trimmed 30%.",
            "RSI 78, parabolic; reasoning explicitly flagged overbought and recommended partial exit."),
        new(new DateOnly(2026, 4, 24), RomanNumeralId.Of("iv"),
            "Re-entered after correction.",
            "Position rebuilt at lower cost basis; capital +18% YTD when written."),
    ];
}
