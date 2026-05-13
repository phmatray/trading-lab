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
| Migration | **One EF migration**, `AiSuggestionPhase3`. Adds `Suggestion.ThinkingText nvarchar NULL`. No data backfill. |
| Replay CLI | **`TradyStrat.Cli`** project (skeleton from the refactor); new `Command<ReplaySettings>` named `replay`. |
| Replay output | `Spectre.Console.Table` with per-action hit-rate, per-action avg fwd return, conviction-weighted score, overall hit-rate. Footer line shows the active `PromptHash` for version-tagging. |
| Anthropic SDK surface | **Stay on `Microsoft.Extensions.AI`** for the call pipeline. The two new things (cache control + thinking) are passed via `ChatMessage.Contents` AIContent objects and `ChatOptions.AdditionalProperties` (Anthropic-specific keys: `cache_control`, `thinking`). Verified compatible with `Anthropic.SDK 5.10` + `Microsoft.Extensions.AI 10.3.0`. |
| PromptHash semantics | **PromptHash now covers focus block only.** New `EnvelopeHash` covers the cacheable envelope. Logged separately for cache-hit reasoning. |
| Behavioural feature flag | **None.** The new prompt shape is the only shape. Replay harness covers regression. |

## 3. Schema migration

A single EF migration `AiSuggestionPhase3`. Idempotent, auto-applied on startup.

### 3.1 Domain model

```
Suggestion (CHANGED — add ThinkingText only)
  Id, InstrumentId, ForDate, Action, QuantityHint, MaxPriceHint,
  Conviction, Rationale, CitationsJson, MarketSnapshotJson, PromptHash,
  CreatedAt
  + ThinkingText       string?  -- NEW, nullable
```

### 3.2 EF migration SQL

```sql
ALTER TABLE Suggestions ADD COLUMN ThinkingText TEXT NULL;
```

No backfill. Existing rows have NULL `ThinkingText`. The Razor card and replay harness both handle NULL by hiding the disclosure / printing `—`.

### 3.3 Settings

One new key registered in `SettingDescriptor`:

```
anthropic.thinkingBudget : int, default 8192, range [1024, 32000]
```

The `AnthropicSettings` record (currently `(string Model, int MaxTokens)`) gains the new field:

```csharp
public sealed record AnthropicSettings(string Model, int MaxTokens, int ThinkingBudget);
```

`SettingsReader.AnthropicAsync` reads the new key. `AnthropicSettingsForm.razor` gains a third input alongside `model` and `maxTokens`.

## 4. Outcome-feedback loop

### 4.1 Snapshot shape

`AiSnapshot` gains one field:

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
    string EnvelopeHash,                                  // NEW
    string PromptHash);
```

```csharp
public sealed record PastSuggestionRow(
    DateOnly Date,
    SuggestionAction Action,
    int Conviction,
    decimal FwdReturnPct,
    bool WasCorrect,
    decimal? NetTradeFlowEur,
    string RationaleHeadline);
```

> **Naming note:** the brainstorming round (Q1 option C) called this field "realized P&L," but for an accumulation strategy with mostly Buys there's no realized P&L inside a 5-day window — Buys don't close. The cleanest implementable signal is **net trade cash flow on this instrument in the forward window**: negative for net buying, positive for net selling. That tells the model whether the user actually acted, in which direction, and how heavily — which is the loop you want closed. Field is renamed accordingly.

### 4.2 How rows are built

`AiSnapshotService.CreateAsync(instrumentId, asOf)` adds a step before computing the prompt hash:

1. Load the last 30 trading days of `Suggestion` rows where `InstrumentId == instrumentId` and `ForDate < asOf`, ordered by `ForDate` ascending.
2. For each suggestion `s`, compute `fwdClose = closing price of this instrument 5 trading days after s.ForDate`. If 5 days haven't elapsed yet (recent suggestions near `asOf`), include the row with `FwdReturnPct = 0` and `WasCorrect = false` — clearly marked so the model can ignore it. (Alternative considered: omit recent rows. Rejected because the model loses signal about momentum.)
3. `FwdReturnPct = (fwdClose - closeAtSuggestionDate) / closeAtSuggestionDate × 100`.
4. `WasCorrect` per §2 rule.
5. `NetTradeFlowEur` — query trades on `instrumentId` where `ExecutedOn` is in `(s.ForDate, s.ForDate + 5 trading days]`. Sum signed cash flow per trade: a Buy contributes `−(Quantity × PricePerShare)` (cash out), a Sell contributes `+(Quantity × PricePerShare)` (cash in). Each trade is converted to EUR using historical FX at its `ExecutedOn`. Field is null when no trade in window.
6. `RationaleHeadline = s.Rationale.Substring(0, Math.Min(80, s.Rationale.Length))`, with trailing whitespace trimmed and no truncation indicator added.

Implementation note: the close-price lookup uses the existing price store. If a needed historical close isn't available (rare, but possible for newly added watchlist tickers), the row is skipped — never throws.

### 4.3 Token budget

30 rows × ~80 tokens/row ≈ 2.4k tokens for the history block. The block sits in the **focus** content (not the envelope) since it's per-instrument.

## 5. Prompt caching via envelope split

### 5.1 User message structure

`SuggestionService.AskAsync` no longer serializes `AiSnapshot` as one JSON blob. Instead the user message becomes two `AIContent` text parts:

```
ChatMessage(User, [
    TextContent(envelopeJson, AdditionalProperties: {
        "anthropic.cache_control": { "type": "ephemeral" }
    }),
    TextContent(focusJson)
])
```

**Envelope JSON** — stable across instruments on the same day:

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

**Focus JSON** — per-instrument, not cached:

```json
{
    "instrument_id": 3,
    "primary_ticker": "CON3.L",
    "recent_suggestions": [ ... 30 rows ... ]
}
```

The system prompt and tool definition sit before the envelope and are implicitly part of the cache prefix (Anthropic's cache control applies to "everything up to and including this point").

### 5.2 Anthropic SDK passthrough

`Microsoft.Extensions.AI`'s `TextContent.AdditionalProperties` is read by `Anthropic.SDK 5.10`'s `Microsoft.Extensions.AI` adapter and emitted as `cache_control` on the corresponding content block. Verified against the SDK's source.

If the property is not honoured by a future SDK version, the fallback is to drop down to raw `Anthropic.SDK` for this call site only. Out of scope for v1; documented as a known risk.

### 5.3 Hashing

`PromptHash` previously covered the entire serialized snapshot. After this change:

- **`EnvelopeHash`** covers the envelope JSON. Used for log breadcrumbs and cache-hit reasoning. Not persisted on `Suggestion`.
- **`PromptHash`** covers the focus JSON. Persisted on `Suggestion` as today. Replay harness compares prompt hashes to group suggestions by prompt version.

> **Compatibility note:** This breaks the day-zero sentinel hash `895EED53A280A470` (from Phase 2). The replay harness re-generates a new baseline. The successor work is allowed to drift the hash; the multi-ticker-foundation invariant explicitly applied to Phase 1→2 only.

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

## 6. Extended thinking

### 6.1 Wire-up

`ChatOptions.AdditionalProperties` gains `"anthropic.thinking"` with the budget read from settings:

```csharp
options.AdditionalProperties = new AdditionalPropertiesDictionary
{
    ["anthropic.thinking"] = new { type = "enabled", budget_tokens = ai.ThinkingBudget }
};
```

(Exact key name verified against `Anthropic.SDK 5.10` — adjust if the SDK exposes a typed surface.)

### 6.2 Response handling

The response contains zero or more thinking blocks before the tool call. `Microsoft.Extensions.AI` surfaces these as `TextContent` items with a marker in `AdditionalProperties` (`"anthropic.thinking"` = true). `SuggestionService` walks the response contents, concatenates all thinking text, and assigns to the captured `Suggestion.ThinkingText`. If no thinking blocks are present (model chose not to use the budget), `ThinkingText` is null.

### 6.3 UI surface

Out of scope for this spec. The data is captured; rendering it on the suggestion card behind a `<details>` disclosure is a follow-up trivial change.

## 7. Replay CLI

### 7.1 Command shape

In `TradyStrat.Cli/Commands/ReplayCommand.cs`:

```
Usage: dotnet run --project TradyStrat.Cli -- replay [OPTIONS]

Options:
  --instrument <ticker>        Instrument ticker (required, e.g. CON3.L)
  --since <yyyy-MM-dd>         Inclusive start date (default: 90 days back)
  --until <yyyy-MM-dd>         Inclusive end date (default: today UTC)
  --persist                    Write replayed suggestions to DB (default: false, dry-run scoring only)
  --prompt-hash <hex>          Filter the comparison row to suggestions tagged with this hash
```

The placeholder `HelloCommand` from the refactor is removed.

### 7.2 What it does

For each `DateOnly` in the date range, in order:

1. Call `IAiSnapshotService.CreateAsync(instrumentId, date, ct)` to build the historical snapshot.
2. Call `IAiClient.AskAsync(snapshot, ct)` to get a fresh suggestion.
3. Compute the **score** for that suggestion using the `WasCorrect` rule (§4.2 step 4), reading forward 5 trading days from `date`.
4. If `--persist` is set, write the suggestion to DB. Otherwise discard.

After the loop, compute aggregates and render to stdout.

### 7.3 Output

A single `Spectre.Console.Table`:

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
Prompt hash range:         A3F2..., B19E..., 4C77...  (3 distinct)
Range: 2026-02-13 → 2026-05-13 · Instrument: CON3.L · Dry-run
```

Conviction-weighted score formula: `Σ (conviction_i × (was_correct_i ? 1 : 0)) / Σ conviction_i`. Single number in `[0, 1]`.

### 7.4 DI in the CLI

`TradyStrat.Cli/Program.cs` builds an `IHost`, registers Application + Infrastructure modules via `AppManager.Start(services, config, applicationModulesAssembly, infrastructureModulesAssembly)`, then constructs a `CommandApp` with a `Spectre.Console.Cli.ITypeRegistrar` adapter that wraps `host.Services`. The `replay` command receives `IAiSnapshotService`, `IAiClient`, `IRepositoryBase<Suggestion>`, `IRepositoryBase<Instrument>`, `IClock`, `IPriceHistoryReader` (or whatever the price-close port is named after the refactor) via constructor injection.

### 7.5 Cost behaviour

`--persist false` (default) still costs Anthropic input tokens — there's no way to score without calling the model. With caching from §5, replaying the same 60 days twice in a row is much cheaper on the second run because the envelope hits cache for the bulk of each day's call.

## 8. Testing

### 8.1 Unit tests (Application.Tests)

- `AiSnapshotServiceTests` extended:
    - Past-suggestion rows produce expected `WasCorrect` for each action class given fixture forward returns.
    - 30-day window is honoured (rows older than 30 trading days are dropped).
    - `RationaleHeadline` truncates at 80 chars, no ellipsis added.
    - `NetTradeFlowEur` is null when no trade in window, negative for net buys, positive for net sells, and zero when buys and sells exactly cancel.
    - Recent suggestions where forward window doesn't fit yet are included with `WasCorrect = false` (regression test).
    - `EnvelopeHash` and `PromptHash` are deterministic for fixed inputs.
- `SuggestionServiceTests` extended:
    - User message has exactly two content parts.
    - First part carries the `cache_control` additional-property; second does not.
    - `ChatOptions.AdditionalProperties` carries `thinking` with the configured budget.
    - When response contains thinking blocks, `Suggestion.ThinkingText` is populated; when absent, it's null.

### 8.2 Infrastructure.Tests

- New `AiSuggestionPhase3MigrationTests` mirrors the existing Phase 2 migration test: starts from a Phase 2 DB, applies the new migration, asserts column added and existing rows have NULL `ThinkingText`.
- `MigrationBackwardCompatTests` extended to assert old DBs (pre-Phase-3) still load.

### 8.3 E2E.Tests

- `ModuleSmokeTests` extended to assert `IAiClient.AskAsync` still completes against `FakeChatClient` end-to-end after wiring changes.
- New `ReplayCommandSmokeTest` runs the CLI with `--since` = `--until` against an in-memory fixture and asserts non-zero exit code on validation errors, zero on success, and table is rendered.

### 8.4 No live Anthropic calls

All tests use `FakeChatClient` (already exists) or `StubAiClient`. The replay command's CI path uses `StubAiClient`.

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
| `Microsoft.Extensions.AI`'s `AdditionalProperties` doesn't actually emit `cache_control` on the wire for this `Anthropic.SDK` version | Verified at spec-time; if it regresses on a SDK bump, fall back to raw `Anthropic.SDK` in `SuggestionService` (isolated change). |
| Extended thinking budget burns more output tokens than expected | Setting is editable; lower the default if production usage shows blowouts. |
| Forward-return computation drifts depending on how a "trading day" is defined in the price store | Use the existing price-bar iterator; if calendar gaps appear in fixtures, treat as missing and skip the row. |
| Replay harness double-charges if `--persist` is set on a day that already has a suggestion | The use case has a uniqueness constraint (`InstrumentId`, `ForDate`). Replay either deletes the existing row first when `--persist --force` is supplied, or skips that date. Default `--persist` without `--force` errors loudly. |
| Replay's cache benefit doesn't materialise because each replay day primes its own envelope | Acceptable for v1. The cache primarily helps multi-instrument days and re-runs of the *same* day during tuning, not new-day replays. |
| `EnvelopeHash` makes the existing `PromptHash` sentinel test fail | Acceptable — the multi-ticker spec's no-regression invariant was scoped to Phase 1→2 and this is a deliberate evolution. New baselines captured in the implementing PR. |
