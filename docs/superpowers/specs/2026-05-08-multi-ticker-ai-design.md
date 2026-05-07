# TradyStrat — Multi-ticker AI (Phase 2 core)

**Status:** Design approved · Ready for implementation plan
**Date:** 2026-05-08
**Author:** Philippe Matray (with Claude)
**Depends on:** [`2026-05-07-multi-ticker-foundation-design.md`](./2026-05-07-multi-ticker-foundation-design.md)
**Successor:** Phase 3 — citations / scorecard / templates (separate spec, not yet written)

---

## 1. Purpose & goal

Phase 1 widened the data model and dashboard so the user can hold N
first-class tickers, but the AI loop deliberately stayed single-ticker
— a daily call for `appsettings.Tickers.Focus` only. Held instruments
other than the focus get no AI guidance.

This phase makes the AI loop **per-ticker for Held instruments**. Each
held instrument gets its own daily Buy/Hold/Sell suggestion. The focus
ticker keeps its lead position in the dashboard's hero call card; other
held tickers surface their call as a chip in the Holdings rail.

The phase deliberately stops short of the full "AI feedback loop" the
broader DB analysis surfaced — citations remain a JSON blob, suggestions
aren't linked to trades, and there's no scorecard. Each of those is a
separate Phase 3 slice that layers cleanly onto this phase's schema.

## 2. Decisions at a glance

| Concern | Decision |
|---|---|
| AI scope | **Held instruments only.** Watchlist stays purely indicator/context. |
| Prompt strategy | **N independent calls per day.** One Anthropic call per held instrument. Failure isolation: a failing per-ticker call doesn't take down the others. |
| UI placement | **Per-ticker chip in Holdings rail (Section VI).** Focus card (Section III) unchanged — fed the focus instrument's call. |
| Backfill scope | **Focus only.** `SuggestionBackfillCoordinator` keeps existing scope. Other held tickers get fresh-only calls. |
| Implementation shape | **Approach A:** widen `GetTodaysSuggestionUseCase` to take `InstrumentId`; new `GetAllTodaysSuggestionsUseCase` orchestrates the loop. Force-refetch widens identically. |
| Migration | **Single EF migration** named `MultiTickerAiPhase2`. Auto-applied on startup. |
| Existing-row backfill | **Hardcoded `'CON3.L'`** in the migration's UPDATE SQL. Same precedent as Phase 1's Trades.InstrumentId backfill. |
| Rollback | **None.** User `cp`s the DB before first run on the new code. |
| PromptHash byte-identity | **Preserved for the focus ticker's day-zero call.** Phase 1's sentinel hash `2EB10B0275AD1282` must still pass post-Phase-2; the SnapshotFactory call site changes from `CreateAsync(asOf)` → `CreateAsync(focusInstrumentId, asOf)` but the hashed payload is unchanged. |
| Concurrency | **Sequential.** N calls run one after another. Parallelisation deferred. |
| Per-ticker exchange timezone | **Used.** Each per-ticker call uses that instrument's `TimezoneId` for "today" — instruments on different exchanges roll their day correctly. |
| Test stack | xunit.v3 + Shouldly + EF Core InMemory + captured JSON fixtures + hand-rolled stubs. **No bUnit.** No live Anthropic calls. |

## 3. Schema migration

A single EF Core migration. The `Suggestions` table gains an
`InstrumentId` FK, gets its UQ index replaced, and existing rows are
backfilled to the focus ticker's seeded id.

### 3.1 Domain model

```
Suggestion (CHANGED)
  + InstrumentId  int FK Instruments.Id   -- NOT NULL after backfill
  -- existing columns unchanged
```

### 3.2 EF configuration (`SuggestionConfiguration.cs`)

```csharp
builder.HasOne<Instrument>()
       .WithMany()
       .HasForeignKey(s => s.InstrumentId)
       .OnDelete(DeleteBehavior.Restrict);
builder.HasIndex(s => new { s.ForDate, s.InstrumentId }).IsUnique();
// Old: HasIndex(s => s.ForDate).IsUnique();   -- removed
```

### 3.3 Migration body (`MultiTickerAiPhase2.cs`)

EF auto-emits `AddColumn` and the index swap. Hand-edit to insert the
backfill UPDATE between adding the nullable column and altering it to
NOT NULL:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 1. Add nullable InstrumentId column.
    migrationBuilder.AddColumn<int>(
        name: "InstrumentId",
        table: "Suggestions",
        type: "INTEGER",
        nullable: true);

    // 2. Backfill: every existing Suggestion row was for the focus
    //    ticker (CON3.L) at the time this migration was authored —
    //    Phase 1 hardcoded the AI loop to the configured focus.
    //    Hardcoded literal matches the precedent set by the
    //    Trades.InstrumentId backfill in MultiTickerFoundation.
    migrationBuilder.Sql(@"
        UPDATE Suggestions
           SET InstrumentId = (SELECT Id FROM Instruments WHERE Ticker = 'CON3.L')
         WHERE InstrumentId IS NULL;");

    // 3. Make NOT NULL + add FK + composite UQ; drop old UQ.
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
```

### 3.4 Migration backfill caveat

The `'CON3.L'` literal is correct **only if** every stored Suggestion
pre-Phase-2 was for the focus ticker. Phase 1 enforces this implicitly:
the AI loop reads `appsettings.Tickers.Focus` (which has stayed
`CON3.L` since Phase 0), and only one Suggestion per ForDate is allowed
by the old UQ. So every existing row's instrument is unambiguously the
focus.

If a user manually changes `Tickers:Focus` between Phase 1 and Phase 2
*and* generates Suggestions for the new focus before running this
migration, the backfill would mis-attribute those rows. We accept this
as a Phase-2-author-time assumption documented in the migration's
commit message; no production guard needed.

### 3.5 Specification classes — signature ripple

The `Suggestions` UQ change ripples to every spec that filters on
`ForDate`. Constructor signatures change from `(DateOnly date)` to
`(DateOnly date, int instrumentId)`. Files affected:

- `Features/AiSuggestion/Specifications/SuggestionForDateSpec.cs`
- `Features/AiSuggestion/Specifications/PriorSuggestionSpec.cs`
- `Features/AiSuggestion/Specifications/SuggestionsInRangeSpec.cs`
- `Features/AiSuggestion/Specifications/LatestSuggestionSpec.cs`

Like Phase 1's FX-stack widening, this is one atomic commit because
the signature change ripples through callers in `LoadDashboardUseCase`,
`GetTodaysSuggestionUseCase`, `ForceRefetchSuggestionUseCase`,
`SuggestionBackfillCoordinator`, `BackfillSuggestionsUseCase`, and the
existing tests for those.

### 3.6 Code-side cleanup that must accompany this migration

- `Suggestion` entity gains `required int InstrumentId`.
- `SuggestionConfiguration.cs` updated per §3.2.
- All call sites of the four spec classes pass an `InstrumentId`.
- Tests that construct `Suggestion` literals (existing test fixtures) gain `InstrumentId = 1` (or the equivalent seeded focus id).

### 3.7 Rollback policy

None. The user `cp`s the live DB before first run on the new code.
Same precedent as Phase 1.

## 4. Use case redesign

### 4.1 `GetTodaysSuggestionUseCase` — per-instrument input

```csharp
public sealed record GetTodaysSuggestionInput(int InstrumentId);

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

Key changes:
- Input record carries `InstrumentId` (was `Unit`).
- Constructor gains `IReadRepositoryBase<Instrument>` for the entity lookup.
- `IConfiguration` injection (added in the recent focus-ticker polish) is **removed** — the "today" call now uses *that instrument's* exchange timezone, not the global focus's. Per-instrument date semantics matter for instruments on different exchanges.
- Spec query is per-instrument.

### 4.2 `ForceRefetchSuggestionUseCase` — same shape change

```csharp
public sealed record ForceRefetchSuggestionInput(int InstrumentId);

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

Constructor mirror of §4.1. `IConfiguration` injection (added in the
recent focus-ticker polish) is removed for the same reason — per-instrument
date semantics use the instrument's own timezone, not the global focus's.

The delete is per-instrument-scoped — refetching the focus's call doesn't touch other instruments' rows.

### 4.3 New: `GetAllTodaysSuggestionsUseCase`

```csharp
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

- Sequential calls, per-ticker `try/catch` on the typed-exception family.
- Other exceptions propagate (signal a real bug).
- Returns the partial list of successful Suggestions.

### 4.4 `BackfillSuggestionsUseCase` — per-day-and-instrument input

The existing per-day worker the coordinator dispatches gains an `InstrumentId`:

```csharp
public sealed record BackfillSuggestionsInput(DateOnly Date, int InstrumentId);
```

Implementation mirrors `GetTodaysSuggestionUseCase` but uses the
`Date` from input rather than computing "today" from the clock.

### 4.5 `SuggestionBackfillCoordinator` — focus-only scope, mechanically widened

The coordinator's contract is unchanged: backfill missing days for the
focus ticker. Two small mechanical changes:

1. Resolves the focus instrument once at the start of `EnsureBackfilledAsync` (via `IConfiguration["Tickers:Focus"]` + `InstrumentByTickerSpec`).
2. Passes that focus's `Id` to every per-day call.

Other Held instruments get **no historical backfill**. If a user adds
ETHE.PA mid-Phase-2, ETHE.PA's first-day call goes through but past
days stay empty for that instrument — acceptable (there's nothing to
look back at anyway).

### 4.6 `SnapshotFactory.CreateAsync` widens to take `instrumentId`

```csharp
// Phase 1:
Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct);

// Phase 2:
Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct);
```

The passed `instrumentId` becomes the prompt's "primary" subject. The
loop over Held + Watchlist instruments runs as before, but the *primary*
identification shifts:

- Pre-Phase-2: primary = `IConfiguration["Tickers:Focus"]`.
- Post-Phase-2: primary = `instruments.GetByIdAsync(instrumentId)` resolved at the start of `CreateAsync`.

The `legacyOrder` trick (Phase 1 commit `7f794c9`) — `["COIN", "BTC-USD"]` for the seeded watchlist — is preserved when the primary is the focus, so day-zero PromptHash for CON3.L stays byte-identical.

### 4.7 `AiSnapshot` shape — `InstrumentId` carried through

`AiSnapshot` gains an `InstrumentId` field so the produced
`Suggestion.InstrumentId` is set by `SuggestionService.AskAsync`
without re-resolving:

```csharp
public sealed record AiSnapshot(
    DateOnly AsOf,
    int InstrumentId,            // NEW
    GoalConfig Goal,
    PortfolioSnapshot Portfolio,
    IReadOnlyList<TickerContext> Tickers,
    IReadOnlyList<TradeRecent> RecentTrades,
    decimal? UsdPerEur,
    string PromptHash);
```

`PromptHash` is computed over a JSON serialisation of the full payload
including `InstrumentId` — different instruments produce different
hashes, even on the same date.

### 4.8 `LoadDashboardUseCase` — call the "all" variant in live mode

```csharp
// Live mode:
var allTodays = await getAllTodays.ExecuteAsync(Unit.Value, ct);

// vm.TodaysCall is the focus instrument's call:
todays = allTodays.SingleOrDefault(s => s.InstrumentId == focusInstrument.Id);

// Per-ticker view-model entries get their own:
foreach (var inst in ordered) {
    var todaysCall = inst.Kind == InstrumentKind.Held
        ? allTodays.FirstOrDefault(s => s.InstrumentId == inst.Id)
        : null;
    tickers.Add(new TickerView(... , todaysCall));
}
```

Historical mode loops `SuggestionForDateSpec(targetDate, inst.Id)` per
held instrument with no AI invocation — same read-only pattern as
Phase 1.

## 5. Dashboard rendering

### 5.1 View-model change

`TickerView` (per-ticker record on `DashboardViewModel.Tickers`) gains
a nullable `Suggestion`:

```csharp
public sealed record TickerView(
    string Ticker,
    string Currency,
    decimal PriceNative,
    decimal? PriceEur,
    decimal? DeltaPct,
    Zone Zone,
    Sparkline Spark,
    Suggestion? TodaysCall);   // NEW — non-null for Held, null for Watchlist
```

### 5.2 `PortfolioRail.razor` — per-ticker call chip

Each per-ticker cell in the rail gains a small chip below the existing
content (price + delta + sparkline + zone label). The exact iteration
variable name (`t`, `Ticker`, `i`, …) is whatever the existing rail's
`@foreach` uses today — implementation reads the file first and matches
the prevailing convention.

```razor
@* substituting <iter> for whichever the existing rail uses *@
@if (<iter>.TodaysCall is { } call)
{
    <div class="rail-call">
        <span class="action @(call.Action.ToString().ToLowerInvariant())">
            @call.Action
        </span>
        <span class="rationale">@TruncateRationale(call.Rationale, 80)</span>
    </div>
}
```

`SuggestionAction` enum already exists (`Acquire` / `Hold` / `Sell`).
`TruncateRationale(text, maxChars)` is a helper that cuts at the
nearest sentence boundary up to ~80 chars; full rationale stays in
`TodaysCallCard` for the focus.

### 5.3 Empty state

If `getAllTodays` returns fewer items than there are Held instruments
(a per-ticker call failed silently), the rail chip is absent for that
instrument. The use case logged the failure at Warning. No error UI on
the rail itself.

### 5.4 `TodaysCallCard.razor` (Section III) — unchanged behavior

Still shows the focus instrument's call. The data flow becomes:

```
LoadDashboardUseCase: vm.TodaysCall = allTodays.SingleOrDefault(s => s.InstrumentId == focus.Id)
DashboardPage.razor: <TodaysCallCard Sug="vm.TodaysCall" ... />
```

No layout changes; just a different upstream query.

### 5.5 Acknowledged UX gaps (deferred)

- **No "force re-fetch this specific non-focus ticker" button.** `Re-run AI` in `TodaysCallCard` only re-fetches the focus. Force-refetching other tickers requires DB inspection or an app restart. Acceptable for v1; can be added by mounting a small `Re-run` action on each rail chip in a follow-up.
- **No tooltip / expand for full rationale on rail chips.** Truncated rationale is the only display. Users wanting depth click through to... well, nothing yet. Future addition: a hover tooltip or modal.

## 6. What this spec deliberately defers

These items are valuable but out of scope for Phase 2 core. Each
layers cleanly onto Phase 2's `Suggestions.InstrumentId` schema
without backfill complications.

- **Structured citations table.** Replace `CitationsJson` blob with `SuggestionCitations(SuggestionId, Indicator, Ticker, Value, Claim)`. Unlocks "show me every Buy where RSI<30" and prompt-quality regression analysis. Phase 3 territory.
- **Suggestion ↔ Trade linkage.** `Trades.SuggestionId` nullable FK + outcome columns on `Suggestions` (`OutcomeEvaluatedAt`, `RealisedReturnPct`). Enables an AI accuracy scorecard. Phase 3.
- **Prompt template versioning.** `PromptTemplates(Id, Version, Body)` + `Suggestions.PromptTemplateId`. Enables A/B testing prompts. Phase 3 or later.
- **Watchlist suggestions.** Watchlist instruments get no AI call; remain context-only.
- **Concurrent / parallel AI calls.** N calls run sequentially. Parallelisation is a follow-up if N grows or wall-clock matters.
- **Per-ticker re-fetch UI.** No button on the rail chip. Force-refetch focus only.
- **Per-ticker rationale expansion.** No tooltip / modal for the truncated rationale on the chip.
- **Backfill for non-focus tickers.** Coordinator stays focus-only; non-focus held instruments get fresh-only.
- **Historical Re-run AI for non-focus tickers.** Historical mode is read-only; calls are queried, never invoked. Same as Phase 1.

## 7. Test plan

### 7.1 New test files

| File | What it covers |
|---|---|
| `TradyStrat.Tests/AiSuggestion/UseCases/GetAllTodaysSuggestionsUseCaseTests.cs` | Three Facts: (a) returns one Suggestion per Held instrument when all calls succeed; (b) Watchlist instruments are excluded from the loop; (c) when one ticker's call throws `PriceFeedUnavailableException`, the others still complete and the result excludes the failed one. |
| `TradyStrat.Tests/AiSuggestion/Snapshot/SnapshotFactoryPerInstrumentTests.cs` | Two Facts: `CreateAsync(focus.Id, asOf, ct)` and `CreateAsync(otherHeld.Id, asOf, ct)` produce different `PromptHash` values. The non-primary instrument appears in `ContextTickers`; the primary does not. |
| `TradyStrat.Tests/Data/MultiTickerAiPhase2MigrationTests.cs` | Real-SQLite migration test (matches Phase 1's `MultiTickerMigrationTests.cs` pattern). Asserts: `Suggestions.InstrumentId` column added; existing rows backfilled to CON3.L's seeded id; old UQ on `(ForDate)` gone; new UQ on `(ForDate, InstrumentId)` present. |

### 7.2 Extended existing tests

- `GetTodaysSuggestionUseCaseTests.cs` — input shape becomes `new GetTodaysSuggestionInput(instrumentId)`. Existing happy-path and cached-hit tests adapt. Add a Fact: an instrument with `TimezoneId = "Europe/Paris"` triggers `clock.TodayInExchangeTzFor("ETHE.PA")` (or the ticker analog).
- `ForceRefetchSuggestionUseCaseTests.cs` — input shape change. Add a Fact: refetching focus doesn't delete other instruments' Suggestions.
- `BackfillSuggestionsUseCaseTests.cs` — input now includes `InstrumentId`. Add a Fact: backfill for instrument A leaves instrument B's stored Suggestions untouched.
- `SuggestionBackfillCoordinatorTests.cs` — coordinator resolves focus once, passes through to per-day calls. Add a Fact: coordinator's window backfills only focus, not other Held instruments.
- `SpecsRoundtripTests.cs` (or wherever Suggestion specs are tested) — `SuggestionForDateSpec`, `PriorSuggestionSpec`, `SuggestionsInRangeSpec`, `LatestSuggestionSpec` constructor signatures all change. Add round-trip tests that confirm `(date, instrumentId)` filtering.
- `LoadDashboardUseCaseTests.cs` — wire `GetAllTodaysSuggestionsUseCase` into `BuildSut`. Seed two Held instruments + a stub `IAiClient` returning per-instrument suggestions. Assert `vm.Tickers` has `TodaysCall` populated for both Held entries and `null` for Watchlist.

### 7.3 Unchanged tests (regression sentinel)

The Phase 1 test `Catalog_produces_byte_identical_PromptHash_against_seeded_set` (`SnapshotFactoryTests.cs`) **must continue to pass**. It asserts `PromptHash == "2EB10B0275AD1282"` for the seeded `[CON3.L Held, COIN Watchlist, BTC-USD Watchlist]` set. After Phase 2:

```csharp
// Before:
var snap = await factory.CreateAsync(asOf, ct);
// After:
var snap = await factory.CreateAsync(focus.Id, asOf, ct);
```

Same data flow, same hashed payload, same expected hash. **If the hash changes**, the prompt input drifted (likely a serialisation change) and Phase 2 has introduced a regression — investigate before merge.

### 7.4 Pre-merge manual smoke test

This is a personal project; manual smoke is part of the contract.

1. Back up the live DB: `cp ~/Library/Application\ Support/TradyStrat/tradystrat.db ~/tradystrat.db.pre-phase2.bak`.
2. Run the new code; verify the migration applies and the two existing CON3.L Suggestions get `InstrumentId` set to CON3.L's seeded id.
3. Verify the dashboard renders today's call for CON3.L unchanged. PromptHash byte-identity is enforced by the sentinel test in §7.3 — this manual step is for confidence, not for catching the regression.
4. Add a second Held instrument (e.g. `ETHE.PA`) via the Settings flow; refresh the dashboard; verify ETHE.PA gets its own AI call rendered as a chip in the Holdings rail with Action + truncated rationale.
5. Tail the app log during a fresh dashboard load and confirm only one "AI call" line fires for the focus ticker via the backfill coordinator if there's a missing-day gap; non-focus tickers' fresh calls fire only once per day via `GetAllTodaysSuggestionsUseCase`. (You don't need to contrive a gap to verify this — the log message naming alone tells you which path fired.)

## 8. GoF pattern drift recorded for this phase

Phase 1's §13 noted two pieces of pattern drift:

1. `SnapshotFactory` becoming a service (was Factory Method).
2. Symbol resolution wanting a future Strategy.

Phase 2 deepens drift #1 — `SnapshotFactory.CreateAsync` now accepts a
runtime parameter (`instrumentId`) and resolves an entity from the DB
inside the factory. The factory has fully crossed into service
territory; "Factory" is now a pure naming holdover.

**Recommendation:** rename `SnapshotFactory` → `AiSnapshotService` (or
similar) at the start of Phase 3 when the citations/scorecard work
warrants other refactoring around AI. Don't rename in this phase —
churn-without-payoff. README §17 should be updated when the rename
lands.
