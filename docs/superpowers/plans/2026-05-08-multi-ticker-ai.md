# Multi-Ticker AI (Phase 2 core) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Spec:** [`docs/superpowers/specs/2026-05-08-multi-ticker-ai-design.md`](../specs/2026-05-08-multi-ticker-ai-design.md)

**Goal:** Make the AI loop per-Held-instrument: each held instrument gets its own daily Buy/Hold/Trim/Wait suggestion, focus-ticker rendering unchanged, non-focus calls render as chips on the Holdings rail.

**Architecture:** `Suggestions` table gains an `InstrumentId` FK with UQ on `(ForDate, InstrumentId)`. `GetTodaysSuggestionUseCase` widens to take an instrument-scoped input. New `GetAllTodaysSuggestionsUseCase` orchestrates the per-ticker loop with Saga-style failure isolation. `SnapshotFactory.CreateAsync` widens to take `instrumentId` (caller-driven primary). Migration backfills existing rows to focus = `'CON3.L'`. PromptHash byte-identity for the focus ticker preserved (sentinel test pinned at `"895EED53A280A470"` post-prediction-markets baseline). Optional final rename of `SnapshotFactory` → `AiSnapshotService` to match its 10-collaborator service shape.

**Tech Stack:** .NET 10 · Blazor Server · EF Core 10 (SQLite) · Ardalis.Specification 9.3 · Anthropic.SDK 5.10 · Microsoft.Extensions.AI 10.3 · xunit.v3 · Shouldly · Microsoft.EntityFrameworkCore.InMemory · Microsoft.Data.Sqlite (migration tests).

---

## Conventions

- **Working directory:** `/Users/philippe/repo/gh-phmatray/TradyStrat`. All `dotnet` commands run from here.
- **File-scoped namespaces** (existing convention).
- **Records for entities, DTOs, and inputs.**
- **Async naming:** every I/O method ends with `Async` and takes `CancellationToken ct` last.
- **Use cases:** inherit `UseCaseBase<TInput, TOutput>(ILogger)`; input is a record (or `Unit`); output is a typed result.
- **Repositories:** Ardalis `IReadRepositoryBase<T>` for reads, `IRepositoryBase<T>` for writes.
- **Source-generated logging:** `[LoggerMessage]` on `private static partial void` methods.
- **Tests:** xunit.v3 (`[Fact]`/`[Theory]`), Shouldly, in-memory EF via `TradyStrat.Tests.Specifications.InMemoryDb.Create()`. **`RepositoryBase<T>` is abstract — use `TradyStrat.Tests.Fx.TestRepo<T>` shim for in-memory tests.** Thread `TestContext.Current.CancellationToken` through every async EF/Ardalis call. Hoist expected arrays to `static readonly` (CA1861).
- **Fakes/stubs:** existing `StubSnapshotFactory`, `StubAiClient`, `FakeClock` in `TradyStrat.Tests/Common/Time/` and `TradyStrat.Tests/AiSuggestion/Snapshot/`.
- **Migrations are auto-applied at startup** by `DatabaseModule`. New migrations belong in `TradyStrat/Data/Migrations/`.
- **EF migration tool:** `dotnet ef ...` (already restored as a local manifest).
- **Build-and-test:** `dotnet build TradyStrat.slnx && dotnet test TradyStrat.slnx`. Both green at the end of every task.
- **Commit hygiene:** every task ends with one commit. Stage explicit paths (no `git add -A`).
- **Pre-flight backup of the live DB** before Task 1's migration runs on the user's real DB: `cp ~/Library/Application\ Support/TradyStrat/tradystrat.db ~/tradystrat.db.pre-phase2.bak`. The `*.bak` is already gitignored.
- **Critical sentinel:** `TradyStrat.Tests/AiSuggestion/Snapshot/SnapshotFactoryTests.cs` test `Catalog_produces_byte_identical_PromptHash_against_seeded_set` asserts `PromptHash == "895EED53A280A470"`. **This must continue to pass after every task.** If it breaks, the prompt input shape drifted and Phase 2 introduced a regression — investigate before proceeding.

---

## File structure overview

**New files:**

```
TradyStrat/
  Features/AiSuggestion/UseCases/
    GetTodaysSuggestionInput.cs        (record)
    ForceRefetchSuggestionInput.cs     (record)
    BackfillSuggestionsInput.cs        (record)
    GetAllTodaysSuggestionsUseCase.cs  (orchestrator)
  Data/Migrations/
    <timestamp>_MultiTickerAiPhase2.cs
    <timestamp>_MultiTickerAiPhase2.Designer.cs
TradyStrat.Tests/
  AiSuggestion/UseCases/
    GetAllTodaysSuggestionsUseCaseTests.cs
  AiSuggestion/Snapshot/
    SnapshotFactoryPerInstrumentTests.cs
  Data/
    MultiTickerAiPhase2MigrationTests.cs
```

**Modified files:**

```
TradyStrat/
  Common/Domain/
    Suggestion.cs                       (+ InstrumentId)
  Data/
    AppDbContextModelSnapshot.cs        (EF auto-regenerates)
  Data/Configurations/
    SuggestionConfiguration.cs          (FK, new UQ)
  Features/AiSuggestion/
    SuggestionService.cs                (sets InstrumentId)
  Features/AiSuggestion/Snapshot/
    AiSnapshot.cs                       (+ InstrumentId)
    SnapshotFactory.cs                  (CreateAsync widens)
  Features/AiSuggestion/Specifications/
    SuggestionForDateSpec.cs            (+ instrumentId arg)
    PriorSuggestionSpec.cs              (+ instrumentId arg)
    SuggestionsInRangeSpec.cs           (+ instrumentId arg)
    LatestSuggestionSpec.cs             (+ instrumentId arg)
  Features/AiSuggestion/UseCases/
    GetTodaysSuggestionUseCase.cs       (input shape)
    ForceRefetchSuggestionUseCase.cs    (input shape)
    BackfillSuggestionsUseCase.cs       (input shape)
  Features/AiSuggestion/Backfill/
    SuggestionBackfillCoordinator.cs    (resolve focus, plumb id)
  Features/Dashboard/UseCases/
    LoadDashboardUseCase.cs             (calls all-variant; populates TickerView.TodaysCall)
  Features/Dashboard/
    TickerView.cs                       (+ Suggestion? TodaysCall)
  Features/Dashboard/Components/
    PortfolioRail.razor                 (+ chip block)
    PortfolioRail.razor.css             (+ chip styles)
  Modules/
    AiSuggestionModule.cs               (registers GetAllTodaysSuggestionsUseCase)
TradyStrat.Tests/
  AiSuggestion/UseCases/
    GetTodaysSuggestionUseCaseTests.cs       (input shape)
    ForceRefetchSuggestionUseCaseTests.cs    (input shape)
    BackfillSuggestionsUseCaseTests.cs       (input shape)
  AiSuggestion/Backfill/
    SuggestionBackfillCoordinatorTests.cs    (focus resolution)
  AiSuggestion/Snapshot/
    SnapshotFactoryTests.cs                  (sentinel updated to per-instrument call)
  Specifications/
    SpecsRoundtripTests.cs                   (Suggestion specs new ctor)
  Dashboard/UseCases/
    LoadDashboardUseCaseTests.cs             (BuildSut wires all-variant; asserts TickerView.TodaysCall)
```

**Optional (Task 5 rename):**

```
TradyStrat/Features/AiSuggestion/Snapshot/
  SnapshotFactory.cs            → AiSnapshotService.cs
  ISnapshotFactory.cs           → IAiSnapshotService.cs
TradyStrat/Modules/AiSuggestionModule.cs    (registration target)
+ all consumers (~6 files) updated to the new name
```

---

## Task 1 — Schema, signature ripple, focus-only AI call (atomic)

**Why atomic:** the `Suggestion.InstrumentId` field, EF migration, four spec class signatures, three use case input shapes, `SnapshotFactory.CreateAsync` signature, `SuggestionService` literal, `AiSnapshot` shape, and dashboard live-mode call site all change in lockstep. Phase 1's Task 9 had the same atomicity constraint. One commit, ~15 production files + ~7 test files.

**Strategy for ordering inside the task:**
1. Domain entity + EF config + spec classes (the schema surface).
2. Migration: `dotnet ef migrations add MultiTickerAiPhase2`, then hand-edit the body for the backfill SQL.
3. `AiSnapshot` + `SnapshotFactory.CreateAsync` widening.
4. `SuggestionService.AskAsync` plumbing.
5. Three use cases' input records.
6. `SuggestionBackfillCoordinator` focus resolution.
7. `LoadDashboardUseCase` live-mode call site update.
8. Adapt all existing tests.
9. Add the new migration test.
10. Build & full-suite green.
11. Single commit.

### Files

**Production:**
- Modify: `TradyStrat/Common/Domain/Suggestion.cs`
- Modify: `TradyStrat/Data/Configurations/SuggestionConfiguration.cs`
- Modify: `TradyStrat/Features/AiSuggestion/Specifications/SuggestionForDateSpec.cs`
- Modify: `TradyStrat/Features/AiSuggestion/Specifications/PriorSuggestionSpec.cs`
- Modify: `TradyStrat/Features/AiSuggestion/Specifications/SuggestionsInRangeSpec.cs`
- Modify: `TradyStrat/Features/AiSuggestion/Specifications/LatestSuggestionSpec.cs`
- Modify: `TradyStrat/Features/AiSuggestion/Snapshot/AiSnapshot.cs`
- Modify: `TradyStrat/Features/AiSuggestion/Snapshot/SnapshotFactory.cs`
- Modify: `TradyStrat/Features/AiSuggestion/SuggestionService.cs`
- Modify: `TradyStrat/Features/AiSuggestion/UseCases/GetTodaysSuggestionUseCase.cs`
- Modify: `TradyStrat/Features/AiSuggestion/UseCases/ForceRefetchSuggestionUseCase.cs`
- Modify: `TradyStrat/Features/AiSuggestion/UseCases/BackfillSuggestionsUseCase.cs`
- Modify: `TradyStrat/Features/AiSuggestion/Backfill/SuggestionBackfillCoordinator.cs`
- Modify: `TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs`
- Create: `TradyStrat/Features/AiSuggestion/UseCases/GetTodaysSuggestionInput.cs`
- Create: `TradyStrat/Features/AiSuggestion/UseCases/ForceRefetchSuggestionInput.cs`
- Create: `TradyStrat/Features/AiSuggestion/UseCases/BackfillSuggestionsInput.cs`
- Create: `TradyStrat/Data/Migrations/<timestamp>_MultiTickerAiPhase2.cs` (+ `.Designer.cs` + snapshot regen)

**Tests:**
- Modify: `TradyStrat.Tests/AiSuggestion/UseCases/GetTodaysSuggestionUseCaseTests.cs`
- Modify: `TradyStrat.Tests/AiSuggestion/UseCases/ForceRefetchSuggestionUseCaseTests.cs`
- Modify: `TradyStrat.Tests/AiSuggestion/UseCases/BackfillSuggestionsUseCaseTests.cs`
- Modify: `TradyStrat.Tests/AiSuggestion/Backfill/SuggestionBackfillCoordinatorTests.cs`
- Modify: `TradyStrat.Tests/AiSuggestion/Snapshot/SnapshotFactoryTests.cs`
- Modify: `TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs`
- Modify: `TradyStrat.Tests/Dashboard/UseCases/LoadDashboardUseCaseTests.cs`
- Create: `TradyStrat.Tests/Data/MultiTickerAiPhase2MigrationTests.cs`

---

#### 1.1 Domain entity

- [ ] **Step 1: Add `InstrumentId` to `Suggestion`**

Open `TradyStrat/Common/Domain/Suggestion.cs`. Add `InstrumentId` directly after `Id`:

```csharp
public required int Id { get; init; }
public required int InstrumentId { get; init; }   // NEW (Phase 2)
public required DateOnly ForDate { get; init; }
// ... rest unchanged ...
```

Don't touch `MarketSnapshotJson`, `Citations`, `OrderValueEur`, etc.

#### 1.2 EF configuration

- [ ] **Step 2: Update `SuggestionConfiguration.cs`**

Replace the existing single-column UQ with the composite, and add the FK to `Instruments`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Common.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
{
    public void Configure(EntityTypeBuilder<Suggestion> builder)
    {
        builder.ToTable("Suggestions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.QuantityHint).HasColumnType("TEXT");
        builder.Property(s => s.MaxPriceHint).HasColumnType("TEXT");
        builder.Property(s => s.Rationale).HasMaxLength(4000);
        builder.Property(s => s.CitationsJson).HasMaxLength(8000);
        builder.Property(s => s.MarketSnapshotJson).HasMaxLength(8000);
        builder.Property(s => s.PromptHash).HasMaxLength(128);
        builder.HasOne<Instrument>()
               .WithMany()
               .HasForeignKey(s => s.InstrumentId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.ForDate, s.InstrumentId }).IsUnique();
        builder.Ignore(s => s.OrderValueEur);
        builder.Ignore(s => s.Citations);
    }
}
```

Notes: the old `builder.HasIndex(s => s.ForDate).IsUnique();` is replaced with the composite. `MarketSnapshotJson` mapping is preserved unchanged.

#### 1.3 Specification class signature ripple

- [ ] **Step 3: Update all four Suggestion specs**

Each constructor gains an `int instrumentId` parameter and the `Where` adds `&& s.InstrumentId == instrumentId`.

`SuggestionForDateSpec.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.AiSuggestion.Specifications;

public sealed class SuggestionForDateSpec : Specification<Suggestion>
{
    public SuggestionForDateSpec(DateOnly date, int instrumentId)
    {
        Query.Where(s => s.ForDate == date && s.InstrumentId == instrumentId)
             .Take(1);
    }
}
```

`PriorSuggestionSpec.cs`:
```csharp
public sealed class PriorSuggestionSpec : Specification<Suggestion>
{
    public PriorSuggestionSpec(DateOnly beforeExclusive, int instrumentId)
    {
        Query.Where(s => s.ForDate < beforeExclusive && s.InstrumentId == instrumentId)
             .OrderByDescending(s => s.ForDate)
             .Take(1);
    }
}
```

`SuggestionsInRangeSpec.cs`:
```csharp
public sealed class SuggestionsInRangeSpec : Specification<Suggestion>
{
    public SuggestionsInRangeSpec(DateOnly fromInclusive, DateOnly toInclusive, int instrumentId)
    {
        Query.Where(s => s.ForDate >= fromInclusive
                      && s.ForDate <= toInclusive
                      && s.InstrumentId == instrumentId)
             .OrderBy(s => s.ForDate);
    }
}
```

`LatestSuggestionSpec.cs`:
```csharp
public sealed class LatestSuggestionSpec : Specification<Suggestion>
{
    public LatestSuggestionSpec(int instrumentId)
    {
        Query.Where(s => s.InstrumentId == instrumentId)
             .OrderByDescending(s => s.ForDate)
             .Take(1);
    }
}
```

#### 1.4 AiSnapshot + SnapshotFactory

- [ ] **Step 4: Add `InstrumentId` to `AiSnapshot`**

Open `TradyStrat/Features/AiSuggestion/Snapshot/AiSnapshot.cs`. Add the field directly after `Today`:

```csharp
public sealed record AiSnapshot(
    DateOnly Today,
    int InstrumentId,                              // NEW (Phase 2)
    GoalConfig Goal,
    PortfolioSnapshot Portfolio,
    IReadOnlyList<TickerContext> Tickers,
    IReadOnlyList<TradeRecent> RecentTrades,
    decimal? UsdPerEur,
    IReadOnlyList<PredictionMarket> Markets,
    string PromptHash);
```

`TickerContext` and `TradeRecent` records are unchanged.

- [ ] **Step 5: Widen `SnapshotFactory.CreateAsync` to take `instrumentId`**

Open `TradyStrat/Features/AiSuggestion/Snapshot/SnapshotFactory.cs`. **Constructor unchanged** (preserves all 10 deps including `IPredictionMarketProvider`). Method body changes:

```csharp
public async Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct)
{
    var goal = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(clock.UtcNow());

    var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
    var primary = instruments.SingleOrDefault(i => i.Id == instrumentId)
        ?? throw new InvalidOperationException(
            $"Instrument id {instrumentId} is not in the Instruments table.");

    // Catalog order: primary first, then watchlist in legacy order, then any
    // newer watchlist instruments alphabetically. Held instruments other than
    // the primary stay context-only — they appear in `Tickers` for indicator
    // analysis but the prompt's primary subject is the passed `instrumentId`.
    var watchlist = instruments
        .Where(i => i.Kind == InstrumentKind.Watchlist)
        .OrderBy(i => Array.IndexOf(LegacyWatchlistOrder, i.Ticker) is var idx && idx < 0
            ? int.MaxValue : idx)
        .ThenBy(i => i.Ticker);
    var catalog = new[] { (primary.Ticker, primary.Currency) }
        .Concat(watchlist.Select(i => (i.Ticker, i.Currency)))
        .ToArray();

    var tickers = new List<TickerContext>();

    foreach (var (ticker, currency) in catalog)
    {
        var reading = await indicators.ComputeFor(ticker, asOf, ct);
        decimal? eur = null;
        if (!string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase))
            eur = await fx.ToEurAsync(reading.Price, currency, asOf, ct);

        tickers.Add(new TickerContext(
            ticker, currency, reading.Price, eur, reading.Zone, reading.Reasons));
    }

    var priceMap = new Dictionary<int, (decimal PriceEur, string Ticker, string Currency)>();
    foreach (var inst in instruments.Where(i => i.Kind == InstrumentKind.Held))
    {
        var ctx = tickers.SingleOrDefault(t => t.Ticker == inst.Ticker);
        var priceEur = ctx?.PriceEur ?? ctx?.PriceNative ?? 0m;
        priceMap[inst.Id] = (priceEur, inst.Ticker, inst.Currency);
    }

    var snap = await portfolio.SnapshotAsync(asOf, priceMap, goal.TargetEur, ct);

    var asOfTrades = await tradeRepo.ListAsync(new TradesAsOfSpec(asOf), ct);
    var recentDtos = asOfTrades
        .OrderByDescending(t => t.ExecutedOn).Take(20)
        .OrderBy(t => t.ExecutedOn)
        .Select(t => new TradeRecent(t.ExecutedOn, t.Side, t.Quantity, t.PricePerShare))
        .ToList();

    decimal? usdPerEur = null;
    try
    {
        var oneUsdInEur = await fx.ToEurAsync(1m, "USD", asOf, ct);
        if (oneUsdInEur != 0m) usdPerEur = 1m / oneUsdInEur;
    }
    catch (Common.Exceptions.FxRateUnavailableException)
    {
    }

    IReadOnlyList<PredictionMarket> markets;
    try
    {
        markets = await predictionMarkets.GetMarketsAsync(ct);
    }
    catch (PolymarketUnavailableException ex)
    {
        SnapshotFactoryLog.PolymarketUnavailable(log, ex);
        markets = [];
    }
    if (markets.Count == 0)
    {
        SnapshotFactoryLog.PolymarketEmpty(log);
    }

    var promptHash = HashPrompt(asOf, snap, tickers, recentDtos, markets);

    return new AiSnapshot(
        asOf,
        instrumentId,                              // NEW
        goal, snap, tickers, recentDtos,
        usdPerEur, markets, promptHash);
}
```

**Critical: `HashPrompt` does NOT include `instrumentId` in the hashed payload.** Per spec §4.7, `InstrumentId` is set on the snapshot record and persisted on the entity, but not part of the hash. The byte-identity sentinel `895EED53A280A470` depends on this — collision-resistance comes from the differentiated `tickers`/`snap`/`recent` payload content, not from `instrumentId`. Don't add `instrumentId` to `HashPrompt`'s payload object.

The `HashPrompt` body stays exactly as it is today (signature unchanged):

```csharp
private static string HashPrompt(DateOnly today, PortfolioSnapshot snap,
    IEnumerable<TickerContext> tickers, IEnumerable<TradeRecent> recent,
    IEnumerable<PredictionMarket> markets)
{
    var payload = new { today, snap, tickers, recent, markets };
    var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOpts.Strict));
    return Convert.ToHexString(SHA256.HashData(bytes))[..16];
}
```

The `ISnapshotFactory` interface signature changes to match:

```csharp
// TradyStrat/Features/AiSuggestion/Snapshot/ISnapshotFactory.cs
namespace TradyStrat.Features.AiSuggestion.Snapshot;

public interface ISnapshotFactory
{
    Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct);
}
```

#### 1.5 SuggestionService plumbs InstrumentId

- [ ] **Step 6: Update `SuggestionService.AskAsync`**

Open `TradyStrat/Features/AiSuggestion/SuggestionService.cs`. Inside the `submit` `AIFunctionFactory.Create` callback, the `Suggestion` literal construction (currently around line 58–71) gains one line:

```csharp
captured = new Suggestion
{
    Id            = 0,
    InstrumentId  = snapshot.InstrumentId,        // NEW (Phase 2)
    ForDate       = snapshot.Today,
    Action        = action,
    QuantityHint  = quantity_hint,
    MaxPriceHint  = max_price_hint,
    Conviction    = conviction,
    Rationale     = rationale,
    CitationsJson = JsonSerializer.Serialize(citationList, JsonOpts.Strict),
    MarketSnapshotJson = marketJson,
    PromptHash    = snapshot.PromptHash,
    CreatedAt     = clock.UtcNow(),
};
```

No other changes to `SuggestionService.cs`.

#### 1.6 Use case input records

- [ ] **Step 7: Create the three input records**

`TradyStrat/Features/AiSuggestion/UseCases/GetTodaysSuggestionInput.cs`:
```csharp
namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed record GetTodaysSuggestionInput(int InstrumentId);
```

`TradyStrat/Features/AiSuggestion/UseCases/ForceRefetchSuggestionInput.cs`:
```csharp
namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed record ForceRefetchSuggestionInput(int InstrumentId);
```

`TradyStrat/Features/AiSuggestion/UseCases/BackfillSuggestionsInput.cs`:
```csharp
namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed record BackfillSuggestionsInput(DateOnly Date, int InstrumentId);
```

#### 1.7 Use case redesign

- [ ] **Step 8: Update `GetTodaysSuggestionUseCase`**

Replace the entire file:

```csharp
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Features.AiSuggestion.Specifications;

namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed class GetTodaysSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotFactory snapshotFactory,
    IAiClient ai,
    IClock clock,
    IReadRepositoryBase<Instrument> instruments,
    ILogger<GetTodaysSuggestionUseCase> log)
    : UseCaseBase<GetTodaysSuggestionInput, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(
        GetTodaysSuggestionInput input, CancellationToken ct)
    {
        var instrument = await instruments.GetByIdAsync(input.InstrumentId, ct)
            ?? throw new InstrumentNotFoundException(
                $"Instrument id {input.InstrumentId} not registered.");

        var today = clock.TodayInExchangeTzFor(instrument.Ticker);

        var existing = await repo.FirstOrDefaultAsync(
            new SuggestionForDateSpec(today, instrument.Id), ct);
        if (existing is not null) return existing;

        var snap  = await snapshotFactory.CreateAsync(instrument.Id, today, ct);
        var fresh = await ai.AskAsync(snap, ct);
        await repo.AddAsync(fresh, ct);
        return fresh;
    }
}
```

`IConfiguration` is removed — per-instrument timezone semantics use `instrument.Ticker` directly.

- [ ] **Step 9: Update `ForceRefetchSuggestionUseCase`**

Replace the entire file:

```csharp
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Features.AiSuggestion.Specifications;

namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed class ForceRefetchSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotFactory snapshotFactory,
    IAiClient ai,
    IClock clock,
    IReadRepositoryBase<Instrument> instruments,
    ILogger<ForceRefetchSuggestionUseCase> log)
    : UseCaseBase<ForceRefetchSuggestionInput, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(
        ForceRefetchSuggestionInput input, CancellationToken ct)
    {
        var instrument = await instruments.GetByIdAsync(input.InstrumentId, ct)
            ?? throw new InstrumentNotFoundException(
                $"Instrument id {input.InstrumentId} not registered.");

        var today = clock.TodayInExchangeTzFor(instrument.Ticker);

        var existing = await repo.FirstOrDefaultAsync(
            new SuggestionForDateSpec(today, instrument.Id), ct);
        if (existing is not null) await repo.DeleteAsync(existing, ct);

        var snap  = await snapshotFactory.CreateAsync(instrument.Id, today, ct);
        var fresh = await ai.AskAsync(snap, ct);
        await repo.AddAsync(fresh, ct);
        return fresh;
    }
}
```

- [ ] **Step 10: Update `BackfillSuggestionsUseCase`**

Replace the entire file:

```csharp
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.AiSuggestion.Snapshot;

namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed class BackfillSuggestionsUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotFactory snapshotFactory,
    IAiClient ai,
    ILogger<BackfillSuggestionsUseCase> log)
    : UseCaseBase<BackfillSuggestionsInput, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(
        BackfillSuggestionsInput input, CancellationToken ct)
    {
        var snapshot   = await snapshotFactory.CreateAsync(input.InstrumentId, input.Date, ct);
        var suggestion = await ai.AskAsync(snapshot, ct);
        await repo.AddAsync(suggestion, ct);
        return suggestion;
    }
}
```

#### 1.8 SuggestionBackfillCoordinator focus resolution

- [ ] **Step 11: Plumb the focus instrument id through the coordinator**

`SuggestionBackfillCoordinator` has two constructors. Both now also resolve `IConfiguration` and `IReadRepositoryBase<Instrument>` so the coordinator can look up the focus's id once at the start of `RunChainAsync` and pass it to every per-day call.

Replace `TradyStrat/Features/AiSuggestion/Backfill/SuggestionBackfillCoordinator.cs`:

```csharp
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.AiSuggestion.Specifications;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.Settings.Specifications;

namespace TradyStrat.Features.AiSuggestion.Backfill;

public sealed partial class SuggestionBackfillCoordinator : ISuggestionBackfillCoordinator
{
    private readonly object _gate = new();
    private Task? _inflight;
    private volatile BackfillStatus _status = BackfillStatus.Idle.Instance;
    private readonly Func<Resolved> _resolveDeps;
    private readonly ILogger<SuggestionBackfillCoordinator> _log;

    private sealed record Resolved(
        IReadRepositoryBase<Suggestion> Suggestions,
        IReadRepositoryBase<Instrument> Instruments,
        BackfillSuggestionsUseCase Backfill,
        IConfiguration Config,
        IDisposable? Scope);

    public SuggestionBackfillCoordinator(
        IReadRepositoryBase<Suggestion> suggestions,
        IReadRepositoryBase<Instrument> instruments,
        BackfillSuggestionsUseCase backfillOne,
        IConfiguration config,
        ILogger<SuggestionBackfillCoordinator> log)
    {
        _resolveDeps = () => new Resolved(suggestions, instruments, backfillOne, config, null);
        _log = log;
    }

    [ActivatorUtilitiesConstructor]
    public SuggestionBackfillCoordinator(
        IServiceScopeFactory scopeFactory,
        ILogger<SuggestionBackfillCoordinator> log)
    {
        _resolveDeps = () =>
        {
            var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            return new Resolved(
                sp.GetRequiredService<IReadRepositoryBase<Suggestion>>(),
                sp.GetRequiredService<IReadRepositoryBase<Instrument>>(),
                sp.GetRequiredService<BackfillSuggestionsUseCase>(),
                sp.GetRequiredService<IConfiguration>(),
                scope);
        };
        _log = log;
    }

    public BackfillStatus Status => _status;
    public event Action<BackfillStatus>? StatusChanged;

    public Task EnsureBackfilledAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct)
    {
        lock (_gate)
        {
            if (_inflight is { IsCompleted: false }) return _inflight;
            _inflight = RunChainAsync(fromExclusive, toInclusive, ct);
            return _inflight;
        }
    }

    private async Task RunChainAsync(DateOnly fromExclusive, DateOnly toInclusive, CancellationToken ct)
    {
        var resolved = _resolveDeps();
        try
        {
            var focusTicker = resolved.Config["Tickers:Focus"]
                ?? throw new InvalidOperationException("Tickers:Focus is not configured.");
            var focus = await resolved.Instruments.FirstOrDefaultAsync(
                new InstrumentByTickerSpec(focusTicker), ct)
                ?? throw new InvalidOperationException(
                    $"Focus instrument '{focusTicker}' is not registered.");

            var firstNeeded = fromExclusive.AddDays(1);
            var existing = await resolved.Suggestions.ListAsync(
                new SuggestionsInRangeSpec(firstNeeded, toInclusive, focus.Id), ct);
            var existingDates = existing.Select(s => s.ForDate).ToHashSet();

            var missing = new List<DateOnly>();
            for (var d = firstNeeded; d <= toInclusive; d = d.AddDays(1))
                if (!existingDates.Contains(d)) missing.Add(d);

            if (missing.Count == 0)
            {
                _status = BackfillStatus.Idle.Instance;
                return;
            }

            DateOnly? lastOk = null;
            for (var i = 0; i < missing.Count; i++)
            {
                var date = missing[i];
                SetStatus(new BackfillStatus.Running(missing.Count - i, missing.Count, date));

                try
                {
                    await resolved.Backfill.ExecuteAsync(
                        new BackfillSuggestionsInput(date, focus.Id), ct);
                    lastOk = date;
                }
                catch (OperationCanceledException)
                {
                    SetStatus(BackfillStatus.Idle.Instance);
                    throw;
                }
                catch (TradyStratException ex)
                {
                    LogChainHalted(_log, date, lastOk, ex);
                    SetStatus(new BackfillStatus.Failed(
                        LastSuccessful: lastOk ?? fromExclusive,
                        FailedAt: date,
                        Reason: ex.Message));
                    return;
                }
            }

            SetStatus(BackfillStatus.Idle.Instance);
        }
        finally
        {
            resolved.Scope?.Dispose();
        }
    }

    private void SetStatus(BackfillStatus next)
    {
        _status = next;
        StatusChanged?.Invoke(next);
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Backfill chain halted at {BackfillDate} (last successful: {LastSuccessfulDate})")]
    private static partial void LogChainHalted(
        ILogger logger, DateOnly backfillDate, DateOnly? lastSuccessfulDate, Exception ex);
}
```

#### 1.9 LoadDashboardUseCase live-mode call site

- [ ] **Step 12: Update the live-mode AI call**

Open `TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs`. Find the live-mode block (around line 94–102):

```csharp
// Before:
Suggestion? todays;
if (input.IsHistorical)
{
    todays = await suggestionRepo.FirstOrDefaultAsync(new SuggestionForDateSpec(target), ct);
}
else
{
    todays = await todaysSuggestion.ExecuteAsync(Unit.Value, ct);
}
```

Replace with:

```csharp
// Resolve the focus instrument id once — both branches need it.
var focusInstrument = ordered.SingleOrDefault(i => i.Ticker == focusTicker)
    ?? throw new InvalidOperationException(
        $"Focus ticker '{focusTicker}' is not in the Instruments table.");

Suggestion? todays;
if (input.IsHistorical)
{
    todays = await suggestionRepo.FirstOrDefaultAsync(
        new SuggestionForDateSpec(target, focusInstrument.Id), ct);
}
else
{
    todays = await todaysSuggestion.ExecuteAsync(
        new GetTodaysSuggestionInput(focusInstrument.Id), ct);
}
```

The `ordered` list (already computed earlier in the method from the `instruments` query) contains the focus instrument by virtue of how the catalog is ordered. We extract its id once and use it for both branches.

The same `focusInstrument.Id` is also needed for any subsequent `PriorSuggestionSpec` call further down — find any call to `PriorSuggestionSpec(target)` and update it to `PriorSuggestionSpec(target, focusInstrument.Id)`. (The `LoadDashboardUseCase` we have shows one such call near the call-diff block; verify and update.)

#### 1.10 Migration

- [ ] **Step 13: Generate the migration**

Run:
```bash
dotnet ef migrations add MultiTickerAiPhase2 --project TradyStrat --output-dir Data/Migrations
```

EF will auto-emit `AddColumn<int> InstrumentId nullable: true`, `DropIndex IX_Suggestions_ForDate`, and an attempt at adding the new composite index. Hand-edit the body to insert the backfill SQL and order the operations correctly.

- [ ] **Step 14: Hand-edit the migration body**

Open the new `Data/Migrations/<timestamp>_MultiTickerAiPhase2.cs` and replace the auto-generated `Up`/`Down` with:

```csharp
public partial class MultiTickerAiPhase2 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Add nullable InstrumentId column.
        migrationBuilder.AddColumn<int>(
            name: "InstrumentId",
            table: "Suggestions",
            type: "INTEGER",
            nullable: true);

        // 2. Backfill: every existing Suggestion row was for the focus ticker
        //    (CON3.L) at the time this migration was authored — Phase 1
        //    hardcoded the AI loop to the configured focus and there's only
        //    ever been one Suggestion per ForDate. Hardcoded literal matches
        //    the precedent set by the Trades.InstrumentId backfill in
        //    MultiTickerFoundation (Phase 1).
        migrationBuilder.Sql(@"
            UPDATE Suggestions
               SET InstrumentId = (SELECT Id FROM Instruments WHERE Ticker = 'CON3.L')
             WHERE InstrumentId IS NULL;");

        // 3. Make NOT NULL + add FK + composite UQ; drop old single-column UQ.
        migrationBuilder.AlterColumn<int>(
            name: "InstrumentId",
            table: "Suggestions",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "INTEGER",
            oldNullable: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Suggestions_Instruments_InstrumentId",
            table: "Suggestions",
            column: "InstrumentId",
            principalTable: "Instruments",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.DropIndex(
            name: "IX_Suggestions_ForDate",
            table: "Suggestions");

        migrationBuilder.CreateIndex(
            name: "IX_Suggestions_ForDate_InstrumentId",
            table: "Suggestions",
            columns: new[] { "ForDate", "InstrumentId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException(
            "Phase 2 multi-ticker-AI migration is forward-only. Restore from a pre-migration DB copy.");
}
```

The `Designer.cs` and `AppDbContextModelSnapshot.cs` files EF auto-generated alongside should be left intact — they encode the new model state.

#### 1.11 Adapt existing tests

- [ ] **Step 15: Update `SnapshotFactoryTests.cs` sentinel**

The sentinel test needs to call the new signature `CreateAsync(focus.Id, asOf, ct)`. The hash assertion stays at `"895EED53A280A470"` because `InstrumentId` is *not* in the hashed payload.

In `TradyStrat.Tests/AiSuggestion/Snapshot/SnapshotFactoryTests.cs`, update every `await sut.CreateAsync(asOf, ct)` call to `await sut.CreateAsync(focusId, asOf, ct)`. Each test needs to know the focus instrument's id; resolve it from the seeded set after `await db.SaveChangesAsync(ct)`:

```csharp
SeedInstruments(db);
// ... seed bars/fx/goal ...
await db.SaveChangesAsync(ct);

var focusId = (await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct)).Id;
var sut  = BuildSut(db);
var snap = await sut.CreateAsync(focusId, asOf, ct);

// Sentinel assertion unchanged:
snap.PromptHash.ShouldBe("895EED53A280A470");
```

The `BuildSut` helper itself doesn't change — `SnapshotFactory`'s constructor is unchanged.

- [ ] **Step 16: Update `GetTodaysSuggestionUseCaseTests.cs` and `ForceRefetchSuggestionUseCaseTests.cs`**

Both tests construct the use case and call `ExecuteAsync(Unit.Value, ct)`. Update to:

1. Drop `IConfiguration config` from the constructor call (it's no longer injected).
2. Add `IReadRepositoryBase<Instrument>` (use `new TestRepo<Instrument>(db)`).
3. Seed an `Instrument` row for the focus before constructing the use case.
4. Resolve its id and pass to `ExecuteAsync(new GetTodaysSuggestionInput(focusId), ct)` (or `ForceRefetchSuggestionInput`).
5. The fake `Suggestion` literals in the test bodies need `InstrumentId = focusId` set.
6. The `StubSnapshotFactory` payload's `AiSnapshot` literal needs to include `InstrumentId` (insert `focusId` between `Today` and `GoalConfig.Default(...)`).

Concrete edited form of the existing happy-path test in `GetTodaysSuggestionUseCaseTests.cs`:

```csharp
[Fact]
public async Task Returns_existing_row_when_today_already_present()
{
    await using var db = InMemoryDb.Create();
    var ct = TestContext.Current.CancellationToken;

    db.Instruments.Add(new Instrument {
        Id = 0, Ticker = "CON3.L", Name = "x", Currency = "USD",
        Exchange = "LSE", TimezoneId = "Europe/London",
        Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow });
    await db.SaveChangesAsync(ct);

    var focusId = (await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct)).Id;

    db.Suggestions.Add(new Suggestion {
        Id = 0, InstrumentId = focusId, ForDate = new(2026,5,6),
        Action = SuggestionAction.Hold, Conviction = 3, Rationale = "cached",
        CitationsJson = "[]", PromptHash = "h", CreatedAt = DateTime.UtcNow });
    await db.SaveChangesAsync(ct);

    var snap = new StubSnapshotFactory(new AiSnapshot(
        new(2026,5,6), focusId, GoalConfig.Default(DateTime.UtcNow),
        new([],0,0,0,0,0,0,0), [], [], 1.08m, [], "h2"));
    var ai = new StubAiClient(new Suggestion {
        Id = 0, InstrumentId = focusId, ForDate = new(2026,5,6),
        Action = SuggestionAction.Acquire, Conviction = 5, Rationale = "fresh",
        CitationsJson = "[]", PromptHash = "h2", CreatedAt = DateTime.UtcNow });

    var uc = new GetTodaysSuggestionUseCase(
        new TestRepo<Suggestion>(db), snap, ai,
        new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc)),
        new TestRepo<Instrument>(db),
        NullLogger<GetTodaysSuggestionUseCase>.Instance);

    var s = await uc.ExecuteAsync(new GetTodaysSuggestionInput(focusId), ct);

    s.Rationale.ShouldBe("cached");
    (await db.Suggestions.CountAsync(ct)).ShouldBe(1);
}
```

`ForceRefetchSuggestionUseCaseTests.cs` follows the same pattern — drop `config`, add `instruments` repo, seed `Instrument`, pass `ForceRefetchSuggestionInput(focusId)`.

**Note:** `StubSnapshotFactory`'s constructor takes a fixed `AiSnapshot` payload. After Phase 2, `ISnapshotFactory.CreateAsync` takes `(int instrumentId, DateOnly asOf, CancellationToken)`. Update `StubSnapshotFactory` (or its test source file) to match the new interface — its `CreateAsync` ignores the new `instrumentId` parameter and returns the fixed payload as before:

```csharp
public Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct)
    => Task.FromResult(_fixed);
```

Locate `StubSnapshotFactory.cs` (likely under `TradyStrat.Tests/AiSuggestion/Snapshot/`) and update its single method.

- [ ] **Step 17: Update `BackfillSuggestionsUseCaseTests.cs`**

The test calls `sut.ExecuteAsync(asOf, ct)` with a bare `DateOnly`. After Phase 2 it becomes `sut.ExecuteAsync(new BackfillSuggestionsInput(asOf, focusId), ct)`. The `StubFactory.CreateAsync` signature also widens to take `instrumentId` (ignored):

```csharp
private sealed class StubFactory : ISnapshotFactory
{
    public DateOnly? CapturedDate { get; private set; }
    public int? CapturedInstrumentId { get; private set; }

    public Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        CapturedDate = asOf;
        CapturedInstrumentId = instrumentId;
        return Task.FromResult(new AiSnapshot(
            asOf, instrumentId,
            GoalConfig.Default(DateTime.UtcNow),
            new PortfolioSnapshot([], 0, 0, 0, 0, 0, 0, 0),
            [], [], 1m, [], "test"));
    }
}
```

Update the test body:

```csharp
[Fact]
public async Task Persists_suggestion_with_ForDate_from_asOf()
{
    var ct = TestContext.Current.CancellationToken;
    await using var db = InMemoryDb.Create();
    var factory = new StubFactory();
    var ai = new StubAiClient();
    var sut = new BackfillSuggestionsUseCase(
        new TestRepo<Suggestion>(db), factory, ai,
        NullLogger<BackfillSuggestionsUseCase>.Instance);

    var asOf = new DateOnly(2026, 5, 4);
    const int focusId = 7;
    var s = await sut.ExecuteAsync(new BackfillSuggestionsInput(asOf, focusId), ct);

    s.ForDate.ShouldBe(asOf);
    factory.CapturedDate.ShouldBe(asOf);
    factory.CapturedInstrumentId.ShouldBe(focusId);
    db.Suggestions.Single().ForDate.ShouldBe(asOf);
}
```

The `StubAiClient` returns a `Suggestion` literal that needs `InstrumentId = snapshot.InstrumentId` set:

```csharp
public Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct) =>
    Task.FromResult(new Suggestion
    {
        Id = 0,
        InstrumentId = snapshot.InstrumentId,    // NEW
        ForDate = snapshot.Today,
        Action = SuggestionAction.Hold,
        Conviction = 5,
        Rationale = "stub",
        CitationsJson = "[]",
        PromptHash = snapshot.PromptHash,
        CreatedAt = DateTime.UtcNow,
    });
```

- [ ] **Step 18: Update `SuggestionBackfillCoordinatorTests.cs`**

The two coordinator constructors now take additional dependencies. Tests that invoke the direct-DI constructor need an `IReadRepositoryBase<Instrument>` and `IConfiguration`. Tests also need to seed an `Instrument` row for CON3.L so the focus resolution succeeds.

Update the `BuildSut` helper:

```csharp
private static (SuggestionBackfillCoordinator coord, AppDbContext ctx, RecordingAi ai)
    BuildSut(Func<DateOnly, Task<Suggestion>>? aiOverride = null)
{
    var ctx = InMemoryDb.Create();

    // Seed CON3.L so focus resolution succeeds.
    ctx.Instruments.Add(new Instrument {
        Id = 0, Ticker = "CON3.L", Name = "x", Currency = "USD",
        Exchange = "LSE", TimezoneId = "Europe/London",
        Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow });
    ctx.SaveChanges();

    var ai = new RecordingAi(aiOverride);
    var factory = new PassthroughFactory();
    var useCase = new BackfillSuggestionsUseCase(
        new TestRepo<Suggestion>(ctx), factory, ai,
        NullLogger<BackfillSuggestionsUseCase>.Instance);

    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Tickers:Focus"] = "CON3.L" })
        .Build();

    var coord = new SuggestionBackfillCoordinator(
        new TestRepo<Suggestion>(ctx),
        new TestRepo<Instrument>(ctx),
        useCase,
        config,
        NullLogger<SuggestionBackfillCoordinator>.Instance);
    return (coord, ctx, ai);
}
```

Update the `PassthroughFactory.CreateAsync` to the new `(int instrumentId, DateOnly asOf, ct)` signature, returning a payload that includes `instrumentId`:

```csharp
private sealed class PassthroughFactory : ISnapshotFactory
{
    public Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct) =>
        Task.FromResult(new AiSnapshot(
            asOf, instrumentId,
            GoalConfig.Default(DateTime.UtcNow),
            new PortfolioSnapshot([], 0, 0, 0, 0, 0, 0, 0), [], [], 1m, [], "test"));
}
```

`RecordingAi.AskAsync` returns `Suggestion` literals that need `InstrumentId = snapshot.InstrumentId` set — same edit as in step 17.

The `StubSuggestion` helper needs `InstrumentId` too — update to take an instrument id parameter or default to `1`:

```csharp
private static Suggestion StubSuggestion(DateOnly d, int instrumentId = 1) => new()
{
    Id = 0, InstrumentId = instrumentId,
    ForDate = d, Action = SuggestionAction.Hold, Conviction = 5,
    Rationale = "stub", CitationsJson = "[]", PromptHash = "test",
    CreatedAt = DateTime.UtcNow,
};
```

Existing test bodies don't need otherwise structural changes — the spec contracts (idle/running/failed/reentrancy/cancellation/multi-subscriber) are all preserved. The only mid-chain-failure assertion that was about per-day call ordering now also verifies focus resolution: `ai.Calls` will still record dates, but each call now goes through with `focus.Id` plumbed in.

- [ ] **Step 19: Update `SpecsRoundtripTests.cs`**

For each Suggestion-spec test in `TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs`:

1. Construct an `Instrument` row before adding `Suggestion` rows; use its id on every `Suggestion` literal.
2. Pass that id to the spec constructor.

`SuggestionForDateSpec_finds_exact_date_match`:
```csharp
[Fact]
public async Task SuggestionForDateSpec_finds_exact_date_match()
{
    var ct = TestContext.Current.CancellationToken;
    await using var db = InMemoryDb.Create();

    db.Instruments.Add(new Instrument {
        Id = 0, Ticker = "CON3.L", Name = "x", Currency = "USD",
        Exchange = "LSE", TimezoneId = "Europe/London",
        Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow });
    await db.SaveChangesAsync(ct);
    var focusId = (await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct)).Id;

    db.Suggestions.Add(new Suggestion {
        Id = 0, InstrumentId = focusId, ForDate = new(2026,5,6),
        Action = SuggestionAction.Hold, Conviction = 3, Rationale = "x",
        CitationsJson = "[]", PromptHash = "h", CreatedAt = DateTime.UtcNow });
    await db.SaveChangesAsync(ct);

    var hit  = await db.Suggestions.WithSpecification(new SuggestionForDateSpec(new(2026,5,6), focusId)).FirstOrDefaultAsync(ct);
    var miss = await db.Suggestions.WithSpecification(new SuggestionForDateSpec(new(2026,5,7), focusId)).FirstOrDefaultAsync(ct);

    hit.ShouldNotBeNull();
    miss.ShouldBeNull();
}
```

Apply the same shape to `SuggestionsInRangeSpec_filters_inclusive_and_orders_ascending` (pass `focusId` to the spec ctor and to every `Sugg(...)` helper) and `PriorSuggestionSpec_returns_most_recent_strictly_before` (likewise).

The `Sugg(int month, int day)` test helper needs widening to `Sugg(int month, int day, int instrumentId = 1)`:

```csharp
private static Suggestion Sugg(int month, int day, int instrumentId = 1) => new()
{
    Id = 0, InstrumentId = instrumentId,
    ForDate = new(2026, month, day), Action = SuggestionAction.Hold,
    Conviction = 3, Rationale = "x", CitationsJson = "[]",
    PromptHash = "h", CreatedAt = DateTime.UtcNow,
};
```

- [ ] **Step 20: Update `LoadDashboardUseCaseTests.cs`**

`BuildSut` constructs `GetTodaysSuggestionUseCase` directly. Its constructor now takes `IReadRepositoryBase<Instrument>` instead of `IConfiguration`:

```csharp
var todays = new GetTodaysSuggestionUseCase(
    new TestRepo<Suggestion>(db), snapStub, aiStub, clock,
    new TestRepo<Instrument>(db),                 // was: config
    NullLogger<GetTodaysSuggestionUseCase>.Instance);
```

The `snapStub` payload's `AiSnapshot` literal needs `InstrumentId` (the focus's id, which the test seeds via `SeedBaseAsync` per Phase 1's pattern):

```csharp
var focusId = 1;   // first seeded Instrument; verify by reading SeedBaseAsync
var snapStub = new StubSnapshotFactory(new AiSnapshot(
    Target, focusId, GoalConfig.Default(DateTime.UtcNow),
    new([],0,0,0,0,0,0,0), [], [], 1.08m, [], "h"));
```

The `aiStub` `Suggestion` literal needs `InstrumentId = focusId` set.

Tests that asserted on `vm.TodaysCall` after the live-mode call need to know the call now goes through `GetTodaysSuggestionInput(focusId)` rather than `Unit.Value` — the use case is invoked with the resolved focus id from inside `LoadDashboardUseCase.ExecuteCore`. The visible test contract is unchanged.

#### 1.12 Migration backward-compat test

- [ ] **Step 21: Add `MultiTickerAiPhase2MigrationTests.cs`**

```csharp
// TradyStrat.Tests/Data/MultiTickerAiPhase2MigrationTests.cs
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Data;
using Xunit;

namespace TradyStrat.Tests.Data;

public class MultiTickerAiPhase2MigrationTests
{
    [Fact]
    public async Task Migration_creates_Suggestions_InstrumentId_and_swaps_unique_index()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync(TestContext.Current.CancellationToken);
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;

        await using var db = new AppDbContext(opts);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);

        // PRAGMA introspection — confirm the column is present and FK exists.
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info(Suggestions);";
            var cols = new List<string>();
            await using var rdr = await cmd.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            while (await rdr.ReadAsync(TestContext.Current.CancellationToken))
                cols.Add(rdr.GetString(1));
            cols.ShouldContain("InstrumentId");
        }

        // Confirm the new composite unique index exists and the old one is gone.
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA index_list(Suggestions);";
            var indices = new List<(string Name, bool Unique)>();
            await using var rdr = await cmd.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            while (await rdr.ReadAsync(TestContext.Current.CancellationToken))
                indices.Add((rdr.GetString(1), rdr.GetBoolean(2)));
            indices.ShouldContain(i => i.Name == "IX_Suggestions_ForDate_InstrumentId" && i.Unique);
            indices.ShouldNotContain(i => i.Name == "IX_Suggestions_ForDate");
        }
    }
}
```

If the test project doesn't already reference `Microsoft.Data.Sqlite` directly, it does for Phase 1's `MultiTickerMigrationTests.cs` — same precedent.

#### 1.13 Build, test, commit

- [ ] **Step 22: Build**

Run:
```bash
dotnet build TradyStrat.slnx
```
Expected: 0 warnings, 0 errors.

- [ ] **Step 23: Test**

Run:
```bash
dotnet test TradyStrat.slnx
```
Expected: all tests pass. The sentinel test `Catalog_produces_byte_identical_PromptHash_against_seeded_set` must pass with hash `"895EED53A280A470"`.

- [ ] **Step 24: Commit**

```bash
git add TradyStrat/Common/Domain/Suggestion.cs \
        TradyStrat/Data/Configurations/SuggestionConfiguration.cs \
        TradyStrat/Data/Migrations/ \
        TradyStrat/Features/AiSuggestion/Specifications/ \
        TradyStrat/Features/AiSuggestion/Snapshot/AiSnapshot.cs \
        TradyStrat/Features/AiSuggestion/Snapshot/SnapshotFactory.cs \
        TradyStrat/Features/AiSuggestion/Snapshot/ISnapshotFactory.cs \
        TradyStrat/Features/AiSuggestion/SuggestionService.cs \
        TradyStrat/Features/AiSuggestion/UseCases/ \
        TradyStrat/Features/AiSuggestion/Backfill/SuggestionBackfillCoordinator.cs \
        TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs \
        TradyStrat.Tests/AiSuggestion/ \
        TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs \
        TradyStrat.Tests/Dashboard/UseCases/LoadDashboardUseCaseTests.cs \
        TradyStrat.Tests/Data/MultiTickerAiPhase2MigrationTests.cs
git commit -m "$(cat <<'EOF'
feat(ai): widen Suggestions schema + signatures for per-instrument AI

Atomic change: Suggestions table gains InstrumentId FK with UQ on
(ForDate, InstrumentId). All four Suggestion specs gain instrumentId
parameter. AiSnapshot, SnapshotFactory.CreateAsync, SuggestionService,
GetTodaysSuggestionUseCase, ForceRefetchSuggestionUseCase,
BackfillSuggestionsUseCase, SuggestionBackfillCoordinator widen in
lockstep. LoadDashboardUseCase live-mode resolves focus instrument id
once and passes through. Migration backfills existing rows to CON3.L's
seeded Instrument id.

Sentinel test (Catalog_produces_byte_identical_PromptHash_against_
seeded_set) preserved at "895EED53A280A470" because InstrumentId is
not part of the hashed prompt payload.

Phase 2 of multi-ticker AI per spec
docs/superpowers/specs/2026-05-08-multi-ticker-ai-design.md
EOF
)"
```

---

## Task 2 — `GetAllTodaysSuggestionsUseCase` orchestrator

**Files:**
- Create: `TradyStrat/Features/AiSuggestion/UseCases/GetAllTodaysSuggestionsUseCase.cs`
- Modify: `TradyStrat/Modules/AiSuggestionModule.cs`
- Test: `TradyStrat.Tests/AiSuggestion/UseCases/GetAllTodaysSuggestionsUseCaseTests.cs`

### Steps

- [ ] **Step 1: Write the new use case**

```csharp
// TradyStrat/Features/AiSuggestion/UseCases/GetAllTodaysSuggestionsUseCase.cs
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Settings.UseCases;

namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed partial class GetAllTodaysSuggestionsUseCase(
    GetTodaysSuggestionUseCase singleTicker,
    ListInstrumentsUseCase listInstruments,
    ILogger<GetAllTodaysSuggestionsUseCase> log)
    : UseCaseBase<Unit, IReadOnlyList<Suggestion>>(log)
{
    protected override async Task<IReadOnlyList<Suggestion>> ExecuteCore(
        Unit _, CancellationToken ct)
    {
        var all  = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var held = all.Where(i => i.Kind == InstrumentKind.Held).ToList();

        var results = new List<Suggestion>(held.Count);
        foreach (var inst in held)
        {
            try
            {
                var s = await singleTicker.ExecuteAsync(
                    new GetTodaysSuggestionInput(inst.Id), ct);
                results.Add(s);
            }
            catch (TradyStratException ex)
            {
                LogPerTickerFailure(log, ex, inst.Ticker);
                // Failure isolation — one ticker's call shouldn't take down the others.
            }
        }
        return results;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI call failed for {Ticker}")]
    private static partial void LogPerTickerFailure(ILogger logger, Exception ex, string ticker);
}
```

- [ ] **Step 2: Register in `AiSuggestionModule.cs`**

Open `TradyStrat/Modules/AiSuggestionModule.cs`. Add one line in the `ConfigureServices` method, alongside the existing use-case registrations:

```csharp
builder.Services.AddScoped<GetTodaysSuggestionUseCase>();
builder.Services.AddScoped<GetAllTodaysSuggestionsUseCase>();   // NEW
builder.Services.AddScoped<ForceRefetchSuggestionUseCase>();
builder.Services.AddScoped<BackfillSuggestionsUseCase>();
```

- [ ] **Step 3: Write tests**

```csharp
// TradyStrat.Tests/AiSuggestion/UseCases/GetAllTodaysSuggestionsUseCaseTests.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.Settings.UseCases;
using TradyStrat.Tests.Common.Time;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using Xunit;

namespace TradyStrat.Tests.AiSuggestion.UseCases;

public class GetAllTodaysSuggestionsUseCaseTests
{
    private static readonly string[] ExpectedTickersInOrder = ["AAA", "BBB"];

    private static async Task<(GetAllTodaysSuggestionsUseCase sut, AppDbContext db, RecordingFactory factory)>
        BuildSutAsync(
            IDictionary<int, Exception>? throwForInstrumentId = null,
            CancellationToken ct = default)
    {
        var db = InMemoryDb.Create();

        // Two Held + one Watchlist.
        db.Instruments.AddRange(
            new Instrument { Id = 0, Ticker = "AAA", Name = "A", Currency = "USD",
                Exchange = "X", TimezoneId = "UTC", Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow },
            new Instrument { Id = 0, Ticker = "BBB", Name = "B", Currency = "USD",
                Exchange = "X", TimezoneId = "UTC", Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow },
            new Instrument { Id = 0, Ticker = "WATCH", Name = "W", Currency = "USD",
                Exchange = "X", TimezoneId = "UTC", Kind = InstrumentKind.Watchlist, AddedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var clock = new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc));
        var factory = new RecordingFactory(throwForInstrumentId);

        var single = new GetTodaysSuggestionUseCase(
            new TestRepo<Suggestion>(db), factory, factory, clock,
            new TestRepo<Instrument>(db),
            NullLogger<GetTodaysSuggestionUseCase>.Instance);

        var list = new ListInstrumentsUseCase(
            new TestRepo<Instrument>(db),
            NullLogger<ListInstrumentsUseCase>.Instance);

        var sut = new GetAllTodaysSuggestionsUseCase(
            single, list,
            NullLogger<GetAllTodaysSuggestionsUseCase>.Instance);

        return (sut, db, factory);
    }

    [Fact]
    public async Task Returns_one_Suggestion_per_Held_instrument()
    {
        var ct = TestContext.Current.CancellationToken;
        var (sut, _, factory) = await BuildSutAsync(ct: ct);

        var results = await sut.ExecuteAsync(Unit.Value, ct);

        results.Count.ShouldBe(2);
        results.Select(r => factory.TickerOf(r.InstrumentId)).ShouldBe(ExpectedTickersInOrder);
    }

    [Fact]
    public async Task Watchlist_instruments_are_excluded()
    {
        var ct = TestContext.Current.CancellationToken;
        var (sut, db, _) = await BuildSutAsync(ct: ct);

        var results = await sut.ExecuteAsync(Unit.Value, ct);

        var watchId = (await db.Instruments.SingleAsync(i => i.Ticker == "WATCH", ct)).Id;
        results.Select(r => r.InstrumentId).ShouldNotContain(watchId);
    }

    [Fact]
    public async Task One_failing_ticker_does_not_block_the_others()
    {
        var ct = TestContext.Current.CancellationToken;
        var failures = new Dictionary<int, Exception> { /* will populate after seed */ };
        var (sut0, db, _) = await BuildSutAsync(ct: ct);
        var aaaId = (await db.Instruments.SingleAsync(i => i.Ticker == "AAA", ct)).Id;
        failures[aaaId] = new PriceFeedUnavailableException("simulated");

        // Re-build with the failure map populated against the actual id.
        var (sut, _, _) = await BuildSutAsync(throwForInstrumentId: failures, ct: ct);

        var results = await sut.ExecuteAsync(Unit.Value, ct);

        // Only BBB succeeds; AAA's PriceFeedUnavailableException is swallowed.
        results.Count.ShouldBe(1);
    }

    private sealed class RecordingFactory : ISnapshotFactory, IAiClient
    {
        private readonly IDictionary<int, Exception>? _throws;
        private readonly Dictionary<int, string> _byId = new();
        public List<int> CalledFor { get; } = new();
        public string TickerOf(int id) => _byId.TryGetValue(id, out var t) ? t : "?";

        public RecordingFactory(IDictionary<int, Exception>? throws) => _throws = throws;

        public Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct)
        {
            if (_throws is not null && _throws.TryGetValue(instrumentId, out var ex)) throw ex;
            CalledFor.Add(instrumentId);
            return Task.FromResult(new AiSnapshot(
                asOf, instrumentId,
                GoalConfig.Default(DateTime.UtcNow),
                new PortfolioSnapshot([], 0, 0, 0, 0, 0, 0, 0),
                [], [], 1m, [], "h"));
        }

        public Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
        {
            _byId[snapshot.InstrumentId] = $"id-{snapshot.InstrumentId}";
            return Task.FromResult(new Suggestion {
                Id = 0, InstrumentId = snapshot.InstrumentId,
                ForDate = snapshot.Today, Action = SuggestionAction.Hold,
                Conviction = 3, Rationale = "ok", CitationsJson = "[]",
                PromptHash = "h", CreatedAt = DateTime.UtcNow });
        }
    }
}
```

- [ ] **Step 4: Build, test**

```bash
dotnet build TradyStrat.slnx
dotnet test  TradyStrat.slnx --filter "FullyQualifiedName~GetAllTodaysSuggestionsUseCaseTests"
```
Expected: 3 passed.

```bash
dotnet test TradyStrat.slnx
```
Expected: full suite green.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/AiSuggestion/UseCases/GetAllTodaysSuggestionsUseCase.cs \
        TradyStrat/Modules/AiSuggestionModule.cs \
        TradyStrat.Tests/AiSuggestion/UseCases/GetAllTodaysSuggestionsUseCaseTests.cs
git commit -m "feat(ai): add GetAllTodaysSuggestionsUseCase Saga aggregator

Per-Held-instrument AI loop. Sequential calls, per-ticker
TradyStratException try/catch for failure isolation. Three tests cover
the success, watchlist-excluded, and partial-failure paths."
```

---

## Task 3 — Dashboard widening (LoadDashboardUseCase + TickerView)

**Files:**
- Modify: `TradyStrat/Features/Dashboard/TickerView.cs`
- Modify: `TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs`
- Modify: `TradyStrat.Tests/Dashboard/UseCases/LoadDashboardUseCaseTests.cs`

### Steps

- [ ] **Step 1: Add `Suggestion? TodaysCall` to `TickerView`**

Replace `TradyStrat/Features/Dashboard/TickerView.cs`:

```csharp
using TradyStrat.Common.Domain;
using TradyStrat.Features.Indicators;

namespace TradyStrat.Features.Dashboard;

public sealed record TickerView(
    string Ticker,
    string Currency,
    decimal Price,
    decimal? PriceEur,
    decimal? DeltaPct,
    Zone Zone,
    IReadOnlyList<decimal> Spark,
    Suggestion? TodaysCall);    // NEW (Phase 2) — non-null for Held with successful call
```

- [ ] **Step 2: Update `LoadDashboardUseCase` to use `GetAllTodaysSuggestionsUseCase`**

Open `TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs`. Two surgical changes:

**Change A:** swap the constructor dependency. Replace `GetTodaysSuggestionUseCase todaysSuggestion` with `GetAllTodaysSuggestionsUseCase getAllTodaysSuggestions`.

**Change B:** in `ExecuteCore`, the live-mode branch becomes a per-ticker enumeration. Replace the focus-only block:

```csharp
// Before (Task 1's update):
Suggestion? todays;
if (input.IsHistorical)
{
    todays = await suggestionRepo.FirstOrDefaultAsync(
        new SuggestionForDateSpec(target, focusInstrument.Id), ct);
}
else
{
    todays = await todaysSuggestion.ExecuteAsync(
        new GetTodaysSuggestionInput(focusInstrument.Id), ct);
}
```

with:

```csharp
// After (Task 3):
IReadOnlyList<Suggestion> allTodays;
Suggestion? todays;
if (input.IsHistorical)
{
    // Historical mode — read-only across all held instruments.
    var heldIds = ordered.Where(i => i.Kind == InstrumentKind.Held).Select(i => i.Id).ToList();
    var historicalRows = new List<Suggestion>();
    foreach (var id in heldIds)
    {
        var row = await suggestionRepo.FirstOrDefaultAsync(
            new SuggestionForDateSpec(target, id), ct);
        if (row is not null) historicalRows.Add(row);
    }
    allTodays = historicalRows;
}
else
{
    // Live mode — Saga aggregator fans out per held instrument.
    allTodays = await getAllTodaysSuggestions.ExecuteAsync(Unit.Value, ct);
}

todays = allTodays.SingleOrDefault(s => s.InstrumentId == focusInstrument.Id);
```

**Change C:** populate `TickerView.TodaysCall` from `allTodays`. Find the per-ticker loop where `tickers.Add(new TickerView(...))` happens. Currently the `TickerView` is constructed with 7 args. Add the 8th — the per-instrument call (or `null` for Watchlist):

```csharp
foreach (var inst in ordered)
{
    var reading = await indicators.ComputeFor(inst.Ticker, target, ct);
    decimal? eur = string.Equals(inst.Currency, "EUR", StringComparison.OrdinalIgnoreCase)
        ? reading.Price
        : await fx.ToEurAsync(reading.Price, inst.Currency, target, ct);

    var deltaPct = await ComputeDeltaPctAsync(inst.Ticker, target, ct);
    var spark    = await ComputeSparkAsync(inst.Ticker, target, ct);

    // NEW — look up this instrument's call from the batch we already fetched.
    var todaysCall = inst.Kind == InstrumentKind.Held
        ? allTodays.FirstOrDefault(s => s.InstrumentId == inst.Id)
        : null;

    tickers.Add(new TickerView(
        inst.Ticker, inst.Currency, reading.Price, eur, deltaPct, reading.Zone, spark, todaysCall));

    if (inst.Kind == InstrumentKind.Held && eur is { } e)
        priceMap[inst.Id] = (e, inst.Ticker, inst.Currency);
}
```

**Ordering caveat:** `allTodays` must be computed *before* the `tickers.Add` loop — but the `tickers` loop also computes the per-ticker indicators that the `priceMap` (which is fed into `portfolio.SnapshotAsync`) depends on. Reorder if needed: compute `allTodays` first (it doesn't depend on `tickers`/`priceMap`), then the `foreach (var inst in ordered)` loop, then `portfolio.SnapshotAsync`. The existing method already orders things this way (the `allTodays` block is below the `tickers` loop in the current code) — invert the order: pull the live-vs-historical block above the per-ticker loop, and keep the rest of the method body downstream of `portfolio.SnapshotAsync` exactly as it is.

- [ ] **Step 3: Update tests for the new dependency and the new `TickerView` field**

Open `TradyStrat.Tests/Dashboard/UseCases/LoadDashboardUseCaseTests.cs`. Update `BuildSut` to construct `GetAllTodaysSuggestionsUseCase` and pass it to `LoadDashboardUseCase` instead of (or in addition to, depending on existing structure) the single-ticker variant:

```csharp
var listInstruments = new ListInstrumentsUseCase(
    new TestRepo<Instrument>(db),
    NullLogger<ListInstrumentsUseCase>.Instance);

var todays = new GetTodaysSuggestionUseCase(
    new TestRepo<Suggestion>(db), snapStub, aiStub, clock,
    new TestRepo<Instrument>(db),
    NullLogger<GetTodaysSuggestionUseCase>.Instance);

var getAllTodays = new GetAllTodaysSuggestionsUseCase(
    todays, listInstruments,
    NullLogger<GetAllTodaysSuggestionsUseCase>.Instance);

var uc = new LoadDashboardUseCase(
    indicators, portfolio, growth, fx,
    new TestRepo<GoalConfig>(db),
    new TestRepo<Trade>(db),
    new TestRepo<PriceBar>(db),
    new TestRepo<Suggestion>(db),
    new TestRepo<FxRate>(db),
    listInstruments,
    config,
    getAllTodays,                 // was: todays
    coord,
    nav,
    NullLogger<LoadDashboardUseCase>.Instance);
```

Existing tests that asserted on `vm.TodaysCall` continue to work because the focus's call is still pulled out via `allTodays.SingleOrDefault(s => s.InstrumentId == focusInstrument.Id)`.

Add a new fact asserting the per-ticker `TickerView.TodaysCall` field gets populated for Held and stays `null` for Watchlist. Use the existing seed pattern from `SeedBaseAsync`; confirm the focus's `TodaysCall` is non-null and any Watchlist ticker's is null:

```csharp
[Fact]
public async Task Tickers_have_TodaysCall_for_Held_only()
{
    var ct = TestContext.Current.CancellationToken;
    await using var db = InMemoryDb.Create();
    await SeedBaseAsync(db);

    var (uc, _, _) = BuildSut(db);
    var vm = await uc.ExecuteAsync(new LoadDashboardInput(Target, false), ct);

    var heldTickers = vm.Tickers.Where(t => t.Currency != "EUR" || t.Ticker == "CON3.L").ToList();
    vm.Tickers.Single(t => t.Ticker == "CON3.L").TodaysCall.ShouldNotBeNull();
    vm.Tickers.Single(t => t.Ticker == "COIN").TodaysCall.ShouldBeNull();   // Watchlist
}
```

(Adjust the assertions to whatever instruments `SeedBaseAsync` actually seeds — the existing test file already has the helper.)

- [ ] **Step 4: Build, test, commit**

```bash
dotnet build TradyStrat.slnx
dotnet test  TradyStrat.slnx
```
Expected: green.

```bash
git add TradyStrat/Features/Dashboard/TickerView.cs \
        TradyStrat/Features/Dashboard/UseCases/LoadDashboardUseCase.cs \
        TradyStrat.Tests/Dashboard/UseCases/LoadDashboardUseCaseTests.cs
git commit -m "feat(dashboard): per-ticker TodaysCall on TickerView via Saga aggregator"
```

---

## Task 4 — `PortfolioRail.razor` per-ticker chip

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/PortfolioRail.razor`
- Modify: `TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.css`

### Steps

- [ ] **Step 1: Add the chip block to the `@foreach` cell**

Open `PortfolioRail.razor`. The current `@foreach (var t in Tickers)` cell has a structure ending with:

```razor
@if (t.Spark.Count >= 2)
{
    <div class="cell-spark">@((MarkupString)Sparkline.Render(t.Spark, 140, 28))</div>
}
```

Append the chip block immediately after the spark block (still inside the cell):

```razor
@if (t.TodaysCall is { } call)
{
    <div class="rail-call">
        <span class="action @(call.Action.ToString().ToLowerInvariant())">@call.Action</span>
        <span class="rationale">@TruncateRationale(call.Rationale, 80)</span>
    </div>
}
```

Add the helper to the existing `@code` block at the bottom of the file. If there is no `@code` block today, append one:

```razor
@code {
    private static string TruncateRationale(string rationale, int maxChars)
    {
        if (string.IsNullOrEmpty(rationale)) return "";
        if (rationale.Length <= maxChars) return rationale;
        var slice = rationale[..maxChars];
        var lastDot = slice.LastIndexOfAny(['.', '!', '?']);
        return lastDot > maxChars / 2
            ? slice[..(lastDot + 1)]
            : slice + "…";
    }
}
```

(If a `@code` block already exists in the file with other helpers, just add the `TruncateRationale` method to it.)

- [ ] **Step 2: Add scoped CSS**

Append to `PortfolioRail.razor.css`:

```css
.rail-call {
    margin-top: 14px;
    display: flex;
    align-items: baseline;
    gap: 10px;
    font-family: var(--font-mono);
    font-size: 11px;
    line-height: 1.4;
}

.rail-call .action {
    font-weight: 600;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    padding: 3px 7px;
    border: 1px solid var(--vault-rule);
    color: var(--vault-ivory);
    flex-shrink: 0;
}
.rail-call .action.acquire { color: var(--vault-green); border-color: var(--vault-green); }
.rail-call .action.hold    { color: var(--ink-3); }
.rail-call .action.trim    { color: #c8a857; border-color: #c8a857; }
.rail-call .action.wait    { color: var(--ink-3); }

.rail-call .rationale {
    color: var(--ink-3);
    overflow: hidden;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    line-clamp: 2;
}
```

(Colour values for `trim` use a muted gold to distinguish from `acquire`'s green and from the focus's other amber accents — adjust to match the palette in `vault.css` if there's a closer fit.)

- [ ] **Step 3: Manual smoke (if port 5180 is free)**

```bash
dotnet build TradyStrat.slnx
dotnet test  TradyStrat.slnx
```
Expected: green.

If a TradyStrat instance isn't already running on 5180, start one:
```bash
dotnet run --project TradyStrat
```

Open http://127.0.0.1:5180/. Confirm:
- Holdings rail (Section VI) renders chips below each held instrument's sparkline.
- Focus ticker (CON3.L) shows the same Action as the `TodaysCallCard` (Section III) — sanity check that they're reading the same Suggestion.

If only one Held instrument exists today (CON3.L), add a second via Settings → Add instrument (e.g. ETHE.PA, currency EUR). After the next dashboard reload the rail will show ETHE.PA's chip. **While doing this**, tail the daily log file:

```bash
tail -f ~/Library/Application\ Support/TradyStrat/logs/tradystrat-$(date +%Y%m%d).log
```

Confirm there are no `Warning: AI call failed for ETHE.PA` lines. If you see one, the call is failing for a real reason (Anthropic auth, quota, network) — investigate.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/PortfolioRail.razor \
        TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.css
git commit -m "feat(dashboard): per-ticker AI call chip on PortfolioRail"
```

---

## Task 5 — Rename `SnapshotFactory` → `AiSnapshotService` (optional but spec-recommended)

**Files (mechanical rename):**
- Rename: `TradyStrat/Features/AiSuggestion/Snapshot/SnapshotFactory.cs` → `AiSnapshotService.cs` (rename file + rename class).
- Rename: `TradyStrat/Features/AiSuggestion/Snapshot/ISnapshotFactory.cs` → `IAiSnapshotService.cs` (rename file + rename interface).
- Modify all consumers: rename `ISnapshotFactory` → `IAiSnapshotService` and `SnapshotFactory` → `AiSnapshotService`.

### Steps

- [ ] **Step 1: Find every reference**

```bash
grep -rln "SnapshotFactory\|ISnapshotFactory" TradyStrat TradyStrat.Tests
```

Expected hits (rough): the two files being renamed, `AiSuggestionModule.cs`, three use cases (`GetTodaysSuggestionUseCase`, `ForceRefetchSuggestionUseCase`, `BackfillSuggestionsUseCase`), `SuggestionFactoryLog` partial class, and several test files. The `SnapshotFactoryLog` partial class is associated with `SnapshotFactory.cs` and gets renamed too (e.g. to `AiSnapshotServiceLog`).

- [ ] **Step 2: Rename files + class names**

```bash
git mv TradyStrat/Features/AiSuggestion/Snapshot/SnapshotFactory.cs \
       TradyStrat/Features/AiSuggestion/Snapshot/AiSnapshotService.cs
git mv TradyStrat/Features/AiSuggestion/Snapshot/ISnapshotFactory.cs \
       TradyStrat/Features/AiSuggestion/Snapshot/IAiSnapshotService.cs
```

In each renamed file: change `class SnapshotFactory` → `class AiSnapshotService`, change `interface ISnapshotFactory` → `interface IAiSnapshotService`, and change the `SnapshotFactoryLog` partial class to `AiSnapshotServiceLog` (and its method calls inside the service body).

- [ ] **Step 3: Update consumers**

Repeat for each file `grep` flagged in Step 1: replace `SnapshotFactory` → `AiSnapshotService` and `ISnapshotFactory` → `IAiSnapshotService`. Test-file `StubSnapshotFactory` can stay as-is (it's a test name, not the production type) — it just needs to implement the renamed `IAiSnapshotService` interface.

- [ ] **Step 4: Update test class names**

`TradyStrat.Tests/AiSuggestion/Snapshot/SnapshotFactoryTests.cs` → optionally rename to `AiSnapshotServiceTests.cs` (or leave; tests can keep the historical name with a comment). Recommend renaming for consistency:

```bash
git mv TradyStrat.Tests/AiSuggestion/Snapshot/SnapshotFactoryTests.cs \
       TradyStrat.Tests/AiSuggestion/Snapshot/AiSnapshotServiceTests.cs
```

Inside the renamed file, change `class SnapshotFactoryTests` → `class AiSnapshotServiceTests`. The `BuildSut(db)` helper returns the renamed concrete type.

`SnapshotFactoryPerInstrumentTests.cs` (if you created it as a separate file in Task 1) should follow the same naming.

- [ ] **Step 5: Build, test**

```bash
dotnet build TradyStrat.slnx
dotnet test  TradyStrat.slnx
```
Expected: green. The sentinel test (now in `AiSnapshotServiceTests.cs`) still asserts `"895EED53A280A470"`.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
refactor(ai): rename SnapshotFactory -> AiSnapshotService (per spec §8)

The class has 10 injected collaborators and orchestrates the snapshot
build process — it's a service, not a factory. The Factory name was a
historical holdover. Pure mechanical rename + log-class rename. No
behavior change. Sentinel test continues to pass at hash
"895EED53A280A470". README §17 should be updated separately to remove
the "Factory Method" entry for this class.
EOF
)"
```

---

## Self-review

**1. Spec coverage:**
- §1 purpose & §2 decisions → captured in plan goal/architecture.
- §3 schema migration → Task 1 (Steps 1–14, 21).
- §4.1 GetTodaysSuggestionUseCase → Task 1 Step 8.
- §4.2 ForceRefetchSuggestionUseCase → Task 1 Step 9.
- §4.3 GetAllTodaysSuggestionsUseCase → Task 2.
- §4.4 BackfillSuggestionsUseCase → Task 1 Step 10.
- §4.5 SuggestionBackfillCoordinator → Task 1 Step 11.
- §4.6 SnapshotFactory.CreateAsync widening → Task 1 Step 5.
- §4.7 AiSnapshot + InstrumentId → Task 1 Step 4 + Step 6 (`SuggestionService.AskAsync` plumbing).
- §4.8 LoadDashboardUseCase live-mode → Task 1 Step 12 + Task 3 Step 2.
- §5 dashboard rendering → Tasks 3 + 4.
- §6 deferred items → no tasks (correctly).
- §7 test plan → Tasks 1, 2, 3 cover all required test additions/updates.
- §8 GoF rename → Task 5.

**2. Placeholder scan:** no "TBD"/"TODO"/"add appropriate"/"similar to" survives. Every code step shows actual code.

**3. Type consistency:**
- `GetTodaysSuggestionInput(int InstrumentId)` consistent across Tasks 1, 2, 3.
- `BackfillSuggestionsInput(DateOnly Date, int InstrumentId)` consistent across Task 1 production + tests.
- `AiSnapshot(Today, InstrumentId, Goal, Portfolio, Tickers, RecentTrades, UsdPerEur, Markets, PromptHash)` matches Task 1 Step 4 + Task 1 Step 5 + Task 2 test fixtures.
- `SuggestionAction.{Acquire, Hold, Trim, Wait}` referenced in Task 4 CSS classes.
- Sentinel hash `"895EED53A280A470"` referenced consistently in Conventions, Task 1 Step 23, Task 5 Step 5, Task 5 Step 6.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-08-multi-ticker-ai.md`. Two execution options:

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration.

**2. Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints.

Which approach?
