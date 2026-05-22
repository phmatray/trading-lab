# TradyStrat — DDD domain rework

**Status:** Design approved · Ready for implementation plan
**Date:** 2026-05-22
**Author:** Philippe Matray (with Claude)
**Predecessor:** [`2026-05-13-hexagonal-refactor-design.md`](./2026-05-13-hexagonal-refactor-design.md) — established the layer split; this spec enriches the Domain inside it.

---

## 1. Purpose & goal

Today's `TradyStrat.Domain` is intentionally light: entities are immutable `record`s with `required init` properties, computed getters cover a few derivations (`GrossEur`, `NetEur`, `IsBuy`), but **business rules live in Application**. `LogTradeUseCase` validates `Quantity > 0`; `PortfolioService.BuildSnapshot` performs FIFO lot accounting, realized P&L, and fee folding; `SuggestionGate` partitions concurrency. The Domain holds shapes, not behavior.

This rework moves to a richer DDD model: aggregate roots that own their invariants, value objects that eliminate primitive obsession, and a small shared kernel (`Money`, `Currency`, `Ticker`, strongly typed ids) that flows through every layer. Application becomes a thin orchestrator — load aggregate, call behavior, save. `PortfolioService` is deleted; FIFO moves inside `Portfolio`.

The rework ships as **six vertical-slice phases** (one per aggregate cluster). Each phase is its own worktree, its own implementation plan, and an isolated PR. The shared kernel grows organically, driven by what each phase actually needs.

## 2. Decisions at a glance

| Concern | Decision |
|---|---|
| Motivation | All three: scattered rules, primitive obsession, fuzzy aggregate boundaries — full DDD makeover. |
| Portfolio boundary | **One `Portfolio` aggregate root** owning `Position` (child entity per instrument), which owns `Lot` and `Trade` (child entities). FIFO accounting moves into `Position.Record(...)`. |
| Persistence | **EF maps directly to the rich domain** — value converters for VOs and strongly typed ids, `OwnsOne`/`OwnsMany` for owned VOs, backing fields for collections, private setters and private constructors. No separate persistence model. |
| Phasing | **Vertical slice per aggregate**, six phases, one worktree per phase, one PR per phase. |
| Null in domain | **No nullable references in domain types.** VOs with optional semantics expose `static Empty`/`None` + `IsEmpty`/`IsSpecified`. Empty list for collections. Empty string for optional strings. Nulls only at adapter/persistence boundaries via EF value-converters. |
| Repository pattern | **Per-aggregate repositories** (`IPortfolioRepository`, `ISuggestionRepository`, `IInstrumentRepository`, `IGoalRepository`) replace generic `IRepositoryBase<T>` for AR access. Ardalis Specifications stay, but only inside Infrastructure as a query-construction detail. |
| Domain events | **Not added yet.** Behavior methods return small "what happened" records (`TradeRecorded`, `SuggestionPersisted`); those become events when a real subscriber appears. |
| Use case shape | `UseCaseBase<TIn, TOut>` unchanged. Use cases become: load AR → call behavior → save → return the result record. No validation, no business logic in use cases. |
| Behavioral change | **None.** Every dashboard number, every FIFO calculation, every AI prompt output must remain identical. Existing tests are the regression contract. |

## 3. Target end state and phase order

When all six phases land:

- **`TradyStrat.Domain`** — rich aggregate roots (`Portfolio`, `Suggestion`, `Instrument`, `Goal`), value objects in `Domain/Shared/` (`Money`, `Currency`, `Ticker`, `Quantity`, `Price`, `Percentage`, `Conviction`, `Exchange`, `TimezoneId`, `CurrencyPair`, `RomanNumeralId`), strongly typed ids, factory methods, invariants, domain services where multi-aggregate logic genuinely needs to live (`SuggestionGate` decision logic, `IndicatorEngine` stays in Application as a port consumer).
- **`TradyStrat.Application`** — use cases shrink to "load → behave → save". `PortfolioService` is deleted. `SuggestionGate` decision logic moves to Domain (semaphore plumbing stays in Application). Specifications move to Infrastructure.
- **`TradyStrat.Infrastructure`** — EF mapping fluent-API expands to handle owned types, value converters, backing fields. Generic `IRepositoryBase<T>` is replaced by per-aggregate repositories for ARs (read-only generic repos stay for `PriceBar`/`FxRate` reads — narrowed and renamed).

**Phase order:**

1. **Shared kernel seed** — only what Portfolio needs: `Money`, `Currency`, `Ticker`, `Quantity`, `Price`, strongly typed ids. Compiles, dormant.
2. **Portfolio aggregate** — biggest payoff, hardest case, exercises every convention.
3. **Suggestion aggregate** — adds `Conviction`, typed `Citation` collection, `MarketSnapshot`, `PromptFingerprint`.
4. **Instrument aggregate** — adds `Exchange`, `TimezoneId`. Absorbs `InstrumentMetadata`.
5. **Cleanup slice** — Goal + MarketData (`PriceBar`, `FxRate`) + Indicators. Adds `Percentage`, `CurrencyPair`, `RomanNumeralId`. Small, low-risk.
6. **Settings refactor** — typed settings aggregates (`AnthropicSettings`, `YahooSettings`, `TickerSettings`, `FxSettings`, `DatabaseSettings`) replace the raw key-value `SettingEntry` read path. Per-aggregate update use cases. Touches every config-consuming module — shipped alone to isolate blast radius.

Each phase produces its own implementation plan; this spec covers the design for all six.

## 4. Shared kernel (final inventory)

These are the value objects and strongly typed ids the aggregates share. Phase 1 introduces the **bold** ones (what Portfolio needs); the rest land with their owning aggregate.

**Value objects (`TradyStrat.Domain/Shared/`):**

- **`Money(decimal Amount, Currency Currency)`** — sealed record. Arithmetic operators require matching `Currency`; throws `CurrencyMismatchException` on mixed-currency operations. `Money.Zero(Currency)` is a real value; `Money.None(Currency)` is the absence sentinel with `IsEmpty = true`.
- **`Currency`** — sealed VO over ISO 4217 string. `Currency.Parse("USD")`, static accessors `Currency.Eur`, `Currency.Usd`, `Currency.Gbp`. Three-letter validation, uppercase normalization.
- **`Ticker`** — sealed VO wrapping the Yahoo symbol. `Ticker.Of("CON3.L")`. Validation: non-empty, no whitespace.
- **`Quantity`** — `decimal` ≥ 0. Arithmetic operators; throws on negative. Internal `IsSpecified` flag so `Quantity.None` is distinct from `Quantity.Zero`.
- **`Price(Money PerUnit)`** — wraps `Money`. `Price × Quantity → Money`. Distinct type from `Money` so a price and a total cannot be swapped. `Price.None(Currency)` available.
- `Percentage` — `decimal` in `[-100..+∞]` for P&L%, progress%. Added in Phase 5.
- `Conviction` — `int` in `[1..10]`. Added in Phase 3.
- `Exchange` — sealed VO over Yahoo `fullExchangeName`. Added in Phase 4.
- `TimezoneId` — sealed VO over IANA identifier; validated at construction via `TimeZoneInfo.TryFindSystemTimeZoneById`. Added in Phase 4.
- `CurrencyPair(Currency Base, Currency Quote)` — `Pair.Of("EURUSD")`. Added in Phase 5.

**Aggregate-local VOs:** `PromptFingerprint` (in `Domain/Suggestions/`), `RomanNumeralId` (in `Domain/Goals/`, validates lowercase `i`/`ii`/`iii`/`iv`/`v`/…). These stay inside their aggregate folder, not in `Shared/`, because they're used by exactly one aggregate.

**Strongly typed ids:** `InstrumentId`, `TradeId`, `SuggestionId`, `GoalId`, `PositionId`. All `readonly record struct Id(int Value)` with a registered EF `ValueConverter`. `Id.New()` returns the "not yet persisted" sentinel (value `0`).

**Computed/read records (stay as records, not full VOs):** `BollingerReading`, `IchimokuReading`, `IndicatorBundle`, `IndicatorReading`, `Citation`, `MarketCitation`, `GrowthPoint`, `CapitalEvent`, `PositionRow`, `PortfolioSnapshot`. These are structured data, not invariant-protected concepts. They adopt the new VOs internally (`BollingerReading.UpperBand : Price`, `Citation.PublishedAt : DateOnly`).

**Cross-cutting absence rules:**

- Every VO with optional semantics provides `static Empty` (or `static None`) and an `IsEmpty` predicate.
- VOs where "absent" must be distinguishable from "zero" carry an internal `IsSpecified` flag — *not* equality with `Zero`.
- Optional strings on entities become non-nullable, defaulted to empty string. Check `string.IsNullOrEmpty` at boundaries.
- Collections on entities use `IReadOnlyList<T>` backed by a field initialized to `[]`, never null.

**Explicit non-additions:** `Email`, `DateRange` (as a kernel VO — phases may add it locally if needed), `Result<T>` / discriminated unions, generic `AggregateRoot` base class. None earn their keep here.

## 5. Conventions used in every aggregate

**Aggregate root and entity shape:**

```csharp
public sealed class Portfolio  // class, not record — identity, not value
{
    public PortfolioId Id { get; private set; }
    private readonly List<Position> _positions = new();
    public IReadOnlyList<Position> Positions => _positions;

    private Portfolio() { }                    // EF
    private Portfolio(PortfolioId id) { Id = id; }

    public static Portfolio Empty(PortfolioId id) => new(id);

    public TradeRecorded RecordTrade(...) { /* invariants + behavior */ }
}
```

- Aggregate roots and child entities are **classes**, not records. Identity matters; equality is by id.
- **Private parameterless constructor** for EF; **private field-backed collections** exposed read-only; **private setters** on properties.
- **No public constructor.** Creation goes through named static factory methods (`Portfolio.Empty(...)`, `Trade.Create(...)`, `Suggestion.From(...)`, `Instrument.Probed(...)`). Factories enforce invariants and throw the appropriate `*Exception` (a child of `TradyStratException`) on failure.
- **Behavior methods return the change they made** as a small record (e.g. `TradeRecorded(TradeId, PositionId, bool CreatedPosition, Money RealizedDelta)`). Useful for logging today; hookable as domain events later without rework.

**Repository pattern shift:**

Generic `IRepositoryBase<Trade>` / `IRepositoryBase<Suggestion>` are replaced by one repository interface per aggregate root, owned by Application:

```csharp
public interface IPortfolioRepository
{
    Task<Portfolio> GetAsync(CancellationToken ct);                 // singleton
    Task SaveAsync(Portfolio portfolio, CancellationToken ct);
}

public interface ISuggestionRepository
{
    Task<Suggestion?> GetForAsync(InstrumentId id, DateOnly date, CancellationToken ct);
    Task<IReadOnlyList<Suggestion>> ListForAsync(InstrumentId id, DateRange range, CancellationToken ct);
    Task<Suggestion?> LatestForAsync(InstrumentId id, CancellationToken ct);
    Task AddAsync(Suggestion suggestion, CancellationToken ct);
}
```

Repository return types may use nullable references (`Suggestion?`) because the repository sits at the adapter boundary — absence is a real outcome of a query. The no-null rule applies inside the aggregate, not on repository queries.

Ardalis Specifications stay, but only inside the repository **implementation** in Infrastructure. They are a query-construction detail, not a port. The `Specifications/` folders move from Application to Infrastructure as part of each phase.

Read-only generic repositories stay for `PriceBar` and `FxRate` (computed/reference time-series, not ARs): `IPriceBarReadRepository`, `IFxRateReadRepository`. These are write-only by the hosted services through narrow ports (`IPriceFeedWriter`, `IFxRateWriter`).

**EF mapping conventions:**

- **Strongly typed ids** → `ValueConverter<TradeId, int>` registered via a `ConfigureConventions` override on the `DbContext`.
- **Value objects as separate columns** → `OwnsOne`. Example: `Money` becomes `<prop>Amount` + `<prop>Currency` (two columns).
- **Value objects as single columns** → `ValueConverter`. Example: `Ticker.Value` ↔ `string`, `Currency.Code` ↔ `string`, `Conviction.Value` ↔ `int`.
- **Optional VO fields (`Money.None`, `Quantity.None`)** → mapped to nullable DB columns; the value converter translates `null` ↔ `Empty` instance. This keeps the no-null rule pure inside the domain and contains the translation to one mapping layer.
- **Collections** → `OwnsMany` or `HasMany` against a backing field: `builder.Navigation(p => p.Positions).HasField("_positions").UsePropertyAccessMode(PropertyAccessMode.Field)`.
- **No EF attributes in Domain.** All mapping is fluent-API in `TradyStrat.Infrastructure/Data/Configurations/*.cs`.

**Use case shape after rework:**

```csharp
public sealed class LogTradeUseCase(
    IPortfolioRepository repo, IClock clock, ILogger<LogTradeUseCase> log)
    : UseCaseBase<LogTradeInput, TradeRecorded>(log)
{
    protected override async Task<TradeRecorded> ExecuteCore(LogTradeInput input, CancellationToken ct)
    {
        var portfolio = await repo.GetAsync(ct);
        var result    = portfolio.RecordTrade(
            input.InstrumentId, input.Ticker, input.Currency,
            input.ExecutedOn, input.Side,
            input.Quantity, input.PricePerShare, input.FeesEur,
            input.Note, clock.UtcNow());
        await repo.SaveAsync(portfolio, ct);
        return result;
    }
}
```

No validation, no FIFO logic, no domain rules — pure orchestration. Validation lives in `Portfolio.RecordTrade` / `Position.Record` and throws `TradeValidationException` on failure.

## 6. Phase 1 — Shared kernel seed

**Scope:** introduce the kernel types Portfolio (Phase 2) needs: `Money`, `Currency`, `Ticker`, `Quantity`, `Price`, and the five strongly typed ids (`InstrumentId`, `TradeId`, `SuggestionId`, `GoalId`, `PositionId`).

**Deliverables:**

- New folder `TradyStrat.Domain/Shared/` with VO files.
- VO unit tests in `TradyStrat.Domain.Tests/Shared/` (one test class per VO covering construction, validation, equality, Empty/None semantics, arithmetic where applicable).
- Strongly typed id `ValueConverter`s in `TradyStrat.Infrastructure/Data/Conventions/StronglyTypedIdConventions.cs`, wired via `AppDbContext.ConfigureConventions`. No tables change — the converters are dormant until Phase 2 wires entities to use them.

**Acceptance:**

- Solution builds, all existing tests pass, no production code consumes the new types yet.
- New VO tests pass.

This phase has no behavior change visible to users; it's pure scaffolding.

## 7. Phase 2 — Portfolio aggregate

**Aggregate structure:**

```
Portfolio (AR, singleton, Id = 1)
├── PortfolioId Id
├── Money GoalEur                       ← copy of GoalConfig.TargetEur, refreshed on save
├── List<Position> _positions
│
└── Position (child entity)
    ├── PositionId Id
    ├── InstrumentId InstrumentId
    ├── Ticker Ticker                   ← snapshot for read paths
    ├── Currency InstrumentCurrency
    ├── List<Lot> _openLots             ← FIFO queue (owned-many)
    ├── Money _realizedPnLEur           ← cumulative; starts at Money.Zero(Eur)
    └── List<Trade> _trades             ← full history, ordered by ExecutedOn
        │
        └── Trade (child entity)
            ├── TradeId Id
            ├── DateOnly ExecutedOn
            ├── TradeSide Side
            ├── Quantity Quantity
            ├── Price PricePerShare
            ├── Money Fees
            ├── string Note             ← empty when absent
            └── DateTime CreatedAt
```

`Position` is a child entity, not a separate AR — it has no identity outside the Portfolio. Only `Portfolio` is loaded/saved through `IPortfolioRepository`. `Trade` is a child entity of `Position`.

**Behavior on `Portfolio`:**

- `RecordTrade(InstrumentId, Ticker, Currency, DateOnly executedOn, TradeSide side, Quantity qty, Price pricePerShare, Money fees, string note, DateTime now)` → returns `TradeRecorded(TradeId, PositionId, bool CreatedPosition, Money RealizedDelta)`. Finds or creates the `Position` for the instrument, calls `position.Record(...)`, throws `TradeValidationException` on oversell or mismatched currency.
- `DeleteTrade(TradeId)` → `TradeDeleted(PositionId, Money RealizedDelta)`. FIFO is path-dependent, so a delete replays remaining trades in the affected position from scratch. Throws if the trade doesn't exist.
- `ImportTrades(IReadOnlyList<TradeDraft>)` → applies a batch atomically; rolls back the entire aggregate state on first failure. Used by `ImportTradesCsvUseCase`.
- `Snapshot(IReadOnlyDictionary<InstrumentId, Price> priceByInstrument)` → `PortfolioSnapshot` read-model.
- `SnapshotAsOf(DateOnly asOf, IReadOnlyDictionary<InstrumentId, Price> priceByInstrument)` → time-travel projection that filters trade history before computing.
- `GrowthSeries(DateRange range, IFxConverter fxConverter)` → `IReadOnlyList<GrowthPoint>`. Absorbs today's `GrowthSeriesBuilder`.

**Behavior on `Position` (internal — only `Portfolio` calls it):**

- `Record(Trade)` — appends to `_trades`, runs FIFO consumption on sells, updates `_openLots` and `_realizedPnLEur`. Verbatim port of today's `PortfolioService.BuildSnapshot` inner loop (lines 41–69), rewritten against `Money` / `Price` / `Quantity`.
- `Quantity TotalQuantity` — `_openLots.Sum(l => l.Quantity)`.
- `Money CostBasisEur` — `_openLots.Sum(l => l.CostBasisEur)`.

**`PortfolioService` deletion:**

After Phase 2, `TradyStrat.Application/Portfolio/PortfolioService.cs` is **deleted** along with `GrowthSeriesBuilder.cs`. Both `SnapshotAsync` overloads become `Portfolio.Snapshot(...)` / `Portfolio.SnapshotAsOf(...)`. Use cases that took `PortfolioService` are updated to take `IPortfolioRepository`:

- `LoadDashboardUseCase`
- `BuildFocusDerivedSliceUseCase`
- `AiSnapshotService.PortfolioSection`
- Any other current call site (verified by compile-time consumers).

**EF mapping (`TradyStrat.Infrastructure/Data/Configurations/PortfolioConfiguration.cs`):**

- `Portfolio` → new `Portfolios` table, singleton row `Id = 1`. `GoalEur` mapped as `OwnsOne<Money>` (`GoalEurAmount`, `GoalEurCurrency`).
- `Position` → `Positions` table, FK to `Portfolios`. `Ticker`, `InstrumentCurrency` mapped via value converters. `_realizedPnLEur` as `OwnsOne<Money>`. `_openLots` as `OwnsMany<Lot>` against the `_openLots` backing field.
- `Trade` → `Trades` table, FK to `Positions`. `Quantity`, `PricePerShare`, `Fees` via value converters / owned types.
- **Schema migration:** existing `Trades.InstrumentId` stays (denormalized for AI snapshot section read path); new `Trades.PositionId` column added and backfilled. New `Portfolios` and `Positions` tables created. Backfill SQL (in the EF migration `Up`):

  ```sql
  INSERT INTO Portfolios (Id, GoalEurAmount, GoalEurCurrency)
    SELECT 1, COALESCE(TargetEur, 1000000), 'EUR' FROM Goals WHERE Id = 1;

  INSERT INTO Positions (PortfolioId, InstrumentId, Ticker, InstrumentCurrency, RealizedPnLEurAmount, RealizedPnLEurCurrency)
    SELECT 1, t.InstrumentId, i.Ticker, 'EUR', 0, 'EUR'
    FROM Trades t JOIN Instruments i ON i.Id = t.InstrumentId
    GROUP BY t.InstrumentId;

  UPDATE Trades SET PositionId = (
    SELECT p.Id FROM Positions p WHERE p.InstrumentId = Trades.InstrumentId
  );
  ```

  Lots and `RealizedPnLEur` are populated on first read by `Portfolio.RehydrateFromTrades()` — a one-time idempotent compute that runs the historical trade list through `Position.Record` to derive the open lots and cumulative realized P&L. This runs in `EfPortfolioRepository.GetAsync` if `_openLots` is empty but `_trades` is non-empty (a marker for "post-migration first load"). After the first save, lots are persisted normally.

**Repository (`TradyStrat.Infrastructure/Portfolio/EfPortfolioRepository.cs`):**

`GetAsync` eager-loads the full graph in one query:

```csharp
return await _ctx.Portfolios
    .Include(p => p.Positions).ThenInclude(pos => pos.Lots)
    .Include(p => p.Positions).ThenInclude(pos => pos.Trades)
    .SingleAsync(p => p.Id == PortfolioId.Singleton, ct);
```

`SaveAsync` is `_ctx.SaveChangesAsync(ct)`. The whole portfolio fits comfortably in memory — single user, single portfolio, ~hundreds of trades over the goal lifetime.

**Use cases updated in Phase 2:**

- `LogTradeUseCase` — conventional shape (load → `portfolio.RecordTrade(...)` → save).
- `DeleteTradeUseCase` — `portfolio.DeleteTrade(id)` + save.
- `ImportTradesCsvUseCase` — `portfolio.ImportTrades(drafts)` + save.
- `LoadDashboardUseCase` — `IPortfolioRepository` injected; `portfolio.Snapshot(priceMap)` replaces `PortfolioService.SnapshotAsync(...)`.
- `BuildFocusDerivedSliceUseCase` — same swap.
- `AiSnapshotService.PortfolioSection` — same swap.

**Test migration:**

- `PortfolioServiceTests` (in `Application.Tests`) move to `Domain.Tests/Portfolio/PortfolioTests` and rewrite against `Portfolio.RecordTrade` / `Portfolio.Snapshot`. **All FIFO and realized-P&L cases preserved as the regression contract.**
- New `Domain.Tests/Portfolio/PortfolioInvariantsTests` for invariants only catchable at the AR (oversell, mismatched-currency fees, deleting a nonexistent trade, batch-import rollback).
- New `Infrastructure.Tests/Portfolio/EfPortfolioRepositoryTests` covering the eager-load graph + save round-trip + first-load rehydration path.

**Behavioral guarantee:** every dashboard number — `CurrentValueEur`, `CostBasisEur`, `UnrealizedPnLEur`, `RealizedPnLEur`, `ProgressPct`, per-position `Quantity`/`AvgCost` — must match today byte-for-byte. The existing `PortfolioServiceTests` fixtures (and their snapshot expectations) are the contract.

## 8. Phase 3 — Suggestion aggregate

**Aggregate structure:**

```
Suggestion (AR — one per (InstrumentId, ForDate))
├── SuggestionId Id
├── InstrumentId InstrumentId
├── DateOnly ForDate
├── SuggestionAction Action
├── Quantity QuantityHint                ← Quantity.None when AI gave no hint
├── Price MaxPriceHint                   ← Price.None(Currency) when AI gave no hint
├── Conviction Conviction                ← VO, 1..10
├── string Rationale                     ← required, non-empty
├── IReadOnlyList<Citation> Citations    ← empty list, never null
├── MarketSnapshot Snapshot              ← MarketSnapshot.Empty when none captured
├── PromptFingerprint Fingerprint        ← VO bundling PromptHash + EnvelopeHash + PromptVersionHash
├── string ThinkingText                  ← empty string when absent
└── DateTime CreatedAt
```

**Key changes from today's `Suggestion`:**

- `CitationsJson : string` → `IReadOnlyList<Citation>` mapped as owned-many in a new `Citations` table. `CitationsJson` column stays during Phase 3 (dual-write via EF interceptor) so the MCP read path continues to work unchanged. Dropping the JSON column is a later cleanup, out of scope here (see §13).
- `MarketSnapshotJson : string?` → `MarketSnapshot.Empty`/non-empty owned VO. EF maps via `ValueConverter` to/from the existing JSON column (`null` ↔ `MarketSnapshot.Empty`).
- `PromptHash` + `EnvelopeHash?` + `PromptVersionHash?` → single `PromptFingerprint` owned VO mapped to the existing three columns. Absent components store empty string.
- `OrderValueEur : decimal?` (derived) → `Money OrderValue` (derived). `Money.None(currency)` when either hint is `IsEmpty`.
- `ThinkingText : string?` → `string ThinkingText` (empty when absent).
- `ICorrectnessRule` / `FixedThresholdCorrectness` move to `Domain/Suggestions/`; the AR exposes `suggestion.WasCorrect(IReadOnlyList<PriceBar> forwardBars, ICorrectnessRule rule)` → `Correctness(bool IsCorrect, Money ForwardReturn)`.

**Behavior on `Suggestion`:**

- `Suggestion.From(InstrumentId, DateOnly, AiResponse, PromptFingerprint, IClock)` — static factory. Enforces conviction range, action enum validity, rationale non-empty, prompt-hash non-empty.
- `Suggestion.WasCorrect(forwardBars, rule)` — returns `Correctness(...)`. Absorbs today's `ForwardReturnCalculator` (which is deleted from Application).

**`SuggestionGate` split:**

Today's `SuggestionGate` is one Application class that combines a decision (should we fetch?) with plumbing (per-`(date, instrumentId)` semaphore + cache). The rework splits these:

- **Decision** moves to `Domain/Suggestions/SuggestionGate.cs` as a pure domain service. Inputs: existing `Suggestion?` for `(instrumentId, today)`, `PromptFingerprint` of the candidate prompt, settings. Output: `GateDecision.Fetch` or `GateDecision.Reuse(Suggestion)`.
- **Plumbing** (semaphore, parallelism limit from `AnthropicSettings.MaxParallelSuggestions`) stays in Application as `SuggestionGatePlumbing` — the orchestration concern around the decision.

This makes the per-instrument concurrency contract explicit in code (plumbing in Application) while keeping the "what is a stale suggestion" rule in Domain.

**EF mapping:**

- `Suggestion` → `Suggestions` table. `Conviction` via value converter (column unchanged). `QuantityHint`, `MaxPriceHint`, `OrderValue` via owned types (columns may need to be added — see migration below).
- `Citation` collection → owned-many in a new `Citations` table (FK to `Suggestion`).
- `PromptFingerprint` → owned VO mapped to the existing `PromptHash`, `EnvelopeHash`, `PromptVersionHash` columns.
- `MarketSnapshot` → owned VO mapped via JSON value converter to the existing `MarketSnapshotJson` column.
- **Migration:** new `Citations` table, backfill from existing `CitationsJson`. EF write-side interceptor keeps `CitationsJson` populated on save for as long as the column exists (until MCP migrates). New columns for `OrderValueAmount` / `OrderValueCurrency` are derived at save time from the hint VOs.

**Repository:**

```csharp
public interface ISuggestionRepository
{
    Task<Suggestion?> GetForAsync(InstrumentId id, DateOnly date, CancellationToken ct);
    Task<IReadOnlyList<Suggestion>> ListForAsync(InstrumentId id, DateRange range, CancellationToken ct);
    Task<Suggestion?> LatestForAsync(InstrumentId id, CancellationToken ct);
    Task AddAsync(Suggestion suggestion, CancellationToken ct);
}
```

Existing Spec classes (`LatestSuggestionSpec`, `PriorSuggestionSpec`, `SuggestionsInRangeSpec`, `SuggestionForDateSpec`, `RecentSuggestionsForInstrumentSpec`, `SuggestionsQuerySpec`) move from `Application/AiSuggestion/Specifications/` to `Infrastructure/AiSuggestion/Specifications/` as implementation details of repository methods.

**Use cases updated in Phase 3:**

- `GetTodaysSuggestionUseCase` — uses `ISuggestionRepository` + domain `SuggestionGate` + Application `SuggestionGatePlumbing`. Decision logic shape unchanged from today; just relocated.
- `StreamTodaysSuggestionsUseCase` — same swap; per-instrument streaming preserved.
- `BackfillSuggestionsUseCase`, `ReplaySuggestionsUseCase`, `QuerySuggestionsUseCase`, `ForceRefetchSuggestionUseCase` — all repo-swap.
- `AiSnapshotService.RecentSuggestionsSection` — repo-swap; `PastSuggestionRow` consumes typed `Citation` list directly.
- `SuggestionBackfillCoordinator` — repo-swap; failure-policy unchanged.

**Tests:**

- Existing `FixedThresholdCorrectnessTests` move to `Domain.Tests/Suggestions/`.
- New `Domain.Tests/Suggestions/SuggestionFactoryTests` for `Suggestion.From` invariants.
- New `Domain.Tests/Suggestions/SuggestionGateTests` for the domain-service decisions (reuse vs fetch).
- New `Infrastructure.Tests/Suggestions/EfSuggestionRepositoryTests` for graph load + dual-write of `CitationsJson` during the transition.

## 9. Phase 4 — Instrument aggregate

**Aggregate structure:**

```
Instrument (AR — small reference-data root)
├── InstrumentId Id
├── Ticker Ticker
├── string Name
├── Currency Currency
├── Exchange Exchange                    ← new VO; today free-string column
├── TimezoneId Timezone                  ← new VO; today free-string column
├── InstrumentKind Kind
└── DateTime AddedAt
```

`InstrumentMetadata` (today a near-duplicate DTO used as a probe result) is **absorbed into `Instrument`** and deleted. `ProbeInstrumentUseCase` returns `Instrument` (probed, not yet persisted) directly. `AddInstrumentUseCase` calls `instrument.Confirm(clock)` then persists.

**New VOs added to kernel:** `Exchange`, `TimezoneId` (see §4).

**Behavior on `Instrument`:**

- `Instrument.Probed(Ticker, string name, Currency, Exchange, TimezoneId, InstrumentKind)` — static factory for "candidate returned from Yahoo probe but not persisted yet". Sets `Id = InstrumentId.New()` (sentinel `0`).
- `Instrument.Existing(InstrumentId, Ticker, string name, Currency, Exchange, TimezoneId, InstrumentKind, DateTime addedAt)` — rehydration factory used by EF mapping.
- `Confirm(IClock clock)` — promotes a probed instrument to persistable; sets `AddedAt = clock.UtcNow()`. Throws if already confirmed.
- `Rename(string newName)` — mutator with non-empty validation (no UI today, but makes the field's lifecycle explicit).

**Invariants:**

- `Ticker` is unique system-wide — enforced at the repository write path (existence check), not the aggregate, because uniqueness spans aggregates. `IInstrumentRepository.AddAsync` throws `DuplicateInstrumentException` when `Ticker` already exists.
- `Currency` must be ISO 4217 — enforced by `Currency` VO construction.
- `TimezoneId` must be IANA-valid — enforced by `TimezoneId` VO construction.

**Repository:**

```csharp
public interface IInstrumentRepository
{
    Task<Instrument?> GetAsync(InstrumentId id, CancellationToken ct);
    Task<Instrument?> FindByTickerAsync(Ticker ticker, CancellationToken ct);
    Task<IReadOnlyList<Instrument>> ListAsync(CancellationToken ct);
    Task AddAsync(Instrument instrument, CancellationToken ct);   // throws DuplicateInstrumentException
}
```

No `Remove` — instruments are append-only by design (reaffirmed by README out-of-scope).

**Use cases updated in Phase 4:**

- `ProbeInstrumentUseCase` — returns probed `Instrument` instead of `InstrumentMetadata`.
- `AddInstrumentUseCase` — receives probed `Instrument`, calls `Confirm(clock)`, delegates to repository.
- `ListInstrumentsUseCase` — repo-swap.
- `LoadDashboardUseCase`, `AiSnapshotService.TickersSection`, `AiSnapshotService.MarketsSection` — all swap to `IInstrumentRepository`.
- MCP `InstrumentTool` — same swap.

**Tests:**

- New `Domain.Tests/Instruments/InstrumentTests` — factory invariants, `Confirm` lifecycle, `Rename`.
- New `Domain.Tests/Shared/TimezoneIdTests`, `ExchangeTests` — VO validation.
- Existing `DuplicateInstrumentException` flow tests move to `Infrastructure.Tests/Instruments/EfInstrumentRepositoryTests`.

**No DB schema migration** — `Instruments` table columns are unchanged; only the projection into the domain type changes.

## 10. Phase 5 — Cleanup slice (Goal + MarketData + Indicators)

Three small absorptions in one phase. Each ~one paragraph; combined risk is low.

**Goal aggregate.** `GoalConfig` becomes `Goal` AR (singleton, `Id = 1`). `TargetEur : decimal` → `Money Target` (always EUR for now; `Money` widens later without schema churn). `TargetDate? : DateOnly?` → `DateOnly TargetDate` with `DateOnly.MinValue` as Empty + `goal.HasDeadline` predicate. New behavior: `goal.Retarget(Money newTarget, DateOnly newDeadline, IClock)` for the Settings page. `GrowthPoint(DateOnly Date, Money Value, Percentage ProgressPct)` becomes typed (today: raw decimal). `CapitalEvent(DateOnly Date, RomanNumeralId RomanId, string Headline, string Body)` adopts `RomanNumeralId`. New `IGoalRepository.GetAsync/SaveAsync` (singleton pattern, like Portfolio). The current `GoalConfig.Default(now)` factory moves to `Goal.Initial(IClock)`.

**MarketData (PriceBar, FxRate, Zone).** These stay close to today's shape — computed read-data, not aggregate roots. Adjustments: `PriceBar` adopts `Ticker` + `Money Open/High/Low/Close` (currency derived from the owning Instrument; the bar itself is single-currency). `FxRate` adopts `CurrencyPair` VO. `Zone` enum unchanged. Current `IRepositoryBase<PriceBar>` / `IRepositoryBase<FxRate>` ports rename to `IPriceBarReadRepository` / `IFxRateReadRepository` (read-only); writes happen via narrow `IPriceFeedWriter` / `IFxRateWriter` ports used only by the hosted services. Spec classes move to Infrastructure (consistent with Phase 2 and 3).

**Indicators.** Already small immutable records — minimal change. Adopt `Price` / `Percentage` VOs internally (`BollingerReading.UpperBand : Price`, `Percentage Rsi`). `IndicatorBundle` becomes non-nullable: each field gets its `.Empty` (`BollingerReading.Empty`, `IchimokuReading.Empty`, `Quantity.None` for `Rsi`). `IndicatorEngine` (Application) keeps its current shape — it's a domain service that crosses aggregate boundaries (reads `IPriceBarReadRepository`, computes via Atypical.TechnicalAnalysis), so it stays in Application.

**New VOs added to kernel in Phase 5:** `Percentage`, `CurrencyPair`, `RomanNumeralId`.

**No DB schema migration** — Goal, PriceBars, FxRates tables unchanged. Only projections change.

**Tests:** `Domain.Tests/Goals/GoalTests`, `Domain.Tests/Shared/CurrencyPairTests`, `Domain.Tests/Shared/PercentageTests`, `Domain.Tests/Shared/RomanNumeralIdTests`, `Domain.Tests/Indicators/IndicatorBundleTests`. Existing tests migrate.

## 11. Phase 6 — Settings refactor

Shipped alone because it touches every config-consuming module.

**Today:** `SettingEntry(Key, Value, UpdatedAt)` rows in a key-value `Settings` table; `ISettingsReader` returns `string` values keyed by string; each consumer parses its own value. The `UpdateSettingUseCase` is one generic use case taking `(key, value)`.

**Target:** typed settings aggregates that own their validation, parsed once at the read boundary.

- `AnthropicSettings(string Model, int MaxTokens, int MaxParallelSuggestions)` — invariants: model non-empty, tokens > 0, parallel ≥ 1.
- `YahooSettings(Uri BaseUrl)` — invariants: absolute URL, https.
- `TickerSettings(Ticker Focus, IReadOnlyList<Ticker> Context)` — invariants: focus non-empty, context distinct, no duplicate of focus.
- `FxSettings(CurrencyPair Pair)`.
- `DatabaseSettings(string Path)`.

`ISettingsReader` returns these typed aggregates; the raw key-value table never escapes Infrastructure. `SettingEntry` becomes a private EF row type.

**Per-aggregate update use cases** replace the generic one:

- `UpdateAnthropicSettingsUseCase`
- `UpdateYahooSettingsUseCase`
- `UpdateTickerSettingsUseCase`
- `UpdateFxSettingsUseCase`

Each takes the typed aggregate as input, validates via the constructor, persists the underlying key-value rows transactionally.

**UI updates:** the Settings page splits its single edit form into one form per aggregate, each posting to its dedicated use case. Form validation moves to the aggregate constructor (consistent with the rest of the domain).

**Migration:** zero schema change — the rows in the `Settings` table stay; only the read/write paths above them get typed wrappers. Boot-time settings load reads every required key, constructs the typed aggregates, and surfaces missing/invalid keys as `SettingValidationException` before the host accepts requests (fail-fast on misconfigured settings).

**Tests:** `Domain.Tests/Settings/AnthropicSettingsTests`, etc. — one test class per typed settings aggregate covering invariants. `Application.Tests/Settings/Update*UseCaseTests` for each typed update use case.

## 12. Domain events

**Decision: not added in this rework.**

Behavior methods on aggregates already return small "what happened" records (`TradeRecorded`, `TradeDeleted`, `SuggestionPersisted`, `PositionClosed`, etc.). Those records carry the same payload an event would, and use cases consume the return value directly for logging.

The hook is in place: any return record can become a published event later without changing the aggregate signature. The future path:

1. Add `IDomainEventBus` port in Application.
2. Use case publishes the existing return record after `SaveAsync`.
3. Handlers subscribe.

No event aggregation on aggregates, no `AggregateRoot` base class, no transactional outbox, no event sourcing — speculative for a single-user personal Blazor app. YAGNI applies. The first concrete event-driven need (e.g. "notify when a Position closes at ≥ 2× cost basis") triggers the minimal wiring above, not retrofit infrastructure.

## 13. Out of scope

Explicitly **not** included in this rework, to keep each phase shippable:

- **Strong-typed id DB column types.** DB columns stay `int`/`text`/etc. Conversion happens at the ORM boundary via `ValueConverter`. Column types unchanged across all six phases.
- **Dropping legacy denormalized columns.** `Trades.InstrumentId` stays alongside `Trades.PositionId`. `Suggestions.CitationsJson` stays alongside the new `Citations` table (dual-write via interceptor). Drop in a later cleanup migration once read paths migrate.
- **Mediator / CQRS bus.** Use cases stay direct-call (`UseCaseBase<TIn, TOut>`). No MediatR.
- **`Result<T>` / discriminated unions for failure.** Use cases keep throwing typed `*Exception` children of `TradyStratException`. Empty VOs cover absence; exceptions cover failure.
- **Multi-portfolio / multi-user.** `Portfolio` stays singleton. Reaffirms today's README out-of-scope note.
- **Backtesting expansion.** `ReplaySuggestionsUseCase` continues to work post-rework. No new replay capabilities added.
- **Razor view-model rewrites.** UI continues to consume use-case outputs in the same shape. Where empty VOs replace nullables, the use case projects to a presentation DTO that re-introduces nullables for Razor binding convenience. The no-null rule stops at the Application/UI boundary.
- **Spec / repository purism.** Ardalis Specifications stay (moved into Infrastructure). No move to compiled queries or hand-written `DbContext` queries.
- **`PortfolioSnapshot` legacy scalars cleanup.** The `Shares` / `AvgCostEur` legacy scalars on `PortfolioSnapshot` are not removed in Phase 2 because the dashboard view-model rewrite (referenced as "Task 14" in the snapshot comment) is its own track. The fields stay until that lands.

## 14. Risks and mitigations

| Risk | Mitigation |
|---|---|
| FIFO behavioral regression in Phase 2 | Today's `PortfolioServiceTests` move to `Domain.Tests/Portfolio` as the regression contract. Every fixture must pass against `Portfolio.Snapshot` byte-for-byte before Phase 2 merges. |
| EF owned-types + backing fields are subtle (EF can mis-track) | Each phase adds an `Infrastructure.Tests/<Aggregate>/Ef*RepositoryTests` covering full-graph load + save round-trip with at least one mutation per behavior method. |
| Phase 6 (Settings) touches many call sites | Phase ships alone (no other phase concurrent). Worktree allows the change to develop without blocking other work. Test coverage per typed settings aggregate. |
| Phase 2 migration backfill complexity | One-time idempotent `Portfolio.RehydrateFromTrades()` on first load; tested in `EfPortfolioRepositoryTests` with both empty-DB and populated-DB starting states. |
| Empty-VO ↔ null mapping drift between phases | The mapping is contained in EF `ValueConverter` definitions and JSON converters — both have unit tests covering round-trip. The no-null rule is enforced inside Domain by code review; no analyzer added (too much yak-shave for a personal app). |

## 15. Acceptance criteria (whole rework)

- Every phase merges via its own PR; main is shippable at each phase boundary.
- All existing tests pass at every phase boundary (after their migration to the new layer).
- No user-visible behavior change — dashboard numbers, AI prompts, MCP responses, CSV import results all identical.
- `TradyStrat.Application/Portfolio/PortfolioService.cs` is deleted by end of Phase 2.
- `Domain` project has zero references to `Microsoft.EntityFrameworkCore.*`, `Ardalis.Specification.EntityFrameworkCore`, `Anthropic.SDK`, or any HTTP/IO library.
- `Domain` types expose **no** nullable references on aggregate/entity/VO surfaces (verified by code review per phase; repository return types and adapter boundaries excepted).
- Every aggregate root is constructed only via static factory methods; no public constructors.
