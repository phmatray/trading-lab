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
- Multi-strategy in the same run (e.g. instruct for some segments, reasoning for others).

## Approach summary

Introduce `ILlmCallStrategy` — a small abstraction that owns one full provider call (request build + transport + response parse + per-call retry) and returns both a `RawSignal` and an optional reasoning trace.

`LlmSignalGenerator` keeps everything that's strategy-agnostic (few-shot prompt assembly, cache key, cache lookup, cache write) and delegates the per-call work to the injected strategy. The strategy also owns the system prompt — instruct and reasoning strategies use different system prompts (see C1 below), so the cache key naturally diverges by strategy.

Two implementations ship in this change:

- **`InstructCallStrategy`** — wraps the existing `IChatClient`. Owns the current schema-first → plain-fallback retry shape. Uses `PromptBuilder.SystemPromptInstruct` (the current text). Returns `LlmCallOutcome(signal, ReasoningContent: null)`. No behavioral change.
- **`ReasoningCallStrategy`** — owns a typed `HttpClient` and POSTs raw OpenAI-compat JSON to `/chat/completions`. No `response_format`. Reads `content` for the signal JSON and `reasoning_content` for the trace. Uses `PromptBuilder.SystemPromptReasoning` (a new prompt that permits thinking and instructs "JSON on the final line"). Retry adds a stricter "JSON only on the final line" reminder.

Strategy selection lives behind a public extension method `AddLlmCallStrategy(this IServiceCollection, AppConfig, ILogger?)` in `TradingSignal.Llm`. The extension switches on `LmStudio.ModelFamily` ("instruct" default, "reasoning" for thinking models) and registers the right `ILlmCallStrategy` singleton. The concrete strategy classes stay `internal` to `TradingSignal.Llm`; the test project gets access via `InternalsVisibleTo`.

## Component design

### New types

```csharp
// src/TradingSignal.Llm/Abstractions/ILlmCallStrategy.cs
public interface ILlmCallStrategy
{
    string SystemPrompt { get; }

    Task<LlmCallOutcome> GenerateAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct);
}

public sealed record LlmCallOutcome(RawSignal Signal, string? ReasoningContent);
```

The strategy exposes its `SystemPrompt` so `LlmSignalGenerator` can use it for both the request and the cache key. The string parameter on `GenerateAsync` is the same prompt the generator just read from `SystemPrompt` — the strategy receives it explicitly to keep the call site self-contained and testable.

```csharp
// src/TradingSignal.Llm/Strategies/InstructCallStrategy.cs
internal sealed partial class InstructCallStrategy : ILlmCallStrategy
{
    // ctor: IChatClient, LmStudioOptions, ILogger<InstructCallStrategy>?
    public string SystemPrompt => PromptBuilder.SystemPromptInstruct;

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
    public string SystemPrompt => PromptBuilder.SystemPromptReasoning;

    // Posts:
    //   { "model": <id>, "messages": [{role:system,content:sys},{role:user,content:user}],
    //     "max_tokens": <opts.MaxOutputTokens>, "temperature": 0.2,
    //     "reasoning_effort": <opts.ReasoningEffort> }
    // Reads choices[0].message.{content, reasoning_content}.
    // Parses content with SignalResponseParser (already handles fenced JSON + prose).
    // On parse failure, retries once with an appended user message:
    //   "Return ONLY a single JSON object on the final line ..."
    // Returns reasoning_content even on parse failure (debugging value).
}
```

```csharp
// src/TradingSignal.Llm/PromptBuilder.cs (split the current static class)
public static class PromptBuilder
{
    public const string SystemPromptInstruct = /* current SystemPrompt text — unchanged */;

    public const string SystemPromptReasoning =
        """
        You are a disciplined crypto trading signal generator.
        You may think step-by-step about the indicators before answering. After your
        reasoning, output exactly ONE JSON object on the final line matching:
          { "action": "BUY" | "SELL" | "HOLD", "confidence": 0.0..1.0, "reason": "string" }

        Rules:
        - The final line must be valid JSON only — no prose, no markdown, no code fences after the JSON.
        - "confidence" must be in [0, 1] and reflect your subjective probability that the
          chosen action will be profitable net of typical transaction fees over the next bar.
        - Prefer HOLD when signals conflict or are weak. Doing nothing is a valid action.
        - "reason" is one short sentence summarizing your conclusion.
        """;

    public static string BuildUserMessage(/* unchanged */) { /* ... */ }
}
```

```csharp
// src/TradingSignal.Llm/ServiceCollectionExtensions.cs (new)
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLlmCallStrategy(
        this IServiceCollection services,
        LmStudioOptions options,
        ILogger? startupLogger = null)
    {
        var family = (options.ModelFamily ?? "instruct").Trim().ToLowerInvariant();
        if (family != "instruct" && family != "reasoning")
        {
            startupLogger?.LogWarning("Unknown ModelFamily '{Family}', falling back to 'instruct'.", options.ModelFamily);
            family = "instruct";
        }

        if (family == "reasoning")
        {
            services.AddSingleton(sp => new HttpClient
            {
                BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
            });
            services.AddSingleton<ILlmCallStrategy, ReasoningCallStrategy>();
        }
        else
        {
            // InstructCallStrategy depends on IChatClient registered separately in Program.cs.
            services.AddSingleton<ILlmCallStrategy, InstructCallStrategy>();
        }

        return services;
    }
}
```

### Edits

| File | Edit |
|---|---|
| `src/TradingSignal.Llm/LlmSignalGenerator.cs` | Constructor takes `ILlmCallStrategy` instead of `IChatClient`. `TryOnceAsync` and the chat-options-building code are removed. New body: read `strategy.SystemPrompt` → build userMessage → compute cache key (including `strategy.SystemPrompt` and `options.ReasoningEffort`) → cache lookup → `strategy.GenerateAsync` → attach `Reasoning` → cache write → return. |
| `src/TradingSignal.Llm/LmStudioOptions.cs` | Add `string ModelFamily { get; set; } = "instruct";` and `string ReasoningEffort { get; set; } = "medium";`. |
| `src/TradingSignal.Llm/Prompts/PromptBuilder.cs` | Rename `SystemPrompt` constant to `SystemPromptInstruct` (unchanged text). Add new `SystemPromptReasoning` constant (drops the "Do not reveal chain-of-thought" rule, instructs final-line JSON). Existing `BuildUserMessage` is unchanged. |
| `src/TradingSignal.Llm/ServiceCollectionExtensions.cs` | **New.** Public `AddLlmCallStrategy(this IServiceCollection, LmStudioOptions, ILogger?)` extension method described above. |
| `src/TradingSignal.Llm/TradingSignal.Llm.csproj` | Add `<InternalsVisibleTo Include="TradingSignal.Llm.Tests" />` so test code can construct `InstructCallStrategy` / `ReasoningCallStrategy` directly. |
| `src/TradingSignal.Core/RawSignal.cs` | Add `string? Reasoning = null` as the last positional property (default makes existing call sites source-compatible). |
| `src/TradingSignal.Evaluation/Stores/SqlitePredictionStore.cs` | Add `reasoning TEXT NULL` to the `CREATE TABLE IF NOT EXISTS`, **plus** an idempotent `ALTER TABLE predictions ADD COLUMN reasoning TEXT NULL` wrapped in `try/catch SqliteException` for forward-compatibility with pre-existing DBs. INSERT parameter list and SELECT projection both include `reasoning`. Row class adds `string? Reasoning`. Mapper reads it into `RawSignal`. |
| `src/TradingSignal.Llm/Caching/SqliteLlmResponseCache.cs` | Same column addition with same idempotent `ALTER TABLE` migration. `TryGetAsync` reads `reasoning`; `SetAsync` writes it. Continues to return `RawSignal?`. |
| `src/TradingSignal.Console/Configuration/AppConfig.cs` | Mirror new fields in `LmStudioConfig` (`ModelFamily`, `ReasoningEffort` with the same defaults as `LmStudioOptions`). |
| `src/TradingSignal.Console/Program.cs` | Restructure the DI registrations in `BuildHost`'s `ConfigureServices` callback: (a) replace the lazy `LmStudioOptions` factory with an eager construction so `ModelFamily` is known at registration time; the new fields `ModelFamily` and `ReasoningEffort` are copied from `LmStudioConfig`. (b) Register `IChatClient` conditionally (only when `ModelFamily=="instruct"`) — this avoids constructing an unused client when running the reasoning path. (c) Call `services.AddLlmCallStrategy(lmOptions, startupLogger)`. (d) Change the `LlmSignalGenerator` registration to inject `ILlmCallStrategy` instead of `IChatClient`. Concrete shape: <br><br>`AppConfig appConfig = new(); configuration.Bind(appConfig); services.AddSingleton(appConfig);`<br>`LmStudioOptions lmOptions = MapFromConfig(appConfig.LmStudio); services.AddSingleton(lmOptions);`<br>`if (lmOptions.IsInstruct()) services.AddSingleton(_ => LmStudioChatClientFactory.Create(lmOptions));`<br>`services.AddLlmCallStrategy(lmOptions, startupLogger);`<br>`services.AddSingleton<ISignalGenerator, LlmSignalGenerator>();` |
| `src/TradingSignal.Console/appsettings.json` | Add `"ModelFamily": "instruct"` and `"ReasoningEffort": "medium"` under `LmStudio` (explicit, so demo flips are visible in diffs). |
| `src/TradingSignal.Console/appsettings.demo.json` | Same additions. |
| `README.md` | Document `ModelFamily` and `ReasoningEffort`. Note that switching to `ModelFamily=reasoning` typically requires bumping `MaxOutputTokens` to 2048+ and `TimeoutSeconds` to 120+ (probe took 1-25s per call at `effort=medium`). Note that the cache and predictions DBs auto-migrate; switching `ModelFamily` or `ReasoningEffort` between runs automatically invalidates the relevant cache entries (both are part of the cache key — no manual wipe needed). |

### Test plan

- `LlmSignalGeneratorTests` shrinks to orchestration coverage only, using a `FakeLlmCallStrategy`:
  - Cache hit short-circuits the strategy (strategy never called).
  - Cache miss → strategy called once → outcome stored → second call hits cache.
  - `Reasoning` from outcome is propagated into the returned `RawSignal` AND persisted to cache (round-trip `Reasoning="thinking trace"`).
  - The strategy's `SystemPrompt` is used in cache-key computation (two strategies with the same `userMessage` but different `SystemPrompt` produce different keys).
  - Different features produce different cache keys.
- `InstructCallStrategyTests` (new) inherits the existing retry-shape coverage using the existing `FakeChatClient`:
  - Schema constraint on first attempt, dropped on retry; stricter reminder on retry.
  - Parses success; garbage → retry → success; both attempts fail → `Hold parse_failure`; HTTP error → `Hold parse_failure`.
  - `SystemPrompt` property returns `PromptBuilder.SystemPromptInstruct`.
- `ReasoningCallStrategyTests` (new) uses a fake `HttpMessageHandler` returning canned JSON bodies:
  - Clean `content` + `reasoning_content` → `LlmCallOutcome(parsed signal, trace)`.
  - Empty `content`, non-empty `reasoning_content` → `parse_failure` with trace preserved.
  - Fenced JSON inside prose `content` parses correctly (real-shape fixture captured from the live probe).
  - Stricter retry happens on first-call parse failure.
  - HTTP 500, transport `HttpRequestException`, and `JsonException` on malformed body → `parse_failure` outcome, no throw.
  - Caller cancellation propagates (`OperationCanceledException` with `ct.IsCancellationRequested == true` is rethrown); HttpClient-internal `TaskCanceledException` (timeout) is logged and degrades to `parse_failure`.
  - Outgoing request body asserts `reasoning_effort` value matches config and `response_format` is absent.
  - `SystemPrompt` property returns `PromptBuilder.SystemPromptReasoning`.
- `ServiceCollectionExtensionsTests` (new):
  - `ModelFamily="instruct"` resolves `ILlmCallStrategy` to `InstructCallStrategy`.
  - `ModelFamily="reasoning"` resolves to `ReasoningCallStrategy` and registers an `HttpClient` with the configured `BaseAddress` and `Timeout`.
  - Unknown `ModelFamily` value resolves to `InstructCallStrategy` and emits a warning log entry.
- `SqlitePredictionStoreTests` — two new tests: (a) round-trip a `Prediction` whose `Signal.Reasoning` is a long string; (b) opening a pre-existing DB without the `reasoning` column triggers the idempotent `ALTER TABLE` and a subsequent INSERT succeeds.
- `SqliteLlmResponseCacheTests` — two new tests with the same shape: (a) round-trip a `RawSignal` with non-null `Reasoning`; (b) ALTER migration of pre-existing DB.

## Data flow

```
LlmSignalGenerator.GenerateAsync(features, fewShot, ct)
  systemPrompt = strategy.SystemPrompt              # strategy-specific
  userMessage  = PromptBuilder.BuildUserMessage(features, fewShot, options.MaxFewShot)
  key          = SHA256(modelId + reasoningEffort + systemPrompt + userMessage)

  cached       = cache.TryGetAsync(key, ct)
  if cached: return cached                          # includes Reasoning if previously stored

  outcome      = strategy.GenerateAsync(systemPrompt, userMessage, ct)
  final        = outcome.Signal with { Reasoning = outcome.ReasoningContent }
  cache.SetAsync(key, final, ct)
  return final
```

The cache key now includes `options.ReasoningEffort` so that changing the effort knob between runs automatically invalidates prior entries — same observable cost as a manual cache wipe, but correctness is automatic. The system prompt baked into the key is strategy-specific, so switching `ModelFamily` between runs also naturally invalidates entries from the prior family even with the same `ModelId`. `InstructCallStrategy` ignores `ReasoningEffort` operationally, but the key still includes it; this is harmless because a config that pairs `ModelFamily=instruct` with a non-default `ReasoningEffort` is meaningless and not worth special-casing.

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
-- Plus idempotent migration for pre-existing DBs:
-- try { ALTER TABLE predictions ADD COLUMN reasoning TEXT NULL; } catch (SqliteException) { /* exists */ }
```

`llm_cache` table:

```sql
CREATE TABLE IF NOT EXISTS llm_cache (
    key TEXT PRIMARY KEY,
    action INTEGER NOT NULL,
    confidence REAL NOT NULL,
    reason TEXT NOT NULL,
    reasoning TEXT NULL,
    created_at TEXT NOT NULL
);
-- Plus same idempotent migration.
```

Both stores execute the `CREATE TABLE IF NOT EXISTS` followed by a guarded `ALTER TABLE` in their `EnsureInitializedAsync` path. The ALTER throws `SqliteException` when the column already exists; the catch handler swallows that specific case and lets other errors propagate. This means:

- Fresh PoCs: column comes in via the CREATE.
- Existing predictions/cache files from prior runs: column is added by the ALTER on first open, then INSERT/SELECT work normally — no cryptic "no such column" runtime errors, no user-visible migration step.

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

`ModelFamily` accepted values: `"instruct"` (default), `"reasoning"`. Unknown values fall back to instruct with a one-time warning at startup (logged via the startup logger passed into `AddLlmCallStrategy`).

`ReasoningEffort` accepted values: `"none"`, `"low"`, `"medium"` (default), `"high"`. Only honored operationally by `ReasoningCallStrategy`; `InstructCallStrategy` ignores it. Included in the cache key regardless (see "Data flow").

For reasoning-model runs, the recommended `MaxOutputTokens` is `2048` (the probe used 1103 tokens for one signal) and `TimeoutSeconds` is `120` (probe ran 1-25s per call at `effort=medium`; `effort=high` will skew longer). These are config decisions per run, not code defaults — `LmStudioOptions` defaults stay 256 / 60 to preserve instruct-path behavior.

## Error handling

- `InstructCallStrategy` preserves current behavior: any provider exception other than `OperationCanceledException` is logged at Warning and treated as a parse miss for that attempt.
- `ReasoningCallStrategy` uses the canonical cancellation-disambiguation pattern to distinguish caller-driven cancellation from HttpClient-internal timeout:

  ```csharp
  try
  {
      var response = await httpClient.PostAsync(...).ConfigureAwait(false);
      // parse...
  }
  catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
  catch (OperationCanceledException ex) { LogTimeout(ex); return ParseFailureWithTrace(); }
  catch (HttpRequestException ex)       { LogTransport(ex); return ParseFailureWithTrace(); }
  catch (JsonException ex)              { LogMalformed(ex); return ParseFailureWithTrace(); }
  ```

  Caller cancellation rethrows so the walk-forward orchestrator can stop cleanly; the HttpClient's internal timeout, transport errors, and malformed-body responses all degrade to a `parse_failure` outcome with whatever `reasoning_content` was already captured (best-effort trace preservation).
- After both attempts fail, both strategies return `RawSignal(Hold, 0d, "parse_failure")`. `ReasoningCallStrategy`'s `LlmCallOutcome.ReasoningContent` carries any trace the model produced before failing, so it ends up in the cache and the predictions DB for offline debugging.

## Migration / rollout

This change is internal to a PoC and has no external consumers. Rollout is:

1. Land the implementation behind the new code path; default `ModelFamily=instruct` means existing runs are unchanged.
2. The idempotent `ALTER TABLE` migration means existing `runs/predictions.db` and `runs/llm-cache.db` files keep working — no user-visible wipe required.
3. Add README guidance for the reasoning model: when to bump `MaxOutputTokens` and `TimeoutSeconds`; note that switching `ReasoningEffort` between runs invalidates cache entries by design.
4. Manual smoke test:
   - Gemma demo run: same numbers as before (zero behavior change).
   - Qwen reasoning demo run: non-empty `reasoning` column on every prediction; trades > 0 on llm-only (confidence distribution will differ from instruct).

## Open questions

None remaining. Earlier soft choices have been resolved:

- Strategies are `internal` to `TradingSignal.Llm`; Program.cs uses the public `AddLlmCallStrategy` extension method. `InternalsVisibleTo` exposes them to the test project.
- `HttpClient` is registered as a direct singleton inside the extension method (no `IHttpClientFactory`). The Console app is short-lived, the abstraction adds no value, and tests inject `HttpClient` constructed from a fake `HttpMessageHandler` via the strategy's constructor.
