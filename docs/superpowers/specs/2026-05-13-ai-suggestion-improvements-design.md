# TradyStrat — AI suggestion improvements

**Status:** Design approved · Ready for implementation plan
**Date:** 2026-05-13
**Author:** Philippe Matray (with Claude)
**Depends on:** [`2026-05-13-hexagonal-refactor-design.md`](./2026-05-13-hexagonal-refactor-design.md) — this spec assumes the refactored project layout has merged.
**Successor:** none yet — future Phase 3 work (regime tag, ATR-scaled threshold, replay-page UI) layers on top.

---

## 1. Purpose & goal

The daily AI loop today is a single-shot call: snapshot of indicators, portfolio, markets goes to Claude, a structured `submit_suggestion` tool call comes back. It works but it's **stateless** — the model has no memory of its own past calls, can't see whether its previous high-conviction Acquire on CON3 played out, and re-pays for the whole snapshot envelope on every call (and Phase 2 made this `N×` per day).

This spec adds three changes that compound:

1. **Closed-loop outcome feedback** — past suggestions and how they played out become part of the snapshot the model sees.
2. **Prompt caching via envelope split** — the stable part of the snapshot gets `cache_control: ephemeral` so multi-instrument days and replay reruns pay for only the variable tail.
3. **Extended thinking** — give the model a thinking budget for the daily call (quality matters more than latency at once-per-day) and persist the thinking text for later audit.

Plus the tooling needed to know whether the changes worked:

4. **Replay CLI** — a Spectre-based command that re-runs the prompt against historical snapshots and scores the results, tagged by prompt hash.

## 2. Decisions at a glance

| Concern | Decision |
|---|---|
| Past-suggestion shape | **30 trading days, this instrument only.** No cross-instrument context. |
| Per-row fields | `{date, action, conviction, fwd_return_pct, was_correct, net_trade_flow_eur?, rationale_headline}` — headline truncated to 80 chars. See §4.1 naming note. |
| "Was correct" rule | **5-day forward return, ±2% fixed threshold.** Acquire correct iff `fwd_return > +2%`. Trim correct iff `fwd_return < −2%`. Hold/Wait correct iff `|fwd_return| < 2%`. |
| Net trade flow | Optional per-row, in EUR. Buys contribute negative (cash out), Sells contribute positive (cash in), summed over the 5-day window following the suggestion date. Null if no trade in window. |
| Prompt caching | **Envelope/focus split.** Two content blocks in the user message; envelope carries `cache_control: { type: ephemeral }`. |
| Cache eligibility | First-of-day call on any instrument primes the cache; subsequent same-day calls (other instruments, force-refetch, replay) hit it within the 5-minute TTL. |
| Extended thinking budget | **8192 tokens.** New setting `anthropic.thinkingBudget` (default 8192, editable in Settings page). |
| Thinking persistence | **Yes** — new nullable `ThinkingText` column on `Suggestion`. Stored at submission time. |
| Migration | **One EF migration**, `AiSuggestionPhase3`. Adds `Suggestion.ThinkingText TEXT NULL` + `Suggestion.EnvelopeHash TEXT NULL` + `Suggestion.PromptVersionHash TEXT NULL`. No data backfill. |
| Replay CLI | **`TradyStrat.Cli`** project (skeleton from the refactor); new `Command<ReplaySettings>` named `replay` — thin adapter over a new `ReplaySuggestionsUseCase` in Application. |
| Replay output | `Spectre.Console.Table` with per-action hit-rate, per-action avg fwd return, conviction-weighted score, overall hit-rate. Grouped by `PromptVersionHash` so version-over-version comparison is well-defined. |
| Anthropic SDK surface | **Stay on `Microsoft.Extensions.AI`** for the call pipeline. Cache control and thinking are added via a **chain of `IChatClient` decorators** (Decorator pattern — see §5.2 and §6.1) registered in Infrastructure: `CacheControlChatClient`, `ThinkingChatClient`, `ThinkingHarvestChatClient`. The typed `ChatOptions.WithThinking(int budgetTokens)` extension shipped in `Anthropic.SDK 5.10` is used directly. Cache-control passthrough requires a custom decorator (the `AdditionalProperties["cache_control"]` path is **not** documented as honoured by `Anthropic.SDK 5.10`'s M.E.AI bridge — see §5.2). |
| Hash semantics | Three hashes, each measuring one thing. `PromptVersionHash = SHA256(system_prompt + tool_def_signature + focus_shape_keys_excluding_history)` — what the replay groups by. `EnvelopeHash = SHA256(envelope_json)` — what the cache prefix is keyed on. `PromptHash = SHA256(envelope_json + focus_json)` — the full-payload integrity hash, equivalent to today's. All three persisted on `Suggestion`. |
| `WasCorrect` rule | Lifted into a **Domain-level `ICorrectnessRule`** with one impl `FixedThresholdCorrectness(0.02m)`. Both `RecentSuggestionsSection` and `ReplaySuggestionsUseCase` consume the rule, never the literal threshold. ATR-scaled swap (§10) becomes a one-class addition. |
| Snapshot construction | `AiSnapshotService` refactored to **Composite of section providers** (`ISnapshotSectionProvider`). Each section (goal, tickers, portfolio, trades, markets, recent-suggestions) is independently testable. The new `RecentSuggestionsSection` carries this spec's new field; existing sections move unchanged. |
| Behavioural feature flag | **None.** The new prompt shape is the only shape. Replay harness covers regression. |

## 3. Schema migration

A single EF migration `AiSuggestionPhase3`. Idempotent, auto-applied on startup.

### 3.1 Domain model

```
Suggestion (CHANGED — add ThinkingText, EnvelopeHash, PromptVersionHash)
  Id, InstrumentId, ForDate, Action, QuantityHint, MaxPriceHint,
  Conviction, Rationale, CitationsJson, MarketSnapshotJson,
  PromptHash,
  CreatedAt
  + ThinkingText       string?  -- NEW, nullable
  + EnvelopeHash       string?  -- NEW, nullable (null on pre-Phase-3 rows)
  + PromptVersionHash  string?  -- NEW, nullable (null on pre-Phase-3 rows)
```

`PromptHash` keeps its existing semantics (whole-payload integrity hash) — the replay harness no longer relies on it for grouping; that's what `PromptVersionHash` is for. See §5.3.

### 3.2 EF migration SQL

```sql
ALTER TABLE Suggestions ADD COLUMN ThinkingText      TEXT NULL;
ALTER TABLE Suggestions ADD COLUMN EnvelopeHash      TEXT NULL;
ALTER TABLE Suggestions ADD COLUMN PromptVersionHash TEXT NULL;
```

SQLite, so `TEXT NULL` for all three. No backfill. Pre-Phase-3 rows have NULL for all three columns. The Razor card hides the disclosure when `ThinkingText` is NULL; the replay harness prints `—` when `PromptVersionHash` is NULL and groups those rows under an explicit "pre-Phase-3" bucket.

### 3.3 Settings

One new key registered in `SettingDescriptor`:

```
anthropic.thinkingBudget : int, default 8192, range [1024, 16000]
```

> **Range note:** Anthropic's documented minimum for extended thinking is 1024 tokens. The upper bound depends on the active model: Sonnet 4.x supports up to 64k thinking tokens, Claude 3.7 Sonnet up to 64k as well, but Sonnet 4.6 token budgets recommended for production use sit in the 8–16k range to keep latency tractable. The setting validates `[1024, 16000]`; widen later if a model gains demonstrably better quality with larger budgets.

The `AnthropicSettings` record (currently `(string Model, int MaxTokens)`) gains the new field:

```csharp
public sealed record AnthropicSettings(string Model, int MaxTokens, int ThinkingBudget);
```

`SettingsReader.AnthropicAsync` reads the new key. `AnthropicSettingsForm.razor` gains a third input alongside `model` and `maxTokens`.

## 4. Outcome-feedback loop

### 4.1 Snapshot shape

`AiSnapshot` gains the `RecentSuggestions` block and two extra hashes (see §5.3):

```csharp
public sealed record AiSnapshot(
    DateOnly Today,
    [property: JsonIgnore] int InstrumentId,
    GoalConfig Goal,
    PortfolioSnapshot Portfolio,
    IReadOnlyList<TickerContext> Tickers,
    IReadOnlyList<TradeRecent> RecentTrades,
    decimal? UsdPerEur,
    IReadOnlyList<PredictionMarket> Markets,
    IReadOnlyList<PastSuggestionRow> RecentSuggestions,  // NEW
    string EnvelopeHash,                                  // NEW — see §5.3
    string PromptVersionHash,                             // NEW — see §5.3
    string PromptHash);
```

```csharp
public sealed record PastSuggestionRow(
    DateOnly Date,
    SuggestionAction Action,
    int Conviction,
    decimal FwdReturnPct,
    bool WasCorrect,
    bool IsForwardWindowComplete,    // NEW — see §4.2 step 2
    decimal? NetTradeFlowEur,
    string RationaleHeadline);
```

> **Naming note (`NetTradeFlowEur`):** the brainstorming round (Q1 option C) called this field "realized P&L," but for an accumulation strategy with mostly Buys there's no realized P&L inside a 5-day window — Buys don't close. The cleanest implementable signal is **net trade cash flow on this instrument in the forward window**: negative for net buying, positive for net selling. That tells the model whether the user actually acted, in which direction, and how heavily — which is the loop you want closed.

### 4.2 How rows are built

`AiSnapshotService` is refactored to a Composite of section providers (§4.4). The new `RecentSuggestionsSection` produces these rows for `(instrumentId, asOf)`:

1. Load the last 30 `Suggestion` rows where `InstrumentId == instrumentId` and `ForDate < asOf`, ordered by `ForDate` ascending. "30 trading days" = the 30 most recent rows in this set — `Suggestion`s are created one-per-trading-day per instrument, so this is mechanically equivalent to a trading-day window and doesn't require calendar arithmetic.
2. For each suggestion `s`, locate the instrument's price bar at `s.ForDate` (`closeAt`) and the price bar 5 trading bars forward in the stored `PriceBar` series (`closeFwd`). "5 trading days" is **5 stored bars forward**, never a calendar offset — weekends, holidays, and exchange closures are naturally skipped by the bar iterator.
3. If `closeFwd` doesn't exist yet (the 5th forward bar hasn't been recorded — happens for the 5 most recent suggestions near `asOf`), emit the row with `FwdReturnPct = 0`, `WasCorrect = false`, **`IsForwardWindowComplete = false`**. The model treats `IsForwardWindowComplete = false` as "ignore the scoring fields, this is here for context only."
4. `FwdReturnPct = (closeFwd − closeAt) / closeAt × 100`.
5. `WasCorrect` is computed by **`ICorrectnessRule.Evaluate(action, fwdReturnPct)`** — see §4.3.
6. `NetTradeFlowEur` — query trades on `instrumentId` where `ExecutedOn` is in `(s.ForDate, fwdBarDate]` where `fwdBarDate` is the calendar date of the 5th forward bar. Sum signed cash flow per trade: a Buy contributes `−(Quantity × PricePerShare)` (cash out), a Sell contributes `+(Quantity × PricePerShare)` (cash in). Each trade converts to EUR via historical FX at its `ExecutedOn`. Null when no trade in window.
7. `RationaleHeadline` — first 80 chars of `s.Rationale`, **trimmed back to the last whitespace boundary** to avoid mid-word fragments. If the first 80 chars contain no whitespace (rare), keep them as-is. No ellipsis appended.

Implementation note: if `closeAt` is missing for an old suggestion (e.g. newly added watchlist instrument with sparse history), the row is skipped — never throws.

### 4.3 `ICorrectnessRule` (Specification pattern)

The "was correct" rule is a domain concept and a future tuning surface (ATR-scaled, regime-aware). It lives in Domain, not in any service:

```csharp
// TradyStrat.Domain
public interface ICorrectnessRule
{
    bool Evaluate(SuggestionAction action, decimal fwdReturnPct);
}

public sealed class FixedThresholdCorrectness(decimal thresholdPct) : ICorrectnessRule
{
    public bool Evaluate(SuggestionAction action, decimal fwd) => action switch
    {
        SuggestionAction.Acquire => fwd >  thresholdPct,
        SuggestionAction.Trim    => fwd < -thresholdPct,
        SuggestionAction.Hold    => Math.Abs(fwd) < thresholdPct,
        SuggestionAction.Wait    => Math.Abs(fwd) < thresholdPct,
        _                        => false,
    };
}
```

Registered in `AiSuggestionApplicationModule` as `services.AddSingleton<ICorrectnessRule>(_ => new FixedThresholdCorrectness(2.0m))`. Both `RecentSuggestionsSection` (this spec) and `ReplaySuggestionsUseCase` (§7) consume it.

Phase 3 work (out of scope §10) introduces `AtrScaledCorrectness(IIndicatorEngine, decimal multiplier)` as a one-class addition; registration changes from `FixedThresholdCorrectness` to the new rule. No other code touches.

### 4.4 Section-provider Composite (Builder pattern)

`AiSnapshotService.CreateAsync` is currently a 90-line orchestrator. Adding `RecentSuggestionsSection` linearly would push it past 120. Instead, the service is refactored to a Composite:

```csharp
// TradyStrat.Application
internal interface ISnapshotSectionProvider
{
    int Order { get; } // lower runs first; allows sections to depend on earlier sections' output
    Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct);
}

internal sealed class SnapshotBuilder
{
    public GoalConfig? Goal { get; set; }
    public PortfolioSnapshot? Portfolio { get; set; }
    public List<TickerContext> Tickers { get; } = [];
    public List<TradeRecent> RecentTrades { get; } = [];
    public decimal? UsdPerEur { get; set; }
    public IReadOnlyList<PredictionMarket> Markets { get; set; } = [];
    public List<PastSuggestionRow> RecentSuggestions { get; } = [];

    public AiSnapshot Build(DateOnly today, int instrumentId) { /* hash + record construction */ }
}
```

Section providers:

| Provider | Order | Reads from |
|---|---|---|
| `GoalSection` | 10 | `IReadRepositoryBase<GoalConfig>` |
| `TickersSection` | 20 | `IndicatorEngine`, `FxConverter` |
| `PortfolioSection` | 30 | `PortfolioService` (consumes `TickersSection`'s output for per-instrument prices) |
| `RecentTradesSection` | 40 | `IReadRepositoryBase<Trade>` |
| `MarketsSection` | 50 | `IPredictionMarketProvider` (graceful degradation on `PolymarketUnavailableException`) |
| `RecentSuggestionsSection` | 60 | `IReadRepositoryBase<Suggestion>`, `IReadRepositoryBase<PriceBar>`, `IReadRepositoryBase<Trade>`, `FxConverter`, `ICorrectnessRule` |
| `UsdPerEurSection` | 70 | `FxConverter` |

`AiSnapshotService` becomes:

```csharp
public sealed class AiSnapshotService(
    IEnumerable<ISnapshotSectionProvider> sections,
    IClock clock) : IAiSnapshotService
{
    private readonly ISnapshotSectionProvider[] _ordered = sections.OrderBy(s => s.Order).ToArray();

    public async Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        var b = new SnapshotBuilder();
        foreach (var section in _ordered)
            await section.ContributeAsync(b, instrumentId, asOf, ct);
        return b.Build(asOf, instrumentId);
    }
}
```

Each provider is unit-testable in isolation against its own fixtures. `SnapshotBuilder.Build` is where the three hashes (§5.3) are computed in one place, with deterministic JSON serialization.

### 4.5 Token budget

30 rows × ~80 tokens/row ≈ 2.4k tokens for the history block. The block sits in the **focus** content (not the envelope) since it's per-instrument.

## 5. Prompt caching via envelope split

### 5.1 User message structure

`SuggestionService` no longer serializes `AiSnapshot` as one JSON blob. The user message becomes two text content blocks:

- **Envelope** — stable across instruments on the same day; carries cache-control metadata.
- **Focus** — per-instrument; not cached.

**Envelope JSON** (shared across same-day calls):

```json
{
    "today": "2026-05-13",
    "goal": { ... },
    "portfolio": { ... },
    "tickers": [ ...all of them, in catalog order... ],
    "recent_trades": [ ... ],
    "usd_per_eur": 1.0857,
    "markets": [ ... ]
}
```

**Focus JSON** (per-instrument):

```json
{
    "instrument_id": 3,
    "primary_ticker": "CON3.L",
    "recent_suggestions": [ ... 30 rows ... ]
}
```

The system prompt and tool definition sit before the envelope and are part of the cache prefix (Anthropic's cache control applies to "everything up to and including this point").

### 5.2 Cache-control via Decorator (chain of `IChatClient`)

`Microsoft.Extensions.AI` does **not** document a generic `cache_control` passthrough on `Anthropic.SDK 5.10` — `Anthropic.SDK.Messaging.CacheControl` is a typed property on native `MessageParameters`/content blocks, not a string-keyed `AdditionalProperties` channel. We add a dedicated decorator that intercepts requests before they reach the SDK's M.E.AI adapter:

```csharp
// TradyStrat.Infrastructure.AiSuggestion
internal sealed class CacheControlChatClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public const string CacheBreakpointKey = "trady.cacheBreakpoint";

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken ct)
    {
        // For every TextContent flagged with CacheBreakpointKey = true, attach the
        // SDK-native CacheControl marker so Anthropic.SDK 5.10 emits cache_control
        // on the corresponding content block when it converts the request.
        foreach (var msg in messages)
        foreach (var content in msg.Contents.OfType<TextContent>())
        {
            if (content.AdditionalProperties?.TryGetValue(CacheBreakpointKey, out var v) == true
                && v is true)
            {
                // Path A: M.E.AI exposes Anthropic.SDK.Messaging.CacheControl via a
                // typed AdditionalProperties key. Use it if available.
                // Path B: replace the content with one carrying the typed marker via
                // the SDK's RawRepresentationFactory<Anthropic.SDK.Messaging.Content>.
                // The implementing PR spikes both and picks the one verified to round-trip.
            }
        }
        return base.GetResponseAsync(messages, options, ct);
    }
}
```

**Critical implementation gate:** before merging this spec, a 30-minute spike confirms whether Path A or Path B (or a third — drop down to raw `Anthropic.SDK` for this call site) is the one that actually round-trips `cache_control` on the wire. The implementation plan opens with this spike; the rest of the spec doesn't change either way.

`SuggestionService` builds messages with the breakpoint marker on the envelope content only:

```csharp
new TextContent(envelopeJson) {
    AdditionalProperties = new AdditionalPropertiesDictionary {
        [CacheControlChatClient.CacheBreakpointKey] = true
    }
},
new TextContent(focusJson),
```

No Anthropic-specific types leak above the decorator boundary; `SuggestionService` and Application tests only see `TextContent` + a `string` flag.

### 5.3 Hashing — three hashes, each measures one thing

Today's single `PromptHash` over the whole snapshot conflates "what prompt template did we use" with "what data did we send" — bad for the replay's grouping needs. Split into three:

| Hash | Inputs | Purpose | Persisted on `Suggestion` |
|---|---|---|---|
| **`PromptVersionHash`** | `SHA256(system_prompt + tool_def_signature + focus_schema_keys_excluding_history)` | Identifies the **prompt template version**. Stable across days, across instruments, across history changes. Changes only when the prompt or schema changes. **Replay groups by this.** | Yes (nullable, NULL on pre-Phase-3 rows) |
| **`EnvelopeHash`** | `SHA256(envelope_json)` | Identifies the **cacheable envelope**. Used for log breadcrumbs and cache-hit reasoning. | Yes (nullable, NULL on pre-Phase-3 rows) |
| **`PromptHash`** | `SHA256(envelope_json + focus_json)` | Full-payload integrity hash, equivalent to today's. Kept for audit / "did we send exactly this payload." | Yes (existing column, unchanged semantics) |

All three are computed once inside `SnapshotBuilder.Build` (§4.4) with deterministic JSON serialization (`JsonOpts.Strict`). The `tool_def_signature` is `SHA256(serialized tool descriptor)` — pre-computed at startup, not per-call.

> **Compatibility note:** This breaks the day-zero `PromptHash` sentinel `895EED53A280A470` (from Phase 2). New baselines captured in the implementing PR. The Phase-1→2 byte-identity invariant explicitly scoped to those phases only.

### 5.4 Cache hit lifecycle

- Call 1 of the day on any instrument: envelope cached (~$X uncached input), focus + thinking metered normally.
- Calls 2..N same day, within 5 minutes of the last call: envelope reads from cache at the cached-prefix discount.
- Force-refetch same day: 100% cache hit on envelope.
- Replay harness reruns: cache hit on first replay of each unique envelope (typically once per replayed date).

Approximate savings, current account (Sonnet pricing, 5k-token envelope):

- Single instrument day: no savings (only one call).
- Three held instruments: ~50% savings on the envelope portion of calls 2 and 3.
- Replay of 60 historical days: cache primes once per day, no win there.
- Iterating on the prompt (multiple replays of same day during tuning): near-100% on subsequent runs.

## 6. Extended thinking — typed API + Decorator

### 6.1 Wire-up via `WithThinking`

`Anthropic.SDK 5.10` ships a typed extension `Anthropic.SDK.Extensions.ChatOptionsExtensions.WithThinking(ChatOptions options, int budgetTokens)` (verified against `~/.nuget/packages/anthropic.sdk/5.10.0/lib/net10.0/Anthropic.SDK.xml`). Use it directly — no untyped `AdditionalProperties` JSON.

A small `ThinkingChatClient` decorator applies the setting consistently:

```csharp
// TradyStrat.Infrastructure.AiSuggestion
using Anthropic.SDK.Extensions; // WithThinking

internal sealed class ThinkingChatClient(IChatClient inner, ISettingsReader settings)
    : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken ct)
    {
        var ai = await settings.AnthropicAsync(ct);
        var withThinking = (options ?? new ChatOptions()).WithThinking(ai.ThinkingBudget);
        return await base.GetResponseAsync(messages, withThinking, ct);
    }
}
```

> **Model gating:** the SDK doc-comment notes extended thinking is for "compatible models like Claude 3.7 Sonnet" and Sonnet 4.x. If the configured `anthropic.model` doesn't support thinking, `WithThinking` is still safe to call — the SDK forwards the parameter and Anthropic returns a clear error rather than silently ignoring it. The implementing plan adds a one-time model-capability check on startup that surfaces a Settings-page warning if the active model + thinking budget combination is unsupported.

### 6.2 Response harvesting — `ThinkingHarvestChatClient`

The response contains zero or more thinking blocks before the tool call. We harvest them in a second decorator so `SuggestionService` stays unaware:

```csharp
internal sealed class ThinkingHarvestChatClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public const string ThinkingTextKey = "trady.thinkingText";

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken ct)
    {
        var response = await base.GetResponseAsync(messages, options, ct);

        // Walk response.Messages.Contents for native thinking blocks emitted by
        // Anthropic.SDK 5.10. The exact surface (RawRepresentation<ThinkingBlock>
        // vs AdditionalProperties marker) is locked in the §5.2 spike — same
        // round-trip question as cache-control.
        var thinkingText = ExtractThinking(response);

        if (!string.IsNullOrEmpty(thinkingText))
        {
            response.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            response.AdditionalProperties[ThinkingTextKey] = thinkingText;
        }
        return response;
    }
}
```

`SuggestionService` reads `response.AdditionalProperties[ThinkingHarvestChatClient.ThinkingTextKey]` (a plain `string?`) when constructing the captured `Suggestion`. If absent or empty, `Suggestion.ThinkingText` is null.

### 6.3 Decorator chain registration

In `AiSuggestionInfrastructureModule`:

```csharp
services.AddSingleton<IChatClient>(sp =>
    new Anthropic.SDK.AnthropicClient(apiKey)
        .Messages
        .AsBuilder()
        .Use(inner => new CacheControlChatClient(inner))     // §5.2
        .Use(inner => new ThinkingChatClient(inner, sp.GetRequiredService<ISettingsReader>())) // §6.1
        .Use(inner => new ThinkingHarvestChatClient(inner))  // §6.2
        .UseFunctionInvocation()
        .Build());
```

Each decorator is independently unit-testable against a fake inner `IChatClient`. `SuggestionService` shrinks to ~20 lines — build envelope + focus content, set tool, call `chat.GetResponseAsync`, read tool result + thinking-text-key, return `Suggestion`.

### 6.4 UI surface

Out of scope for this spec. The data is captured; rendering it on the suggestion card behind a `<details>` disclosure is a follow-up trivial change.

## 7. Replay — Use case + thin CLI adapter

The replay loop is **business logic** (snapshot → call AI → score), so it lives in Application as `ReplaySuggestionsUseCase`, not in the driving adapter. The Spectre command's sole job is translating CLI flags into the use-case's input and rendering its output. This keeps the hexagonal boundary clean and matches the existing `UseCaseBase<TInput, TOutput>` convention.

### 7.1 `ReplaySuggestionsUseCase` (Application)

```csharp
// TradyStrat.Application.AiSuggestion.UseCases
public sealed record ReplaySuggestionsInput(
    int InstrumentId, DateOnly Since, DateOnly Until, bool Persist, bool Force);

public sealed record ReplayReport(
    int InstrumentId, DateOnly Since, DateOnly Until,
    IReadOnlyList<ReplayedSuggestion> Rows,
    IReadOnlyDictionary<SuggestionAction, ActionAggregate> PerAction,
    ActionAggregate Overall,
    decimal ConvictionWeightedScore,
    IReadOnlyList<string> DistinctPromptVersionHashes);

public sealed record ReplayedSuggestion(
    DateOnly ForDate, SuggestionAction Action, int Conviction,
    decimal FwdReturnPct, bool WasCorrect, string PromptVersionHash);

public sealed record ActionAggregate(int Count, decimal HitRatePct, decimal AvgFwdReturnPct, decimal AvgConviction);

public sealed class ReplaySuggestionsUseCase(
    IAiSnapshotService snapshots,
    IAiClient ai,
    IReadRepositoryBase<PriceBar> bars,
    IRepositoryBase<Suggestion> suggestionRepo,
    ICorrectnessRule correctness,
    ILogger<ReplaySuggestionsUseCase> log)
    : UseCaseBase<ReplaySuggestionsInput, ReplayReport>(log)
{ ... }
```

The use case is independently testable in `Application.Tests` against a `FakeChatClient` and an in-memory price/suggestion store, with no Spectre dependency.

### 7.2 What it does

For each `DateOnly` in `[Since, Until]` where a price bar exists for `InstrumentId`:

1. Call `snapshots.CreateAsync(instrumentId, date, ct)` to build the historical snapshot.
2. Call `ai.AskAsync(snapshot, ct)` to get a fresh suggestion.
3. Look up the 5-forward-bar price (same rule as §4.2 step 2). If the forward window isn't complete (recent dates), skip that day from scoring but still include it in the row list with `IsForwardWindowComplete = false` echoed forward.
4. Compute `WasCorrect` via the injected `ICorrectnessRule` — single source of truth shared with §4.2.
5. If `Persist`: write the suggestion via `suggestionRepo.AddAsync`. If a row already exists for `(InstrumentId, ForDate)` and `Force` is true, replace; if `Force` is false, abort the entire replay with a clear error (no half-persist).

After the loop, aggregate per action class and produce the `ReplayReport`. Grouping is by `PromptVersionHash` so cross-version comparisons are well-defined.

### 7.3 CLI adapter

In `TradyStrat.Cli/Commands/ReplayCommand.cs`:

```
Usage: dotnet run --project TradyStrat.Cli -- replay [OPTIONS]

Options:
  --instrument <ticker>        Instrument ticker (required, e.g. CON3.L)
  --since <yyyy-MM-dd>         Inclusive start date (default: 90 calendar days back from --until)
  --until <yyyy-MM-dd>         Inclusive end date (default: today UTC)
  --persist                    Write replayed suggestions to DB (default: dry-run scoring only)
  --force                      With --persist, replace existing rows for the same (instrument, date)
  --prompt-version <hex>       Filter the rendered table to one prompt version
```

`ReplayCommand` resolves `ReplaySuggestionsUseCase` + an `IReadRepositoryBase<Instrument>` (for ticker→id lookup), maps the flags to `ReplaySuggestionsInput`, calls `useCase.ExecuteAsync`, then renders the returned `ReplayReport` to a `Spectre.Console.Table`. Zero business logic in the command; ~60 lines including the Spectre wiring.

The placeholder `HelloCommand` from the refactor is removed.

### 7.4 Output

```
┌─────────┬───────┬────────────┬──────────────┬─────────────┐
│ Action  │ Count │ Hit-rate % │ Avg fwd ret  │ Avg convict │
├─────────┼───────┼────────────┼──────────────┼─────────────┤
│ Acquire │  18   │   61.1     │   +3.2%      │     7.2     │
│ Hold    │  31   │   71.0     │   +0.4%      │     5.8     │
│ Trim    │   5   │   40.0     │   −1.1%      │     6.8     │
│ Wait    │   6   │   83.3     │   −0.2%      │     6.1     │
├─────────┼───────┼────────────┼──────────────┼─────────────┤
│ Overall │  60   │   66.7     │   +1.1%      │     6.3     │
└─────────┴───────┴────────────┴──────────────┴─────────────┘

Conviction-weighted score: 0.62  (Σ conviction × was_correct / Σ conviction)
Prompt versions:           A3F2…, B19E…, 4C77…  (3 distinct)
Range: 2026-02-13 → 2026-05-13 · Instrument: CON3.L · Dry-run
```

Conviction-weighted score formula: `Σ (conviction_i × (was_correct_i ? 1 : 0)) / Σ conviction_i`. Single number in `[0, 1]`.

### 7.5 Cost behaviour

Dry-run (default) still costs Anthropic input tokens — there's no way to score without calling the model. With caching from §5, replaying the same date range twice in a row hits cache on the envelope for the bulk of each day's call after the first run (cache is keyed on envelope content; envelope is identical across replays of the same date).

## 8. Testing

Tests follow the layer split from the refactor: Application tests assert on use-case + section-provider behaviour; Infrastructure tests cover decorators and migrations; E2E tests cover the wired-up CLI and Blazor host.

### 8.1 Application.Tests

- **Section-provider tests** (one fixture per provider; each uses a `FakeSnapshotBuilder` to inspect contributed state):
    - `GoalSection`, `TickersSection`, `PortfolioSection`, `RecentTradesSection`, `MarketsSection`, `UsdPerEurSection` — light coverage; mostly regression of the existing `AiSnapshotServiceTests` reorganised.
    - `RecentSuggestionsSection` — full coverage:
        - 30 most recent rows are emitted; older are dropped.
        - `WasCorrect` is computed via the injected `ICorrectnessRule` (test injects a recording fake to assert it was called per row).
        - `IsForwardWindowComplete = false` when the 5th forward bar is missing; corresponding `FwdReturnPct = 0` and `WasCorrect = false` are sentinel values, not real grades.
        - `RationaleHeadline` truncates at 80 chars then trims back to the last whitespace; no ellipsis.
        - `NetTradeFlowEur`: null with no trades; negative on net buys; positive on net sells; zero on cancelling buys+sells. FX uses the per-trade `ExecutedOn` rate.
        - Skips suggestions whose `closeAt` bar is missing (no throw).
- **`FixedThresholdCorrectness` tests** (Domain.Tests, since the rule lives in Domain):
    - All four action classes × {below, within, above} threshold = 12 cases.
- **`SnapshotBuilder` / `AiSnapshotService` integration test**:
    - All section providers wired in order; resulting `AiSnapshot.PromptVersionHash`, `EnvelopeHash`, `PromptHash` are deterministic for fixed inputs.
    - Changing past `Suggestion.Rationale` content does **not** change `PromptVersionHash` (regression for §5.3 bug).
    - Changing the registered `IAiClient` tool-def signature **does** change `PromptVersionHash` (when the tool-def signature wires through; see §5.3).
- **`ReplaySuggestionsUseCaseTests`**:
    - Dry-run does not write to the suggestion repo (assert via mock repo).
    - `--persist` without `--force` against an existing-row date throws a clear typed exception; no partial writes.
    - `--persist --force` replaces the existing row.
    - Recent dates without a complete forward window are included in the row list but excluded from per-action aggregates.
    - Report groups by `PromptVersionHash` correctly (test feeds two stub clients with different hashes).
    - Uses `FakeChatClient` / `StubAiClient` — no live Anthropic calls.

### 8.2 Infrastructure.Tests

- **Decorator tests** — each decorator is exercised against a recording inner `IChatClient`:
    - `CacheControlChatClientTests`: a `TextContent` flagged with the breakpoint key has the SDK-native cache-control marker attached when the inner client receives the request. A content without the flag is unchanged. (This test pins the decision the implementation spike makes — if Path B is chosen, the assertion is on the rewritten content's raw representation; if Path A, on `AdditionalProperties`.)
    - `ThinkingChatClientTests`: configured budget from `ISettingsReader` is applied via `options.WithThinking(...)`. Verifies the budget value flows through.
    - `ThinkingHarvestChatClientTests`: response containing thinking blocks → `response.AdditionalProperties[ThinkingTextKey]` contains the concatenated text. Response with none → the key is absent.
- **`SuggestionServiceTests`** (now thin):
    - Builds exactly one user message with exactly two text contents (envelope + focus).
    - Envelope content carries the breakpoint flag; focus does not.
    - Returned `Suggestion.ThinkingText` mirrors the harvest decorator's output via the recorded response.
- **`AiSuggestionPhase3MigrationTests`** — Phase 2 DB → applies migration → asserts three new columns exist, NULL on existing rows.
- **`MigrationBackwardCompatTests`** — pre-Phase-3 DBs still load; reading an old `Suggestion` returns NULL for all three new fields.

### 8.3 E2E.Tests

- `ModuleSmokeTests` — full Application + Infrastructure module discovery wires `IChatClient` with all three decorators in order; `IAiClient.AskAsync` against `FakeChatClient` returns a populated `Suggestion`.
- `ReplayCommandSmokeTest` — CLI invocation with `--since` = `--until` against an in-memory fixture; asserts zero exit on success, non-zero on validation errors, table rendered to captured stdout.

### 8.4 No live Anthropic calls

All tests use `FakeChatClient` (today) or `StubAiClient`. The replay command's CI path uses `StubAiClient`.

## 9. Open carryover bugs

The Phase 2 spec noted three carryover bugs. None of them block this work, but the AI improvements shouldn't regress them. The implementing plan should verify they remain in their current state (passing or known-bad) after this lands.

## 10. Out of scope

- ATR-scaled `WasCorrect` threshold — A simple swap once the replay harness shows the fixed 2% is mis-calibrated.
- Regime tag in the snapshot — would help the model switch posture but adds an indicator definition discussion.
- Few-shot examples in the system prompt — explicitly considered and rejected during brainstorming.
- Embedding-based memory / vector store — overkill given DB-backed history is sufficient.
- UI surface for `ThinkingText` — captured but not rendered yet.
- Replay-as-Razor-page (Q7 option C) — deferred until the CLI shows which metrics are actually load-bearing.
- Cost dashboard / token-spend telemetry — adjacent but separate.

## 11. Risks

| Risk | Mitigation |
|---|---|
| **Critical: cache_control passthrough path unverified.** `Anthropic.SDK 5.10`'s M.E.AI bridge does not document a generic `AdditionalProperties["cache_control"]` channel; `CacheControl` is a typed native marker. | The implementation plan opens with a 30-minute spike that tries both paths (typed `AdditionalProperties` key vs. raw-representation rewrite vs. drop-down to native SDK) against a real Anthropic call, and locks in the one that round-trips. The `CacheControlChatClient` skeleton accommodates either; only its body changes. If none of the paths work, scope cuts to thinking + outcome-feedback + replay, and caching ships in Phase 4. |
| Extended thinking model gating — `WithThinking` against a non-thinking model | Startup capability probe surfaces a Settings-page warning. Spec 6.1. |
| Extended thinking budget burns more output tokens than expected | Setting is editable; lower the default if production usage shows blowouts. |
| Replay's cache benefit doesn't materialise because each replay day primes its own envelope | Acceptable for v1. The cache primarily helps multi-instrument same-day calls and re-runs of the *same* day during tuning, not new-day replays. |
| Three hashes are confusing for operators reading logs | Spec 5.3 gives each one a one-line semantic. The replay output explicitly labels grouping ("Prompt versions: …"). `PromptHash` keeps its existing audit role unchanged. |
| `PromptHash` sentinel `895EED53A280A470` (Phase 2) drifts under this spec | Acceptable — Phase 1→2 byte-identity invariant explicitly scoped to those phases only. New baselines captured in the implementing PR. |
| Section-provider Composite adds DI registration boilerplate | `AiSuggestionApplicationModule` registers each provider with `services.AddTransient<ISnapshotSectionProvider, GoalSection>()` etc. — seven lines, one per provider. `AiSnapshotService` no longer takes 7 constructor parameters; it takes one `IEnumerable<ISnapshotSectionProvider>`. Net negative on lines of DI wiring. |
