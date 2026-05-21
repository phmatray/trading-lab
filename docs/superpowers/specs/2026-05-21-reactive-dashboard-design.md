# Reactive Dashboard — Design (Phase 4-A)

**Status:** Draft — awaiting user review
**Author:** brainstorm session 2026-05-21
**Predecessor:** [`2026-05-13-ai-suggestion-improvements-design.md`](2026-05-13-ai-suggestion-improvements-design.md) (shipped via PRs #3, #4)

## 1. Problem

After AI Suggestion Improvements (Phase 3) shipped, two user-facing pains remain:

- **Slow first paint.** `LoadDashboardUseCase` calls `GetAllTodaysSuggestionsUseCase` inside its synchronous orchestration (live mode, lines 82–86 of the current file). With N held instruments, the user stares at a loading state until every Anthropic call completes. Prompt caching from Phase 3 helped per-call latency but did not unblock the sequential dependency on the render path.
- **Fake parallelism risk.** Even a naive `Task.WhenAll` would not actually parallelize today: `SuggestionGate.Instance` is a single process-wide `SemaphoreSlim(1,1)`, so concurrent calls for *different* instruments serialize through the same lock.

This spec addresses *first paint speed only*. The complementary "Trust surface" spec (ThinkingText UI, yesterday-outcome strip, confidence band) is tracked separately and ships after this one.

## 2. Goals

- Dashboard renders positions, indicators, FX, sparklines, growth, and capital events **before** any Anthropic call runs.
- Each held instrument's suggestion streams into place as soon as its call returns:
  - The focus ticker's arrival fills `TodaysCallCard`, `CallDiff`, `IndicatorHistories` (citations), and `MarketSnapshot`.
  - Non-focus held tickers' arrivals fill their respective row callouts in the per-ticker views.
- Concurrent Anthropic calls run truly in parallel up to a configured cap.
- One ticker's AI failure leaves other tickers unaffected; the failing card shows a retry affordance.
- Cancellation propagates from page navigation through to in-flight `IChatClient` calls.

## 3. Non-goals

- ThinkingText UI surface (separate spec).
- Yesterday's-call outcome strip (separate spec).
- Confidence/uncertainty score on suggestions (separate spec).
- Streaming the *contents* of a single Anthropic response (tokens or thinking) — only per-ticker streaming.
- Changes to the replay CLI path.
- Changes to `SuggestionBackfillCoordinator`'s use of `GetAllTodaysSuggestionsUseCase`.
- Historical mode (`input.IsHistorical == true`): unchanged. Historical loads remain read-only against existing rows and continue to render in one shot (no Anthropic calls happen).

## 4. Architecture overview

```
DashboardPage (Razor, Blazor Server, live mode)
  │
  ├─ OnParametersSetAsync ──────────► LoadDashboardUseCase  (no AI calls)
  │                                    returns DashboardViewModel with:
  │                                      - FocusCallState   = Pending
  │                                      - Tickers[i].CallState = Pending (held) / null (watchlist)
  │                                      - CallDiff         = None
  │                                      - IndicatorHistories = empty
  │                                      - MarketSnapshot   = Empty
  │
  ├─ first render: dashboard skeleton + Pending cards
  │
  └─ OnAfterRenderAsync(firstRender=true)
        │
        └─ ConsumeStreamAsync(_cts.Token)  ← fire-and-forget on UI thread
              │
              └─ await foreach ev in StreamTodaysSuggestionsUseCase.StreamAsync(...)
                    │       (Application)
                    │
                    │   internal: Channel<SuggestionStreamEvent> +
                    │             SemaphoreSlim(maxParallel) +
                    │             one worker Task per held instrument
                    │             each calls GetTodaysSuggestionUseCase
                    │
                    ├─ if ev.InstrumentId is non-focus:
                    │     update Tickers[i].CallState (via _tickerStates dict)
                    │     await InvokeAsync(StateHasChanged)
                    │
                    └─ if ev.InstrumentId is focus:
                          update FocusCallState
                          ev.ToReadyOrNull() →
                            await BuildFocusDerivedSliceUseCase.BuildAsync(suggestion, ct)
                              → returns (CallDiff, IndicatorHistories, MarketSnapshot)
                            update those VM fields (via _focusDerived)
                          (focus failures: derived slice stays at empty defaults)
                          await InvokeAsync(StateHasChanged)
```

## 5. Components

### 5.1 New types

#### `Application/AiSuggestion/UseCases/StreamTodaysSuggestionsUseCase.cs`

Orchestrator. Exposes:

```csharp
public sealed class StreamTodaysSuggestionsUseCase(
    GetTodaysSuggestionUseCase getOne,
    ISettingsReader settings,
    ILogger<StreamTodaysSuggestionsUseCase> log)
{
    public async IAsyncEnumerable<SuggestionStreamEvent> StreamAsync(
        IReadOnlyCollection<int> heldInstrumentIds,
        [EnumeratorCancellation] CancellationToken ct);
}
```

Internally:

1. Open an unbounded `Channel<SuggestionStreamEvent>`.
2. Resolve `maxParallel` from `anthropic.maxParallelSuggestions` (default 3).
3. For each `heldInstrumentId`, spawn `Task.Run(async () => { ... }, ct)`:
   - `await semaphore.WaitAsync(ct)`
   - `try { var s = await getOne.ExecuteAsync(new(id), ct); chan.Writer.TryWrite(new Ready(id, s)); }`
   - `catch (OperationCanceledException) { /* swallow */ }`
   - `catch (Exception ex) { chan.Writer.TryWrite(new Failed(id, ex.Message)); log.LogWarning(ex, "..."); }`
   - `finally { semaphore.Release(); }`
4. Use a `CountdownEvent`-style counter or `Task.WhenAll` of the worker tasks to drive `chan.Writer.Complete()` exactly once after every worker reaches its `finally`.
5. `await foreach (var ev in chan.Reader.ReadAllAsync(ct)) yield return ev;`

Cancellation cascades from the consumer to all worker tasks.

#### `Application/AiSuggestion/SuggestionStreamEvent.cs`

```csharp
public abstract record SuggestionStreamEvent(int InstrumentId)
{
    public sealed record Ready(int InstrumentId, Suggestion Suggestion)
        : SuggestionStreamEvent(InstrumentId);
    public sealed record Failed(int InstrumentId, string Reason)
        : SuggestionStreamEvent(InstrumentId);
}
```

#### `Application/Dashboard/SuggestionState.cs`

```csharp
public abstract record SuggestionState
{
    public sealed record Pending : SuggestionState;
    public sealed record Ready(Suggestion Suggestion) : SuggestionState;
    public sealed record Failed(string Reason) : SuggestionState;
}
```

#### `Application/Dashboard/UseCases/BuildFocusDerivedSliceUseCase.cs`

Computes the focus-specific derived slice from a Suggestion. Lifts the existing logic in `LoadDashboardUseCase` (lines 135–178 of the current file) out of the orchestration so it can be called late.

```csharp
public sealed class BuildFocusDerivedSliceUseCase(
    IReadRepositoryBase<Suggestion> suggestionRepo,
    IIndicatorEngine indicators,
    ILogger<BuildFocusDerivedSliceUseCase> log)
{
    public async Task<FocusDerivedSlice> BuildAsync(
        Suggestion focus,
        DateOnly targetDate,
        CancellationToken ct);
}

public sealed record FocusDerivedSlice(
    CallDiff CallDiff,
    IReadOnlyDictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries> IndicatorHistories,
    MarketSnapshot MarketSnapshot);
```

The use case:
- Reads the prior suggestion (`PriorSuggestionSpec`), builds `CallDiff`.
- Reads `IndicatorHistories` for each unique citation indicator.
- Deserializes `MarketSnapshot` from `focus.MarketSnapshotJson`.

### 5.2 Replace `SuggestionGate` with per-instrument partitioning

`Application/AiSuggestion/UseCases/SuggestionGate.cs` becomes:

```csharp
internal static class SuggestionGate
{
    private static readonly ConcurrentDictionary<(DateOnly Date, int InstrumentId), SemaphoreSlim> Gates = new();

    public static SemaphoreSlim For(DateOnly date, int instrumentId)
        => Gates.GetOrAdd((date, instrumentId), _ => new SemaphoreSlim(1, 1));
}
```

Memory note: each `(date, instrumentId)` allocates one `SemaphoreSlim`. With ~10 held instruments and daily granularity, growth is ~3650 entries per year — negligible for a single-user app. No reclamation in this spec.

The single call site in `GetTodaysSuggestionUseCase` (line 38 of current file) changes from `SuggestionGate.Instance.WaitAsync(ct)` to `SuggestionGate.For(today, instrument.Id).WaitAsync(ct)`. The dup-insert guarantee is preserved per-key.

### 5.3 Modified

#### `Application/Dashboard/DashboardViewModel.cs`

Three field changes; everything else stays:

| Before | After |
|---|---|
| `Suggestion? TodaysCall` | `SuggestionState? FocusCallState` |
| `IReadOnlyList<TickerView> Tickers` (with `Suggestion? TodaysCall` inside) | `IReadOnlyList<TickerView> Tickers` (with `SuggestionState? CallState` inside) |

State contract:

- `null` means "no row expected" — used for watchlist tickers (which never get AI calls) and for historical-mode loads with a missing row.
- `Pending` means "call in flight" — only set in live mode after the skeleton load.
- `Ready(Suggestion)` means a row exists (live or historical).
- `Failed(reason)` means the live-mode call threw.

The other fields (`CallDiff`, `IndicatorHistories`, `MarketSnapshot`, `BackfillStatus`, `CallAsOfRelative`) remain on the record. The skeleton load initializes them at empty defaults; the page mutates a parallel `FocusDerivedSlice` holder and routes UI reads through it after the focus's suggestion arrives.

Implementation choice for late-mutating VM fields: rather than expanding the immutable record into a builder, the page maintains the live VM as the skeleton return plus three private mutable fields (`_focusState`, `_tickerStates`, `_focusDerived`). The Razor markup consults the mutable fields first, falling back to the VM's defaults. This keeps the record record-like and avoids dictionary-copy thrash on each event.

#### `Application/Dashboard/TickerView.cs`

```csharp
public sealed record TickerView(
    string Ticker,
    string Currency,
    decimal Price,
    decimal? PriceEur,
    decimal? DeltaPct,
    Zone Zone,
    IReadOnlyList<decimal> Spark,
    SuggestionState? CallState);   // was: Suggestion? TodaysCall
```

`CallState` is `null` for watchlist instruments (no AI call expected); `Pending`/`Ready`/`Failed` for held.

#### `Application/Dashboard/UseCases/LoadDashboardUseCase.cs`

Skeleton-only refactor:

- Drop `GetAllTodaysSuggestionsUseCase getAllTodaysSuggestions` from the constructor.
- Live mode (lines 82–86): no longer fetches suggestions. Builds an empty `allTodays`.
- Per-ticker loop (lines 93–113): set `CallState = inst.Kind == InstrumentKind.Held ? new SuggestionState.Pending() : null`.
- Historical mode (lines 69–81): unchanged in shape, but build `CallState = row is null ? null : new Ready(row)` so the new state contract is honored on the historical path (a missing historical row is a permanent absence, not a pending load).
- Focus-derived computation (lines 127–179 of current file): **move to `BuildFocusDerivedSliceUseCase`**. In live mode the skeleton returns `CallDiff = CallDiff.None`, empty histories, `MarketSnapshot = Empty`. In historical mode the use case is called inline if the focus row exists, so historical render remains one-shot.
- Backfill chain (lines 200–210): moved to fire after the focus state arrives Ready in live mode. Page-level concern; the page invokes `_backfillCoord.EnsureBackfilledAsync(...)` with the same fire-and-forget pattern when the focus arrives. (Historical mode does not trigger backfill — unchanged.)

#### `TradyStrat/Features/Dashboard/DashboardPage.razor.cs`

Inject `StreamTodaysSuggestionsUseCase _stream` and `BuildFocusDerivedSliceUseCase _buildSlice` and `ISuggestionBackfillCoordinator _backfillCoord`.

Add three private mutable fields:
- `SuggestionState? _focusState`  (initialized from skeleton: `Pending` in live mode, `Ready`/`null` in historical mode)
- `Dictionary<int, SuggestionState?> _tickerStates = new()` keyed by `Instrument.Id`
- `FocusDerivedSlice _focusDerived = FocusDerivedSlice.Empty`

Add `CancellationTokenSource _cts = new()`.

Flow:
1. `OnParametersSetAsync` — call `LoadDashboardUseCase` as today; initialize `_focusState` and `_tickerStates` from the skeleton (`Pending` for each held instrument).
2. `OnAfterRenderAsync(bool firstRender)` — on `firstRender && !_vm.IsHistorical`, spawn `_ = ConsumeStreamAsync(_cts.Token)`.
3. `ConsumeStreamAsync(CancellationToken ct)`:
   - Compute `heldIds = _vm.Tickers.Where(t => t.CallState is SuggestionState.Pending).Select(...)`. Resolution of `Id` requires the ticker→id mapping; the skeleton VM carries `Id` already in the underlying instrument flow but not on `TickerView` today. Add `int InstrumentId` to `TickerView` (cheap, already available in `ordered` at line 109).
   - `await foreach (var ev in _stream.StreamAsync(heldIds, ct))`:
     - Map `ev` to a `SuggestionState`.
     - If `ev.InstrumentId == focusInstrumentId`: set `_focusState`; if Ready, also build the focus slice via `_buildSlice.BuildAsync(ev.Suggestion, _vm.Today, ct)` and assign `_focusDerived`. If Ready, also kick the backfill chain (preserving the existing fire-and-forget pattern from `LoadDashboardUseCase`).
     - Else: `_tickerStates[ev.InstrumentId] = state`.
     - `await InvokeAsync(StateHasChanged)`.
4. `IAsyncDisposable.DisposeAsync` → `_cts.Cancel(); _cts.Dispose();`.
5. "Re-run AI" button: cancel `_cts`, create new CTS, reset `_focusState`/`_tickerStates`/`_focusDerived` to Pending/empty, spawn new stream.

UI binding rules:
- `TodaysCallCard.razor` parameter: `_focusState`.
- Per-ticker rendering in zones/positions: lookup `_tickerStates[t.InstrumentId]` (or `t.CallState` from skeleton, which is just the Pending default).
- `CallDiff`, `IndicatorHistories`, `MarketSnapshot` reads go to `_focusDerived` not `_vm.*`.

#### `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor`

Parameter changes from `Suggestion? Suggestion` to `SuggestionState State`. Three render branches:

| State | Appearance |
|---|---|
| Pending | Skeleton shimmer matching the card's existing layout dimensions (action chip, conviction badge, rationale lines, citation row) so no reflow on arrival. Pulse animation. |
| Ready | Existing card rendering, no visual change. |
| Failed | Compact error row: warning icon, "AI call failed: {reason}" (truncated to ~80 chars), Retry button on the right. Retry raises `EventCallback OnRetry`; the page resets `_focusState` to Pending and invokes `getOne` directly out-of-band of the main stream. |

Per-ticker components in zones/citations render their `CallState` analogously: a compact pending dot, the existing presence indicator on Ready, or a small "!" on Failed.

### 5.4 Settings

- `Application/Settings/Config/SettingsModels.cs` — extend `AnthropicSettings` record with `int MaxParallelSuggestions { get; init; } = 3;`.
- `Application/Settings/Config/SettingsKeys.cs` — add `AnthropicMaxParallelSuggestions` constant.
- `TradyStrat/Features/Settings/SettingsPage.razor` — add numeric input bound to the new field, mirroring the existing `ThinkingBudget` input. Range 1–10. Tooltip: "How many AI calls can run at once. Higher = faster dashboard, but Anthropic may rate-limit shared keys above 3."

### 5.5 DI

- `AiSuggestionApplicationModule` registers `StreamTodaysSuggestionsUseCase` and `BuildFocusDerivedSliceUseCase` as Scoped.
- `LoadDashboardUseCase`'s `GetAllTodaysSuggestionsUseCase` ctor dep is removed. `GetAllTodaysSuggestionsUseCase` itself remains registered (used by `SuggestionBackfillCoordinator`).

## 6. Data flow detail

### 6.1 Cold path (no suggestion rows yet today), live mode

1. `t=0`: User opens `/`. `OnParametersSetAsync` runs.
2. `t=0–200ms`: `LoadDashboardUseCase` reads positions, indicators, FX, growth. Builds skeleton VM with Pending states everywhere. Returns.
3. `t=200ms`: First render — full dashboard visible, 3 held cards showing skeleton, focus card showing skeleton.
4. `t=200ms`: `OnAfterRenderAsync(firstRender=true)` fires `ConsumeStreamAsync`.
5. `t=200ms`: `StreamAsync` spawns 3 worker tasks, all enter the semaphore (assuming `maxParallel ≥ 3`).
6. Each worker:
   - Enters its per-`(date, instrumentId)` partition via partitioned `SuggestionGate`.
   - Fast-path miss → `IAiClient.AskAsync(snap, ct)` → Anthropic call → row inserted.
   - Yields `Ready`.
7. As each event arrives in completion order:
   - Non-focus: `_tickerStates[id] = Ready(s)`; `InvokeAsync(StateHasChanged)`.
   - Focus: `_focusState = Ready(s)`; `_focusDerived = await _buildSlice.BuildAsync(s, today, ct)`; fire backfill chain; `InvokeAsync(StateHasChanged)`.
8. Each `StateHasChanged` triggers Blazor Server diff push over the existing SignalR circuit; only the changed sub-tree re-renders.

### 6.2 Warm path (rows exist), live mode

1. Same first-paint cost.
2. Each worker hits the fast path in `GetTodaysSuggestionUseCase` (row exists; bypasses the per-key gate; no AI call).
3. All 3 `Ready` events fire within ~10ms of each other; user sees three near-simultaneous card flips and the focus slice rebuild completes within the same render batch.

### 6.3 Mixed (some warm, some cold), live mode

Whichever instrument's row exists yields immediately; others stream in as their AI calls return. No serialization between them now that `SuggestionGate` is partitioned.

### 6.4 Historical mode

`LoadDashboardUseCase` does all the existing work synchronously (no stream, no skeleton split). `_focusState` and each `_tickerStates[id]` are initialized directly from the loaded VM: `Ready(row)` if a Suggestion row exists for that instrument on the historical date, `null` otherwise. `_focusDerived` is built inline by `BuildFocusDerivedSliceUseCase` only if the focus row exists. The page's `OnAfterRenderAsync` short-circuits the stream when `_vm.IsHistorical`.

## 7. Error isolation & cancellation

| Scenario | Behavior |
|---|---|
| One non-focus ticker's Anthropic call throws | That worker yields `Failed(id, reason)`. Other workers unaffected. The ticker's row shows the error indicator with Retry. |
| Focus ticker's Anthropic call throws | `_focusState = Failed(reason)`. Derived slice stays at `FocusDerivedSlice.Empty`. TodaysCallCard shows the error row with Retry. Other tickers unaffected. |
| User clicks "Re-run AI" | Page cancels current `_cts`, resets all states to Pending, creates new CTS, starts new stream. In-flight workers see `OperationCanceledException`, swallow, release semaphore. |
| User navigates away | Page `DisposeAsync` cancels `_cts`. Stream consumer exits its `await foreach`. Workers see cancel, swallow. |
| Network outage to Anthropic | Each worker eventually surfaces an exception. All cards end up `Failed`. Per-card Retry is the recovery path; no global retry in this spec. |
| Browser tab #2 opens during tab #1's in-flight call (same instrument, same date) | Per-key gate in tab #2's worker blocks until tab #1's worker writes the row, then tab #2 sees the existing row via the fast path and yields `Ready` quickly. |

## 8. UI render states

| Component | Pending | Ready | Failed |
|---|---|---|---|
| `TodaysCallCard` (focus) | Skeleton shimmer matching real card dimensions, pulse animation. | Existing card unchanged. | Compact error row with reason + Retry. |
| Per-ticker zone/position row callout | Subtle pending dot or shimmer on the call indicator. | Existing badge/indicator unchanged. | Small "!" with hover tooltip showing reason. |
| `CallDiff`, `IndicatorHistories`, `MarketSnapshot`-derived UI | Empty / absent (default state for these is already empty). | Existing render. | Same as Pending — these components are bound to `_focusDerived` and stay empty when focus failed. |

No new components. CSS extensions on `TodaysCallCard.razor.css` (`.skeleton`, `.skeleton-pulse`, `.error-row`) follow existing naming.

## 9. Concurrency, cancellation, and ordering guarantees

- **No ordering guarantee on `IAsyncEnumerable` output.** Events arrive in worker-completion order. Each card routes events by `InstrumentId`, not position.
- **Cancellation flows** from page `_cts` → `StreamAsync`'s `ct` parameter → `Channel.ReadAllAsync(ct)` (cooperative) → each worker's `ct` → `IChatClient.GetResponseAsync(ct)` inside `SuggestionService`. Anthropic SDK honors `CancellationToken`.
- **Semaphore correctness.** `SemaphoreSlim` cap enforces in-flight worker count; verifiable in tests via a gauge in a fake `GetTodaysSuggestionUseCase` substitute.
- **Channel completion is exactly-once.** Driven by `Task.WhenAll(workers).ContinueWith(_ => chan.Writer.Complete())` so writers never race the close.

## 10. Testing strategy

Test stack per project conventions: **xUnit v3 + Shouldly + hand-rolled fakes** in `TradyStrat.TestKit`. No Moq, no NSubstitute, no bUnit.

### 10.1 `StreamTodaysSuggestionsUseCase` (Application.Tests)

| Test | Verifies |
|---|---|
| `Yields_one_event_per_instrument` | Three instruments → three events (any order). |
| `Yields_in_completion_order_not_input_order` | Stub returns after configurable delays; shortest-delay ticker yields first. |
| `One_failed_instrument_does_not_block_others` | One stub throws; others return. Receive one Failed + remaining Ready. |
| `Respects_max_parallel_cap` | Stub records concurrent-call gauge. With `maxParallel=2` and 5 instruments, gauge never exceeds 2. |
| `Cancellation_stops_new_starts_and_completes_in_flight` | Cancel mid-stream; assert no more events after cancellation, semaphore returns to full count after small wait. |

A new `FakeGetTodaysSuggestion` test double is added under `TradyStrat.TestKit/AiSuggestion/`, exposing per-id delay/error configuration and a concurrency gauge.

### 10.2 Partitioned `SuggestionGate` (Application.Tests)

| Test | Verifies |
|---|---|
| `Different_keys_do_not_block_each_other` | Two acquires on different `(date, instrumentId)` keys proceed concurrently. |
| `Same_key_serializes` | Two acquires on the same key — second waits until first releases. |

### 10.3 `BuildFocusDerivedSliceUseCase` (Application.Tests)

| Test | Verifies |
|---|---|
| `Returns_empty_when_no_prior` | No prior suggestion row → CallDiff is None, slice is otherwise fully populated. |
| `Builds_histories_only_for_cited_indicators` | Citations limited to RSI + Bollinger → only those keys in IndicatorHistories. |
| `Handles_malformed_market_json` | `MarketSnapshotJson` invalid → MarketSnapshot is Empty, logged as warning, no throw (parity with current `LoadDashboardUseCase` behavior at line 144). |

### 10.4 `LoadDashboardUseCase` (Application.Tests)

| Test | Verifies |
|---|---|
| `Live_mode_initializes_Pending_states` | Skeleton VM has `FocusCallState = Pending`, each held `Tickers[i].CallState = Pending`, watchlist `CallState = null`. |
| `Historical_mode_missing_row_is_null_not_Pending` | Historical input with no Suggestion row → `FocusCallState = null` (and corresponding `Tickers[i].CallState = null`); never Pending. |
| `Live_mode_does_not_call_AI` | Stub `IAiClient.AskAsync` throws on call; `ExecuteAsync` completes normally. |
| `Historical_mode_remains_synchronous` | Historical input populates the full VM in one call; no Pending states leak through. |

### 10.5 `DashboardPage` E2E (E2E.Tests via `WebApplicationFactory`)

| Test | Verifies |
|---|---|
| `Live_initial_render_shows_skeleton_for_held_tickers` | GET `/` → HTML contains skeleton markers for each held instrument and the focus card; no "submitted suggestion" tell yet. |
| `Historical_initial_render_is_complete` | GET `/?date=...` for a historical date → no skeleton markers; full focus card present if a row exists. |

Streaming behavior (cards flipping over SignalR) is not asserted via E2E because Blazor Server's SignalR-pushed updates require a Blazor-aware test harness, which is not in the project's current toolchain. Manual verification covers this; if regressions appear later, we add Playwright in a follow-up.

### 10.6 Manual verification checklist

- [ ] Cold cache, 3 held tickers: dashboard renders within ~200ms, cards flip in independently within ~3–8s each.
- [ ] Warm cache: cards visible within ~300ms total.
- [ ] One ticker's API key revoked (or rate-limit forced): that card shows Failed; others succeed.
- [ ] "Re-run AI" mid-stream: cards reset to skeleton, stream restarts.
- [ ] Navigate away mid-stream: no orphan tasks (verify via logs).
- [ ] `maxParallelSuggestions=1`: cards flip one-by-one, longest call last.
- [ ] Focus ticker is one of N held: focus card and zone/position views all show appropriate state.
- [ ] Historical mode (`?date=YYYY-MM-DD`): full dashboard renders in one shot, no skeleton flash.

## 11. Risks & open questions

- **Blazor Server `InvokeAsync(StateHasChanged)`.** Workers run on `Task.Run`-spawned threads, not the SignalR circuit's synchronization context. Updates **must** route through `InvokeAsync` or risk circuit corruption. Enforced in code review and called out in the implementer prompt.
- **Channel completion ordering.** If a worker writes to the channel *after* `chan.Writer.Complete()` (race), the write throws `ChannelClosedException`. Mitigation: complete only after `Task.WhenAll(workers)` resolves (see §5.1 step 4).
- **Anthropic rate limits.** Default `maxParallel=3` is conservative. Users with higher tier keys can raise it. No adaptive backoff in this spec; persistent rate-limit failures surface as Failed cards.
- **Page-level mutable state vs immutable VM.** This spec adopts a pragmatic split: VM stays immutable for skeleton data; per-card state lives in three mutable fields on the page. This deviates slightly from the otherwise pure functional flavor of the dashboard but avoids dictionary-copy thrash on every stream event. If the team prefers strict immutability, the alternative is `_vm` replacement on each event (more allocation but no shared mutable state). Lean toward the current proposal during implementation; reverse only if review surfaces a concrete reason.
- **Backfill chain timing.** Backfill currently runs inside `LoadDashboardUseCase` after `prior` is read. With the skeleton split, backfill must run after the focus state arrives Ready. The page kicks the chain in `ConsumeStreamAsync`. Failure mode unchanged (fire-and-forget with crash logging).
- **`TickerView.InstrumentId` addition.** Stream events carry `InstrumentId` but `TickerView` does not. Adding the field is a one-line change. Verify no downstream consumer hardcodes the field count of the record (Razor binders typically don't).

## 12. File touch list

**New:**
- `TradyStrat.Application/AiSuggestion/UseCases/StreamTodaysSuggestionsUseCase.cs`
- `TradyStrat.Application/AiSuggestion/SuggestionStreamEvent.cs`
- `TradyStrat.Application/Dashboard/SuggestionState.cs`
- `TradyStrat.Application/Dashboard/UseCases/BuildFocusDerivedSliceUseCase.cs`
- `TradyStrat.Application/Dashboard/FocusDerivedSlice.cs`
- `TradyStrat.Application.Tests/AiSuggestion/StreamTodaysSuggestionsUseCaseTests.cs`
- `TradyStrat.Application.Tests/AiSuggestion/SuggestionGateTests.cs`
- `TradyStrat.Application.Tests/Dashboard/BuildFocusDerivedSliceUseCaseTests.cs`
- `TradyStrat.Application.Tests/Dashboard/LoadDashboardUseCaseSkeletonTests.cs` (or extend any existing dashboard test file)
- `TradyStrat.TestKit/AiSuggestion/FakeGetTodaysSuggestion.cs`

**Modified:**
- `TradyStrat.Application/AiSuggestion/UseCases/SuggestionGate.cs`
- `TradyStrat.Application/AiSuggestion/UseCases/GetTodaysSuggestionUseCase.cs` (gate call site only)
- `TradyStrat.Application/AiSuggestion/AiSuggestionApplicationModule.cs` (DI)
- `TradyStrat.Application/Dashboard/UseCases/LoadDashboardUseCase.cs`
- `TradyStrat.Application/Dashboard/DashboardViewModel.cs` (field types)
- `TradyStrat.Application/Dashboard/TickerView.cs` (field types + add `InstrumentId`)
- `TradyStrat.Application/Settings/Config/SettingsModels.cs`
- `TradyStrat.Application/Settings/Config/SettingsKeys.cs`
- `TradyStrat/Features/Dashboard/DashboardPage.razor.cs`
- `TradyStrat/Features/Dashboard/DashboardPage.razor` (binding updates for `_focusState`, `_tickerStates`, `_focusDerived`)
- `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor`
- `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css`
- `TradyStrat/Features/Settings/SettingsPage.razor`
- `TradyStrat.E2E.Tests/SmokeTests.cs` (or add a new file with the two §10.5 tests)

**Not touched:**
- `SuggestionBackfillCoordinator` and its use of `GetAllTodaysSuggestionsUseCase`.
- Replay CLI path.
- All Phase 3 decorators (`CacheControlChatClient`, `ThinkingChatClient`, `ThinkingHarvestChatClient`).
- MCP tools.
- Historical dashboard rendering (still synchronous and complete).

## 13. Acceptance

Spec is implementable when:
- All section 5 components are defined precisely enough that an implementer can write each file from this document alone.
- Section 10 tests pass.
- Section 10.6 manual checks succeed against a real Anthropic key.

Shipping criterion: the manual checklist in §10.6 passes on the user's actual portfolio with cold cache.
