# TradyStrat — Dashboard depth pass

**Status:** Design approved · Ready for implementation plan
**Date:** 2026-05-07
**Author:** Philippe Matray (with Claude)
**Depends on:** [`2026-05-06-tradystrat-dashboard-design.md`](./2026-05-06-tradystrat-dashboard-design.md)

---

## 1. Purpose & goal

The dashboard is functional and visually distinctive but *static* in its surface — chart hover, freshness, day-over-day deltas, and indicator context exist as data but not as features. This spec adds five enhancements that turn existing data into *interaction depth* without expanding the surface area:

1. **Chart crosshair** — hairline scrub + rich tooltip on the growth chart.
2. **Goal-pace banner** — derived three-stat row under the hero.
3. **Today's Call freshness + diff vs. prior** — staleness pill, copy, change summary, row-level highlights, eager AI backfill of missing days.
4. **Last-updated pills (inline)** — preserve editorial restraint; surface AsOf timestamps in existing typography.
5. **Indicator sparklines** — always-on hairline sparklines, right-aligned column, threshold lines.

Out of scope (for separate spec): the Entries archive (turning `ENTRY NO. 0003` into a real route).

## 2. Decisions at a glance

| Concern | Decision |
|---|---|
| Schema changes | **None.** All historical data already persisted (`PriceBars`, `FxRates`, `Suggestions`); indicator history derived on-the-fly. |
| New exception types | **None.** All failure modes map to the existing `TradyStratException` hierarchy. |
| Indicator history strategy | Compute on demand from full price series via TaLib; expose what is already computed internally. |
| AI backfill of missing days | Eager, sequential, **chronological** (oldest first), persisted to `Suggestions` table. |
| AI context fidelity for backfilled days | Out of scope (later phase). `CreatedAt` honestly records when the call ran. |
| Diff source | `PriorSuggestionSpec(today)` — most-recent prior, sharpens to N-1 as backfill progresses. |
| Chart crosshair tooltip | Rich (date, capital, Δ-prior-day, position, focus-ticker close, vs.-plan). |
| Goal-pace banner | Three stats: vs. plan · monthly compound % needed · implied CAGR. |
| Freshness pill placement | Inline in existing typographic labels. No new visual elements. |
| Today's Call diff highlighting | Italic summary header bar + faint background tint on changed rows. |
| Sparkline reveal | Always-on, right-aligned column, vertically aligned across rows. |
| Backfill ordering | Chronological (ascending). Future AI prompts may depend on prior suggestions. |
| Backfill state | Singleton coordinator + Observer events. Reverse-chronological backfill rejected. |
| Test framework | xunit.v3 + Shouldly + EF Core InMemory + existing `FakeChatClient`. No bUnit. |
| Razor verification | Manual smoke via Chrome DevTools MCP at completion. |

## 3. Patterns inventory

| Component | GoF role | Notes |
|---|---|---|
| `SuggestionBackfillCoordinator` | **Singleton** + **Observer** | Single shared instance; pushes `StatusChanged` events to subscribers. |
| `BackfillStatus` discriminated record | **State** (record-shaped) | `Idle` / `Running` / `Failed` — exhaustive switch in the UI. |
| `IIndicatorHistoryProvider` per kind | **Strategy** | Parallels existing `IZoneRule` strategies. Resolved via `IIndicatorHistoryProviderFactory`. |
| `IIndicatorHistoryProviderFactory` | **Factory Method** | Maps `IndicatorKind` → strategy. |
| `ISnapshotFactory` | **Factory Method** | Replaces existing `SnapshotBuilder` constructor-style call site. Today's snapshot becomes `factory.CreateAsync(today)`. |
| `RetryingAiClient` *(optional)* | **Decorator** | Wraps `IAiClient` with retry + jittered backoff. Wired via a delegate factory in `AiSuggestionModule` (the project does not use Scrutor). |
| `CallDiffBuilder` | **Builder** | `.WithToday(s).WithPrior(s?).Build()` → `CallDiff`. Composable for tests. |
| `RelativeTimeFormatter` | **Strategy** internally | Bucket strategies (just-now / minutes / hours / days / absolute) selected by elapsed range. |
| Persistence access | **Specification** (Ardalis) | All new queries land in `Specifications/<Aggregate>/`. |

## 4. Project layout — additions

### 4.1 New / modified production files

| File | Layer | Purpose |
|---|---|---|
| `Features/Indicators/IndicatorEngine.cs` *(extend)* | Domain | Add `HistoryFor(ticker, kind, lastN)` returning `IndicatorSeries` (Values, ThresholdHi?, ThresholdLo?). |
| `Features/Indicators/HistoryProviders/{Rsi,Bollinger,Ichimoku,Sma200}HistoryProvider.cs` | Domain | One `IIndicatorHistoryProvider` per kind. |
| `Features/Indicators/IndicatorHistoryProviderFactory.cs` | Domain | Factory mapping `IndicatorKind` → strategy. |
| `Features/Dashboard/GoalPaceCalculator.cs` | Domain | Pure derivation → `GoalPaceVm`. |
| `Features/AiSuggestion/CallDiffBuilder.cs` + `CallDiff.cs` | Domain | Diff `(today, prior?)` → typed record. `CallDiff.None` sentinel for null prior. |
| `Features/AiSuggestion/SuggestionBackfillCoordinator.cs` (+ `BackfillStatus.cs`) | Service | Singleton + Observer. |
| `Features/AiSuggestion/ISnapshotFactory.cs` (+ `SnapshotFactory.cs` impl) | Domain | New interface and implementation. The implementation absorbs the body of the existing `SnapshotBuilder.BuildAsync()`, accepts an as-of `DateOnly`, and consumes the new as-of specs. The existing `SnapshotBuilder` class is **deleted** in favor of `SnapshotFactory` (call sites updated). |
| `Application/UseCases/AiSuggestion/BackfillSuggestionsUseCase.cs` | Use case | For one missing date: reconstruct snapshot → call AI → persist `Suggestion`. |
| `Shared/Time/RelativeTimeFormatter.cs` | Utility | "12 min ago" / "14h ago" / "yesterday" / absolute. |
| `wwwroot/js/growth-chart.js` | UI | Scoped ES module for crosshair tooltip via JSInterop. |

### 4.2 New Specifications (Ardalis)

```
Specifications/Suggestions/SuggestionsInRangeSpec.cs
Specifications/Suggestions/PriorSuggestionSpec.cs
Specifications/Trades/TradesAsOfSpec.cs
Specifications/PriceBars/PriceBarsAsOfSpec.cs
Specifications/FxRates/FxRateAsOfSpec.cs
```

Definitions:

```csharp
public sealed class SuggestionsInRangeSpec : Specification<Suggestion>
{
    public SuggestionsInRangeSpec(DateOnly fromInclusive, DateOnly toInclusive)
        => Query.Where(s => s.ForDate >= fromInclusive && s.ForDate <= toInclusive)
                .OrderBy(s => s.ForDate);
}

public sealed class PriorSuggestionSpec : Specification<Suggestion>
{
    public PriorSuggestionSpec(DateOnly beforeExclusive)
        => Query.Where(s => s.ForDate < beforeExclusive)
                .OrderByDescending(s => s.ForDate)
                .Take(1);
}

public sealed class TradesAsOfSpec : Specification<Trade>
{
    public TradesAsOfSpec(DateOnly asOfInclusive)
        => Query.Where(t => t.ExecutedOn <= asOfInclusive)
                .OrderBy(t => t.ExecutedOn);
}

public sealed class PriceBarsAsOfSpec : Specification<PriceBar>
{
    public PriceBarsAsOfSpec(string ticker, DateOnly asOfInclusive)
        => Query.Where(p => p.Ticker == ticker && p.Date <= asOfInclusive)
                .OrderBy(p => p.Date);
}

public sealed class FxRateAsOfSpec : Specification<FxRate>
{
    public FxRateAsOfSpec(string pair, DateOnly asOfInclusive)
        => Query.Where(f => f.Pair == pair && f.Date <= asOfInclusive)
                .OrderByDescending(f => f.Date)
                .Take(1);
}
```

### 4.3 Modified Razor (4)

- `Features/Dashboard/Components/GrowthChart.razor.cs` — register JS module via `OnAfterRenderAsync`; pass aligned-by-date arrays.
- `Features/Dashboard/Components/HeroCapital.razor` — add the three-stat goal-pace row below "BY ... DAYS LEFT".
- `Features/Dashboard/Components/TodaysCallCard.razor` — render diff summary header, sparklines column right-aligned, inline freshness, backfill progress.
- `Features/Dashboard/Components/VaultMasthead.razor` *(or wherever the date strip lives)* — extend "TODAY'S CALL · 7 MAI" with " · 14H AGO".

### 4.4 ViewModel deltas — `DashboardViewModel`

```csharp
public sealed record DashboardViewModel(
    // existing fields unchanged
    DateOnly Today,
    int EntryNumber,
    PortfolioSnapshot Portfolio,
    GoalConfig Goal,
    Suggestion TodaysCall,
    TickerView[] Tickers,
    GrowthPoint[] Growth,
    DateOnly? LatestPriceDate,
    // new fields
    GoalPaceVm GoalPace,
    CallDiff CallDiff,
    BackfillStatus BackfillStatus,
    string PriceAsOfRelative,
    string CallAsOfRelative,
    string FxAsOfRelative,
    IReadOnlyDictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries> IndicatorHistories);
```

## 5. Components

### 5.1 `SuggestionBackfillCoordinator` (Singleton + Observer)

Public surface:

```csharp
public interface ISuggestionBackfillCoordinator
{
    BackfillStatus Status { get; }
    event Action<BackfillStatus> StatusChanged;
    Task EnsureBackfilledAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct);
}

public abstract record BackfillStatus
{
    public sealed record Idle : BackfillStatus;
    public sealed record Running(int Remaining, int Total, DateOnly CurrentDate) : BackfillStatus;
    public sealed record Failed(DateOnly LastSuccessful, DateOnly FailedAt, string Reason) : BackfillStatus;
}
```

Behavior:

- Registered `AddSingleton<ISuggestionBackfillCoordinator, SuggestionBackfillCoordinator>()`.
- `EnsureBackfilledAsync` queries `SuggestionsInRangeSpec(from, to)`, computes the missing set.
- If empty → returns immediately with `Status = Idle`, no events.
- Otherwise → fires a single background `Task` processing dates **chronologically (ascending)**, raises `StatusChanged` per completion.
- **Reentrancy**: guarded by `lock`. Second call while running returns the cached in-flight `Task`.
- **Failure**: `TradyStratException` halts the chain at that date and emits `Failed`. Prior persisted dates remain.
- **Cancellation**: propagates `OperationCanceledException`; not treated as failure.

### 5.2 `SnapshotFactory` (Factory Method) + as-of extension

```csharp
public interface ISnapshotFactory
{
    Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct);
}
```

Replaces the existing `SnapshotBuilder` class (deleted; not wrapped). Today's snapshot becomes `factory.CreateAsync(today)`. Internally consumes the new as-of specs (`PriceBarsAsOfSpec`, `FxRateAsOfSpec`, `TradesAsOfSpec`) so portfolio state and indicator inputs reflect only data available on that date. Existing call sites in `GetTodaysSuggestionUseCase` and `ForceRefetchSuggestionUseCase` migrate to `ISnapshotFactory`.

### 5.3 `IIndicatorHistoryProvider` (Strategy) + Factory

```csharp
public interface IIndicatorHistoryProvider
{
    IndicatorKind Kind { get; }
    IndicatorSeries Compute(IReadOnlyList<PriceBar> bars, int lastN);
}

public sealed record IndicatorSeries(
    IReadOnlyList<decimal> Values,
    decimal? ThresholdHi,
    decimal? ThresholdLo);

public interface IIndicatorHistoryProviderFactory
{
    IIndicatorHistoryProvider For(IndicatorKind kind);
}
```

Concrete strategies (one each for RSI, Bollinger, Ichimoku, SMA-200) live in `Features/Indicators/HistoryProviders/`. Threshold semantics:

- **RSI**: `ThresholdHi = 70`, `ThresholdLo = 30`.
- **SMA-200**: `ThresholdHi = SMA value at lastN-1` (drawn as horizontal line).
- **Bollinger**: `ThresholdHi/Lo = upper/lower band at lastN-1`.
- **Ichimoku**: thresholds null; sparkline shows price line only.

`IndicatorEngine.HistoryFor(ticker, kind, lastN)` is a thin façade calling the factory.

### 5.4 `CallDiffBuilder` (Builder)

```csharp
public sealed class CallDiffBuilder
{
    public CallDiffBuilder WithToday(Suggestion today);
    public CallDiffBuilder WithPrior(Suggestion? prior);
    public CallDiff Build();   // returns CallDiff.None when prior is null
}

public sealed record CallDiff(
    bool ActionChanged,
    TradeAction PriorAction,
    int? ConvictionDelta,
    IReadOnlyList<string> AddedCitationKeys,    // "RSI(14):BTC-USD"
    IReadOnlyList<string> RemovedCitationKeys,
    IReadOnlyList<CitationChange> ChangedCitations,
    string SummaryParagraph)
{
    public static CallDiff None { get; } = new(
        ActionChanged: false,
        PriorAction: default,
        ConvictionDelta: null,
        AddedCitationKeys: [],
        RemovedCitationKeys: [],
        ChangedCitations: [],
        SummaryParagraph: "");
}

public sealed record CitationChange(string Key, string PriorValue, string NewValue);
```

Citation identity = `(IndicatorKind, Ticker)` tuple stable across days. `SummaryParagraph` is templated deterministically from typed deltas — no AI call. Example output: *"Action unchanged. Conviction 5 (+1). Ichimoku regained cloud · 200-SMA CON3.L drifted lower · BTC RSI(14) added to citations."*

### 5.5 `GoalPaceCalculator` (pure)

```csharp
public sealed record GoalPaceVm(
    decimal VsPlanEur,
    decimal MonthlyCompoundPct,
    decimal ImpliedCagrPct,
    GoalPaceMode Mode);

public enum GoalPaceMode { Active, NotStarted, GoalDatePassed, TargetReached }

public static class GoalPaceCalculator
{
    public static GoalPaceVm Compute(
        decimal todayCapitalEur,
        GoalConfig goal,
        DateOnly today,
        DateOnly? firstTradeDate);
}
```

Math:

- **Linear plan baseline** = `goal.TargetEur × (daysSinceFirstTrade / totalPlanDays)`.
- **VsPlanEur** = `todayCapitalEur − linearPlanBaseline`.
- **MonthlyCompoundPct** = `(target / today)^(1 / monthsLeft) − 1`.
- **ImpliedCagrPct** = `(target / today)^(1 / yearsLeft) − 1`.

Sentinel modes:

- `firstTradeDate is null` → `Mode = NotStarted`, all values zero.
- `today > goal.TargetDate` → `Mode = GoalDatePassed`.
- `todayCapitalEur >= goal.TargetEur` → `Mode = TargetReached`.
- otherwise → `Mode = Active`.

### 5.6 `RetryingAiClient` (Decorator, optional)

```csharp
public sealed class RetryingAiClient(IAiClient inner, RetryPolicy policy) : IAiClient
{
    public Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
        => /* exponential backoff over inner.AskAsync; max 3 attempts; only retry transient */;
}
```

Wired manually in `AiSuggestionModule` (the existing concrete `AnthropicAiClient` is registered first, then wrapped):

```csharp
builder.Services.AddScoped<AnthropicAiClient>();
builder.Services.AddScoped<IAiClient>(sp =>
    new RetryingAiClient(sp.GetRequiredService<AnthropicAiClient>(), RetryPolicy.Default));
```

Marked **optional** for the implementation plan — implement only if the no-retry baseline causes friction during backfill chains.

### 5.7 `RelativeTimeFormatter`

```csharp
public static class RelativeTimeFormatter
{
    public static string Format(DateTime asOfUtc, DateTime nowUtc);
}
```

Internal bucket strategies (`JustNow`, `Minutes`, `Hours`, `DaysAndYesterday`, `Absolute`) probed in order. Outputs lowercase non-localised English to match the existing label voice (e.g. `"12 min ago"`, `"14h ago"`, `"yesterday"`, `"06 may"`).

### 5.8 Crosshair JS module — `wwwroot/js/growth-chart.js`

ES module, scoped (no globals). Public surface:

```javascript
export function init(svgElement, data, locale)
export function dispose(svgElement)
```

`data` is a single object with parallel arrays:

```javascript
{
  dates: ["2026-04-02", ...],
  capital: [25400, ...],
  vsPlan: [-130, ...],
  position: [42100, ...],
  focusTickerEur: [0.518, ...],
  targetEur: 500000,
  targetDate: "2027-06-30"
}
```

Behavior:

- `pointermove` → binary search by x-coordinate ratio → render tooltip + vertical guide.
- `pointerleave` → hide.
- Touch: tap pins tooltip to the chart's top edge; subsequent tap moves it.
- All formatting (currency, percentages) reads `locale` to match `Newsreader` typography.

Server-side (`GrowthChart.razor.cs`):

```csharp
private IJSObjectReference? _module;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;
    try
    {
        _module = await JS.InvokeAsync<IJSObjectReference>(
            "import", "./js/growth-chart.js");
        await _module.InvokeVoidAsync("init", _svgRef, _data, "fr-FR");
    }
    catch (JSException) { /* graceful degradation: chart works without crosshair */ }
}

public async ValueTask DisposeAsync()
{
    if (_module is null) return;
    try { await _module.InvokeVoidAsync("dispose", _svgRef); await _module.DisposeAsync(); }
    catch (JSDisconnectedException) { /* circuit gone, nothing to clean */ }
}
```

## 6. Data flow

### 6.1 Dashboard load — synchronous critical path

```
1.  GetTodaysSuggestionUseCase            → ensures today's Suggestion exists (existing flow)
2.  PortfolioService.SnapshotAsync()      → existing
3.  GrowthSeriesBuilder.BuildAsync()      → existing
4.  IndicatorEngine.ComputeFor(focus)     → existing live-zone classification
5.  PriorSuggestionSpec(today)            → most recent prior, may be N-1 or N-K
6.  CallDiffBuilder
        .WithToday(today)
        .WithPrior(prior)
        .Build()                          → CallDiff
7.  For each citation kind in today:
       IndicatorEngine.HistoryFor(focus, kind, 30)   → IndicatorSeries
8.  GoalPaceCalculator.Compute(...)       → GoalPaceVm
9.  RelativeTimeFormatter.Format(...)     → preformatted freshness strings
10. missingRange = (lastEntryBefore today, today - 1)
    if non-empty → coordinator.EnsureBackfilledAsync(missingRange)   // fire-and-forget
    backfillStatus = coordinator.Status                              // snapshot now
11. return DashboardViewModel
```

Steps 5–10 are added; 1–4 are unchanged. Step 10 enqueues the backfill but does not await.

### 6.2 Dashboard load — async / observer phase

`TodaysCallCard.razor` subscribes to the coordinator:

```
OnInitialized:
  coordinator.StatusChanged += OnBackfillProgress

OnBackfillProgress(status):
  await InvokeAsync(async () => {
    BackfillStatusLabel = status switch {
        Running r => $"Backfilling {r.Total - r.Remaining} of {r.Total} — {r.CurrentDate}",
        Failed f  => $"Stopped at {f.FailedAt} — {f.Reason}",
        _         => null
    };
    // SuggestionRepo is IRepositoryBase<Suggestion> (Ardalis); inject via [Inject].
    var prior = await SuggestionRepo.FirstOrDefaultAsync(new PriorSuggestionSpec(_today));
    CallDiff = new CallDiffBuilder().WithToday(_today).WithPrior(prior).Build();
    StateHasChanged();
  });

Dispose:
  coordinator.StatusChanged -= OnBackfillProgress
```

Each event re-runs the prior-suggestion spec and rebuilds the diff so it sharpens forward as the chronological chain progresses.

### 6.3 Backfill chain — sequential, chronological

```
sort missingDates ASCENDING
foreach date in missingDates:
    Status = Running(remaining, total, date); fire StatusChanged
    snapshot = await snapshotFactory.CreateAsync(date, ct)
    suggestion = await aiClient.AskAsync(snapshot, ct) with { ForDate = date }
    await repo.AddAsync(suggestion, ct)
Status = Idle; fire StatusChanged
```

### 6.4 Crosshair — JS-side data flow

`GrowthChart.razor.cs` calls `growthChart.init(svgRef, data, locale)` once on first render. Hover/touch handling stays client-side; no SignalR round-trip per pixel.

### 6.5 What does *not* update during the session

Freshness pills are formatted once at load. They go stale during a long-open session. Acceptable for spec 1 — manual refresh re-derives them.

### 6.6 Known constraint — today vs. backfilled history

`GetTodaysSuggestionUseCase` runs *before* the backfill chain and uses whatever prior data exists at that moment. Today's call is therefore generated with a potentially incomplete history. This is consistent with current behavior because today's snapshot prompt does not currently depend on prior suggestions. **Future** AI prompt designs that incorporate prior suggestions must either gate today's generation on chain completion or regenerate today after the chain emits `Idle`. Out of scope for this spec.

## 7. Error handling

### 7.1 Failure-mode matrix

All failures map to existing exceptions in `Shared/Exceptions/`. **Zero new exception classes.**

| Failure point | Existing exception | Recovery | UI |
|---|---|---|---|
| Anthropic transient (5xx, timeout, rate-limit) | `AnthropicCallFailedException` | `RetryingAiClient` decorator (optional): exp. backoff, max 3 | Invisible if recovered. |
| Anthropic permanent (auth, malformed JSON, content policy) | `AnthropicCallFailedException` | Coordinator halts → `Failed(lastOk, failedAt, ex.Message)` | Inline pill on call card with retry affordance. |
| Anthropic config bad | `AnthropicConfigurationException` | Halts immediately. Existing typed-error surface used for today; backfill pill mirrors it. | Existing display + backfill pill. |
| Snapshot — missing price bars for past date | `PriceFeedUnavailableException` | Halts chain; UI offers retry after price backfill | Failed pill with reason. |
| Snapshot — missing FX rate for past date | `FxRateUnavailableException` | Same | Same. |
| Indicator history insufficient bars | `IndicatorComputationException` (only if strategy genuinely cannot produce a value; else returns truncated `IndicatorSeries`) | Sparkline path: empty cell. Diff path: drop sparkline column for that row. | Cell blank; rest of row intact. |
| DB write failure during backfill persist | `DbUpdateException` (EF) | Halts chain → `Failed`; per-row atomicity preserves earlier dates. | Same Failed pill. |
| Today's AI call fails | `AnthropicCallFailedException` | Existing typed-error surface unchanged | Existing display. Diff/sparklines render against `prior` if available. |
| `PriorSuggestionSpec` returns null (fresh install) | *No exception* — `CallDiff.None` sentinel | — | Diff bar + "vs. yesterday" hidden. |
| GoalPace edge cases | *No exception* — `Mode = NotStarted/GoalDatePassed/TargetReached` | — | Stats row replaced or hidden per mode. |
| JSInterop crosshair init failure | *No exception* — `try/catch` in `OnAfterRenderAsync` | Chart renders without crosshair | Graceful degradation, silent. |
| Multiple tabs race on `EnsureBackfilledAsync` | *No exception* — coordinator `lock` + cached in-flight `Task` | All tabs receive same events. | — |
| Cancellation (app shutdown mid-chain) | `OperationCanceledException` | Persisted dates stay; remaining picked up on next load. | Resume on restart. |

### 7.2 Coordinator catch granularity

```csharp
foreach (var date in missingDates)
{
    try
    {
        var snapshot = await snapshotFactory.CreateAsync(date, ct);
        var suggestion = (await aiClient.AskAsync(snapshot, ct)) with { ForDate = date };
        await repo.AddAsync(suggestion, ct);
    }
    catch (TradyStratException ex)
    {
        _status = new Failed(lastOk, date, ex.Message);
        StatusChanged?.Invoke(_status);
        return;
    }
    catch (OperationCanceledException) { throw; }
    // anything else: programmer error, propagates to host logger
}
```

Single base-type catch covers Anthropic / PriceFeed / FxRate / IndicatorComputation in one branch.

### 7.3 Operator diagnostics

Every chain-halting catch logs at `Error` via Serilog with structured properties: `BackfillDate`, `LastSuccessfulDate`, `Reason`, `InnerExceptionType`. Daily-rolling log file (`~/Library/Application Support/TradyStrat/logs/`) captures the trail.

### 7.4 Explicitly not handled

- Anthropic returning structurally-different JSON for past prompts beyond schema validation.
- Clock skew / timezone drift on `ForDate`.
- Concurrent `Suggestion` row inserts (unique constraint surfaces as `DbUpdateException` → `Failed`).

## 8. Testing

Stack: `xunit.v3` + `Shouldly` + EF Core InMemory + existing `FakeChatClient`. No bUnit.

### 8.1 Pure unit tests

| Component | Test file | Coverage |
|---|---|---|
| `GoalPaceCalculator.Compute` | `Dashboard/GoalPaceCalculatorTests.cs` | Active math, all sentinel modes, `firstTradeDate > today`. |
| `CallDiffBuilder.Build` | `AiSuggestion/CallDiffBuilderTests.cs` | Action change, conviction Δ, citation added/removed/zone-changed, no-prior → `CallDiff.None`, identical prior → empty deltas, summary paragraph. |
| `RelativeTimeFormatter.Format` | `Time/RelativeTimeFormatterTests.cs` | Each bucket boundary; `now` injected. |
| Each `IIndicatorHistoryProvider` strategy | `Indicators/HistoryProviders/{Kind}HistoryProviderTests.cs` | Series correctness vs. fixtures; threshold-line correctness; insufficient-bars graceful return. |
| `IIndicatorHistoryProviderFactory` | `Indicators/IndicatorHistoryProviderFactoryTests.cs` | Each kind resolves; unknown kind throws `IndicatorComputationException`. |

### 8.2 Specifications — round-trip in InMemoryDb

Extend `Specifications/SpecsRoundtripTests.cs` (one [Fact] per spec):

- `SuggestionsInRangeSpec`, `PriorSuggestionSpec`, `TradesAsOfSpec`, `PriceBarsAsOfSpec`, `FxRateAsOfSpec`.

### 8.3 SnapshotFactory as-of test

`AiSuggestion/SnapshotFactoryTests.cs`:

- Seed multi-date data → `CreateAsync(asOf)` reflects only `<= asOf`.
- Missing price bars for `asOf` → `PriceFeedUnavailableException`.
- Missing FX rate for `asOf` → `FxRateUnavailableException`.
- Existing `SnapshotBuilderTests.cs` migrates / extends.

### 8.4 BackfillSuggestionsUseCase test

`Application/UseCases/AiSuggestion/BackfillSuggestionsUseCaseTests.cs`:

- `FakeChatClient` + stub snapshot factory.
- Verify: snapshot built with right `asOf`, AI client called, persisted row has correct `ForDate` / `CreatedAt`.
- Verify: domain-typed exceptions propagate unchanged.

### 8.5 SuggestionBackfillCoordinator tests

`AiSuggestion/SuggestionBackfillCoordinatorTests.cs`:

- Empty missing range → stays `Idle`, no events.
- Single missing date → `Running(0,1,date)` then `Idle`; one row persisted.
- Multi-day chronological order → events fire ascending.
- Reentrancy: second call returns same in-flight `Task`.
- Multi-subscriber fan-out.
- Mid-chain failure → `Failed(date-2, date-3, msg)`; partial persistence preserved.
- Cancellation throws `OperationCanceledException`; no `Failed` event.
- `PriceFeedUnavailableException` from snapshot factory halts chain.

### 8.6 LoadDashboardUseCase integration smoke

`Application/UseCases/Dashboard/LoadDashboardUseCaseBackfillTests.cs`:

- Seed: today exists, prior is N-3 (gap of 2 days).
- Run → ViewModel has `BackfillStatus = Running(...)`, `CallDiff` against N-3 prior, `GoalPace` populated, `IndicatorHistories` populated, freshness strings non-null, coordinator invoked with `(N-3 exclusive, today-1 inclusive)`.

### 8.7 Razor verification — manual via Chrome DevTools MCP

- Crosshair tooltip: hover at three positions; verify all six fields render. Mobile viewport: tooltip pins to top.
- Goal-pace stats row: three stats present; sentinel cases verified by toggling Settings.
- Today's Call diff: seed known prior, refresh, verify summary bar + row tints.
- Freshness pills: inline text in labels; no wrapping breakage on narrow viewports.
- Sparklines: right-aligned column, threshold lines correct.
- Backfill pill: simulate gap, reload, verify progress + final disappearance.

### 8.8 Explicitly not tested

- Live Anthropic / Yahoo / FX endpoints (existing project policy).
- JS module as JS unit tests (no JS test infra introduced).
- Browser interop in CI.

## 9. Out of scope / future phases

- **Entries archive** — turning `ENTRY NO. 0003` into a real route. Separate spec.
- **AI context fidelity for backfilled days** — prompt versioning, model lock, `BackfilledAt` flag, `RequiresCompleteHistory` gate. Will become load-bearing if a future prompt depends on prior suggestions.
- **Live ticking freshness pills** — currently formatted once at load. Add a 60s `Timer` only if friction emerges.
- **Today's call regeneration after backfill** — gated on the future "AI context fidelity" work.
- **JS unit-test infrastructure** — manual MCP smoke is the bar.

## 10. Visual reference

Visual decisions made during brainstorming, archived in `.superpowers/brainstorm/91990-1778140618/content/`:

- `intro.html` — overview of the six features in scope across both specs.
- `crosshair-content.html` — chart crosshair tooltip (Variant B selected).
- `goal-pace.html` — three-stat row layout (Variant B selected; middle stat amended to compound monthly %).
- `freshness.html` — inline-in-typography placement (Variant C selected).
- `call-diff.html` — summary header + row tints (Variant B selected).
- `sparklines.html` — always-on hairline (Variant A selected; refined to right-aligned column for vertical alignment across rows).
