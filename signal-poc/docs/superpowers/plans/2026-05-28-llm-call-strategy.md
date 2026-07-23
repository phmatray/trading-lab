# LLM Call Strategy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a pluggable `ILlmCallStrategy` seam so the trading-signal pipeline can target instruct models (Gemma, Qwen2.5-instruct — current behavior) or Qwen3 thinking models that emit chain-of-thought into `reasoning_content`. The reasoning trace is captured into a new `RawSignal.Reasoning` field, persisted alongside predictions and cached responses.

**Architecture:** `LlmSignalGenerator` keeps orchestration (few-shot prompt assembly, cache key, cache I/O) and delegates the per-call work to an `ILlmCallStrategy`. `InstructCallStrategy` wraps the existing `IChatClient`; `ReasoningCallStrategy` POSTs raw OpenAI-compat JSON via `HttpClient` so it can read `reasoning_content`. Strategy selection lives behind a public `AddLlmCallStrategy(...)` extension method driven by `LmStudio.ModelFamily` config. SQLite schemas gain a `reasoning` column with an idempotent `ALTER TABLE` migration so pre-existing DBs auto-upgrade.

**Tech Stack:** .NET 10, C# 12, Microsoft.Extensions.AI 10.6.0, Microsoft.Data.Sqlite, Dapper, OpenAI SDK (for the existing instruct chat client), xUnit, Shouldly.

**Reference spec:** `docs/superpowers/specs/2026-05-28-llm-call-strategy-design.md`. Re-read it whenever a task description feels under-specified.

**Working tree assumption:** Branch is clean before starting. Each task ends with a commit. Run `dotnet build TradingSignalPoc.slnx` and `dotnet test TradingSignalPoc.slnx` from the repo root.

---

### Task 1: Add `Reasoning` field to `RawSignal`

A pure additive record change. Default value keeps every existing call site source-compatible.

**Files:**
- Modify: `src/TradingSignal.Core/RawSignal.cs`

- [ ] **Step 1: Confirm baseline tests pass**

```bash
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -5
```

Expected: `Passed!` summary, 85 tests passing.

- [ ] **Step 2: Add the new field**

Edit `src/TradingSignal.Core/RawSignal.cs` to read exactly:

```csharp
namespace TradingSignal.Core;

public sealed record RawSignal(
    TradeAction Action,
    double Confidence,
    string Reason,
    string? Reasoning = null);
```

- [ ] **Step 3: Build and run tests**

```bash
dotnet build TradingSignalPoc.slnx --nologo 2>&1 | tail -3
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -5
```

Expected: 0 warnings, 0 errors, all 85 tests still pass — the new field defaults to `null` everywhere.

- [ ] **Step 4: Commit**

```bash
git add src/TradingSignal.Core/RawSignal.cs
git commit -m "Add optional Reasoning field to RawSignal"
```

---

### Task 2: Add `ModelFamily` and `ReasoningEffort` to `LmStudioOptions` and `LmStudioConfig`

Two new string properties on both the options object and the appsettings binding type.

**Files:**
- Modify: `src/TradingSignal.Llm/LmStudioOptions.cs`
- Modify: `src/TradingSignal.Console/Configuration/AppConfig.cs`

- [ ] **Step 1: Update `LmStudioOptions`**

Edit `src/TradingSignal.Llm/LmStudioOptions.cs` to add two properties below `MaxOutputTokens`:

```csharp
namespace TradingSignal.Llm;

public sealed class LmStudioOptions
{
    public string Endpoint { get; set; } = "http://localhost:1234/v1";

    public string ModelId { get; set; } = "qwen2.5-14b-instruct";

    public int TimeoutSeconds { get; set; } = 60;

    public int MaxFewShot { get; set; } = 3;

    public int MaxOutputTokens { get; set; } = 256;

    public string ModelFamily { get; set; } = "instruct";

    public string ReasoningEffort { get; set; } = "medium";
}
```

- [ ] **Step 2: Update `LmStudioConfig` (appsettings binding)**

Edit `src/TradingSignal.Console/Configuration/AppConfig.cs` — only the `LmStudioConfig` class. Add the same two properties:

```csharp
public sealed class LmStudioConfig
{
    public string Endpoint { get; set; } = "http://localhost:1234/v1";
    public string ModelId { get; set; } = "qwen2.5-14b-instruct";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxFewShot { get; set; } = 3;
    public int MaxOutputTokens { get; set; } = 256;
    public string ModelFamily { get; set; } = "instruct";
    public string ReasoningEffort { get; set; } = "medium";
}
```

- [ ] **Step 3: Build**

```bash
dotnet build TradingSignalPoc.slnx --nologo 2>&1 | tail -3
```

Expected: 0 warnings, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/TradingSignal.Llm/LmStudioOptions.cs src/TradingSignal.Console/Configuration/AppConfig.cs
git commit -m "Add ModelFamily and ReasoningEffort options"
```

---

### Task 3: Split `PromptBuilder.SystemPrompt` into instruct + reasoning constants

Rename the existing `SystemPrompt` constant to `SystemPromptInstruct` (text unchanged) and add a new `SystemPromptReasoning` constant that permits thinking. Fix the one consumer in `LlmSignalGenerator` so the build stays green.

**Files:**
- Modify: `src/TradingSignal.Llm/Prompts/PromptBuilder.cs`
- Modify: `src/TradingSignal.Llm/LlmSignalGenerator.cs:28` (rename usage)

- [ ] **Step 1: Update `PromptBuilder`**

In `src/TradingSignal.Llm/Prompts/PromptBuilder.cs`, replace the `SystemPrompt` constant with two constants:

```csharp
public const string SystemPromptInstruct =
    """
    You are a disciplined crypto trading signal generator.
    You will receive a snapshot of pre-computed technical indicators for a single asset
    and you must respond with exactly ONE JSON object matching the provided schema:
      { "action": "BUY" | "SELL" | "HOLD", "confidence": 0.0..1.0, "reason": "string" }

    Rules:
    - Output JSON only. No prose, no markdown, no code fences.
    - "confidence" must be in [0, 1] and reflect your subjective probability that the
      chosen action will be profitable net of typical transaction fees over the next bar.
    - Prefer HOLD when signals conflict or are weak. Doing nothing is a valid action.
    - "reason" is one short sentence. Do not reveal chain-of-thought.
    """;

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
```

- [ ] **Step 2: Update the one consumer in `LlmSignalGenerator`**

In `src/TradingSignal.Llm/LlmSignalGenerator.cs`, line 28 (or wherever `PromptBuilder.SystemPrompt` appears) — change references to `PromptBuilder.SystemPromptInstruct`. There is exactly one reference today (`ComputeCacheKey(options.ModelId, PromptBuilder.SystemPrompt, userMessage)` and `new(ChatRole.System, PromptBuilder.SystemPrompt)`). Replace both. After Task 6 this file no longer references the constant directly, but keep it working in the meantime.

```bash
grep -n "PromptBuilder.SystemPrompt\b" src/TradingSignal.Llm/LlmSignalGenerator.cs
```

For each match, change `PromptBuilder.SystemPrompt` to `PromptBuilder.SystemPromptInstruct`.

- [ ] **Step 3: Build and test**

```bash
dotnet build TradingSignalPoc.slnx --nologo 2>&1 | tail -3
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -5
```

Expected: build clean, all 85 tests still pass.

- [ ] **Step 4: Commit**

```bash
git add src/TradingSignal.Llm/Prompts/PromptBuilder.cs src/TradingSignal.Llm/LlmSignalGenerator.cs
git commit -m "Split PromptBuilder system prompt into Instruct and Reasoning variants"
```

---

### Task 4: Create `ILlmCallStrategy` and `LlmCallOutcome`

Pure additions; no consumers yet.

**Files:**
- Create: `src/TradingSignal.Llm/Abstractions/ILlmCallStrategy.cs`

- [ ] **Step 1: Create the directory and file**

```bash
mkdir -p src/TradingSignal.Llm/Abstractions
```

Write `src/TradingSignal.Llm/Abstractions/ILlmCallStrategy.cs` with exactly:

```csharp
using TradingSignal.Core;

namespace TradingSignal.Llm.Abstractions;

public interface ILlmCallStrategy
{
    /// <summary>
    /// The system prompt this strategy uses. Exposed so the orchestrator
    /// (LlmSignalGenerator) can include it in the cache key.
    /// </summary>
    string SystemPrompt { get; }

    Task<LlmCallOutcome> GenerateAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct);
}

public sealed record LlmCallOutcome(RawSignal Signal, string? ReasoningContent);
```

- [ ] **Step 2: Build**

```bash
dotnet build TradingSignalPoc.slnx --nologo 2>&1 | tail -3
```

Expected: 0 warnings, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/TradingSignal.Llm/Abstractions/ILlmCallStrategy.cs
git commit -m "Add ILlmCallStrategy abstraction and LlmCallOutcome record"
```

---

### Task 5: Extract `InstructCallStrategy` from `LlmSignalGenerator` and move its tests

**The biggest single task** — lift the retry-shape logic from `LlmSignalGenerator.TryOnceAsync` into a new `InstructCallStrategy`, expose it as `internal` to the test project, and move the five retry-shape tests into a new `InstructCallStrategyTests` file. `LlmSignalGenerator` is *not* yet refactored to use the strategy — that's Task 6 — so the new class is parallel scaffolding tested in isolation.

**Files:**
- Create: `src/TradingSignal.Llm/Strategies/InstructCallStrategy.cs`
- Modify: `src/TradingSignal.Llm/TradingSignal.Llm.csproj` (add InternalsVisibleTo)
- Create: `tests/TradingSignal.Llm.Tests/InstructCallStrategyTests.cs`

- [ ] **Step 1: Write the new failing test file**

```bash
mkdir -p src/TradingSignal.Llm/Strategies
```

Create `tests/TradingSignal.Llm.Tests/InstructCallStrategyTests.cs` with the lifted tests (these mirror the existing `LlmSignalGeneratorTests` cases that exercise retry behavior, plus a `SystemPrompt` property assertion):

```csharp
using Microsoft.Extensions.AI;
using Shouldly;
using TradingSignal.Core;
using TradingSignal.Llm.Prompts;
using TradingSignal.Llm.Strategies;
using TradingSignal.Llm.Tests.Fakes;

namespace TradingSignal.Llm.Tests;

public sealed class InstructCallStrategyTests
{
    private static LmStudioOptions Options() => new() { ModelId = "test-model", MaxOutputTokens = 128 };
    private const string Sys = "system";
    private const string User = "user";

    [Fact]
    public void SystemPrompt_Returns_Instruct_Constant()
    {
        FakeChatClient chat = new();
        InstructCallStrategy sut = new(chat, Options());
        sut.SystemPrompt.ShouldBe(PromptBuilder.SystemPromptInstruct);
    }

    [Fact]
    public async Task Parses_Successful_Response()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("""{"action":"SELL","confidence":0.62,"reason":"overbought"}""");
        InstructCallStrategy sut = new(chat, Options());

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Sell);
        outcome.Signal.Confidence.ShouldBe(0.62);
        outcome.ReasoningContent.ShouldBeNull();
        chat.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Retries_Once_When_First_Response_Is_Garbage()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("definitely not json");
        chat.EnqueueText("""{"action":"BUY","confidence":0.55,"reason":"retry win"}""");
        InstructCallStrategy sut = new(chat, Options());

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Buy);
        outcome.Signal.Reason.ShouldBe("retry win");
        chat.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Degrades_To_Hold_When_Both_Attempts_Fail()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("garbage one");
        chat.EnqueueText("garbage two");
        InstructCallStrategy sut = new(chat, Options());

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Hold);
        outcome.Signal.Confidence.ShouldBe(0d);
        outcome.Signal.Reason.ShouldBe("parse_failure");
        outcome.ReasoningContent.ShouldBeNull();
        chat.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Degrades_To_Hold_When_Llm_Throws_On_Both_Attempts()
    {
        FakeChatClient chat = new();
        chat.EnqueueError(new HttpRequestException("boom"));
        chat.EnqueueError(new HttpRequestException("boom again"));
        InstructCallStrategy sut = new(chat, Options());

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Hold);
        outcome.Signal.Reason.ShouldBe("parse_failure");
    }

    [Fact]
    public async Task Structured_Output_Used_On_First_Attempt_Only()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("garbage");
        chat.EnqueueText("""{"action":"HOLD","confidence":0.3,"reason":"calm"}""");
        InstructCallStrategy sut = new(chat, Options());

        await sut.GenerateAsync(Sys, User, CancellationToken.None);

        chat.ReceivedOptions.Count.ShouldBe(2);
        chat.ReceivedOptions[0]!.ResponseFormat.ShouldNotBeNull();
        chat.ReceivedOptions[1]!.ResponseFormat.ShouldBeNull();
    }
}
```

- [ ] **Step 2: Run the new test file — verify it fails (compile error)**

```bash
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -15
```

Expected: build fails with `error CS0246: The type or namespace name 'InstructCallStrategy' could not be found`. That's the failing "test" — the type doesn't exist yet.

- [ ] **Step 3: Add `InternalsVisibleTo`**

Edit `src/TradingSignal.Llm/TradingSignal.Llm.csproj`. Inside the `<Project>` element, add an `<ItemGroup>` with the assembly attribute:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="TradingSignal.Llm.Tests" />
</ItemGroup>
```

- [ ] **Step 4: Implement `InstructCallStrategy`**

Create `src/TradingSignal.Llm/Strategies/InstructCallStrategy.cs` with:

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TradingSignal.Core;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Parsing;
using TradingSignal.Llm.Prompts;
using TradingSignal.Llm.Schemas;

namespace TradingSignal.Llm.Strategies;

internal sealed partial class InstructCallStrategy : ILlmCallStrategy
{
    private readonly IChatClient _chatClient;
    private readonly LmStudioOptions _options;
    private readonly ILogger<InstructCallStrategy> _logger;

    public InstructCallStrategy(
        IChatClient chatClient,
        LmStudioOptions options,
        ILogger<InstructCallStrategy>? logger = null)
    {
        _chatClient = chatClient;
        _options = options;
        _logger = logger ?? NullLogger<InstructCallStrategy>.Instance;
    }

    public string SystemPrompt => PromptBuilder.SystemPromptInstruct;

    public async Task<LlmCallOutcome> GenerateAsync(
        string systemPrompt, string userMessage, CancellationToken ct)
    {
        RawSignal? signal = await TryOnceAsync(systemPrompt, userMessage, useSchema: true, stricterReminder: false, ct).ConfigureAwait(false);
        if (signal is not null) return new LlmCallOutcome(signal, null);

        signal = await TryOnceAsync(systemPrompt, userMessage, useSchema: false, stricterReminder: true, ct).ConfigureAwait(false);
        return new LlmCallOutcome(signal ?? new RawSignal(TradeAction.Hold, 0d, "parse_failure"), null);
    }

    private async Task<RawSignal?> TryOnceAsync(
        string systemPrompt, string userMessage, bool useSchema, bool stricterReminder, CancellationToken ct)
    {
        List<ChatMessage> messages = new()
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userMessage),
        };
        if (stricterReminder)
        {
            messages.Add(new ChatMessage(ChatRole.User,
                "Return ONLY valid JSON matching the schema. No prose, no markdown."));
        }

        ChatOptions chatOptions = new()
        {
            Temperature = 0.2f,
            MaxOutputTokens = _options.MaxOutputTokens,
            ResponseFormat = useSchema
                ? ChatResponseFormat.ForJsonSchema(
                    SignalResponseSchema.Element,
                    SignalResponseSchema.SchemaName,
                    SignalResponseSchema.SchemaDescription)
                : null,
        };

        try
        {
            ChatResponse response = await _chatClient.GetResponseAsync(messages, chatOptions, ct).ConfigureAwait(false);
            string text = response.Text ?? string.Empty;
            if (SignalResponseParser.TryParse(text, out RawSignal parsed)) return parsed;

            if (_logger.IsEnabled(LogLevel.Warning))
                LogParseFailure(_logger, useSchema, Truncate(text, 400));
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogCallFailed(_logger, useSchema, ex);
            return null;
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "LLM parse failure (schema={UseSchema}). Body: {Body}")]
    private static partial void LogParseFailure(ILogger logger, bool useSchema, string body);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "LLM call failed (schema={UseSchema})")]
    private static partial void LogCallFailed(ILogger logger, bool useSchema, Exception ex);
}
```

- [ ] **Step 5: Run the new tests**

```bash
dotnet test TradingSignalPoc.slnx --filter "FullyQualifiedName~InstructCallStrategyTests" --nologo 2>&1 | tail -10
```

Expected: 6 tests pass.

- [ ] **Step 6: Run the full test suite — confirm no regression**

```bash
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -5
```

Expected: original 85 + 6 new = 91 tests pass (the old `LlmSignalGeneratorTests` retry-shape tests still pass against `LlmSignalGenerator`'s current code; we'll prune them in Task 6).

- [ ] **Step 7: Commit**

```bash
git add src/TradingSignal.Llm/TradingSignal.Llm.csproj src/TradingSignal.Llm/Strategies/InstructCallStrategy.cs tests/TradingSignal.Llm.Tests/InstructCallStrategyTests.cs
git commit -m "Add InstructCallStrategy with extracted retry-shape tests"
```

---

### Task 6: Refactor `LlmSignalGenerator` to delegate to `ILlmCallStrategy`

Replace the `IChatClient` constructor parameter with an `ILlmCallStrategy`. Cache key now includes `_options.ReasoningEffort` and the strategy's `SystemPrompt`. The retry-shape tests in `LlmSignalGeneratorTests` move to `InstructCallStrategyTests` (already created); only orchestration tests remain, rewritten to use a `FakeLlmCallStrategy`.

**Files:**
- Modify: `src/TradingSignal.Llm/LlmSignalGenerator.cs`
- Create: `tests/TradingSignal.Llm.Tests/Fakes/FakeLlmCallStrategy.cs`
- Modify: `tests/TradingSignal.Llm.Tests/LlmSignalGeneratorTests.cs`

- [ ] **Step 1: Write the `FakeLlmCallStrategy` test double**

Create `tests/TradingSignal.Llm.Tests/Fakes/FakeLlmCallStrategy.cs`:

```csharp
using TradingSignal.Core;
using TradingSignal.Llm.Abstractions;

namespace TradingSignal.Llm.Tests.Fakes;

internal sealed class FakeLlmCallStrategy : ILlmCallStrategy
{
    public string SystemPrompt { get; set; } = "fake-system-prompt";
    public Queue<LlmCallOutcome> Outcomes { get; } = new();
    public List<(string SystemPrompt, string UserMessage)> Calls { get; } = new();
    public int CallCount => Calls.Count;

    public Task<LlmCallOutcome> GenerateAsync(
        string systemPrompt, string userMessage, CancellationToken ct)
    {
        Calls.Add((systemPrompt, userMessage));
        if (Outcomes.Count == 0)
            throw new InvalidOperationException("FakeLlmCallStrategy: no outcomes queued");
        return Task.FromResult(Outcomes.Dequeue());
    }

    public void EnqueueSignal(TradeAction action, double confidence, string reason, string? reasoning = null)
        => Outcomes.Enqueue(new LlmCallOutcome(new RawSignal(action, confidence, reason, reasoning), reasoning));
}
```

- [ ] **Step 2: Rewrite `LlmSignalGeneratorTests` to use the fake strategy**

Replace `tests/TradingSignal.Llm.Tests/LlmSignalGeneratorTests.cs` with orchestration-only tests:

```csharp
using Shouldly;
using TradingSignal.Core;
using TradingSignal.Llm.Tests.Fakes;

namespace TradingSignal.Llm.Tests;

public sealed class LlmSignalGeneratorTests
{
    private static FeatureSet Features(string symbol = "BTCUSDT") => new(
        AsOfUtc: new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc),
        Symbol: symbol,
        Close: 65_000m,
        Rsi14: 55, MacdLine: 12, MacdSignal: 10, MacdHistogram: 2,
        Ema20: 64_500, Ema50: 64_000, Atr14: 800,
        Return1: 0.002, Return5: 0.01, VolatilityPct: 1.4);

    private static LmStudioOptions Options() => new()
    {
        ModelId = "test-model",
        MaxFewShot = 0,
        MaxOutputTokens = 128,
        ReasoningEffort = "medium",
    };

    [Fact]
    public async Task Returns_Cached_Signal_Without_Calling_Strategy()
    {
        FakeLlmCallStrategy strategy = new();
        strategy.EnqueueSignal(TradeAction.Buy, 0.71, "primer");
        InMemoryLlmCache cache = new();
        LlmSignalGenerator sut = new(strategy, Options(), cache);

        RawSignal primed = await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);
        strategy.CallCount.ShouldBe(1);
        cache.Store.Count.ShouldBe(1);

        RawSignal second = await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);
        strategy.CallCount.ShouldBe(1);
        second.ShouldBe(primed);
    }

    [Fact]
    public async Task Different_Features_Produce_Different_Cache_Keys()
    {
        FakeLlmCallStrategy strategy = new();
        strategy.EnqueueSignal(TradeAction.Buy, 0.5, "a");
        strategy.EnqueueSignal(TradeAction.Sell, 0.5, "b");

        InMemoryLlmCache cache = new();
        LlmSignalGenerator sut = new(strategy, Options(), cache);

        await sut.GenerateAsync(Features("BTCUSDT"), Array.Empty<FewShotCase>(), CancellationToken.None);
        await sut.GenerateAsync(Features("ETHUSDT"), Array.Empty<FewShotCase>(), CancellationToken.None);

        cache.Store.Count.ShouldBe(2);
        strategy.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Propagates_Reasoning_Into_RawSignal_And_Cache()
    {
        FakeLlmCallStrategy strategy = new();
        strategy.EnqueueSignal(TradeAction.Buy, 0.6, "short", reasoning: "long thinking trace");
        InMemoryLlmCache cache = new();
        LlmSignalGenerator sut = new(strategy, Options(), cache);

        RawSignal signal = await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);

        signal.Reasoning.ShouldBe("long thinking trace");
        cache.Store.Values.Single().Reasoning.ShouldBe("long thinking trace");
    }

    [Fact]
    public async Task System_Prompt_Of_Strategy_Affects_Cache_Key()
    {
        FakeLlmCallStrategy strategyA = new() { SystemPrompt = "prompt A" };
        FakeLlmCallStrategy strategyB = new() { SystemPrompt = "prompt B" };
        strategyA.EnqueueSignal(TradeAction.Buy, 0.5, "x");
        strategyB.EnqueueSignal(TradeAction.Sell, 0.5, "y");

        InMemoryLlmCache cache = new();
        LlmSignalGenerator a = new(strategyA, Options(), cache);
        LlmSignalGenerator b = new(strategyB, Options(), cache);

        await a.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);
        await b.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);

        cache.Store.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Reasoning_Effort_Affects_Cache_Key()
    {
        FakeLlmCallStrategy strategy = new();
        strategy.EnqueueSignal(TradeAction.Buy, 0.5, "x");
        strategy.EnqueueSignal(TradeAction.Sell, 0.5, "y");

        InMemoryLlmCache cache = new();
        LlmSignalGenerator a = new(strategy, new LmStudioOptions { ModelId = "m", MaxFewShot = 0, ReasoningEffort = "low" }, cache);
        LlmSignalGenerator b = new(strategy, new LmStudioOptions { ModelId = "m", MaxFewShot = 0, ReasoningEffort = "high" }, cache);

        await a.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);
        await b.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);

        cache.Store.Count.ShouldBe(2);
    }
}
```

- [ ] **Step 3: Run the rewritten tests — verify they fail (compile)**

```bash
dotnet test TradingSignalPoc.slnx --filter "FullyQualifiedName~LlmSignalGeneratorTests" --nologo 2>&1 | tail -15
```

Expected: build fails — `LlmSignalGenerator` ctor still wants `IChatClient`.

- [ ] **Step 4: Refactor `LlmSignalGenerator`**

Replace the contents of `src/TradingSignal.Llm/LlmSignalGenerator.cs` with:

```csharp
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Caching;
using TradingSignal.Llm.Prompts;

namespace TradingSignal.Llm;

public sealed partial class LlmSignalGenerator(
    ILlmCallStrategy strategy,
    LmStudioOptions options,
    ILlmResponseCache cache,
    ILogger<LlmSignalGenerator>? logger = null)
    : ISignalGenerator
{
    private readonly ILogger<LlmSignalGenerator> _logger = logger ?? NullLogger<LlmSignalGenerator>.Instance;

    public async Task<RawSignal> GenerateAsync(
        FeatureSet features, IReadOnlyList<FewShotCase> memory, CancellationToken ct)
    {
        string systemPrompt = strategy.SystemPrompt;
        string userMessage = PromptBuilder.BuildUserMessage(features, memory, options.MaxFewShot);
        string cacheKey = ComputeCacheKey(options.ModelId, options.ReasoningEffort, systemPrompt, userMessage);

        RawSignal? cached = await cache.TryGetAsync(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            LogCacheHit(_logger, features.AsOfUtc, features.Symbol);
            return cached;
        }

        LlmCallOutcome outcome = await strategy.GenerateAsync(systemPrompt, userMessage, ct).ConfigureAwait(false);
        RawSignal final = outcome.Signal with { Reasoning = outcome.ReasoningContent };
        await cache.SetAsync(cacheKey, final, ct).ConfigureAwait(false);
        return final;
    }

    private static string ComputeCacheKey(
        string modelId, string reasoningEffort, string systemPrompt, string userMessage)
    {
        byte[] payload = Encoding.UTF8.GetBytes($"{modelId} {reasoningEffort} {systemPrompt} {userMessage}");
        byte[] hash = SHA256.HashData(payload);
        return Convert.ToHexStringLower(hash);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "LLM cache hit for {AsOf} {Symbol}")]
    private static partial void LogCacheHit(ILogger logger, DateTime asOf, string symbol);
}
```

- [ ] **Step 5: Run tests — verify everything passes**

```bash
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -5
```

Expected: All tests pass. The old `LlmSignalGeneratorTests` 7-test set is gone (replaced by 5 orchestration tests); `InstructCallStrategyTests` (6) provides the retry-shape coverage; everything else unchanged.

- [ ] **Step 6: Commit**

```bash
git add src/TradingSignal.Llm/LlmSignalGenerator.cs tests/TradingSignal.Llm.Tests/Fakes/FakeLlmCallStrategy.cs tests/TradingSignal.Llm.Tests/LlmSignalGeneratorTests.cs
git commit -m "Refactor LlmSignalGenerator to delegate to ILlmCallStrategy"
```

---

### Task 7: Add `reasoning` column + ALTER migration to `SqliteLlmResponseCache`

Idempotent `ALTER TABLE` so pre-existing cache DBs auto-upgrade. CREATE TABLE includes the column for fresh DBs. INSERT/SELECT both write and read `reasoning`.

**Files:**
- Modify: `src/TradingSignal.Llm/Caching/SqliteLlmResponseCache.cs`
- Modify: `tests/TradingSignal.Llm.Tests/SqliteLlmResponseCacheTests.cs`

- [ ] **Step 1: Inspect existing test file for setup patterns**

```bash
cat tests/TradingSignal.Llm.Tests/SqliteLlmResponseCacheTests.cs
```

Note the test fixture pattern (temp file paths, cleanup). Reuse it.

- [ ] **Step 2: Add failing tests for round-trip and migration**

Append the following two tests to `tests/TradingSignal.Llm.Tests/SqliteLlmResponseCacheTests.cs` inside the existing test class. If the file uses a `using` of `Microsoft.Data.Sqlite`, no new using needed; otherwise add it.

```csharp
[Fact]
public async Task Round_Trips_RawSignal_With_Reasoning()
{
    string dbPath = TempDbPath();
    try
    {
        await using SqliteLlmResponseCache sut = new(dbPath);
        RawSignal signal = new(TradeAction.Buy, 0.7, "short reason", "the full thinking trace text");

        await sut.SetAsync("k", signal, CancellationToken.None);
        RawSignal? got = await sut.TryGetAsync("k", CancellationToken.None);

        got.ShouldNotBeNull();
        got!.Reasoning.ShouldBe("the full thinking trace text");
    }
    finally { Cleanup(dbPath); }
}

[Fact]
public async Task Migrates_PreExisting_Db_Without_Reasoning_Column()
{
    string dbPath = TempDbPath();
    try
    {
        // Create the DB with the OLD schema (no reasoning column).
        await using (Microsoft.Data.Sqlite.SqliteConnection conn = new($"Data Source={dbPath}"))
        {
            await conn.OpenAsync();
            await using Microsoft.Data.Sqlite.SqliteCommand cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE llm_cache (
                    key TEXT PRIMARY KEY,
                    action INTEGER NOT NULL,
                    confidence REAL NOT NULL,
                    reason TEXT NOT NULL,
                    created_at TEXT NOT NULL
                );
                """;
            await cmd.ExecuteNonQueryAsync();
        }
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        // Open with new code — ALTER migration should add the column.
        await using SqliteLlmResponseCache sut = new(dbPath);
        await sut.SetAsync("k", new RawSignal(TradeAction.Sell, 0.3, "r", "trace"), CancellationToken.None);
        RawSignal? got = await sut.TryGetAsync("k", CancellationToken.None);

        got.ShouldNotBeNull();
        got!.Reasoning.ShouldBe("trace");
    }
    finally { Cleanup(dbPath); }
}

private static string TempDbPath()
    => Path.Combine(Path.GetTempPath(), $"tsig-llmcache-{Guid.NewGuid():N}.db");

private static void Cleanup(string dbPath)
{
    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    if (File.Exists(dbPath)) File.Delete(dbPath);
}
```

If `TempDbPath`/`Cleanup` already exist in the file under different names, reuse them and skip the helper definitions.

- [ ] **Step 3: Run new tests — verify they fail**

```bash
dotnet test TradingSignalPoc.slnx --filter "FullyQualifiedName~SqliteLlmResponseCacheTests" --nologo 2>&1 | tail -15
```

Expected: both new tests fail. Round-trip test fails because `Reasoning` is `null` after read. Migration test fails with `SqliteException: SQLite Error 1: 'table llm_cache has no column named reasoning'` on INSERT.

- [ ] **Step 4: Implement schema + migration + read/write changes**

Replace the body of `EnsureInitializedAsync` in `src/TradingSignal.Llm/Caching/SqliteLlmResponseCache.cs` with the new schema and the idempotent ALTER. Update `TryGetAsync` and `SetAsync` to include the `reasoning` column.

Full replacement file:

```csharp
using Microsoft.Data.Sqlite;
using TradingSignal.Core;

namespace TradingSignal.Llm.Caching;

public sealed class SqliteLlmResponseCache : ILlmResponseCache, IAsyncDisposable
{
    private readonly string _connString;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public SqliteLlmResponseCache(string dbPath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        _connString = new SqliteConnectionStringBuilder { DataSource = dbPath, Cache = SqliteCacheMode.Shared }.ToString();
    }

    private async ValueTask EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_initialized) return;
            await using var conn = new SqliteConnection(_connString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            await using (var create = conn.CreateCommand())
            {
                create.CommandText = """
                    CREATE TABLE IF NOT EXISTS llm_cache (
                        key TEXT PRIMARY KEY,
                        action INTEGER NOT NULL,
                        confidence REAL NOT NULL,
                        reason TEXT NOT NULL,
                        reasoning TEXT NULL,
                        created_at TEXT NOT NULL
                    );
                    """;
                await create.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            // Idempotent migration for pre-existing DBs.
            await using (var alter = conn.CreateCommand())
            {
                alter.CommandText = "ALTER TABLE llm_cache ADD COLUMN reasoning TEXT NULL";
                try { await alter.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
                catch (SqliteException) { /* column already exists — expected on fresh DBs */ }
            }

            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<RawSignal?> TryGetAsync(string key, CancellationToken ct)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT action, confidence, reason, reasoning FROM llm_cache WHERE key = $key";
        cmd.Parameters.AddWithValue("$key", key);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;

        var action = (TradeAction)reader.GetInt32(0);
        var confidence = reader.GetDouble(1);
        var reason = reader.GetString(2);
        string? reasoning = reader.IsDBNull(3) ? null : reader.GetString(3);
        return new RawSignal(action, confidence, reason, reasoning);
    }

    public async Task SetAsync(string key, RawSignal signal, CancellationToken ct)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO llm_cache (key, action, confidence, reason, reasoning, created_at)
            VALUES ($key, $action, $confidence, $reason, $reasoning, $createdAt);
            """;
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$action", (int)signal.Action);
        cmd.Parameters.AddWithValue("$confidence", signal.Confidence);
        cmd.Parameters.AddWithValue("$reason", signal.Reason);
        cmd.Parameters.AddWithValue("$reasoning", (object?)signal.Reasoning ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        _gate.Dispose();
        SqliteConnection.ClearAllPools();
        return ValueTask.CompletedTask;
    }
}
```

- [ ] **Step 5: Run tests — verify green**

```bash
dotnet test TradingSignalPoc.slnx --filter "FullyQualifiedName~SqliteLlmResponseCacheTests" --nologo 2>&1 | tail -10
```

Expected: all tests in this class pass, including the two new ones.

- [ ] **Step 6: Run full suite**

```bash
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -5
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/TradingSignal.Llm/Caching/SqliteLlmResponseCache.cs tests/TradingSignal.Llm.Tests/SqliteLlmResponseCacheTests.cs
git commit -m "Persist Reasoning in LLM response cache with idempotent migration"
```

---

### Task 8: Add `reasoning` column + ALTER migration to `SqlitePredictionStore`

Same pattern as Task 7 but for predictions. Uses Dapper. The `JoinedRow` projection class adds a `Reasoning` property.

**Files:**
- Modify: `src/TradingSignal.Evaluation/Stores/SqlitePredictionStore.cs`
- Modify: `tests/TradingSignal.Evaluation.Tests/SqlitePredictionStoreTests.cs` (or equivalent file in that project — find it first)

- [ ] **Step 1: Find the existing test file**

```bash
find tests/TradingSignal.Evaluation.Tests -name "*PredictionStore*" -type f
```

Use the resulting file. If none exists, create `tests/TradingSignal.Evaluation.Tests/SqlitePredictionStoreTests.cs`.

- [ ] **Step 2: Add failing tests**

In that file, add two tests (adapt namespace/using directives to match existing patterns in the project):

```csharp
[Fact]
public async Task Round_Trips_Prediction_With_Reasoning()
{
    string dbPath = Path.Combine(Path.GetTempPath(), $"tsig-pred-{Guid.NewGuid():N}.db");
    try
    {
        await using SqlitePredictionStore sut = new(dbPath);
        Prediction p = MakePrediction(reasoning: "long thinking trace");

        await sut.SavePredictionAsync(p, CancellationToken.None);
        IReadOnlyList<(Prediction, Outcome?)> got = await sut.GetSegmentAsync(p.WalkForwardSegment, CancellationToken.None);

        got.Count.ShouldBe(1);
        got[0].Item1.Signal.Reasoning.ShouldBe("long thinking trace");
    }
    finally { Cleanup(dbPath); }
}

[Fact]
public async Task Migrates_PreExisting_Predictions_Db_Without_Reasoning_Column()
{
    string dbPath = Path.Combine(Path.GetTempPath(), $"tsig-pred-{Guid.NewGuid():N}.db");
    try
    {
        await using (Microsoft.Data.Sqlite.SqliteConnection conn = new($"Data Source={dbPath}"))
        {
            await conn.OpenAsync();
            await using Microsoft.Data.Sqlite.SqliteCommand cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE predictions (
                    id TEXT PRIMARY KEY,
                    as_of_utc TEXT NOT NULL,
                    symbol TEXT NOT NULL,
                    segment INTEGER NOT NULL,
                    action INTEGER NOT NULL,
                    confidence REAL NOT NULL,
                    reason TEXT NOT NULL,
                    features_json TEXT NOT NULL
                );
                """;
            await cmd.ExecuteNonQueryAsync();
        }
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        await using SqlitePredictionStore sut = new(dbPath);
        Prediction p = MakePrediction(reasoning: "trace");
        await sut.SavePredictionAsync(p, CancellationToken.None);
        IReadOnlyList<(Prediction, Outcome?)> got = await sut.GetSegmentAsync(p.WalkForwardSegment, CancellationToken.None);

        got[0].Item1.Signal.Reasoning.ShouldBe("trace");
    }
    finally { Cleanup(dbPath); }
}

private static Prediction MakePrediction(string? reasoning)
{
    FeatureSet features = new(
        AsOfUtc: new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
        Symbol: "BTCUSDT",
        Close: 65_000m,
        Rsi14: 55, MacdLine: 12, MacdSignal: 10, MacdHistogram: 2,
        Ema20: 64_500, Ema50: 64_000, Atr14: 800,
        Return1: 0.002, Return5: 0.01, VolatilityPct: 1.4);
    RawSignal signal = new(TradeAction.Buy, 0.7, "short", reasoning);
    return new Prediction(Guid.NewGuid(), features.AsOfUtc, features.Symbol, features, signal, WalkForwardSegment: 0);
}

private static void Cleanup(string dbPath)
{
    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    if (File.Exists(dbPath)) File.Delete(dbPath);
}
```

If the test class file already defines `MakePrediction` or `Cleanup`, reuse the existing helpers.

- [ ] **Step 3: Run new tests — verify failure**

```bash
dotnet test TradingSignalPoc.slnx --filter "FullyQualifiedName~SqlitePredictionStoreTests" --nologo 2>&1 | tail -15
```

Expected: both new tests fail (round-trip → `Reasoning` is `null`; migration → SQL error on INSERT or schema mismatch).

- [ ] **Step 4: Update `SqlitePredictionStore`**

Edit `src/TradingSignal.Evaluation/Stores/SqlitePredictionStore.cs`:

In `EnsureInitializedAsync`, add `reasoning TEXT NULL` to the CREATE TABLE and add the idempotent ALTER after it. The replacement block (inside the existing `try {}` of `EnsureInitializedAsync`):

```csharp
await using var conn = new SqliteConnection(_connString);
await conn.OpenAsync(ct).ConfigureAwait(false);
await conn.ExecuteAsync("""
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
    CREATE INDEX IF NOT EXISTS ix_predictions_segment ON predictions(segment);
    CREATE TABLE IF NOT EXISTS outcomes (
        prediction_id TEXT PRIMARY KEY,
        entry_price REAL NOT NULL,
        exit_price REAL NOT NULL,
        realized_return_pct REAL NOT NULL,
        direction_correct INTEGER NOT NULL,
        FOREIGN KEY(prediction_id) REFERENCES predictions(id)
    );
    """).ConfigureAwait(false);

// Idempotent migration for pre-existing DBs.
try
{
    await conn.ExecuteAsync("ALTER TABLE predictions ADD COLUMN reasoning TEXT NULL").ConfigureAwait(false);
}
catch (SqliteException) { /* column already exists — expected on fresh DBs */ }

_initialized = true;
```

In `SavePredictionAsync`, update the INSERT and the anonymous parameter object:

```csharp
public async Task SavePredictionAsync(Prediction prediction, CancellationToken ct)
{
    await using var conn = await OpenAsync(ct).ConfigureAwait(false);
    await conn.ExecuteAsync(
        """
        INSERT OR REPLACE INTO predictions
          (id, as_of_utc, symbol, segment, action, confidence, reason, reasoning, features_json)
        VALUES (@Id, @AsOf, @Symbol, @Segment, @Action, @Confidence, @Reason, @Reasoning, @Features);
        """,
        new
        {
            Id = prediction.Id.ToString("N"),
            AsOf = prediction.AsOfUtc.ToString("o", CultureInfo.InvariantCulture),
            prediction.Symbol,
            Segment = prediction.WalkForwardSegment,
            Action = (int)prediction.Signal.Action,
            prediction.Signal.Confidence,
            prediction.Signal.Reason,
            prediction.Signal.Reasoning,
            Features = JsonSerializer.Serialize(prediction.Features, FeaturesJson),
        }).ConfigureAwait(false);
}
```

In `GetSegmentAsync`, add `p.reasoning AS Reasoning` to the SELECT and read it into the new RawSignal:

```csharp
var rows = await conn.QueryAsync<JoinedRow>(
    """
    SELECT p.id AS Id,
           p.as_of_utc AS AsOfUtc,
           p.symbol AS Symbol,
           p.segment AS Segment,
           p.action AS Action,
           p.confidence AS Confidence,
           p.reason AS Reason,
           p.reasoning AS Reasoning,
           p.features_json AS FeaturesJson,
           o.entry_price AS EntryPrice,
           o.exit_price AS ExitPrice,
           o.realized_return_pct AS RealizedReturnPct,
           o.direction_correct AS DirectionCorrect
    FROM predictions p
    LEFT JOIN outcomes o ON o.prediction_id = p.id
    WHERE p.segment = @segment
    ORDER BY p.as_of_utc;
    """,
    new { segment }).ConfigureAwait(false);
```

And in the mapping loop, the `Signal` construction:

```csharp
Signal: new RawSignal((TradeAction)r.Action, r.Confidence, r.Reason, r.Reasoning),
```

Add `Reasoning` to the `JoinedRow` class:

```csharp
private sealed class JoinedRow
{
    public string Id { get; set; } = "";
    public string AsOfUtc { get; set; } = "";
    public string Symbol { get; set; } = "";
    public int Segment { get; set; }
    public int Action { get; set; }
    public double Confidence { get; set; }
    public string Reason { get; set; } = "";
    public string? Reasoning { get; set; }
    public string FeaturesJson { get; set; } = "";
    public double? EntryPrice { get; set; }
    public double? ExitPrice { get; set; }
    public double? RealizedReturnPct { get; set; }
    public int? DirectionCorrect { get; set; }
}
```

- [ ] **Step 5: Run tests — verify green**

```bash
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -5
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/TradingSignal.Evaluation/Stores/SqlitePredictionStore.cs tests/TradingSignal.Evaluation.Tests/SqlitePredictionStoreTests.cs
git commit -m "Persist Reasoning in predictions DB with idempotent migration"
```

---

### Task 9: Create `ReasoningCallStrategy`

Raw `HttpClient` POST to `/chat/completions`. Reads `content` and `reasoning_content`. Retries once on parse failure with a stricter reminder appended to the messages. Cancellation disambiguation per spec.

**Files:**
- Create: `src/TradingSignal.Llm/Strategies/ReasoningCallStrategy.cs`
- Create: `tests/TradingSignal.Llm.Tests/Fakes/FakeHttpMessageHandler.cs`
- Create: `tests/TradingSignal.Llm.Tests/ReasoningCallStrategyTests.cs`

- [ ] **Step 1: Create the test fake**

`tests/TradingSignal.Llm.Tests/Fakes/FakeHttpMessageHandler.cs`:

```csharp
using System.Net;
using System.Text;
using System.Text.Json;

namespace TradingSignal.Llm.Tests.Fakes;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responders = new();

    public List<HttpRequestMessage> ReceivedRequests { get; } = new();
    public List<JsonElement> ReceivedBodies { get; } = new();

    public void EnqueueJson(object body, HttpStatusCode status = HttpStatusCode.OK)
        => _responders.Enqueue(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        });

    public void EnqueueRawJson(string rawJson, HttpStatusCode status = HttpStatusCode.OK)
        => _responders.Enqueue(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(rawJson, Encoding.UTF8, "application/json"),
        });

    public void EnqueueException(Exception ex)
        => _responders.Enqueue(_ => throw ex);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ReceivedRequests.Add(request);
        if (request.Content is not null)
        {
            string body = await request.Content.ReadAsStringAsync(cancellationToken);
            ReceivedBodies.Add(JsonDocument.Parse(body).RootElement.Clone());
        }
        if (_responders.Count == 0)
            throw new InvalidOperationException("FakeHttpMessageHandler: no responses queued");
        var responder = _responders.Dequeue();
        return responder(request);
    }
}
```

- [ ] **Step 2: Write failing tests**

Create `tests/TradingSignal.Llm.Tests/ReasoningCallStrategyTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using Shouldly;
using TradingSignal.Core;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Prompts;
using TradingSignal.Llm.Strategies;
using TradingSignal.Llm.Tests.Fakes;

namespace TradingSignal.Llm.Tests;

public sealed class ReasoningCallStrategyTests
{
    private const string Sys = "system";
    private const string User = "user";

    private static LmStudioOptions Options() => new()
    {
        ModelId = "qwen/qwen3.6-35b-a3b",
        MaxOutputTokens = 2048,
        TimeoutSeconds = 120,
        ReasoningEffort = "medium",
    };

    private static (ReasoningCallStrategy sut, FakeHttpMessageHandler handler) Build()
    {
        FakeHttpMessageHandler handler = new();
        HttpClient http = new(handler) { BaseAddress = new Uri("http://localhost:1234/v1/") };
        return (new ReasoningCallStrategy(http, Options()), handler);
    }

    private static object OkBody(string content, string? reasoning = null)
        => new
        {
            choices = new[]
            {
                new
                {
                    finish_reason = "stop",
                    message = new { role = "assistant", content, reasoning_content = reasoning },
                },
            },
        };

    [Fact]
    public void SystemPrompt_Returns_Reasoning_Constant()
    {
        var (sut, _) = Build();
        sut.SystemPrompt.ShouldBe(PromptBuilder.SystemPromptReasoning);
    }

    [Fact]
    public async Task Parses_Content_And_Captures_Reasoning_Content()
    {
        var (sut, handler) = Build();
        handler.EnqueueJson(OkBody(
            content: """{"action":"BUY","confidence":0.7,"reason":"trend up"}""",
            reasoning: "step 1... step 2..."));

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Buy);
        outcome.Signal.Confidence.ShouldBe(0.7);
        outcome.Signal.Reason.ShouldBe("trend up");
        outcome.ReasoningContent.ShouldBe("step 1... step 2...");
    }

    [Fact]
    public async Task Parses_Fenced_Json_Inside_Prose_Content()
    {
        var (sut, handler) = Build();
        string content = "Reasoning summary.\n\n```json\n{\"action\":\"SELL\",\"confidence\":0.6,\"reason\":\"rsi extreme\"}\n```\n";
        handler.EnqueueJson(OkBody(content: content, reasoning: "longer trace"));

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Sell);
        outcome.ReasoningContent.ShouldBe("longer trace");
    }

    [Fact]
    public async Task Retries_With_Stricter_Reminder_When_First_Parse_Fails()
    {
        var (sut, handler) = Build();
        handler.EnqueueJson(OkBody(content: "garbage no json here", reasoning: "first trace"));
        handler.EnqueueJson(OkBody(
            content: """{"action":"HOLD","confidence":0.4,"reason":"unsure"}""",
            reasoning: "second trace"));

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Hold);
        outcome.ReasoningContent.ShouldBe("second trace");
        handler.ReceivedRequests.Count.ShouldBe(2);

        // Second request body has the stricter reminder appended as a user message.
        JsonElement secondMessages = handler.ReceivedBodies[1].GetProperty("messages");
        secondMessages.GetArrayLength().ShouldBe(3); // system, user, reminder
        secondMessages[2].GetProperty("role").GetString().ShouldBe("user");
        secondMessages[2].GetProperty("content").GetString().ShouldContain("ONLY");
    }

    [Fact]
    public async Task Empty_Content_With_Non_Empty_Reasoning_Returns_ParseFailure_With_Trace_Preserved()
    {
        var (sut, handler) = Build();
        handler.EnqueueJson(OkBody(content: "", reasoning: "thought but did not answer"));
        handler.EnqueueJson(OkBody(content: "", reasoning: "still no answer"));

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Hold);
        outcome.Signal.Reason.ShouldBe("parse_failure");
        outcome.ReasoningContent.ShouldBe("still no answer");
    }

    [Fact]
    public async Task Http_500_Degrades_To_ParseFailure_Without_Throwing()
    {
        var (sut, handler) = Build();
        handler.EnqueueRawJson("server error", HttpStatusCode.InternalServerError);
        handler.EnqueueRawJson("server error", HttpStatusCode.InternalServerError);

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Hold);
        outcome.Signal.Reason.ShouldBe("parse_failure");
    }

    [Fact]
    public async Task Transport_Exception_Degrades_To_ParseFailure()
    {
        var (sut, handler) = Build();
        handler.EnqueueException(new HttpRequestException("connection refused"));
        handler.EnqueueException(new HttpRequestException("again"));

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Hold);
        outcome.Signal.Reason.ShouldBe("parse_failure");
    }

    [Fact]
    public async Task Caller_Cancellation_Propagates()
    {
        var (sut, handler) = Build();
        handler.EnqueueException(new OperationCanceledException());
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.GenerateAsync(Sys, User, cts.Token));
    }

    [Fact]
    public async Task Request_Body_Has_Expected_Shape()
    {
        var (sut, handler) = Build();
        handler.EnqueueJson(OkBody(
            content: """{"action":"BUY","confidence":0.5,"reason":"x"}""",
            reasoning: null));

        await sut.GenerateAsync(Sys, User, CancellationToken.None);

        JsonElement body = handler.ReceivedBodies[0];
        body.GetProperty("model").GetString().ShouldBe("qwen/qwen3.6-35b-a3b");
        body.GetProperty("max_tokens").GetInt32().ShouldBe(2048);
        body.GetProperty("reasoning_effort").GetString().ShouldBe("medium");
        body.TryGetProperty("response_format", out _).ShouldBeFalse();
        body.GetProperty("messages").GetArrayLength().ShouldBe(2);
    }
}
```

- [ ] **Step 3: Run new tests — verify they fail (compile)**

```bash
dotnet test TradingSignalPoc.slnx --filter "FullyQualifiedName~ReasoningCallStrategyTests" --nologo 2>&1 | tail -15
```

Expected: build fails — `ReasoningCallStrategy` does not exist.

- [ ] **Step 4: Implement `ReasoningCallStrategy`**

Create `src/TradingSignal.Llm/Strategies/ReasoningCallStrategy.cs`:

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TradingSignal.Core;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Parsing;
using TradingSignal.Llm.Prompts;

namespace TradingSignal.Llm.Strategies;

internal sealed partial class ReasoningCallStrategy : ILlmCallStrategy
{
    private const string StricterReminder =
        "Return ONLY a single JSON object on the final line with action, confidence, reason. No prose after the JSON.";

    private readonly HttpClient _http;
    private readonly LmStudioOptions _options;
    private readonly ILogger<ReasoningCallStrategy> _logger;

    public ReasoningCallStrategy(
        HttpClient http,
        LmStudioOptions options,
        ILogger<ReasoningCallStrategy>? logger = null)
    {
        _http = http;
        _options = options;
        _logger = logger ?? NullLogger<ReasoningCallStrategy>.Instance;
    }

    public string SystemPrompt => PromptBuilder.SystemPromptReasoning;

    public async Task<LlmCallOutcome> GenerateAsync(
        string systemPrompt, string userMessage, CancellationToken ct)
    {
        (RawSignal? signal, string? trace) first = await CallOnceAsync(systemPrompt, userMessage, stricter: false, ct).ConfigureAwait(false);
        if (first.signal is not null)
            return new LlmCallOutcome(first.signal, first.trace);

        (RawSignal? signal, string? trace) second = await CallOnceAsync(systemPrompt, userMessage, stricter: true, ct).ConfigureAwait(false);
        string? lastTrace = second.trace ?? first.trace;
        return new LlmCallOutcome(
            second.signal ?? new RawSignal(TradeAction.Hold, 0d, "parse_failure"),
            lastTrace);
    }

    private async Task<(RawSignal? Signal, string? Trace)> CallOnceAsync(
        string systemPrompt, string userMessage, bool stricter, CancellationToken ct)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new { role = "user",   content = userMessage },
        };
        if (stricter)
        {
            messages.Add(new { role = "user", content = StricterReminder });
        }

        var body = new
        {
            model = _options.ModelId,
            messages = messages.ToArray(),
            max_tokens = _options.MaxOutputTokens,
            temperature = 0.2,
            reasoning_effort = _options.ReasoningEffort,
        };

        try
        {
            using HttpResponseMessage response = await _http.PostAsJsonAsync("chat/completions", body, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpError(_logger, (int)response.StatusCode);
                return (null, null);
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

            JsonElement message = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message");

            string content = message.TryGetProperty("content", out JsonElement c) && c.ValueKind == JsonValueKind.String
                ? c.GetString() ?? string.Empty
                : string.Empty;
            string? trace = message.TryGetProperty("reasoning_content", out JsonElement r) && r.ValueKind == JsonValueKind.String
                ? r.GetString()
                : null;

            if (SignalResponseParser.TryParse(content, out RawSignal parsed))
                return (parsed, trace);

            if (_logger.IsEnabled(LogLevel.Warning))
                LogParseFailure(_logger, stricter, Truncate(content, 400));
            return (null, trace);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            LogTimeout(_logger, ex);
            return (null, null);
        }
        catch (HttpRequestException ex)
        {
            LogTransport(_logger, ex);
            return (null, null);
        }
        catch (JsonException ex)
        {
            LogMalformed(_logger, ex);
            return (null, null);
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Reasoning LLM HTTP error: status {Status}")]
    private static partial void LogHttpError(ILogger logger, int status);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Reasoning LLM parse failure (stricter={Stricter}). Body: {Body}")]
    private static partial void LogParseFailure(ILogger logger, bool stricter, string body);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Reasoning LLM call timed out (HttpClient internal timeout)")]
    private static partial void LogTimeout(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Reasoning LLM transport error")]
    private static partial void LogTransport(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Reasoning LLM returned malformed JSON")]
    private static partial void LogMalformed(ILogger logger, Exception ex);
}
```

- [ ] **Step 5: Run reasoning tests — verify they pass**

```bash
dotnet test TradingSignalPoc.slnx --filter "FullyQualifiedName~ReasoningCallStrategyTests" --nologo 2>&1 | tail -10
```

Expected: 9 tests pass.

- [ ] **Step 6: Run full suite**

```bash
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -5
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/TradingSignal.Llm/Strategies/ReasoningCallStrategy.cs tests/TradingSignal.Llm.Tests/Fakes/FakeHttpMessageHandler.cs tests/TradingSignal.Llm.Tests/ReasoningCallStrategyTests.cs
git commit -m "Add ReasoningCallStrategy with raw HTTP transport and reasoning_content parsing"
```

---

### Task 10: Add the `AddLlmCallStrategy` DI extension method

Public extension that selects the strategy from `LmStudioOptions.ModelFamily`. Falls back to instruct with a warning on unknown values.

**Files:**
- Create: `src/TradingSignal.Llm/ServiceCollectionExtensions.cs`
- Create: `tests/TradingSignal.Llm.Tests/ServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/TradingSignal.Llm.Tests/ServiceCollectionExtensionsTests.cs`:

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradingSignal.Llm;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Caching;
using TradingSignal.Llm.Strategies;
using TradingSignal.Llm.Tests.Fakes;

namespace TradingSignal.Llm.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void ModelFamily_Instruct_Registers_InstructCallStrategy()
    {
        ServiceCollection services = new();
        LmStudioOptions options = new() { ModelFamily = "instruct", ModelId = "m" };
        services.AddSingleton(options);
        services.AddSingleton<IChatClient>(new FakeChatClient());
        services.AddSingleton<ILlmResponseCache>(NullLlmResponseCache.Instance);

        services.AddLlmCallStrategy(options);

        using ServiceProvider sp = services.BuildServiceProvider();
        ILlmCallStrategy strategy = sp.GetRequiredService<ILlmCallStrategy>();
        strategy.ShouldBeOfType<InstructCallStrategy>();
    }

    [Fact]
    public void ModelFamily_Reasoning_Registers_ReasoningCallStrategy_With_Configured_HttpClient()
    {
        ServiceCollection services = new();
        LmStudioOptions options = new()
        {
            ModelFamily = "reasoning",
            Endpoint = "http://localhost:9999/v1",
            TimeoutSeconds = 30,
        };
        services.AddSingleton(options);
        services.AddSingleton<ILlmResponseCache>(NullLlmResponseCache.Instance);

        services.AddLlmCallStrategy(options);

        using ServiceProvider sp = services.BuildServiceProvider();
        ILlmCallStrategy strategy = sp.GetRequiredService<ILlmCallStrategy>();
        strategy.ShouldBeOfType<ReasoningCallStrategy>();

        HttpClient http = sp.GetRequiredService<HttpClient>();
        http.BaseAddress!.ToString().ShouldBe("http://localhost:9999/v1/");
        http.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Unknown_ModelFamily_Falls_Back_To_Instruct_With_Warning()
    {
        ServiceCollection services = new();
        LmStudioOptions options = new() { ModelFamily = "thinking-deluxe-2000", ModelId = "m" };
        services.AddSingleton(options);
        services.AddSingleton<IChatClient>(new FakeChatClient());
        services.AddSingleton<ILlmResponseCache>(NullLlmResponseCache.Instance);

        CapturingLogger logger = new();
        services.AddLlmCallStrategy(options, logger);

        using ServiceProvider sp = services.BuildServiceProvider();
        sp.GetRequiredService<ILlmCallStrategy>().ShouldBeOfType<InstructCallStrategy>();
        logger.Warnings.ShouldNotBeEmpty();
        logger.Warnings[0].ShouldContain("thinking-deluxe-2000");
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<string> Warnings { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) Warnings.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
```

- [ ] **Step 2: Run test — verify failure**

```bash
dotnet test TradingSignalPoc.slnx --filter "FullyQualifiedName~ServiceCollectionExtensionsTests" --nologo 2>&1 | tail -15
```

Expected: build fails — `AddLlmCallStrategy` not found.

- [ ] **Step 3: Implement the extension**

Create `src/TradingSignal.Llm/ServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Strategies;

namespace TradingSignal.Llm;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLlmCallStrategy(
        this IServiceCollection services,
        LmStudioOptions options,
        ILogger? startupLogger = null)
    {
        string family = (options.ModelFamily ?? "instruct").Trim().ToLowerInvariant();
        if (family != "instruct" && family != "reasoning")
        {
            (startupLogger ?? NullLogger.Instance).LogWarning(
                "Unknown ModelFamily '{Family}', falling back to 'instruct'.", options.ModelFamily);
            family = "instruct";
        }

        if (family == "reasoning")
        {
            services.AddSingleton<HttpClient>(_ => new HttpClient
            {
                BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
            });
            services.AddSingleton<ILlmCallStrategy, ReasoningCallStrategy>();
        }
        else
        {
            services.AddSingleton<ILlmCallStrategy, InstructCallStrategy>();
        }

        return services;
    }
}
```

- [ ] **Step 4: Run tests — verify green**

```bash
dotnet test TradingSignalPoc.slnx --filter "FullyQualifiedName~ServiceCollectionExtensionsTests" --nologo 2>&1 | tail -10
```

Expected: 3 tests pass.

- [ ] **Step 5: Run full suite**

```bash
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -5
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/TradingSignal.Llm/ServiceCollectionExtensions.cs tests/TradingSignal.Llm.Tests/ServiceCollectionExtensionsTests.cs
git commit -m "Add AddLlmCallStrategy DI extension method"
```

---

### Task 11: Wire the new DI in `Program.cs`

Eagerly construct `LmStudioOptions`, register it as a singleton, conditionally register `IChatClient` (only for `ModelFamily=instruct`), call `AddLlmCallStrategy`, and update the `LlmSignalGenerator` registration to auto-resolve dependencies from DI.

**Files:**
- Modify: `src/TradingSignal.Console/Program.cs`

- [ ] **Step 1: Read current Program.cs to locate the LmStudioOptions / IChatClient / LlmSignalGenerator registrations**

```bash
grep -n "LmStudioOptions\|IChatClient\|LlmSignalGenerator\|LmStudioChatClientFactory" src/TradingSignal.Console/Program.cs
```

Lines 79-107 (approx) contain the relevant block.

- [ ] **Step 2: Replace the relevant DI block**

Open `src/TradingSignal.Console/Program.cs`. In `BuildHost`, inside `ConfigureServices((ctx, services) => { ... })`, replace the existing block that constructs `LmStudioOptions`, registers `IChatClient`, and registers `ISignalGenerator` (the `LlmSignalGenerator` registration) with the following code:

```csharp
AppConfig appConfig = new();
configuration.Bind(appConfig);
services.AddSingleton(appConfig);

// Eagerly materialize LmStudioOptions so ModelFamily is known at registration time.
LmStudioOptions lmOptions = new()
{
    Endpoint = appConfig.LmStudio.Endpoint,
    ModelId = appConfig.LmStudio.ModelId,
    TimeoutSeconds = appConfig.LmStudio.TimeoutSeconds,
    MaxFewShot = appConfig.LmStudio.MaxFewShot,
    MaxOutputTokens = appConfig.LmStudio.MaxOutputTokens,
    ModelFamily = appConfig.LmStudio.ModelFamily,
    ReasoningEffort = appConfig.LmStudio.ReasoningEffort,
};
services.AddSingleton(lmOptions);

services.AddSingleton<ICandleCache>(sp =>
    new CsvCandleCache(sp.GetRequiredService<AppConfig>().Output.DataCacheDir));
services.AddSingleton<IKlineFetcher>(_ => new BinanceKlineFetcher());
services.AddSingleton<IMarketDataSource>(sp => new BinanceMarketDataSource(
    sp.GetRequiredService<IKlineFetcher>(),
    sp.GetRequiredService<ICandleCache>(),
    sp.GetService<ILogger<BinanceMarketDataSource>>()));

services.AddSingleton<ILlmResponseCache>(sp =>
    new SqliteLlmResponseCache(sp.GetRequiredService<AppConfig>().Output.LlmCachePath));

// IChatClient is only needed for the instruct path; skip allocating it under reasoning.
if (string.Equals(lmOptions.ModelFamily, "instruct", StringComparison.OrdinalIgnoreCase))
{
    services.AddSingleton(_ => LmStudioChatClientFactory.Create(lmOptions));
}

// startupLogger is null here: Serilog's ILogger isn't trivially convertible to
// Microsoft.Extensions.Logging.ILogger at this point. The extension uses a
// NullLogger fallback when null is passed. Unknown-ModelFamily values still get
// mapped to "instruct" — they just don't emit a warning here. Acceptable for a
// PoC; tighten if/when needed.
services.AddLlmCallStrategy(lmOptions, startupLogger: null);

services.AddSingleton<ISignalGenerator, LlmSignalGenerator>();

services.AddSingleton<IFeatureEngine>(sp => new FeatureEngine(sp.GetRequiredService<AppConfig>().Market.Symbol));
services.AddSingleton<IPredictionStore>(sp =>
    new SqlitePredictionStore(sp.GetRequiredService<AppConfig>().Output.DbPath));

services.AddTransient<IngestCommand>();
services.AddTransient<RunCommand>();
services.AddTransient<ReportCommand>();
```

Notes:
- Remove the prior `services.AddSingleton<LmStudioOptions>(sp => ...)` lambda — it's replaced by the eager construction above.
- Remove the prior `services.AddSingleton<ISignalGenerator>(sp => new LlmSignalGenerator(...))` block — `services.AddSingleton<ISignalGenerator, LlmSignalGenerator>()` handles DI auto-injection.
- The startup-logger parameter on `AddLlmCallStrategy` is `Microsoft.Extensions.Logging.ILogger`. We pass `null` here because the only Serilog logger available at this point is `Log.Logger`, a Serilog-typed `ILogger`, not the M.E.Logging one. The unknown-ModelFamily path remains correct (falls back to instruct); it just doesn't emit the diagnostic warning here. The smoke test in Task 13 exercises the happy path; unknown-family handling is covered by `ServiceCollectionExtensionsTests`.

- [ ] **Step 3: Add the new namespace import**

At the top of `Program.cs`, add:

```csharp
using TradingSignal.Llm;
```

if it isn't already there (the file already uses `TradingSignal.Llm` types).

- [ ] **Step 4: Build**

```bash
dotnet build TradingSignalPoc.slnx --nologo 2>&1 | tail -3
```

Expected: 0 warnings, 0 errors.

- [ ] **Step 5: Run all tests**

```bash
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -5
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/TradingSignal.Console/Program.cs
git commit -m "Wire ILlmCallStrategy selection into Program.cs DI"
```

---

### Task 12: Add `ModelFamily` and `ReasoningEffort` to appsettings JSON files

Explicit "instruct" + "medium" defaults under `LmStudio` in both configs. Demo flips become diff-visible.

**Files:**
- Modify: `src/TradingSignal.Console/appsettings.json`
- Modify: `src/TradingSignal.Console/appsettings.demo.json`

- [ ] **Step 1: Update `appsettings.json`**

Edit `src/TradingSignal.Console/appsettings.json`. Inside the `LmStudio` object, after `MaxOutputTokens`, add the two keys:

```json
"LmStudio": {
  "Endpoint": "http://localhost:1234/v1",
  "ModelId": "qwen2.5-14b-instruct",
  "TimeoutSeconds": 60,
  "MaxFewShot": 3,
  "MaxOutputTokens": 256,
  "ModelFamily": "instruct",
  "ReasoningEffort": "medium"
},
```

- [ ] **Step 2: Update `appsettings.demo.json`**

Same edit in `src/TradingSignal.Console/appsettings.demo.json`.

- [ ] **Step 3: Build + run tests**

```bash
dotnet build TradingSignalPoc.slnx --nologo 2>&1 | tail -3
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -5
```

Expected: clean build, all tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/TradingSignal.Console/appsettings.json src/TradingSignal.Console/appsettings.demo.json
git commit -m "Add ModelFamily and ReasoningEffort to appsettings"
```

---

### Task 13: End-to-end smoke build + ingest sanity check

A "did anything break" gate before touching the README. We don't run the LLM here (no guarantee LM Studio is up); we just confirm the binary builds, the test suite is green, and the `ingest` command (no LLM calls) still works against cached data.

- [ ] **Step 1: Clean rebuild**

```bash
dotnet build TradingSignalPoc.slnx --nologo -c Release 2>&1 | tail -5
```

Expected: build succeeds, 0 warnings, 0 errors.

- [ ] **Step 2: Full test pass**

```bash
dotnet test TradingSignalPoc.slnx --nologo 2>&1 | tail -10
```

Expected: all tests pass. Test count moves from 85 → ~105: -7 (old LlmSignalGeneratorTests removed) +5 (new orchestration tests) +6 (InstructCallStrategyTests, retry shape moved here) +9 (ReasoningCallStrategyTests) +3 (ServiceCollectionExtensionsTests) +2 (SqliteLlmResponseCacheTests new) +2 (SqlitePredictionStoreTests new). Exact number depends on the existing per-store test count.

- [ ] **Step 3: Ingest smoke (no LLM dependency)**

```bash
dotnet run --project src/TradingSignal.Console --no-build -- ingest 2>&1 | tail -10
```

Expected: "Ingested N candles" log line. No exceptions.

- [ ] **Step 4: Verify no uncommitted changes**

```bash
git status --short
```

Expected: empty (clean working tree).

---

### Task 14: README updates

Document the new config keys, the recommended values for reasoning-model runs, and the auto-migration behavior.

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Read current README configuration section**

```bash
grep -n "Configuration\|appsettings\|LmStudio\|environment" README.md | head -10
```

Locate the `Configuration` section (around line 89 in the version observed during planning).

- [ ] **Step 2: Replace the LmStudio bullet and add two new paragraphs**

In the `Configuration` section of `README.md`, expand the `LmStudio` bullet and add a new "Reasoning models" subsection right below it. The replacement reads:

```markdown
- `LmStudio` — endpoint, model id, timeouts, max few-shot, max output tokens. Two extra knobs control which call strategy is used:
  - `ModelFamily`: `"instruct"` (default — Gemma, Qwen2.5-instruct, etc.) or `"reasoning"` (Qwen3 thinking models like `qwen/qwen3.6-35b-a3b`).
  - `ReasoningEffort`: `"none" | "low" | "medium" | "high"` (default `"medium"`). Only honored by the reasoning strategy. Included in the cache key, so changing the knob between runs automatically invalidates stale entries.

### Reasoning models

When switching `ModelFamily` to `"reasoning"`, bump these two values to give the model room to think:

- `MaxOutputTokens`: from 256 to **2048** (a single reasoning response averages ~1000 tokens).
- `TimeoutSeconds`: from 60 to **120** (per-call latency runs 1–25 s at `medium` effort and skews higher at `high`).

The chain-of-thought trace is persisted to a new `reasoning` column on both `predictions.db` and `llm-cache.db`. Pre-existing DBs are migrated automatically on first open (an idempotent `ALTER TABLE`), so you can switch between families without wiping `runs/`.
```

- [ ] **Step 3: Build + test (smoke)**

```bash
dotnet build TradingSignalPoc.slnx --nologo 2>&1 | tail -3
```

Expected: no code change, build still clean.

- [ ] **Step 4: Commit**

```bash
git add README.md
git commit -m "Document ModelFamily, ReasoningEffort, and reasoning-model tuning"
```

---

## Self-review checklist (for the implementing engineer)

After finishing Task 14, confirm:

1. **Build:** `dotnet build TradingSignalPoc.slnx -c Release` — 0 warnings, 0 errors.
2. **Tests:** `dotnet test TradingSignalPoc.slnx` — all green; new test classes present (`InstructCallStrategyTests`, `ReasoningCallStrategyTests`, `ServiceCollectionExtensionsTests`, new tests added to `SqliteLlmResponseCacheTests` and `SqlitePredictionStoreTests`).
3. **Working tree:** `git status` clean.
4. **Manual reasoning-model smoke (optional — needs LM Studio):**
   - Flip `ModelFamily` to `"reasoning"` and `ModelId` to a loaded thinking model in `appsettings.json`.
   - Bump `MaxOutputTokens` to 2048 and `TimeoutSeconds` to 120.
   - `dotnet run --project src/TradingSignal.Console -- ingest` (cache hit).
   - `dotnet run --project src/TradingSignal.Console -- run`.
   - Query `runs/predictions.db`: `sqlite3 runs/predictions.db "SELECT reasoning IS NULL FROM predictions LIMIT 1"` should return `0` (column populated).
