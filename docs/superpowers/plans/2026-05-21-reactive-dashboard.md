# Reactive Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the dashboard render before any Anthropic call runs, then stream per-instrument suggestions into place as they arrive — with truly parallel calls, per-card error isolation, and cooperative cancellation.

**Architecture:** `LoadDashboardUseCase` becomes AI-free (live mode). A new `StreamTodaysSuggestionsUseCase` returns `IAsyncEnumerable<SuggestionStreamEvent>` driven by a `Channel<T>` + per-worker tasks gated by `SemaphoreSlim`. `SuggestionGate` is repartitioned by `(date, instrumentId)` so different instruments parallelize for real. A new `BuildFocusDerivedSliceUseCase` lifts the focus-derived data (CallDiff, IndicatorHistories, MarketSnapshot) out of the orchestration so it can be computed late. `DashboardPage.razor.cs` consumes the stream and mutates three private fields with `InvokeAsync(StateHasChanged)` per arrival.

**Tech Stack:** C# 13, .NET 10, Blazor Server, EF Core 10 (Sqlite), Ardalis.Specification 9.3, `Microsoft.Extensions.AI` 10.3 + `Anthropic.SDK` 5.10, xUnit v3 + Shouldly + hand-rolled fakes from `TradyStrat.TestKit` (no Moq, no NSubstitute, no bUnit), `WebApplicationFactory` for E2E.

**Worktree:** Work in `.claude/worktrees/phase3-ai-suggestion-improvements` (already created on branch `worktree-phase3-ai-suggestion-improvements`). Single atomic PR ships from this branch.

**Spec:** [`docs/superpowers/specs/2026-05-21-reactive-dashboard-design.md`](../specs/2026-05-21-reactive-dashboard-design.md) (commit `4e1f811`). Read it before starting — every task references back to specific spec sections.

**Out of scope:** ThinkingText UI surface, yesterday-outcome strip, confidence band, single-response streaming, replay CLI changes, MCP changes, `SuggestionBackfillCoordinator` changes, historical-mode rendering changes (stays synchronous).

---

## Phase 0 — Setup & baseline

### Task 0.1: Verify worktree + baseline

**Files:** none

- [ ] **Step 1:** Verify the working directory.

```bash
pwd
git branch --show-current
git log --oneline -1
```

Expected:
- pwd: `/Users/philippe/repo/gh-phmatray/TradyStrat/.claude/worktrees/phase3-ai-suggestion-improvements`
- branch: `worktree-phase3-ai-suggestion-improvements`
- latest commit subject contains `docs(specs): reactive dashboard design (Phase 4-A)`

If not, stop and reconcile with the controller.

- [ ] **Step 2:** Restore + build the full solution to establish a green baseline.

```bash
dotnet restore TradyStrat.slnx
dotnet build TradyStrat.slnx -c Debug --nologo
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3:** Run the full test suite.

```bash
dotnet test TradyStrat.slnx --nologo --verbosity quiet
```

Expected: all tests pass. Record the pass count (will be used in the final check).

- [ ] **Step 4:** No commit. This task records the baseline only.

---

## Phase 1 — Settings: `anthropic.maxParallelSuggestions`

Spec ref: §5.4.

### Task 1.1: Add the SettingsKeys constant

**Files:**
- Modify: `TradyStrat.Application/Settings/Config/SettingsKeys.cs`

- [ ] **Step 1:** Edit the file. Add the new constant inside the class, immediately after `AnthropicThinkingBudget`:

```csharp
namespace TradyStrat.Application.Settings.Config;

public static class SettingsKeys
{
    public const string AnthropicModel                = "anthropic.model";
    public const string AnthropicMaxTokens            = "anthropic.maxTokens";
    public const string AnthropicThinkingBudget       = "anthropic.thinkingBudget";
    public const string AnthropicMaxParallelSuggestions = "anthropic.maxParallelSuggestions";
    public const string PolymarketSearchQueries      = "polymarket.searchQueries";
    public const string PolymarketMaxMarkets         = "polymarket.maxMarkets";
    public const string PolymarketMinVolumeUsd       = "polymarket.minVolumeUsd";
    public const string PolymarketMaxHorizonDays     = "polymarket.maxHorizonDays";
    public const string TickersFocus                 = "tickers.focus";
}
```

(Realign columns to taste; the only semantic addition is `AnthropicMaxParallelSuggestions`.)

- [ ] **Step 2:** Verify it compiles.

```bash
dotnet build TradyStrat.Application/TradyStrat.Application.csproj --nologo
```

Expected: Build succeeded, 0 errors.

### Task 1.2: Extend the `AnthropicSettings` record

**Files:**
- Modify: `TradyStrat.Application/Settings/Config/SettingsModels.cs`

- [ ] **Step 1:** Edit the file. Add `MaxParallelSuggestions` as a positional parameter on the record:

```csharp
namespace TradyStrat.Application.Settings.Config;

public sealed record AnthropicSettings(
    string Model,
    int MaxTokens,
    int ThinkingBudget,
    int MaxParallelSuggestions);

public sealed record PolymarketSettings(
    IReadOnlyList<string> SearchQueries,
    int MaxMarkets,
    decimal MinVolumeUsd,
    int MaxHorizonDays);
```

- [ ] **Step 2:** This will break every construction site of `AnthropicSettings` until the next task. Acceptable for now.

### Task 1.3: Update `SettingsReader` to populate the new field

**Files:**
- Modify: `TradyStrat.Infrastructure/Settings/Config/SettingsReader.cs`

- [ ] **Step 1:** Edit `AnthropicAsync` to read the new key:

```csharp
public async Task<AnthropicSettings> AnthropicAsync(CancellationToken ct) => new(
    Model:                  await settings.GetAsync<string>(SettingsKeys.AnthropicModel, ct),
    MaxTokens:              await settings.GetAsync<int>(SettingsKeys.AnthropicMaxTokens, ct),
    ThinkingBudget:         await settings.GetAsync<int>(SettingsKeys.AnthropicThinkingBudget, ct),
    MaxParallelSuggestions: await settings.GetAsync<int>(SettingsKeys.AnthropicMaxParallelSuggestions, ct));
```

- [ ] **Step 2:** Verify it compiles.

```bash
dotnet build TradyStrat.Infrastructure/TradyStrat.Infrastructure.csproj --nologo
```

Expected: Build succeeded, 0 errors.

### Task 1.4: Register the setting in `SettingDescriptor`

**Files:**
- Modify: `TradyStrat.Application/Settings/Config/SettingDescriptor.cs`

- [ ] **Step 1:** Locate the existing block for `AnthropicThinkingBudget` (defaults `8192`, range `1024..16000`). Add a new entry immediately after it:

```csharp
new()
{
    Key = SettingsKeys.AnthropicMaxParallelSuggestions,
    DefaultRaw = "3",
    Parse = s => int.Parse(s, CultureInfo.InvariantCulture),
    Validate = v => RequireRange((int)v, 1, 10, "Max parallel suggestions"),
    Format = v => ((int)v).ToString(CultureInfo.InvariantCulture),
},
```

If `RequireRange` or `CultureInfo` are not already imported in this file, leave the existing pattern as-is — the surrounding entries already import them. Copy whichever local helper signature is used by the `ThinkingBudget` entry.

- [ ] **Step 2:** Verify it compiles.

```bash
dotnet build TradyStrat.Application/TradyStrat.Application.csproj --nologo
```

Expected: Build succeeded, 0 errors.

### Task 1.5: Round-trip test in `SettingsReaderTests`

**Files:**
- Modify: `TradyStrat.Infrastructure.Tests/Settings/Config/SettingsReaderTests.cs`

- [ ] **Step 1:** Locate the existing test that exercises `AnthropicAsync` round-trip (it currently sets `AnthropicThinkingBudget` via `svc.SetAsync(...)` and asserts the parsed record). Add an additional `SetAsync` for `AnthropicMaxParallelSuggestions` and a corresponding `ShouldBe(...)` assertion. If a single test covers the round-trip, extend it. If there is no such test today, add a new `[Fact]`:

```csharp
[Fact]
public async Task AnthropicAsync_round_trips_all_fields_including_MaxParallelSuggestions()
{
    await using var db = InMemoryDb.Create();
    var svc = BuildSettingsService(db);  // existing helper in this file
    var ct = TestContext.Current.CancellationToken;

    await svc.SetAsync(SettingsKeys.AnthropicModel, "claude-opus-4-7", ct);
    await svc.SetAsync(SettingsKeys.AnthropicMaxTokens, "1500", ct);
    await svc.SetAsync(SettingsKeys.AnthropicThinkingBudget, "8192", ct);
    await svc.SetAsync(SettingsKeys.AnthropicMaxParallelSuggestions, "5", ct);

    var reader = new SettingsReader(svc);
    var ai = await reader.AnthropicAsync(ct);

    ai.Model.ShouldBe("claude-opus-4-7");
    ai.MaxTokens.ShouldBe(1500);
    ai.ThinkingBudget.ShouldBe(8192);
    ai.MaxParallelSuggestions.ShouldBe(5);
}
```

(If `BuildSettingsService` is named differently, follow the existing test's helper.)

- [ ] **Step 2:** Run the test.

```bash
dotnet test TradyStrat.Infrastructure.Tests/TradyStrat.Infrastructure.Tests.csproj \
  --filter "FullyQualifiedName~SettingsReader" --nologo
```

Expected: PASS (including the new test).

### Task 1.6: Commit Phase 1

- [ ] **Step 1:** Stage and commit.

```bash
git add \
  TradyStrat.Application/Settings/Config/SettingsKeys.cs \
  TradyStrat.Application/Settings/Config/SettingsModels.cs \
  TradyStrat.Application/Settings/Config/SettingDescriptor.cs \
  TradyStrat.Infrastructure/Settings/Config/SettingsReader.cs \
  TradyStrat.Infrastructure.Tests/Settings/Config/SettingsReaderTests.cs

git commit -m "$(cat <<'EOF'
feat(settings): anthropic.maxParallelSuggestions (default 3)

New AnthropicSettings.MaxParallelSuggestions, wired through SettingsReader
+ SettingDescriptor with range 1..10. UI input lands in a later phase.
EOF
)"
```

- [ ] **Step 2:** Verify the working tree is clean.

```bash
git status --short
```

Expected: empty output.

---

## Phase 2 — Foundation types

Spec ref: §5.1.

### Task 2.1: Create `SuggestionState`

**Files:**
- Create: `TradyStrat.Application/Dashboard/SuggestionState.cs`

- [ ] **Step 1:** Create the file with the three cases:

```csharp
using TradyStrat.Domain;

namespace TradyStrat.Application.Dashboard;

/// <summary>
/// Live state of an AI suggestion on the dashboard. <c>null</c> (at the
/// reference site) means "no suggestion expected" (watchlist instruments,
/// or historical-mode loads where the row simply doesn't exist).
/// </summary>
public abstract record SuggestionState
{
    public sealed record Pending : SuggestionState;
    public sealed record Ready(Suggestion Suggestion) : SuggestionState;
    public sealed record Failed(string Reason) : SuggestionState;
}
```

- [ ] **Step 2:** Verify it compiles.

```bash
dotnet build TradyStrat.Application/TradyStrat.Application.csproj --nologo
```

Expected: Build succeeded, 0 errors.

### Task 2.2: Create `SuggestionStreamEvent`

**Files:**
- Create: `TradyStrat.Application/AiSuggestion/SuggestionStreamEvent.cs`

- [ ] **Step 1:** Create the file:

```csharp
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion;

/// <summary>
/// One event emitted by <see cref="UseCases.StreamTodaysSuggestionsUseCase"/>
/// — either a successful suggestion or a per-instrument failure. The stream
/// emits one event per held instrument; failures never block other workers.
/// </summary>
public abstract record SuggestionStreamEvent(int InstrumentId)
{
    public sealed record Ready(int InstrumentId, Suggestion Suggestion)
        : SuggestionStreamEvent(InstrumentId);

    public sealed record Failed(int InstrumentId, string Reason)
        : SuggestionStreamEvent(InstrumentId);
}
```

- [ ] **Step 2:** Verify it compiles.

```bash
dotnet build TradyStrat.Application/TradyStrat.Application.csproj --nologo
```

Expected: Build succeeded, 0 errors.

### Task 2.3: Create `FocusDerivedSlice`

**Files:**
- Create: `TradyStrat.Application/Dashboard/FocusDerivedSlice.cs`

- [ ] **Step 1:** Create the file:

```csharp
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Domain;

namespace TradyStrat.Application.Dashboard;

/// <summary>
/// Focus-specific derived data computed from a single Suggestion: the
/// call-diff against the prior suggestion, citation-keyed indicator
/// histories, and the deserialized market snapshot. Built late (after
/// the focus suggestion arrives) by <see cref="UseCases.BuildFocusDerivedSliceUseCase"/>.
/// </summary>
public sealed record FocusDerivedSlice(
    CallDiff CallDiff,
    IReadOnlyDictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries> IndicatorHistories,
    MarketSnapshot MarketSnapshot)
{
    public static readonly FocusDerivedSlice Empty = new(
        CallDiff.None,
        new Dictionary<(string, IndicatorKind), IndicatorSeries>(),
        MarketSnapshot.Empty);
}
```

If `CallDiff.None` or `MarketSnapshot.Empty` is not the exact accessor name in the current code, adjust:

```bash
grep -n "public static.*None" TradyStrat.Application/AiSuggestion/CallDiff/CallDiff.cs
grep -n "public static.*Empty" TradyStrat.Application/PredictionMarkets/MarketSnapshot.cs
```

Use whatever exists. The intent: `FocusDerivedSlice.Empty` is the "no focus suggestion yet" sentinel.

- [ ] **Step 2:** Verify it compiles.

```bash
dotnet build TradyStrat.Application/TradyStrat.Application.csproj --nologo
```

Expected: Build succeeded, 0 errors.

### Task 2.4: Commit Phase 2

- [ ] **Step 1:** Stage and commit.

```bash
git add \
  TradyStrat.Application/Dashboard/SuggestionState.cs \
  TradyStrat.Application/Dashboard/FocusDerivedSlice.cs \
  TradyStrat.Application/AiSuggestion/SuggestionStreamEvent.cs

git commit -m "$(cat <<'EOF'
feat(dashboard): SuggestionState, SuggestionStreamEvent, FocusDerivedSlice

Foundation types for the reactive dashboard. SuggestionState models the
three live states (Pending / Ready / Failed); SuggestionStreamEvent is
the stream payload; FocusDerivedSlice carries the focus-only late-built
data (CallDiff, IndicatorHistories, MarketSnapshot).
EOF
)"
```

- [ ] **Step 2:** Verify clean tree.

```bash
git status --short
```

Expected: empty.

---

## Phase 3 — Partition `SuggestionGate`

Spec ref: §5.2.

### Task 3.1: Failing tests for the partitioned gate

**Files:**
- Create: `TradyStrat.Application.Tests/AiSuggestion/UseCases/SuggestionGateTests.cs`

Note: `SuggestionGate` is currently `internal static`. The test project needs access. Check if `InternalsVisibleTo` is configured:

```bash
grep -n "InternalsVisibleTo" TradyStrat.Application/TradyStrat.Application.csproj
```

If `TradyStrat.Application.Tests` is **not** listed, add it. Edit `TradyStrat.Application/TradyStrat.Application.csproj` to include an `ItemGroup` (if one isn't present already with the test assembly attribute):

```xml
<ItemGroup>
  <InternalsVisibleTo Include="TradyStrat.Application.Tests" />
</ItemGroup>
```

- [ ] **Step 1:** Create the test file:

```csharp
using Shouldly;
using TradyStrat.Application.AiSuggestion.UseCases;
using Xunit;

namespace TradyStrat.Application.Tests.AiSuggestion.UseCases;

public class SuggestionGateTests
{
    [Fact]
    public async Task Different_keys_do_not_block_each_other()
    {
        var date = new DateOnly(2026, 5, 21);
        var gateA = SuggestionGate.For(date, 1);
        var gateB = SuggestionGate.For(date, 2);

        await gateA.WaitAsync(TestContext.Current.CancellationToken);
        try
        {
            // gateB must be immediately acquirable while gateA is held.
            var acquiredB = await gateB.WaitAsync(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
            acquiredB.ShouldBeTrue();
            gateB.Release();
        }
        finally
        {
            gateA.Release();
        }
    }

    [Fact]
    public async Task Same_key_serializes()
    {
        var date = new DateOnly(2026, 5, 21);
        var first = SuggestionGate.For(date, 42);
        var second = SuggestionGate.For(date, 42);

        // Same key must return the same SemaphoreSlim instance.
        first.ShouldBeSameAs(second);

        await first.WaitAsync(TestContext.Current.CancellationToken);
        try
        {
            var acquired = await second.WaitAsync(TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);
            acquired.ShouldBeFalse();
        }
        finally
        {
            first.Release();
        }
    }

    [Fact]
    public void Different_dates_same_instrument_do_not_share_a_gate()
    {
        var monday = SuggestionGate.For(new DateOnly(2026, 5, 18), 1);
        var tuesday = SuggestionGate.For(new DateOnly(2026, 5, 19), 1);
        monday.ShouldNotBeSameAs(tuesday);
    }
}
```

- [ ] **Step 2:** Run the tests; they must fail because `SuggestionGate.For` does not exist yet.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~SuggestionGateTests" --nologo
```

Expected: **compile error** (`'SuggestionGate' does not contain a definition for 'For'`).

### Task 3.2: Repartition `SuggestionGate`

**Files:**
- Modify: `TradyStrat.Application/AiSuggestion/UseCases/SuggestionGate.cs`

- [ ] **Step 1:** Replace the file contents entirely:

```csharp
using System.Collections.Concurrent;

namespace TradyStrat.Application.AiSuggestion.UseCases;

/// <summary>
/// Per-(date, instrumentId) mutex serializing today's-suggestion writes.
///
/// The UQ(ForDate, InstrumentId) constraint means two concurrent inserts
/// for the same (date, instrument) pair race. The gate forces the second
/// writer to wait, then re-check; if the first inserted, the second sees
/// the existing row instead of calling the AI a second time.
///
/// Different (date, instrument) keys do not block each other — that's the
/// whole point of partitioning: the dashboard fans out per held instrument
/// and we want those calls to run truly in parallel.
///
/// Static because use cases are scoped per request, but the lock must be
/// shared across all scopes. Entries are not reclaimed; growth is bounded
/// by (held instruments) × (days), which is negligible for a single-user app.
/// </summary>
internal static class SuggestionGate
{
    private static readonly ConcurrentDictionary<(DateOnly Date, int InstrumentId), SemaphoreSlim> Gates = new();

    public static SemaphoreSlim For(DateOnly date, int instrumentId)
        => Gates.GetOrAdd((date, instrumentId), _ => new SemaphoreSlim(1, 1));
}
```

- [ ] **Step 2:** Run the failing tests; they should now pass.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~SuggestionGateTests" --nologo
```

Expected: PASS (3/3).

### Task 3.3: Update `GetTodaysSuggestionUseCase` to use the partitioned gate

**Files:**
- Modify: `TradyStrat.Application/AiSuggestion/UseCases/GetTodaysSuggestionUseCase.cs`

- [ ] **Step 1:** Locate the existing line `await SuggestionGate.Instance.WaitAsync(ct);` (~line 38) and the matching `SuggestionGate.Instance.Release();` (~line 52). Replace both:

```csharp
var gate = SuggestionGate.For(today, instrument.Id);
await gate.WaitAsync(ct);
try
{
    existing = await repo.FirstOrDefaultAsync(
        new SuggestionForDateSpec(today, instrument.Id), ct);
    if (existing is not null) return existing;

    var snap  = await snapshotService.CreateAsync(instrument.Id, today, ct);
    var fresh = await ai.AskAsync(snap, ct);
    await repo.AddAsync(fresh, ct);
    return fresh;
}
finally
{
    gate.Release();
}
```

(Replaces lines ~38–53 of the current file. The new code captures the per-key semaphore in a local and releases the same instance.)

- [ ] **Step 2:** Build everything that depends on this.

```bash
dotnet build TradyStrat.slnx --nologo
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3:** Run all Application + Infrastructure tests to confirm nothing regressed:

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj --nologo --verbosity quiet
dotnet test TradyStrat.Infrastructure.Tests/TradyStrat.Infrastructure.Tests.csproj --nologo --verbosity quiet
```

Expected: all green.

### Task 3.4: Commit Phase 3

- [ ] **Step 1:** Stage and commit.

```bash
git add \
  TradyStrat.Application/AiSuggestion/UseCases/SuggestionGate.cs \
  TradyStrat.Application/AiSuggestion/UseCases/GetTodaysSuggestionUseCase.cs \
  TradyStrat.Application.Tests/AiSuggestion/UseCases/SuggestionGateTests.cs \
  TradyStrat.Application/TradyStrat.Application.csproj
# (only include the csproj line if InternalsVisibleTo was actually added in Task 3.1)

git commit -m "$(cat <<'EOF'
feat(ai-suggestion): partition SuggestionGate by (date, instrumentId)

Different instruments no longer serialize through the same process-wide
mutex — only racing writers for the SAME (date, instrumentId) pair wait
on each other. Unblocks per-ticker parallelism for the reactive
dashboard. Same UQ-violation prevention guarantee, now per-key.
EOF
)"
```

- [ ] **Step 2:** Clean tree check.

```bash
git status --short
```

Expected: empty.

---

## Phase 4 — TestKit: `FakeAiClient`

Spec ref: §10.1 ("A new `FakeGetTodaysSuggestion` test double…"). We implement it as a `FakeAiClient` because the codebase wires `IAiClient` into the use case — the cleanest seam.

### Task 4.1: Build `FakeAiClient`

**Files:**
- Create: `TradyStrat.TestKit/AiSuggestion/FakeAiClient.cs`

- [ ] **Step 1:** Create the file:

```csharp
using System.Collections.Concurrent;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Domain;

namespace TradyStrat.TestKit.AiSuggestion;

/// <summary>
/// Test double for <see cref="IAiClient"/> with per-instrument scripting.
/// Records peak observed concurrent calls so callers can assert
/// SemaphoreSlim caps. Unknown instrument ids throw — tests must
/// configure every instrument they invoke through.
/// </summary>
public sealed class FakeAiClient : IAiClient
{
    private readonly ConcurrentDictionary<int, Func<CancellationToken, Task<Suggestion>>> _byInstrument = new();
    private int _inFlight;
    private int _maxObserved;

    public int MaxObservedConcurrency => Volatile.Read(ref _maxObserved);

    /// <summary>Returns the configured suggestion immediately.</summary>
    public void ConfigureFor(int instrumentId, Suggestion result)
        => _byInstrument[instrumentId] = _ => Task.FromResult(result);

    /// <summary>Returns the configured suggestion after <paramref name="delay"/> (cooperative cancellation).</summary>
    public void ConfigureFor(int instrumentId, Suggestion result, TimeSpan delay)
        => _byInstrument[instrumentId] = async ct =>
        {
            await Task.Delay(delay, ct);
            return result;
        };

    /// <summary>Throws <paramref name="error"/> when the worker for this id runs.</summary>
    public void ConfigureFailureFor(int instrumentId, Exception error)
        => _byInstrument[instrumentId] = _ => Task.FromException<Suggestion>(error);

    /// <summary>Throws after <paramref name="delay"/>.</summary>
    public void ConfigureFailureFor(int instrumentId, Exception error, TimeSpan delay)
        => _byInstrument[instrumentId] = async ct =>
        {
            await Task.Delay(delay, ct);
            throw error;
        };

    public async Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
    {
        var current = Interlocked.Increment(ref _inFlight);
        try
        {
            // Atomic max — CompareExchange loop.
            int observed;
            do { observed = _maxObserved; }
            while (current > observed &&
                   Interlocked.CompareExchange(ref _maxObserved, current, observed) != observed);

            if (!_byInstrument.TryGetValue(snapshot.InstrumentId, out var handler))
                throw new InvalidOperationException(
                    $"FakeAiClient: no configuration for InstrumentId {snapshot.InstrumentId}.");

            return await handler(ct);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }
}
```

If `AiSnapshot.InstrumentId` is not the actual property name (verify), substitute the real one. Quick check:

```bash
grep -n "InstrumentId\|public int" TradyStrat.Application/AiSuggestion/Snapshot/AiSnapshot.cs | head -10
```

Use the actual property. If the snapshot exposes the id under a different name, adapt the dictionary lookup.

- [ ] **Step 2:** Build the TestKit project.

```bash
dotnet build TradyStrat.TestKit/TradyStrat.TestKit.csproj --nologo
```

Expected: Build succeeded, 0 errors.

### Task 4.2: Commit Phase 4

- [ ] **Step 1:** Stage and commit.

```bash
git add TradyStrat.TestKit/AiSuggestion/FakeAiClient.cs

git commit -m "$(cat <<'EOF'
test(testkit): FakeAiClient with per-instrument config + concurrency gauge

Enables stream-fan-out tests to assert ordering, failure isolation,
and the SemaphoreSlim cap. Unknown instrument ids throw to force
explicit test setup.
EOF
)"
```

- [ ] **Step 2:** Clean tree check.

```bash
git status --short
```

Expected: empty.

---

## Phase 5 — `StreamTodaysSuggestionsUseCase`

Spec ref: §5.1, §6.

### Task 5.1: Failing test — empty input yields no events

**Files:**
- Create: `TradyStrat.Application.Tests/AiSuggestion/UseCases/StreamTodaysSuggestionsUseCaseTests.cs`

- [ ] **Step 1:** Create the test file with helpers and the first test:

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;
using TradyStrat.TestKit;
using TradyStrat.TestKit.AiSuggestion;
using TradyStrat.TestKit.Settings;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.AiSuggestion.UseCases;

public class StreamTodaysSuggestionsUseCaseTests
{
    private static readonly DateOnly Today = new(2026, 5, 21);

    [Fact]
    public async Task Empty_input_yields_no_events()
    {
        var sut = BuildSut(out _);

        var events = new List<SuggestionStreamEvent>();
        await foreach (var ev in sut.StreamAsync(Array.Empty<int>(), TestContext.Current.CancellationToken))
            events.Add(ev);

        events.ShouldBeEmpty();
    }

    // ---------- Helpers ----------

    /// <summary>Builds the SUT with a fresh in-memory DB and a fake AI client. Seeds one instrument per id supplied.</summary>
    private static StreamTodaysSuggestionsUseCase BuildSut(
        out FakeAiClient fake,
        int maxParallel = 3,
        params int[] instrumentIds)
    {
        fake = new FakeAiClient();
        var db = InMemoryDb.Create();
        foreach (var id in instrumentIds)
        {
            db.Instruments.Add(new Instrument
            {
                Id = id,
                Ticker = $"T{id}",
                Currency = "USD",
                Kind = InstrumentKind.Held,
                Exchange = "TST",
            });
        }
        db.SaveChanges();

        var instrumentRepo = new TestRepo<Instrument>(db);
        var suggestionRepo = new TestRepo<Suggestion>(db);
        var snapshot = new StubSnapshotFactory(Today);
        var clock = new FixedClock(Today);

        var getOne = new GetTodaysSuggestionUseCase(
            suggestionRepo,
            snapshot,
            fake,
            clock,
            instrumentRepo,
            NullLogger<GetTodaysSuggestionUseCase>.Instance);

        var anthropic = new AnthropicSettings("test", 1500, 8192, maxParallel);
        var reader = new FakeSettingsReader(anthropic: anthropic);

        return new StreamTodaysSuggestionsUseCase(
            getOne,
            reader,
            NullLogger<StreamTodaysSuggestionsUseCase>.Instance);
    }

    /// <summary>Cheap fixed-date clock for tests.</summary>
    private sealed class FixedClock(DateOnly today) : IClock
    {
        public DateOnly TodayInExchangeTzFor(string ticker) => today;
        public DateTime UtcNow() => DateTime.UtcNow;
    }
}
```

Verifications before assuming the snippet compiles:
- `StubSnapshotFactory` exists in `TradyStrat.TestKit/AiSuggestion/`. If its constructor signature differs from `(DateOnly today)`, adjust to whatever the existing test fixtures use.
- `IClock` interface members: confirm via `grep -n "interface IClock" TradyStrat.Domain/IClock.cs`. If `UtcNow()` is not on it, drop that line.
- `Instrument` construction: this codebase uses positional init-only props; verify and adjust property names if necessary.

If any of these mismatch, fix the helper in this task (it'll be reused by every test below).

- [ ] **Step 2:** Run the test.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~StreamTodaysSuggestionsUseCase" --nologo
```

Expected: **compile error** — `StreamTodaysSuggestionsUseCase` does not exist yet.

### Task 5.2: Implement the use case skeleton

**Files:**
- Create: `TradyStrat.Application/AiSuggestion/UseCases/StreamTodaysSuggestionsUseCase.cs`

- [ ] **Step 1:** Create the file with the bare skeleton (will be expanded in Task 5.4):

```csharp
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.UseCases;

/// <summary>
/// Streams today's AI suggestion for each held instrument as soon as it
/// arrives. Fans out per-instrument calls under a SemaphoreSlim gate; one
/// failure does not block other workers (each yields its own
/// <see cref="SuggestionStreamEvent.Failed"/>). Cancellation cascades from
/// the consumer to every in-flight Anthropic call.
/// </summary>
public sealed class StreamTodaysSuggestionsUseCase(
    GetTodaysSuggestionUseCase getOne,
    ISettingsReader settings,
    ILogger<StreamTodaysSuggestionsUseCase> log)
{
    public async IAsyncEnumerable<SuggestionStreamEvent> StreamAsync(
        IReadOnlyCollection<int> heldInstrumentIds,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (heldInstrumentIds.Count == 0) yield break;

        var ai = await settings.AnthropicAsync(ct);
        var maxParallel = Math.Max(1, ai.MaxParallelSuggestions);

        var chan = Channel.CreateUnbounded<SuggestionStreamEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
        using var sem = new SemaphoreSlim(maxParallel, maxParallel);

        var workers = heldInstrumentIds
            .Select(id => RunWorkerAsync(id, sem, chan.Writer, ct))
            .ToArray();

        _ = Task.WhenAll(workers).ContinueWith(
            _ => chan.Writer.TryComplete(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        await foreach (var ev in chan.Reader.ReadAllAsync(ct))
            yield return ev;
    }

    private async Task RunWorkerAsync(
        int instrumentId,
        SemaphoreSlim sem,
        ChannelWriter<SuggestionStreamEvent> writer,
        CancellationToken ct)
    {
        try
        {
            await sem.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var s = await getOne.ExecuteAsync(new GetTodaysSuggestionInput(instrumentId), ct).ConfigureAwait(false);
                writer.TryWrite(new SuggestionStreamEvent.Ready(instrumentId, s));
            }
            finally
            {
                sem.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Cooperative cancellation — drop silently.
        }
        catch (Exception ex)
        {
            LogPerInstrumentFailure(log, ex, instrumentId);
            writer.TryWrite(new SuggestionStreamEvent.Failed(instrumentId, ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stream worker failed for instrument {InstrumentId}")]
    private static partial void LogPerInstrumentFailure(ILogger logger, Exception ex, int instrumentId);
}
```

Note: the class must be marked `partial` to use `[LoggerMessage]`. Add `partial`:

```csharp
public sealed partial class StreamTodaysSuggestionsUseCase(
```

- [ ] **Step 2:** Run the empty-input test.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~StreamTodaysSuggestionsUseCase" --nologo
```

Expected: `Empty_input_yields_no_events` PASS. Other tests don't exist yet.

### Task 5.3: Test — single instrument happy path

**Files:**
- Modify: `TradyStrat.Application.Tests/AiSuggestion/UseCases/StreamTodaysSuggestionsUseCaseTests.cs`

- [ ] **Step 1:** Add the test (and a helper for building a sample Suggestion):

```csharp
[Fact]
public async Task Single_instrument_yields_one_Ready_event()
{
    var sut = BuildSut(out var fake, maxParallel: 3, instrumentIds: [7]);
    fake.ConfigureFor(7, MkSuggestion(7));

    var events = new List<SuggestionStreamEvent>();
    await foreach (var ev in sut.StreamAsync(new[] { 7 }, TestContext.Current.CancellationToken))
        events.Add(ev);

    events.Count.ShouldBe(1);
    events[0].ShouldBeOfType<SuggestionStreamEvent.Ready>().InstrumentId.ShouldBe(7);
}

private static Suggestion MkSuggestion(int instrumentId, SuggestionAction action = SuggestionAction.Hold, int conviction = 5)
    => new()
    {
        InstrumentId = instrumentId,
        ForDate = Today,
        Action = action,
        Conviction = conviction,
        Rationale = "test",
        Citations = Array.Empty<Citation>(),
        CreatedAt = DateTime.UtcNow,
    };
```

Verify `Suggestion`'s actual init shape:

```bash
head -50 TradyStrat.Domain/Suggestion.cs
```

If properties differ (e.g. required-init, different names), align the helper to whatever compiles.

- [ ] **Step 2:** Run the test.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~Single_instrument_yields_one_Ready_event" --nologo
```

Expected: PASS.

### Task 5.4: Test — multi-instrument, ordering by completion

**Files:**
- Modify: `TradyStrat.Application.Tests/AiSuggestion/UseCases/StreamTodaysSuggestionsUseCaseTests.cs`

- [ ] **Step 1:** Add the test:

```csharp
[Fact]
public async Task Yields_in_completion_order_not_input_order()
{
    var sut = BuildSut(out var fake, maxParallel: 3, instrumentIds: [1, 2, 3]);
    fake.ConfigureFor(1, MkSuggestion(1), TimeSpan.FromMilliseconds(120));
    fake.ConfigureFor(2, MkSuggestion(2), TimeSpan.FromMilliseconds(20));
    fake.ConfigureFor(3, MkSuggestion(3), TimeSpan.FromMilliseconds(60));

    var order = new List<int>();
    await foreach (var ev in sut.StreamAsync(new[] { 1, 2, 3 }, TestContext.Current.CancellationToken))
        order.Add(ev.InstrumentId);

    order.ShouldBe(new[] { 2, 3, 1 });
}
```

- [ ] **Step 2:** Run it.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~Yields_in_completion_order" --nologo
```

Expected: PASS.

### Task 5.5: Test — one failure does not block others

**Files:**
- Modify: `TradyStrat.Application.Tests/AiSuggestion/UseCases/StreamTodaysSuggestionsUseCaseTests.cs`

- [ ] **Step 1:** Add:

```csharp
[Fact]
public async Task One_failed_instrument_does_not_block_others()
{
    var sut = BuildSut(out var fake, maxParallel: 3, instrumentIds: [10, 20, 30]);
    fake.ConfigureFor(10, MkSuggestion(10));
    fake.ConfigureFailureFor(20, new InvalidOperationException("boom"));
    fake.ConfigureFor(30, MkSuggestion(30));

    var events = new List<SuggestionStreamEvent>();
    await foreach (var ev in sut.StreamAsync(new[] { 10, 20, 30 }, TestContext.Current.CancellationToken))
        events.Add(ev);

    events.Count.ShouldBe(3);
    events.OfType<SuggestionStreamEvent.Ready>().Select(e => e.InstrumentId).OrderBy(x => x).ShouldBe(new[] { 10, 30 });
    var fail = events.OfType<SuggestionStreamEvent.Failed>().ShouldHaveSingleItem();
    fail.InstrumentId.ShouldBe(20);
    fail.Reason.ShouldContain("boom");
}
```

- [ ] **Step 2:** Run it.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~One_failed_instrument" --nologo
```

Expected: PASS.

### Task 5.6: Test — concurrency cap

**Files:**
- Modify: `TradyStrat.Application.Tests/AiSuggestion/UseCases/StreamTodaysSuggestionsUseCaseTests.cs`

- [ ] **Step 1:** Add:

```csharp
[Fact]
public async Task Respects_max_parallel_cap()
{
    var ids = new[] { 1, 2, 3, 4, 5 };
    var sut = BuildSut(out var fake, maxParallel: 2, instrumentIds: ids);
    foreach (var id in ids)
        fake.ConfigureFor(id, MkSuggestion(id), TimeSpan.FromMilliseconds(30));

    await foreach (var _ in sut.StreamAsync(ids, TestContext.Current.CancellationToken)) { }

    fake.MaxObservedConcurrency.ShouldBeLessThanOrEqualTo(2);
    fake.MaxObservedConcurrency.ShouldBeGreaterThanOrEqualTo(1);
}
```

- [ ] **Step 2:** Run it.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~Respects_max_parallel_cap" --nologo
```

Expected: PASS.

### Task 5.7: Test — cancellation halts the stream

**Files:**
- Modify: `TradyStrat.Application.Tests/AiSuggestion/UseCases/StreamTodaysSuggestionsUseCaseTests.cs`

- [ ] **Step 1:** Add:

```csharp
[Fact]
public async Task Cancellation_stops_new_starts()
{
    var ids = new[] { 1, 2, 3 };
    var sut = BuildSut(out var fake, maxParallel: 1, instrumentIds: ids);
    foreach (var id in ids)
        fake.ConfigureFor(id, MkSuggestion(id), TimeSpan.FromMilliseconds(100));

    using var cts = new CancellationTokenSource();
    var received = new List<int>();

    var enumerate = Task.Run(async () =>
    {
        try
        {
            await foreach (var ev in sut.StreamAsync(ids, cts.Token))
                received.Add(ev.InstrumentId);
        }
        catch (OperationCanceledException) { /* expected */ }
    });

    await Task.Delay(40);   // let the first worker get started
    cts.Cancel();
    await enumerate;

    received.Count.ShouldBeLessThan(ids.Length);
}
```

- [ ] **Step 2:** Run it.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~Cancellation_stops_new_starts" --nologo
```

Expected: PASS.

### Task 5.8: Test — duplicate ids are deduplicated

**Files:**
- Modify: `TradyStrat.Application.Tests/AiSuggestion/UseCases/StreamTodaysSuggestionsUseCaseTests.cs`

This guards against the page passing the same id twice (a code smell, but cheap to test).

- [ ] **Step 1:** Add a test asserting one event per *distinct* id:

```csharp
[Fact]
public async Task Duplicate_ids_yield_one_event_each_input_position()
{
    var sut = BuildSut(out var fake, maxParallel: 3, instrumentIds: [9]);
    fake.ConfigureFor(9, MkSuggestion(9));

    // Pass the same id twice. The stream is dumb: each input element gets a
    // worker. This test documents that contract so callers don't expect dedup.
    var events = new List<SuggestionStreamEvent>();
    await foreach (var ev in sut.StreamAsync(new[] { 9, 9 }, TestContext.Current.CancellationToken))
        events.Add(ev);

    events.Count.ShouldBe(2);
    events.ShouldAllBe(e => e.InstrumentId == 9);
}
```

- [ ] **Step 2:** Run it.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~Duplicate_ids" --nologo
```

Expected: PASS.

### Task 5.9: Register the use case in DI

**Files:**
- Modify: `TradyStrat.Application/AiSuggestion/AiSuggestionApplicationModule.cs`

- [ ] **Step 1:** Read the file to learn the registration pattern:

```bash
cat TradyStrat.Application/AiSuggestion/AiSuggestionApplicationModule.cs
```

- [ ] **Step 2:** Add a Scoped registration for `StreamTodaysSuggestionsUseCase` next to the existing `GetAllTodaysSuggestionsUseCase` registration. Match the pattern used by the surrounding entries — if they use `services.AddScoped<X>()`, do the same:

```csharp
services.AddScoped<StreamTodaysSuggestionsUseCase>();
```

- [ ] **Step 3:** Build the solution.

```bash
dotnet build TradyStrat.slnx --nologo
```

Expected: Build succeeded, 0 errors.

### Task 5.10: Commit Phase 5

- [ ] **Step 1:** Stage and commit.

```bash
git add \
  TradyStrat.Application/AiSuggestion/UseCases/StreamTodaysSuggestionsUseCase.cs \
  TradyStrat.Application/AiSuggestion/AiSuggestionApplicationModule.cs \
  TradyStrat.Application.Tests/AiSuggestion/UseCases/StreamTodaysSuggestionsUseCaseTests.cs

git commit -m "$(cat <<'EOF'
feat(ai-suggestion): StreamTodaysSuggestionsUseCase — per-instrument stream

Returns IAsyncEnumerable<SuggestionStreamEvent>. Channel-based fan-out
gated by SemaphoreSlim with configurable cap. Per-worker failure
isolation, cooperative cancellation, completion-order yield.
EOF
)"
```

- [ ] **Step 2:** Clean tree.

```bash
git status --short
```

Expected: empty.

---

## Phase 6 — `BuildFocusDerivedSliceUseCase`

Spec ref: §5.1.

### Task 6.1: Failing test — empty inputs

**Files:**
- Create: `TradyStrat.Application.Tests/Dashboard/UseCases/BuildFocusDerivedSliceUseCaseTests.cs`

The current focus-derivation logic lives in `LoadDashboardUseCase` lines 135–179 (call diff, indicator histories, market snapshot). This use case lifts it.

- [ ] **Step 1:** Create the test file:

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Application.Dashboard;
using TradyStrat.Application.Dashboard.UseCases;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Indicators;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.Dashboard.UseCases;

public class BuildFocusDerivedSliceUseCaseTests
{
    private static readonly DateOnly Today = new(2026, 5, 21);
    private const int InstrId = 1;
    private const string Ticker = "TST";

    [Fact]
    public async Task No_prior_returns_CallDiff_None_and_populated_others()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, InstrId, Ticker);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var focus = MkSuggestion(InstrId, Today,
            citations: Array.Empty<Citation>(),
            marketJson: null);

        var sut = BuildSut(db);
        var slice = await sut.BuildAsync(focus, Today, TestContext.Current.CancellationToken);

        slice.CallDiff.ShouldBe(CallDiff.None);
        slice.IndicatorHistories.ShouldBeEmpty();
        slice.MarketSnapshot.ShouldBe(MarketSnapshot.Empty);
    }

    // ---------- Helpers ----------

    private static BuildFocusDerivedSliceUseCase BuildSut(AppDbContext db)
        => new(
            new TestRepo<Suggestion>(db),
            new StubIndicatorEngine(),
            NullLogger<BuildFocusDerivedSliceUseCase>.Instance);

    private static void SeedInstrument(AppDbContext db, int id, string ticker)
        => db.Instruments.Add(new Instrument { Id = id, Ticker = ticker, Currency = "USD", Kind = InstrumentKind.Held, Exchange = "TST" });

    private static Suggestion MkSuggestion(int instrumentId, DateOnly forDate,
        IReadOnlyList<Citation>? citations = null,
        string? marketJson = null)
        => new()
        {
            InstrumentId = instrumentId,
            ForDate = forDate,
            Action = SuggestionAction.Hold,
            Conviction = 5,
            Rationale = "t",
            Citations = citations ?? Array.Empty<Citation>(),
            MarketSnapshotJson = marketJson,
            CreatedAt = DateTime.UtcNow,
        };
}
```

If `StubIndicatorEngine` exists in `TradyStrat.TestKit/Indicators/`, use it. Otherwise, declare a minimal nested stub class in the test file:

```csharp
private sealed class StubIndicatorEngine : IIndicatorEngine
{
    public Task<IndicatorBundle> ComputeFor(string ticker, DateOnly asOf, CancellationToken ct)
        => throw new NotSupportedException("ComputeFor not used in this test.");
    public Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int window, DateOnly asOf, CancellationToken ct)
        => Task.FromResult(new IndicatorSeries(ticker, kind, Array.Empty<IndicatorReading>()));
}
```

Match `IIndicatorEngine`'s actual method signatures:

```bash
grep -n "public\|interface" TradyStrat.Application/Indicators/IIndicatorEngine.cs
grep -n "record IndicatorSeries\|class IndicatorSeries" TradyStrat.Application/Indicators/IndicatorSeries.cs
```

- [ ] **Step 2:** Run; expect compile failure (`BuildFocusDerivedSliceUseCase` does not exist).

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~BuildFocusDerivedSliceUseCase" --nologo
```

Expected: **compile error**.

### Task 6.2: Implement the use case

**Files:**
- Create: `TradyStrat.Application/Dashboard/UseCases/BuildFocusDerivedSliceUseCase.cs`

- [ ] **Step 1:** Create the file. The implementation lifts the existing logic from `LoadDashboardUseCase` lines 135–179:

```csharp
using System.Text.Json;
using Ardalis.Specification;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Application.AiSuggestion.Specifications;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Domain;

namespace TradyStrat.Application.Dashboard.UseCases;

/// <summary>
/// Computes the focus-only derived slice from a single suggestion:
/// CallDiff vs the immediately prior suggestion, citation-keyed indicator
/// histories, and the deserialized market snapshot. Designed to be called
/// after the focus suggestion arrives (i.e. after the dashboard skeleton
/// has already rendered).
/// </summary>
public sealed partial class BuildFocusDerivedSliceUseCase(
    IReadRepositoryBase<Suggestion> suggestionRepo,
    IIndicatorEngine indicators,
    ILogger<BuildFocusDerivedSliceUseCase> log)
{
    private const int SparklineWindow = 30;

    public async Task<FocusDerivedSlice> BuildAsync(
        Suggestion focus,
        DateOnly targetDate,
        CancellationToken ct)
    {
        // 1. Prior + CallDiff
        var prior = await suggestionRepo.FirstOrDefaultAsync(
            new PriorSuggestionSpec(targetDate, focus.InstrumentId), ct);

        var callDiff = new CallDiffBuilder()
            .WithToday(focus)
            .WithPrior(prior)
            .Build();

        // 2. Indicator histories per citation (de-duped by (ticker, kind))
        var histories = new Dictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries>();
        foreach (var c in focus.Citations)
        {
            var kind = IndicatorKindParser.From(c.Indicator);
            if (kind is null) continue;
            var key = (c.Ticker, kind.Value);
            if (histories.ContainsKey(key)) continue;
            histories[key] = await indicators.HistoryFor(c.Ticker, kind.Value, SparklineWindow, targetDate, ct);
        }

        // 3. Market snapshot — JSON is best-effort; malformed → Empty + log.
        var marketSnap = MarketSnapshot.Empty;
        if (focus.MarketSnapshotJson is { Length: > 0 } marketJson)
        {
            try
            {
                marketSnap = JsonSerializer.Deserialize<MarketSnapshot>(marketJson, JsonOpts.Strict)
                             ?? MarketSnapshot.Empty;
            }
            catch (JsonException ex)
            {
                LogMarketSnapshotMalformed(log, ex);
            }
        }

        return new FocusDerivedSlice(callDiff, histories, marketSnap);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "MarketSnapshotJson malformed; rail will not render")]
    private static partial void LogMarketSnapshotMalformed(ILogger logger, Exception ex);
}
```

Note: `JsonOpts.Strict` is referenced in `LoadDashboardUseCase` (line 141); same import here.

- [ ] **Step 2:** Run the existing failing test:

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~No_prior_returns_CallDiff_None" --nologo
```

Expected: PASS.

### Task 6.3: Test — with prior, CallDiff populated

**Files:**
- Modify: `TradyStrat.Application.Tests/Dashboard/UseCases/BuildFocusDerivedSliceUseCaseTests.cs`

- [ ] **Step 1:** Add:

```csharp
[Fact]
public async Task With_prior_builds_CallDiff_diff()
{
    await using var db = InMemoryDb.Create();
    SeedInstrument(db, InstrId, Ticker);

    var yesterday = Today.AddDays(-1);
    var priorRow = MkSuggestion(InstrId, yesterday) with { Action = SuggestionAction.Acquire };
    db.Suggestions.Add(priorRow);
    await db.SaveChangesAsync(TestContext.Current.CancellationToken);

    var focus = MkSuggestion(InstrId, Today) with { Action = SuggestionAction.Hold };

    var sut = BuildSut(db);
    var slice = await sut.BuildAsync(focus, Today, TestContext.Current.CancellationToken);

    slice.CallDiff.ShouldNotBe(CallDiff.None);
}
```

(Exact `CallDiff` API may differ; if `CallDiff.None` is the only static, asserting `ShouldNotBe(None)` is sufficient as a smoke. If the existing `CallDiffBuilder` exposes more, tighten the assertion.)

- [ ] **Step 2:** Run.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~With_prior_builds" --nologo
```

Expected: PASS.

### Task 6.4: Test — citations drive indicator history lookups, deduped

**Files:**
- Modify: `TradyStrat.Application.Tests/Dashboard/UseCases/BuildFocusDerivedSliceUseCaseTests.cs`

- [ ] **Step 1:** Refit the stub indicator engine inside the test class to record calls:

```csharp
private sealed class RecordingIndicatorEngine : IIndicatorEngine
{
    public List<(string Ticker, IndicatorKind Kind)> Calls { get; } = new();

    public Task<IndicatorBundle> ComputeFor(string ticker, DateOnly asOf, CancellationToken ct)
        => throw new NotSupportedException();

    public Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int window, DateOnly asOf, CancellationToken ct)
    {
        Calls.Add((ticker, kind));
        return Task.FromResult(new IndicatorSeries(ticker, kind, Array.Empty<IndicatorReading>()));
    }
}
```

(Replace the existing `StubIndicatorEngine` you added in Task 6.1 with this one — or keep both and route Task 6.4 through the recording one.)

Then add the test:

```csharp
[Fact]
public async Task Histories_built_only_for_cited_indicators_and_deduped()
{
    await using var db = InMemoryDb.Create();
    SeedInstrument(db, InstrId, Ticker);
    await db.SaveChangesAsync(TestContext.Current.CancellationToken);

    var citations = new[]
    {
        new Citation(Ticker: Ticker, Indicator: "RSI", Claim: "x"),
        new Citation(Ticker: Ticker, Indicator: "RSI", Claim: "y"),     // duplicate — dedup expected
        new Citation(Ticker: Ticker, Indicator: "Bollinger", Claim: "z"),
        new Citation(Ticker: Ticker, Indicator: "NotAnIndicator", Claim: "?"),  // unparseable — skipped
    };
    var focus = MkSuggestion(InstrId, Today, citations: citations);

    var engine = new RecordingIndicatorEngine();
    var sut = new BuildFocusDerivedSliceUseCase(
        new TestRepo<Suggestion>(db),
        engine,
        NullLogger<BuildFocusDerivedSliceUseCase>.Instance);

    var slice = await sut.BuildAsync(focus, Today, TestContext.Current.CancellationToken);

    engine.Calls.Count.ShouldBe(2);   // RSI once + Bollinger once
    slice.IndicatorHistories.Count.ShouldBe(2);
}
```

If `Citation`'s constructor differs from the named positional args, follow the actual record signature:

```bash
cat TradyStrat.Domain/Citation.cs
```

- [ ] **Step 2:** Run.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~Histories_built_only" --nologo
```

Expected: PASS.

### Task 6.5: Test — malformed market JSON is logged + empty

**Files:**
- Modify: `TradyStrat.Application.Tests/Dashboard/UseCases/BuildFocusDerivedSliceUseCaseTests.cs`

- [ ] **Step 1:** Add:

```csharp
[Fact]
public async Task Malformed_market_json_returns_Empty_and_does_not_throw()
{
    await using var db = InMemoryDb.Create();
    SeedInstrument(db, InstrId, Ticker);
    await db.SaveChangesAsync(TestContext.Current.CancellationToken);

    var focus = MkSuggestion(InstrId, Today, marketJson: "{not valid json");

    var sut = BuildSut(db);
    var slice = await sut.BuildAsync(focus, Today, TestContext.Current.CancellationToken);

    slice.MarketSnapshot.ShouldBe(MarketSnapshot.Empty);
}
```

- [ ] **Step 2:** Run.

```bash
dotnet test TradyStrat.Application.Tests/TradyStrat.Application.Tests.csproj \
  --filter "FullyQualifiedName~Malformed_market_json" --nologo
```

Expected: PASS.

### Task 6.6: Register `BuildFocusDerivedSliceUseCase` in DI

**Files:**
- Modify: `TradyStrat.Application/Dashboard/DashboardApplicationModule.cs` (or whatever the dashboard module file is named — verify)

- [ ] **Step 1:** Locate the dashboard's DI module:

```bash
find TradyStrat.Application/Dashboard -name "*Module.cs"
```

- [ ] **Step 2:** Add `services.AddScoped<BuildFocusDerivedSliceUseCase>();` next to the existing `LoadDashboardUseCase` registration.

- [ ] **Step 3:** Build.

```bash
dotnet build TradyStrat.slnx --nologo
```

Expected: Build succeeded, 0 errors.

### Task 6.7: Commit Phase 6

- [ ] **Step 1:** Stage and commit.

```bash
git add \
  TradyStrat.Application/Dashboard/UseCases/BuildFocusDerivedSliceUseCase.cs \
  TradyStrat.Application/Dashboard/DashboardApplicationModule.cs \
  TradyStrat.Application.Tests/Dashboard/UseCases/BuildFocusDerivedSliceUseCaseTests.cs
# (if dashboard module file is named differently, substitute the right path)

git commit -m "$(cat <<'EOF'
feat(dashboard): BuildFocusDerivedSliceUseCase — lift focus-derived data

CallDiff, citation-keyed indicator histories, and market snapshot
deserialization move out of LoadDashboardUseCase. The new use case is
called after the focus suggestion arrives so the dashboard skeleton
can render without it.
EOF
)"
```

- [ ] **Step 2:** Clean tree.

---

## Phase 7 — `TickerView` and `DashboardViewModel` type changes

Spec ref: §5.3.

This phase intentionally breaks every consumer of `Suggestion? TodaysCall` on the VM. The compiler is our safety net. We fix `LoadDashboardUseCase` first to populate the new shape, then sweep the Razor consumers (Phase 10) and finally restore green build.

### Task 7.1: Update `TickerView`

**Files:**
- Modify: `TradyStrat.Application/Dashboard/TickerView.cs`

- [ ] **Step 1:** Replace the file:

```csharp
using TradyStrat.Domain;

namespace TradyStrat.Application.Dashboard;

public sealed record TickerView(
    int InstrumentId,
    string Ticker,
    string Currency,
    decimal Price,
    decimal? PriceEur,
    decimal? DeltaPct,
    Zone Zone,
    IReadOnlyList<decimal> Spark,
    SuggestionState? CallState);   // null = no AI expected (watchlist or historical-missing)
```

- [ ] **Step 2:** Don't build yet — known to break consumers.

### Task 7.2: Update `DashboardViewModel`

**Files:**
- Modify: `TradyStrat.Application/Dashboard/DashboardViewModel.cs`

- [ ] **Step 1:** Rename `Suggestion? TodaysCall` to `SuggestionState? FocusCallState`:

```csharp
using TradyStrat.Application.AiSuggestion.Backfill;
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Domain;

namespace TradyStrat.Application.Dashboard;

public sealed record DashboardViewModel(
    DateOnly Today,
    int EntryNumber,
    PortfolioSnapshot Portfolio,
    GoalConfig Goal,
    SuggestionState? FocusCallState,                                 // was: Suggestion? TodaysCall
    IReadOnlyList<TickerView> Tickers,
    IReadOnlyList<PositionRow> Positions,
    string FocusTicker,
    IReadOnlyList<GrowthPoint> Growth,
    DateOnly? LatestPriceDate,
    GoalPaceVm GoalPace,
    CallDiff CallDiff,
    BackfillStatus BackfillStatus,
    string PriceAsOfRelative,
    string CallAsOfRelative,
    string FxAsOfRelative,
    IReadOnlyDictionary<(string Ticker, IndicatorKind Kind), IndicatorSeries> IndicatorHistories,
    IReadOnlyList<CapitalEvent> CapitalEvents,
    bool IsHistorical,
    DateOnly EarliestTradingDay,
    DateOnly LatestTradingDay,
    DateOnly? PrevTradingDay,
    DateOnly? NextTradingDay)
{
    public MarketSnapshot MarketSnapshot { get; init; } = MarketSnapshot.Empty;
}
```

- [ ] **Step 2:** Don't build yet — known to break consumers.

### Task 7.3: Update `LoadDashboardUseCase` — historical mode

**Files:**
- Modify: `TradyStrat.Application/Dashboard/UseCases/LoadDashboardUseCase.cs`

We split the refactor into historical (this task) and live (next task) because the historical path still computes the full focus-derived slice synchronously.

- [ ] **Step 1:** Inject the new `BuildFocusDerivedSliceUseCase` and remove `GetAllTodaysSuggestionsUseCase`:

```csharp
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
    BuildFocusDerivedSliceUseCase buildFocusSlice,                  // NEW
    ISuggestionBackfillCoordinator backfillCoord,
    IEntryNavigationService nav,
    ILogger<LoadDashboardUseCase> log)
    : UseCaseBase<LoadDashboardInput, DashboardViewModel>(log)
{
```

(Remove the `GetAllTodaysSuggestionsUseCase getAllTodaysSuggestions` parameter entirely.)

- [ ] **Step 2:** In `ExecuteCore`, rewrite the historical block (current lines 69–81) to construct `SuggestionState? CallState` per held instrument:

```csharp
// Historical mode — read-only across all held instruments.
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
```

- [ ] **Step 3:** In the per-ticker loop (current lines 88–113), set `CallState`:

```csharp
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
```

- [ ] **Step 4:** Replace the focus-derived block (current lines 127–179, `var focusInstrument` through end of `histories` population) with:

```csharp
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
```

- [ ] **Step 5:** Replace the existing `prior + callDiff` block and the indicator-histories block (lines 154–179 in the original) — they're now inside `BuildFocusDerivedSliceUseCase`. Drop them from this file.

- [ ] **Step 6:** Replace the backfill chain block (lines 200–210):
  - **Historical mode:** unchanged behavior — no backfill in historical.
  - **Live mode:** the page will now fire backfill when the focus state arrives. Move the lines out of this use case entirely. The page picks this up in Phase 9.

Concretely, delete lines 200–210. The page (Phase 9) gains responsibility for kicking the backfill chain when the focus arrives.

- [ ] **Step 7:** Update the final `DashboardViewModel` construction:

```csharp
return new DashboardViewModel(
    Today: target,
    EntryNumber: entryNum,
    Portfolio: snap,
    Goal: goal,
    FocusCallState: focusState,                                      // was: TodaysCall: todays
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
```

- [ ] **Step 8:** Build the Application layer.

```bash
dotnet build TradyStrat.Application/TradyStrat.Application.csproj --nologo
```

Expected: Build succeeded, 0 errors in the Application project. Razor consumers in `TradyStrat/` will still be broken; we fix them in Phase 10.

### Task 7.4: Verify the live-load path is AI-free (deferred to E2E)

Spec §10.4 ("LoadDashboardUseCase tests") proposes three small tests on the use case. In practice, `LoadDashboardUseCase`'s constructor pulls in `PortfolioService`, `GrowthSeriesBuilder`, `FxConverter`, `ListInstrumentsUseCase`, `IEntryNavigationService`, and `ISuggestionBackfillCoordinator` — a fixture large enough to outweigh the test's value. The same behavior is covered structurally by:

- **Phase 12.1 E2E test** (`Live_initial_render_includes_skeleton_marker`) — proves the live path produces a Pending-rendered dashboard without crashing on an unreachable Anthropic.
- **Phase 12.2 E2E test** (`Historical_initial_render_has_no_skeleton`) — proves historical loads do not emit Pending markers.
- **Phase 6 unit tests** — cover the focus-derivation logic that historical mode invokes synchronously.
- **Phase 5 unit tests** — cover the streaming path that lives outside the use case.

- [ ] **Step 1:** No new test file in this task. Document the decision in the commit message of Task 7.7.

### Task 7.5: Spike — manually verify the live load does not throw at runtime

This is a one-shot manual check, not a committed test. It catches the case where `LoadDashboardUseCase` accidentally retained an AI call site (e.g. via a forgotten dependency in the constructor).

- [ ] **Step 1:** With the Application layer compiling green, run:

```bash
dotnet build TradyStrat.slnx --nologo 2>&1 | grep -E "GetAllTodaysSuggestionsUseCase|getAllTodaysSuggestions" || echo "No leftover references — OK"
```

Expected: `No leftover references — OK`. If any matches print, those are stale references in code outside `LoadDashboardUseCase` (e.g. `SuggestionBackfillCoordinator` — which legitimately retains the dep). Verify each match is intentional.

- [ ] **Step 2:** Search for the source-level invariant — no `IAiClient` calls remain in the use case file:

```bash
grep -n "IAiClient\|AskAsync\|GetAllTodaysSuggestions" TradyStrat.Application/Dashboard/UseCases/LoadDashboardUseCase.cs && echo "FAIL: AI reference still present" || echo "OK: no AI references in use case"
```

Expected: `OK: no AI references in use case`.

### Task 7.6: Update the DI module

**Files:**
- Modify: `TradyStrat.Application/Dashboard/DashboardApplicationModule.cs` (whichever file owns `LoadDashboardUseCase`'s registration)

- [ ] **Step 1:** No change required in the registration itself if it's `services.AddScoped<LoadDashboardUseCase>()` — the runtime resolves constructor deps. But verify the file compiles:

```bash
dotnet build TradyStrat.Application/TradyStrat.Application.csproj --nologo
```

Expected: Build succeeded, 0 errors.

### Task 7.7: Commit Phase 7

- [ ] **Step 1:** Stage and commit. The Web project will still be broken; that's fine.

```bash
git add \
  TradyStrat.Application/Dashboard/TickerView.cs \
  TradyStrat.Application/Dashboard/DashboardViewModel.cs \
  TradyStrat.Application/Dashboard/UseCases/LoadDashboardUseCase.cs

git commit -m "$(cat <<'EOF'
refactor(dashboard): VM/TickerView use SuggestionState; load is AI-free

DashboardViewModel.FocusCallState (was TodaysCall) and TickerView.CallState
(was TodaysCall) carry SuggestionState. Live mode initializes Pending;
historical maps to Ready/null. LoadDashboardUseCase no longer calls
GetAllTodaysSuggestionsUseCase and delegates focus-derived data to
BuildFocusDerivedSliceUseCase. Razor consumers still need updating —
follow-up in Phase 9/10.

Verification of the live path's AI-free invariant is deferred to the E2E
test in Phase 12.1; the use case's fixture is too heavy for a focused
unit test relative to its value.
EOF
)"
```

- [ ] **Step 2:** Clean tree.

---

## Phase 8 — `DashboardPage` injection scaffolding

This phase only injects the new dependencies and sets up state fields. The actual stream consumption + state mutation come in Phase 9. Splitting keeps each commit reviewable.

### Task 8.1: Inject new use cases + state fields

**Files:**
- Modify: `TradyStrat/Features/Dashboard/DashboardPage.razor.cs`

- [ ] **Step 1:** Add three `[Inject]` declarations near the existing ones:

```csharp
[Inject] private StreamTodaysSuggestionsUseCase StreamSuggestions { get; set; } = default!;
[Inject] private BuildFocusDerivedSliceUseCase BuildFocusSlice { get; set; } = default!;
[Inject] private ISuggestionBackfillCoordinator BackfillCoord { get; set; } = default!;
```

(`ISuggestionBackfillCoordinator` is being added at the page level since the use case no longer fires backfill.)

- [ ] **Step 2:** Add three mutable state fields below the existing `_vm` / `_error` / `_busy` declarations:

```csharp
// Live-mode mutable state mutated by the stream consumer. `null` here means
// "use the VM's value" (skeleton default) — i.e. the stream hasn't filled it yet.
private SuggestionState? _focusState;
private Dictionary<int, SuggestionState?> _tickerStates = new();
private FocusDerivedSlice _focusDerived = FocusDerivedSlice.Empty;
private CancellationTokenSource? _streamCts;
```

- [ ] **Step 3:** Build the Web project. Expect compile errors in Razor consumers of `TodaysCall` — they remain to be fixed in Phase 10. Add a temporary suppression by **not building TradyStrat/ yet**; build only the code-behind via the Application layer:

```bash
dotnet build TradyStrat.Application/TradyStrat.Application.csproj --nologo
```

Expected: Build succeeded.

### Task 8.2: Initialize state from skeleton after load

**Files:**
- Modify: `TradyStrat/Features/Dashboard/DashboardPage.razor.cs`

- [ ] **Step 1:** After the successful `LoadDashboard.ExecuteAsync(...)` call at the end of `OnParametersSetAsync`, copy the skeleton state into the page fields. Replace the `_vm = await LoadDashboard...` line with this expanded block (still inside the `try`):

```csharp
_vm = await LoadDashboard.ExecuteAsync(
    new LoadDashboardInput(target, isHistorical), ct);
_error = null;

// Mirror the skeleton's state into mutable page fields. The stream consumer
// (OnAfterRenderAsync) mutates these per arrival.
_focusState = _vm.FocusCallState;
_tickerStates.Clear();
foreach (var t in _vm.Tickers)
    _tickerStates[t.InstrumentId] = t.CallState;
_focusDerived = new FocusDerivedSlice(_vm.CallDiff, _vm.IndicatorHistories, _vm.MarketSnapshot);
```

- [ ] **Step 2:** Build (Application only).

```bash
dotnet build TradyStrat.Application/TradyStrat.Application.csproj --nologo
```

Expected: Build succeeded.

### Task 8.3: Commit Phase 8

- [ ] **Step 1:** Stage and commit. (Web project still broken — fixed later.)

```bash
git add TradyStrat/Features/Dashboard/DashboardPage.razor.cs

git commit -m "$(cat <<'EOF'
refactor(dashboard): inject stream + slice use cases on DashboardPage

Adds the three new injections and mutable state fields. Initialization
mirrors the skeleton VM into the fields after load. Stream consumption
itself lands in the next commit.
EOF
)"
```

---

## Phase 9 — `DashboardPage` stream consumption

### Task 9.1: Implement `ConsumeStreamAsync`

**Files:**
- Modify: `TradyStrat/Features/Dashboard/DashboardPage.razor.cs`

- [ ] **Step 1:** Add the consumer method below the existing handlers (e.g. after `OnDateSelected`):

```csharp
private async Task ConsumeStreamAsync(CancellationToken ct)
{
    if (_vm is null) return;

    var heldIds = _vm.Tickers
        .Where(t => t.CallState is SuggestionState.Pending)
        .Select(t => t.InstrumentId)
        .ToArray();
    if (heldIds.Length == 0) return;

    var focusInstrumentId = _vm.Tickers
        .FirstOrDefault(t => t.Ticker == _vm.FocusTicker)?.InstrumentId ?? -1;

    try
    {
        await foreach (var ev in StreamSuggestions.StreamAsync(heldIds, ct))
        {
            SuggestionState newState = ev switch
            {
                SuggestionStreamEvent.Ready r  => new SuggestionState.Ready(r.Suggestion),
                SuggestionStreamEvent.Failed f => new SuggestionState.Failed(f.Reason),
                _ => throw new InvalidOperationException($"Unknown event type: {ev.GetType()}"),
            };

            _tickerStates[ev.InstrumentId] = newState;

            if (ev.InstrumentId == focusInstrumentId)
            {
                _focusState = newState;
                if (newState is SuggestionState.Ready ready)
                {
                    _focusDerived = await BuildFocusSlice.BuildAsync(ready.Suggestion, _vm.Today, ct);
                    KickBackfillChain(ready.Suggestion);
                }
            }

            await InvokeAsync(StateHasChanged);
        }
    }
    catch (OperationCanceledException)
    {
        // Cancellation requested — silent.
    }
}

private void KickBackfillChain(Suggestion focus)
{
    if (_vm is null || _vm.IsHistorical) return;

    // Replicates the prior LoadDashboardUseCase backfill kickoff. We only have
    // the focus suggestion here; the chain reads its own bounds from the DB.
    var today = _vm.Today;
    var prevTarget = today.AddDays(-1);

    _ = BackfillCoord
        .EnsureBackfilledAsync(prevTarget.AddDays(-1), prevTarget, CancellationToken.None)
        .ContinueWith(
            t => DashboardPageLog.BackfillCrashed(LoggerFor(this), t.Exception),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
}

private static ILogger LoggerFor(DashboardPage page) =>
    // Replace with whatever logger surface DashboardPage already injects (or add one).
    NullLogger.Instance;
```

If `DashboardPage` does not already inject `ILogger<DashboardPage>`, add the injection:

```csharp
[Inject] private ILogger<DashboardPage> Log { get; set; } = default!;
```

…and change `LoggerFor(this)` calls to `Log`.

Define `DashboardPageLog`:

```csharp
internal static partial class DashboardPageLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Backfill chain crashed unobserved")]
    public static partial void BackfillCrashed(ILogger logger, Exception? ex);
}
```

(Mirrors the existing `LoadDashboardLog.BackfillCrashed` pattern; can live at the bottom of `DashboardPage.razor.cs` outside the class declaration.)

- [ ] **Step 2:** Verify the file compiles (Web project not yet).

```bash
dotnet build TradyStrat.Application/TradyStrat.Application.csproj --nologo
```

### Task 9.2: Wire stream consumption into `OnAfterRenderAsync`

**Files:**
- Modify: `TradyStrat/Features/Dashboard/DashboardPage.razor.cs`

- [ ] **Step 1:** Edit `OnAfterRenderAsync` to spawn the stream consumer on first render (live mode only):

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        _keysModule = await JS.InvokeAsync<IJSObjectReference>(
            "import", "./js/dashboard-keys.js");
        _selfRef = DotNetObjectReference.Create(this);
        await _keysModule.InvokeVoidAsync("attach", _selfRef);

        if (_vm is { IsHistorical: false })
        {
            _streamCts?.Cancel();
            _streamCts?.Dispose();
            _streamCts = new CancellationTokenSource();
            _ = ConsumeStreamAsync(_streamCts.Token);
        }
    }

    if (_vm is not null)
    {
        _stickyModule ??= await JS.InvokeAsync<IJSObjectReference>(
            "import", "./js/sticky-bar.js");
        await _stickyModule.InvokeVoidAsync("observeHero");
    }
}
```

### Task 9.3: Cancel stream on dispose and on re-run

**Files:**
- Modify: `TradyStrat/Features/Dashboard/DashboardPage.razor.cs`

- [ ] **Step 1:** Extend `DisposeAsync` to cancel and dispose the CTS:

```csharp
public async ValueTask DisposeAsync()
{
    try { _streamCts?.Cancel(); }
    catch (ObjectDisposedException) { }
    _streamCts?.Dispose();
    _streamCts = null;

    if (_stickyModule is not null) { /* …existing… */ }
    try { /* …existing keysModule teardown… */ } catch (JSDisconnectedException) { }
    _selfRef?.Dispose();
    GC.SuppressFinalize(this);
}
```

(Keep the existing JS interop teardown — only add the CTS handling at the top.)

- [ ] **Step 2:** Update `ConfirmRerun` to restart the stream rather than just reload:

```csharp
private async Task ConfirmRerun()
{
    if (_vm?.IsHistorical == true) return;
    _showRerunConfirm = false;
    _busy = true;
    try
    {
        var ct = CancellationToken.None;
        var focusTicker = _vm?.FocusTicker
            ?? await Settings.FocusTickerAsync(ct);
        var instruments = await ListInstruments.ExecuteAsync(Application.UseCases.Unit.Value, ct);
        var focus = instruments.SingleOrDefault(i => i.Ticker == focusTicker)
            ?? throw new InvalidOperationException(
                $"Focus ticker '{focusTicker}' is not in the Instruments table.");

        await ForceRefetch.ExecuteAsync(new ForceRefetchSuggestionInput(focus.Id), ct);

        // Reload the skeleton (also resets non-focus tickers' rows in DB if force-refetch widened).
        await ReloadAsync();

        // Restart the stream — explicit cancel/restart so an in-flight stream from
        // the initial load is replaced.
        _streamCts?.Cancel();
        _streamCts?.Dispose();
        _streamCts = new CancellationTokenSource();
        _ = ConsumeStreamAsync(_streamCts.Token);
    }
    finally { _busy = false; }
}
```

- [ ] **Step 3:** Also update `ReloadAsync` to reinitialize the mutable state fields from the new VM:

```csharp
private async Task ReloadAsync()
{
    if (_vm is null) return;
    _vm = await LoadDashboard.ExecuteAsync(
        new LoadDashboardInput(_vm.Today, _vm.IsHistorical), CancellationToken.None);

    _focusState = _vm.FocusCallState;
    _tickerStates.Clear();
    foreach (var t in _vm.Tickers)
        _tickerStates[t.InstrumentId] = t.CallState;
    _focusDerived = new FocusDerivedSlice(_vm.CallDiff, _vm.IndicatorHistories, _vm.MarketSnapshot);
}
```

- [ ] **Step 4:** Build Application (verify the imports for `SuggestionStreamEvent`, `SuggestionState`, `FocusDerivedSlice`, `Suggestion`, `BackfillStatus`).

```bash
dotnet build TradyStrat.Application/TradyStrat.Application.csproj --nologo
```

### Task 9.4: Commit Phase 9

- [ ] **Step 1:** Stage and commit. Web project still broken — Razor markup fixed next.

```bash
git add TradyStrat/Features/Dashboard/DashboardPage.razor.cs

git commit -m "$(cat <<'EOF'
feat(dashboard): consume StreamTodaysSuggestionsUseCase on the page

ConsumeStreamAsync routes per-instrument events into mutable state, builds
the focus-derived slice when the focus arrives, kicks the backfill chain
post-arrival, and routes all UI updates through InvokeAsync(StateHasChanged).
Re-run AI cancels and restarts the stream.
EOF
)"
```

---

## Phase 10 — Razor markup updates

This phase finally restores a green Web build. The compiler enumerates the broken sites.

### Task 10.1: Enumerate consumers of the old VM fields

- [ ] **Step 1:** Find every Razor reference to the renamed/typed fields:

```bash
grep -rn "\.TodaysCall\b" --include="*.razor" --include="*.razor.cs" TradyStrat/ 2>&1
grep -rn "TickerView" --include="*.razor" --include="*.razor.cs" TradyStrat/ 2>&1
```

Capture the list. Each becomes a step below.

### Task 10.2: Update `TodaysCallCard.razor` parameter + rendering branches

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor`
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs` (if it exists)
- Modify: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css`

- [ ] **Step 1:** Read the existing markup so we know the layout dimensions to mirror in the skeleton.

```bash
cat TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor
cat TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.cs 2>/dev/null
```

- [ ] **Step 2:** Replace the parameter declaration and render with a state switch. The existing `[Parameter] public Suggestion? Suggestion { get; set; }` (or similar) becomes `[Parameter] public SuggestionState? State { get; set; }` plus an optional `[Parameter] public EventCallback OnRetry { get; set; }`. The markup wraps in:

```razor
@using TradyStrat.Application.Dashboard
@using TradyStrat.Domain

@if (State is null)
{
    <!-- No call expected (watchlist or historical-missing) — render nothing or an empty rail per existing convention. -->
}
else
{
    @switch (State)
    {
        case SuggestionState.Pending:
            <div class="call-card call-card--skeleton" aria-busy="true" aria-label="Loading AI suggestion">
                <div class="skeleton-line skeleton-action"></div>
                <div class="skeleton-line skeleton-conviction"></div>
                <div class="skeleton-line skeleton-rationale"></div>
                <div class="skeleton-line skeleton-citation"></div>
            </div>
            break;

        case SuggestionState.Ready r:
            <!-- Existing full card markup, with `r.Suggestion` substituted everywhere the old field was used. -->
            @* … paste-and-adapt the previous markup here, reading from `r.Suggestion` … *@
            break;

        case SuggestionState.Failed f:
            <div class="call-card call-card--error" role="alert">
                <span class="error-icon" aria-hidden="true">!</span>
                <span class="error-msg">AI call failed: @Truncate(f.Reason, 80)</span>
                <button class="btn btn-sm retry" @onclick="OnRetry">Retry</button>
            </div>
            break;
    }
}

@code {
    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s.AsSpan(0, max - 1).ToString() + "…";
}
```

- [ ] **Step 3:** Add the new CSS rules at the bottom of `TodaysCallCard.razor.css`:

```css
.call-card--skeleton .skeleton-line {
    height: 1rem;
    margin: 0.5rem 0;
    border-radius: 4px;
    background: linear-gradient(90deg,
        var(--surface-2, #eee) 0%,
        var(--surface-3, #f5f5f5) 50%,
        var(--surface-2, #eee) 100%);
    background-size: 200% 100%;
    animation: shimmer 1.4s linear infinite;
}
.call-card--skeleton .skeleton-action     { width: 30%; height: 1.5rem; }
.call-card--skeleton .skeleton-conviction { width: 20%; }
.call-card--skeleton .skeleton-rationale  { width: 90%; height: 2.5rem; }
.call-card--skeleton .skeleton-citation   { width: 60%; height: 0.85rem; }
@keyframes shimmer {
    0%   { background-position: 200% 0; }
    100% { background-position: -200% 0; }
}

.call-card--error {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.75rem 1rem;
    color: var(--err-fg, #b00);
    background: var(--err-bg, #fff0f0);
    border: 1px solid var(--err-border, #fbb);
    border-radius: 6px;
}
.call-card--error .error-icon  { font-weight: bold; }
.call-card--error .error-msg   { flex: 1; }
.call-card--error .retry       { flex-shrink: 0; }
```

(If the project's design system uses different CSS variable names, follow it. The classes themselves stay as named here.)

### Task 10.3: Update the parent that hands the call card a value

**Files:**
- Modify: whichever component renders `<TodaysCallCard ... />` — usually `DashboardPage.razor` or a sub-component.

- [ ] **Step 1:** Find the call site:

```bash
grep -rn "TodaysCallCard" --include="*.razor" TradyStrat/
```

- [ ] **Step 2:** Update the parameter binding from `Suggestion="@_vm.TodaysCall"` (or similar) to `State="@_focusState" OnRetry="@OnRetryFocus"`. Add the handler:

```csharp
private async Task OnRetryFocus()
{
    if (_vm is null) return;
    var focus = _vm.Tickers.FirstOrDefault(t => t.Ticker == _vm.FocusTicker);
    if (focus is null) return;

    _focusState = new SuggestionState.Pending();
    _tickerStates[focus.InstrumentId] = new SuggestionState.Pending();
    await InvokeAsync(StateHasChanged);

    _streamCts?.Cancel();
    _streamCts?.Dispose();
    _streamCts = new CancellationTokenSource();
    _ = ConsumeStreamAsync(_streamCts.Token);
}
```

### Task 10.4: Update per-ticker call-state consumers

**Files:**
- Modify: every Razor component that read `TickerView.TodaysCall` previously (from the enumeration in Task 10.1)

For each file:

- [ ] **Step 1:** Replace property reads of `t.TodaysCall` with reads of the page's mutable state. Components that receive `IReadOnlyList<TickerView>` should now also accept `IReadOnlyDictionary<int, SuggestionState?>`:

```razor
@code {
    [Parameter] public IReadOnlyList<TickerView> Tickers { get; set; } = Array.Empty<TickerView>();
    [Parameter] public IReadOnlyDictionary<int, SuggestionState?> CallStates { get; set; } = new Dictionary<int, SuggestionState?>();

    private SuggestionState? StateFor(TickerView t)
        => CallStates.TryGetValue(t.InstrumentId, out var s) ? s : t.CallState;
}
```

`DashboardPage.razor` passes `CallStates="@_tickerStates"` alongside the existing `Tickers` parameter.

The exact rendering branches (badge for Ready, dot for Pending, "!" for Failed) follow the spec §8 table. The minimal version inside the per-ticker template:

```razor
@switch (StateFor(t))
{
    case null: break;
    case SuggestionState.Pending: <span class="call-pending" title="AI call in flight">•</span> break;
    case SuggestionState.Ready r: <!-- existing badge using r.Suggestion --> break;
    case SuggestionState.Failed: <span class="call-failed" title="AI call failed">!</span> break;
}
```

Apply the same shape to each enumerated component.

### Task 10.5: Replace direct VM reads for focus-derived data

**Files:**
- Modify: components that previously read `_vm.CallDiff`, `_vm.IndicatorHistories`, `_vm.MarketSnapshot`.

- [ ] **Step 1:** Find these reads:

```bash
grep -rn "_vm\.\(CallDiff\|IndicatorHistories\|MarketSnapshot\)" --include="*.razor" --include="*.razor.cs" TradyStrat/
```

- [ ] **Step 2:** Replace each with the mutable field:

| Was | Becomes |
|---|---|
| `_vm.CallDiff` | `_focusDerived.CallDiff` |
| `_vm.IndicatorHistories` | `_focusDerived.IndicatorHistories` |
| `_vm.MarketSnapshot` | `_focusDerived.MarketSnapshot` |

If those reads happen inside child components via parameters, change the page to pass `_focusDerived.*` instead of `_vm.*` for those parameters.

### Task 10.6: Build the whole solution

- [ ] **Step 1:** Build everything.

```bash
dotnet build TradyStrat.slnx --nologo
```

Expected: Build succeeded, 0 errors. If there are leftover references to the old field names, the compiler tells you precisely where — fix and rebuild.

### Task 10.7: Commit Phase 10

- [ ] **Step 1:** Stage and commit.

```bash
git add TradyStrat/

git commit -m "$(cat <<'EOF'
feat(ui): Razor consumers switch to SuggestionState rendering

TodaysCallCard renders Pending (skeleton shimmer) / Ready (existing card) /
Failed (error row + Retry). Per-ticker zone and position components read
CallStates from the page's mutable state. Restores green Web build.
EOF
)"
```

- [ ] **Step 2:** Clean tree.

---

## Phase 11 — Settings UI: `MaxParallelSuggestions`

Spec ref: §5.4.

### Task 11.1: Add the input to `AnthropicSettingsForm`

**Files:**
- Modify: `TradyStrat/Features/Settings/Components/AnthropicSettingsForm.razor`
- Modify: `TradyStrat/Features/Settings/Components/AnthropicSettingsForm.razor.cs`

- [ ] **Step 1:** In the code-behind, add the field and load/save plumbing alongside the existing `_thinkingBudget`:

```csharp
private static readonly string[] Keys =
[
    SettingsKeys.AnthropicModel,
    SettingsKeys.AnthropicMaxTokens,
    SettingsKeys.AnthropicThinkingBudget,
    SettingsKeys.AnthropicMaxParallelSuggestions,
];

private int _maxParallel = 3;
private int _initialMaxParallel = 3;

protected override async Task OnInitializedAsync()
{
    var ai = await Settings.AnthropicAsync(CancellationToken.None);
    _model = _initialModel = ai.Model;
    _maxTokens = _initialMaxTokens = ai.MaxTokens;
    _thinkingBudget = _initialThinkingBudget = ai.ThinkingBudget;
    _maxParallel = _initialMaxParallel = ai.MaxParallelSuggestions;
    _lastUpdated = await Settings.LastUpdatedAsync(Keys, CancellationToken.None);
}
```

And inside `SaveAsync`, add a save branch after the `_thinkingBudget` block:

```csharp
if (_maxParallel != _initialMaxParallel)
{
    await UpdateSetting.ExecuteAsync(new UpdateSettingInput(SettingsKeys.AnthropicMaxParallelSuggestions,
        _maxParallel.ToString(CultureInfo.InvariantCulture)), CancellationToken.None);
    _initialMaxParallel = _maxParallel;
    changed++;
}
```

- [ ] **Step 2:** In the razor markup, add a new field after the Thinking budget input:

```razor
<label class="field">
    <span class="lbl">Max parallel AI calls</span>
    <input type="number" min="1" max="10" step="1"
           @bind="_maxParallel" @bind:after="OnChanged"
           title="How many Anthropic calls can run at once. Higher = faster dashboard, but Anthropic may rate-limit shared keys above 3." />
</label>
```

- [ ] **Step 3:** Build.

```bash
dotnet build TradyStrat/TradyStrat.csproj --nologo
```

Expected: Build succeeded, 0 errors.

### Task 11.2: Commit Phase 11

- [ ] **Step 1:** Stage and commit.

```bash
git add \
  TradyStrat/Features/Settings/Components/AnthropicSettingsForm.razor \
  TradyStrat/Features/Settings/Components/AnthropicSettingsForm.razor.cs

git commit -m "$(cat <<'EOF'
feat(settings-ui): max parallel AI calls input on the Anthropic form

Numeric input bound to anthropic.maxParallelSuggestions, range 1..10,
tooltip noting Anthropic rate-limit guidance.
EOF
)"
```

---

## Phase 12 — E2E tests

Spec ref: §10.5.

### Task 12.1: Skeleton-present-on-initial-render test

**Files:**
- Modify or create: `TradyStrat.E2E.Tests/DashboardSmokeTests.cs`

- [ ] **Step 1:** Inspect the existing E2E setup. `WebApplicationFactory` is already referenced in the csproj. If no `DashboardSmokeTests` exists, create one:

```csharp
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace TradyStrat.E2E.Tests;

public class DashboardSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DashboardSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Live_initial_render_includes_skeleton_marker()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Skeleton class is on the call card in Pending state.
        html.ShouldContain("call-card--skeleton");
    }
}
```

If `Program` is in an internal namespace, add `[assembly: InternalsVisibleTo("TradyStrat.E2E.Tests")]` to the Web project — but typically `Program` is `public partial` in Blazor templates; check `TradyStrat/Program.cs`.

- [ ] **Step 2:** Run.

```bash
dotnet test TradyStrat.E2E.Tests/TradyStrat.E2E.Tests.csproj --nologo --verbosity quiet
```

Expected: PASS.

If the E2E factory requires DB seeding, follow whatever the existing E2E setup does (look at `SmokeTests.cs`). If no DB is available and the page errors out, the test should be adapted to assert against the error template instead — or to seed minimal data via a test fixture. Use the pattern already established.

### Task 12.2: Historical render has no skeleton

**Files:**
- Modify: `TradyStrat.E2E.Tests/DashboardSmokeTests.cs`

- [ ] **Step 1:** Add:

```csharp
[Fact]
public async Task Historical_initial_render_has_no_skeleton()
{
    using var client = _factory.CreateClient();
    var response = await client.GetAsync("/?on=2026-01-15", TestContext.Current.CancellationToken);

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    html.ShouldNotContain("call-card--skeleton");
}
```

- [ ] **Step 2:** Run.

```bash
dotnet test TradyStrat.E2E.Tests/TradyStrat.E2E.Tests.csproj --nologo --verbosity quiet
```

Expected: PASS.

### Task 12.3: Commit Phase 12

- [ ] **Step 1:** Stage and commit.

```bash
git add TradyStrat.E2E.Tests/

git commit -m "$(cat <<'EOF'
test(e2e): dashboard skeleton renders on live, absent on historical

WebApplicationFactory-driven HTTP assertion. Streaming behavior (cards
flipping via SignalR) is not asserted here — see spec §10.5 for the
manual verification checklist that covers it.
EOF
)"
```

---

## Phase 13 — Final integration + manual verification

### Task 13.1: Full solution build + test

- [ ] **Step 1:** Clean restore + build.

```bash
dotnet restore TradyStrat.slnx
dotnet build TradyStrat.slnx -c Debug --nologo
```

Expected: Build succeeded, 0 errors, 0 warnings (or only pre-existing warnings).

- [ ] **Step 2:** Full test run.

```bash
dotnet test TradyStrat.slnx --nologo --verbosity quiet
```

Expected: every test in every project PASS. Compare the pass count to the baseline from Task 0.1 — should be N + (new tests added in this plan).

### Task 13.2: Manual verification checklist (spec §10.6)

Each item below is one checkbox. Requires a real Anthropic key in dev settings.

- [ ] **Cold cache, 3 held tickers**: open `/`. Dashboard skeleton + page chrome render within ~200ms (subjective). Cards flip in independently within 3–8s each.
- [ ] **Warm cache**: refresh `/`. All cards visible within ~300ms.
- [ ] **Forced failure**: temporarily invalidate the Anthropic key (or rename the env var). Reload. All cards transition to Failed with retry buttons. Restore the key. Click Retry on one card — it should stream back successfully.
- [ ] **"Re-run AI" mid-stream**: while cards are still flipping in, click Re-run. All cards reset to skeleton, stream restarts.
- [ ] **Navigate away mid-stream**: open `/`, then navigate to `/settings` within 1s. No orphan errors in console; no log entries about cancelled-but-not-cleaned tasks.
- [ ] **maxParallelSuggestions = 1**: set it to 1 via Settings page. Reload `/`. Cards flip in one at a time, longest call last.
- [ ] **Focus ticker is one of N held**: confirm the focus card and the per-ticker zone/positions rows show correct states.
- [ ] **Historical mode**: navigate to `/?on=2026-01-15` (or any date with seeded suggestions). Full dashboard renders in one shot, no skeleton flash, focus card populated.

### Task 13.3: Address any verification regressions

- [ ] **Step 1:** If any checklist item fails, open the relevant file(s), fix, and commit with a descriptive message:

```bash
git commit -m "fix(dashboard): <one-line description>"
```

Re-run §13.1 and §13.2 until all items pass.

### Task 13.4: Final summary commit (optional)

- [ ] **Step 1:** If any small cleanup remained (formatting, stray comments), commit it now. Otherwise skip.

---

## Done

The branch `worktree-phase3-ai-suggestion-improvements` now contains the reactive dashboard implementation across the commits introduced by Phases 1–13. Open a PR against `main` whose body summarizes:

- Spec link: `docs/superpowers/specs/2026-05-21-reactive-dashboard-design.md`
- Plan link: this file
- One-line per major behavior change (skeleton render, parallel AI, per-ticker error isolation, partitioned gate, max-parallel setting)
- Manual checklist results from §13.2

---

## Appendix: Architectural invariants the implementer must preserve

These are not optional. The reviewer will reject any commit that violates them.

1. **Workers run on `Task.Run`-spawned threads, not the SignalR sync context.** Every UI update from `ConsumeStreamAsync` MUST go through `await InvokeAsync(StateHasChanged)`. Bare `StateHasChanged()` calls from worker threads risk circuit corruption.
2. **The Channel writer's `Complete()` happens exactly once.** Driven by `Task.WhenAll(workers).ContinueWith(...)`. Worker writes must not race the complete.
3. **`SuggestionGate` keys are `(DateOnly, int)` — never collapse them.** Two instruments on the same date must not share a lock.
4. **`LoadDashboardUseCase` never calls `IAiClient` (live or historical).** Live mode initializes Pending; historical reads existing rows directly via the repository. If you find yourself adding an AI call inside `LoadDashboardUseCase`, stop — it belongs in `BuildFocusDerivedSliceUseCase` (for derived data from an already-loaded suggestion) or in the streaming use case (for fetching).
5. **Watchlist tickers' `CallState` is always `null`, never Pending.** Pending strictly means "live mode, in flight".
6. **Historical mode never fires the stream.** `OnAfterRenderAsync` short-circuits via the `_vm.IsHistorical` check.
