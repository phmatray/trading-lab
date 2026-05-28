# LLM Call Strategy: Pluggable Instruct / Reasoning Models

**Date:** 2026-05-28
**Status:** Design approved, pending implementation plan

## Motivation

`LlmSignalGenerator` is currently hard-wired to the response shape of instruct models (Gemma, Qwen2.5-instruct): it reads `choices[0].message.content` via `Microsoft.Extensions.AI`'s `IChatClient` and uses strict JSON-schema response formatting.

Probing `qwen/qwen3.6-35b-a3b` in LM Studio showed:

- With JSON-schema response_format on, LM Studio routes the structured JSON to `reasoning_content` and leaves `content` empty. The current pipeline parses an empty string and degrades every signal to `RawSignal(Hold, 0d, "parse_failure")`. The backtest produces zero trades on every strategy.
- With schema dropped and a "think then output JSON" prompt, the model produces a clean ~900-token chain-of-thought in `reasoning_content` and a fenced JSON answer in `content` (which the existing parser already handles). Per-call latency rises from ~1.5s (Gemma 4B) to ~23s.

We want to keep the existing instruct path working unchanged while adding a parallel path that:

1. Talks to LM Studio in a way the reasoning model actually answers on.
2. Captures the reasoning trace as a first-class artifact alongside the signal, available for post-hoc analysis (prompt tuning, indicator selection, trade explanation).

## Goals

- A pluggable strategy seam so additional model families (reasoning, future hybrid models) can be added without touching `LlmSignalGenerator` again.
- Drop-in support for `qwen/qwen3.6-35b-a3b` (and similar Qwen3 thinking models) via a config flip.
- Persist the reasoning trace alongside each prediction and inside the LLM response cache.
- Zero behavior change for the existing Gemma / Qwen2.5-instruct configurations.

## Non-goals

- Removing `Microsoft.Extensions.AI` from the project.
- Auto-detecting model family from the model id.
- Including reasoning traces inside few-shot examples (would multiply prompt tokens).
- Streaming reasoning_content as it's produced.
- Live ALTER-TABLE migration of existing `predictions.db` files. The PoC's predictions DB is per-run and gitignored; a release note is sufficient.
- Multi-strategy in the same run (e.g. instruct for some segments, reasoning for others).

## Approach summary

Introduce `ILlmCallStrategy` — a small abstraction that owns one full provider call (request build + transport + response parse + per-call retry) and returns both a `RawSignal` and an optional reasoning trace.

`LlmSignalGenerator` keeps everything that's strategy-agnostic (few-shot prompt assembly, cache key, cache lookup, cache write) and delegates the per-call work to the injected strategy.

Two implementations ship in this change:

- **`InstructCallStrategy`** — wraps the existing `IChatClient`. Owns the current schema-first → plain-fallback retry shape. Returns `LlmCallOutcome(signal, ReasoningContent: null)`. No behavioral change.
- **`ReasoningCallStrategy`** — owns a typed `HttpClient` and POSTs raw OpenAI-compat JSON to `/chat/completions`. No `response_format`. Reads `content` for the signal JSON and `reasoning_content` for the trace. Retry adds a stricter "JSON only on the final line" reminder.

DI selects the strategy from a new `LmStudio.ModelFamily` config field (`"instruct"` default, `"reasoning"` for thinking models).

## Component design

### New types

```csharp
// src/TradingSignal.Llm/Abstractions/ILlmCallStrategy.cs
public interface ILlmCallStrategy
{
    Task<LlmCallOutcome> GenerateAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct);
}

public sealed record LlmCallOutcome(RawSignal Signal, string? ReasoningContent);
```

```csharp
// src/TradingSignal.Llm/Strategies/InstructCallStrategy.cs
internal sealed partial class InstructCallStrategy : ILlmCallStrategy
{
    // ctor: IChatClient, LmStudioOptions, ILogger<InstructCallStrategy>?

    public async Task<LlmCallOutcome> GenerateAsync(string sys, string user, CancellationToken ct)
    {
        var s1 = await TryOnce(sys, user, useSchema: true,  stricter: false, ct);
        if (s1 is not null) return new(s1, null);

        var s2 = await TryOnce(sys, user, useSchema: false, stricter: true, ct);
        return new(s2 ?? new RawSignal(TradeAction.Hold, 0d, "parse_failure"), null);
    }

    // TryOnce body lifts from current LlmSignalGenerator.TryOnceAsync verbatim.
}
```

```csharp
// src/TradingSignal.Llm/Strategies/ReasoningCallStrategy.cs
internal sealed partial class ReasoningCallStrategy : ILlmCallStrategy
{
    // ctor: HttpClient (BaseAddress = endpoint), LmStudioOptions, ILogger?
    // Posts:
    //   { "model": <id>, "messages": [...], "max_tokens": <opts.MaxOutputTokens>,
    //     "temperature": 0.2, "reasoning_effort": <opts.ReasoningEffort> }
    // Reads choices[0].message.{content, reasoning_content}.
    // Parses content with SignalResponseParser.
    // On parse failure, retries once with an appended user message:
    //   "Return ONLY a single JSON object on the final line ..."
    // Returns reasoning_content even on parse failure (debugging value).
}
```

### Edits

| File | Edit |
|---|---|
| `src/TradingSignal.Llm/LlmSignalGenerator.cs` | Constructor takes `ILlmCallStrategy` instead of `IChatClient`. `TryOnceAsync` and the chat-options-building code are removed. New body: prompt → cache key → cache lookup → `strategy.GenerateAsync` → attach `Reasoning` → cache write → return. |
| `src/TradingSignal.Llm/LmStudioOptions.cs` | Add `string ModelFamily { get; set; } = "instruct";` and `string ReasoningEffort { get; set; } = "medium";`. |
| `src/TradingSignal.Core/RawSignal.cs` | Add `string? Reasoning = null` as the last positional property (default makes existing call sites source-compatible). |
| `src/TradingSignal.Evaluation/Stores/SqlitePredictionStore.cs` | Add `reasoning TEXT NULL` to the `CREATE TABLE IF NOT EXISTS`. INSERT parameter list and SELECT projection both include `reasoning`. Row class adds `string? Reasoning`. Mapper reads it into `RawSignal`. |
| `src/TradingSignal.Llm/Caching/SqliteLlmResponseCache.cs` | Same column addition. `TryGetAsync` reads `reasoning`; `SetAsync` writes it. Continues to return `RawSignal?`. |
| `src/TradingSignal.Console/Configuration/*` | Mirror new fields in the `LmStudioConfig` record bound from appsettings. |
| `src/TradingSignal.Console/Program.cs` | DI: `services.AddSingleton<ILlmCallStrategy>(sp => ...)` switches on `appConfig.LmStudio.ModelFamily`. Reasoning branch builds a typed `HttpClient` with `BaseAddress = Endpoint` and `Timeout = TimeSpan.FromSeconds(TimeoutSeconds)`. Instruct branch keeps using the existing `IChatClient` registration. |
| `src/TradingSignal.Console/appsettings.json` | Add `"ModelFamily": "instruct"` under `LmStudio`. |
| `src/TradingSignal.Console/appsettings.demo.json` | Add `"ModelFamily": "instruct"` under `LmStudio`. |
| `README.md` | Document `ModelFamily` and `ReasoningEffort`. Note that switching to a reasoning model requires deleting `runs/predictions.db` and `runs/llm-cache.db` from prior runs (column-add doesn't ALTER existing files). |

### Test plan

- `LlmSignalGeneratorTests` shrinks to orchestration coverage only, using a `FakeLlmCallStrategy`:
  - Cache hit short-circuits the strategy (strategy never called).
  - Cache miss → strategy called once → outcome stored → second call hits cache.
  - `Reasoning` from outcome is propagated into the returned `RawSignal` and persisted to cache.
  - Different features produce different cache keys.
- `InstructCallStrategyTests` (new) inherits the existing retry-shape coverage using the existing `FakeChatClient`:
  - Schema constraint on first attempt, dropped on retry; stricter reminder on retry.
  - Parses success; garbage → retry → success; both attempts fail → `Hold parse_failure`; HTTP error → `Hold parse_failure`.
- `ReasoningCallStrategyTests` (new) uses a fake `HttpMessageHandler` returning canned JSON bodies:
  - Clean `content` + `reasoning_content` → `LlmCallOutcome(parsed signal, trace)`.
  - Empty `content`, non-empty `reasoning_content` → `parse_failure` with trace preserved.
  - Fenced JSON inside prose `content` parses correctly (real-shape fixture from the curl probe).
  - Stricter retry happens on first-call parse failure.
  - HTTP 500 or transport exception → `parse_failure` outcome, no throw.
  - Outgoing request body asserts `reasoning_effort` and absence of `response_format`.
- `SqlitePredictionStoreTests` — one new round-trip test for a `Prediction` whose `Signal.Reasoning` is a long string.
- `SqliteLlmResponseCacheTests` — one new round-trip test for `RawSignal` with non-null `Reasoning`.

## Data flow

```
LlmSignalGenerator.GenerateAsync(features, fewShot, ct)
  systemPrompt = PromptBuilder.SystemPrompt
  userMessage  = PromptBuilder.BuildUserMessage(features, fewShot, options.MaxFewShot)
  key          = SHA256(modelId + systemPrompt + userMessage)

  cached       = cache.TryGetAsync(key, ct)
  if cached: return cached                        # includes Reasoning if previously stored

  outcome      = strategy.GenerateAsync(systemPrompt, userMessage, ct)
  final        = outcome.Signal with { Reasoning = outcome.ReasoningContent }
  cache.SetAsync(key, final, ct)
  return final
```

Cache key intentionally does **not** include `ModelFamily` or `ReasoningEffort`; `modelId` already implies the family, and bumping effort is a deliberate behavior change that should invalidate the cache manually (delete `runs/llm-cache.db`).

## Schema changes

`predictions` table:

```sql
CREATE TABLE IF NOT EXISTS predictions (
    id TEXT PRIMARY KEY,
    as_of_utc TEXT NOT NULL,
    symbol TEXT NOT NULL,
    segment INTEGER NOT NULL,
    action INTEGER NOT NULL,
    confidence REAL NOT NULL,
    reason TEXT NOT NULL,
    reasoning TEXT NULL,
    features_json TEXT NOT NULL
);
```

`llm_cache` table (column-add only; column names line up with the existing schema):

```sql
CREATE TABLE IF NOT EXISTS llm_cache (
    key TEXT PRIMARY KEY,
    action INTEGER NOT NULL,
    confidence REAL NOT NULL,
    reason TEXT NOT NULL,
    reasoning TEXT NULL,
    created_at TEXT NOT NULL
);
```

Both tables continue to use `CREATE TABLE IF NOT EXISTS`. Users running a fresh PoC see no migration. Users with an existing run must delete `runs/predictions.db` and `runs/llm-cache.db` before running with the new code; this is called out in the README. The user-facing impact is acceptable because the predictions DB is per-run and gitignored.

## Config changes

`LmStudio` section (defaults shown):

```json
{
  "LmStudio": {
    "Endpoint": "http://localhost:1234/v1",
    "ModelId": "qwen2.5-14b-instruct",
    "ModelFamily": "instruct",
    "ReasoningEffort": "medium",
    "TimeoutSeconds": 60,
    "MaxFewShot": 3,
    "MaxOutputTokens": 256
  }
}
```

`ModelFamily` accepted values: `"instruct"` (default), `"reasoning"`. Unknown values fall back to instruct with a one-time warning at startup.

`ReasoningEffort` accepted values: `"none"`, `"low"`, `"medium"` (default), `"high"`. Only honored by `ReasoningCallStrategy`; `InstructCallStrategy` ignores it.

For reasoning-model runs, the recommended `MaxOutputTokens` is `2048` (the probe used 1103 tokens for one signal). This is a config decision per run, not a code default — `LmStudioOptions.MaxOutputTokens` keeps its current 256 default.

## Error handling

- `InstructCallStrategy` preserves current behavior: any provider exception other than `OperationCanceledException` is logged at Warning and treated as a parse miss for that attempt.
- `ReasoningCallStrategy` does the same: `HttpRequestException`, `TaskCanceledException` (timeout, not caller cancellation), and `JsonException` are logged at Warning and counted as parse misses. Caller cancellation propagates.
- After both attempts fail, the strategy returns `RawSignal(Hold, 0d, "parse_failure")`. `ReasoningCallStrategy` still surfaces whatever `reasoning_content` the model produced before failing, so the trace is available for debugging via the predictions DB and cache.

## Migration / rollout

This change is internal to a PoC and has no external consumers. Rollout is:

1. Land the implementation behind the new code path; default `ModelFamily=instruct` means existing runs are unchanged.
2. Add README guidance for the reasoning model.
3. Manual smoke test:
   - Gemma demo run: same numbers as before (zero behavior change).
   - Qwen reasoning demo run: non-empty `reasoning` column on every prediction; trades > 0 on llm-only (confidence distribution will differ from instruct).

Cache + predictions DBs from prior runs must be deleted before testing the reasoning path. The README will say so; an alternative is to wipe `runs/` for any new run, which the orchestrator already tolerates.

## Open questions

None blocking. Two soft choices the implementation plan can finalize:

- Whether `InstructCallStrategy` and `ReasoningCallStrategy` should be `internal` to `TradingSignal.Llm` (tests use `InternalsVisibleTo`) or `public`. Default: `internal`, since they're DI-only details, with `InternalsVisibleTo` for the test project.
- Whether to expose an `IHttpClientFactory` registration for the reasoning client or build the `HttpClient` directly in DI. Default: direct `HttpClient` singleton — the Console app is short-lived, the abstraction adds no value here, and tests inject `HttpClient` constructed from a fake `HttpMessageHandler`.
