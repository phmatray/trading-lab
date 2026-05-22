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
| Portfolio boundary | **One `Portfolio` aggregate root** owning `Position` (child entity per instrument), which owns `Lot` and `Trade` (child entities). FIFO accounting moves into `Position.Record(...)`. Acknowledged trade-off — see §2.1. |
| Cross-aggregate references | **By ID only.** `Position` holds `InstrumentId`, not `Ticker` or `Currency` snapshots. Read paths resolve names/currencies by passing an `IReadOnlyDictionary<InstrumentId, Instrument>` into `Portfolio.Snapshot(...)`. |
| Persistence | **EF maps directly to the rich domain** — value converters for VOs and strongly typed ids, `OwnsOne`/`OwnsMany` for owned VOs, backing fields for collections, private setters and private constructors. No separate persistence model. EF reflection access leaks shape into domain classes — itemized in §2.1. |
| Phasing | **Vertical slice per aggregate**, six phases, one worktree per phase, one PR per phase. |
| Null in domain | **No nullable references in domain types.** VOs with optional semantics expose `static Empty`/`None` + `IsEmpty`/`IsSpecified`. Empty list for collections. Empty string for optional strings. Nulls only at adapter/persistence boundaries via EF value-converters. |
| Repository pattern | **Per-aggregate repositories** (`IPortfolioRepository`, `ISuggestionRepository`, `IInstrumentRepository`, `IGoalRepository`) replace generic `IRepositoryBase<T>` for AR access. Ardalis Specifications stay, but only inside Infrastructure as a query-construction detail. |
| Domain events | **Not added yet.** Behavior methods return small "what happened" records (`TradeRecorded`, `SuggestionPersisted`); those become events when a real subscriber appears. |
| Use case shape | `UseCaseBase<TIn, TOut>` unchanged. Use cases become: load AR → call behavior → save → return the result record. No validation, no business logic in use cases. |
| Behavioral change | **None.** Every dashboard number, every FIFO calculation, every AI prompt output must remain identical. Existing tests are the regression contract. |

## 2.1 Pragmatic DDD trade-offs

Three deliberate compromises a DDD-strict reader will notice. Each is acknowledged here so it is treated as conscious design, not accidental.

**Large `Portfolio` aggregate.** Vernon's *Effective Aggregate Design* counsels small aggregates with by-ID references between them. The cleaner shape would be `Position`-per-instrument as its own AR, with `Portfolio` referencing positions by `PositionId` and cross-position consistency reached via domain events. We deliberately violate this rule because:

- Single-user, single-portfolio: no concurrent-modification pressure.
- Today's read paths all want a unified portfolio view (dashboard, AI snapshot section, MCP `get_portfolio`).
- O(hundreds) of trades across the goal lifetime; load cost is negligible.

Revisit triggers — *any* of these justifies splitting into per-instrument `Position` ARs: (a) trade count exceeds ~2k with measurable save-latency regression, (b) multi-portfolio or multi-user requirement appears, (c) a domain event need emerges that crosses position boundaries.

**Deferred domain events couple to the large-aggregate choice.** §12 defers events; this is not independent from the large-aggregate decision. Events are how you keep aggregates small while maintaining cross-aggregate consistency. By not introducing events now, we reinforce the single-AR shape — splitting `Portfolio` later requires events as a prerequisite. The "behavior methods return change records" pattern is a stand-in: it carries the same payload an event would, but only the calling use case can consume it.

**EF reflection access leaks into domain class shape.** Choosing "map EF directly to rich domain" (instead of a separate persistence model) means every AR and child entity exposes shape that exists only for the materializer:

- `private` parameterless constructor per AR and entity (so EF can construct via `Activator.CreateInstance`).
- `private set` on the `Id` property (so EF can assign after materialization).
- Field-backed `IReadOnlyList<T>` collections with the `_field` naming convention (so EF can populate the collection without a public setter).
- The repository, not the AR, owns post-migration rehydration logic (see §7) — keeps persistence concerns off the AR's public surface.

These are transparent, named compromises, not accidents. A reader maintaining the domain class shape should recognize them as load-bearing for EF and not "clean them up."

## 3. Target end state and phase order

When all six phases land:

- **`TradyStrat.Domain`** — rich aggregate roots (`Portfolio`, `Suggestion`, `Instrument`, `Goal`), value objects in `Domain/Shared/` (`Money`, `Currency`, `Ticker`, `Quantity`, `Price`, `DateRange`, `Percentage`, `Conviction`, `Exchange`, `TimezoneId`, `CurrencyPair`), strongly typed ids, factory methods, invariants, **domain services** for multi-aggregate logic: `SuggestionGate` (decision), `IndicatorEngine` (computes readings from price bars via the `IPriceBarReadRepository` port + `Atypical.TechnicalAnalysis`), `ZoneClassifier` (composes `IZoneRule` implementations). Domain services live in `Domain/<Aggregate>/Services/`. Aggregate-local VOs (`PromptFingerprint`, `RomanNumeralId`) stay in their aggregate folder.
- **`TradyStrat.Application`** — use cases shrink to "load → behave → save". `PortfolioService` is deleted. `SuggestionGate` decision logic moves to Domain (the per-instrument semaphore plumbing stays in Application as `SuggestionGatePlumbing`). `IndicatorEngine` / `ZoneClassifier` move out of Application into Domain (they were domain services miscategorized). Specifications move to Infrastructure.
- **`TradyStrat.Infrastructure`** — EF mapping fluent-API expands to handle owned types, value converters, backing fields. Generic `IRepositoryBase<T>` is replaced by per-aggregate repositories for ARs (read-only generic repos stay for `PriceBar`/`FxRate` reads — narrowed and renamed).

**Phase order:**

1. **Shared kernel seed** — only what Portfolio needs: `Money`, `Currency`, `Ticker`, `Quantity`, `Price`, `DateRange`, strongly typed ids. Compiles, dormant.
2. **Portfolio aggregate** — biggest payoff, hardest case, exercises every convention.
3. **Suggestion aggregate** — adds `Conviction`, typed `Citation` collection, `MarketSnapshot`, `PromptFingerprint`.
4. **Instrument aggregate** — adds `Exchange`, `TimezoneId`. Absorbs `InstrumentMetadata`.
5. **Cleanup slice** — Goal + MarketData (`PriceBar`, `FxRate`) + Indicators. Adds `Percentage`, `CurrencyPair`, `RomanNumeralId`. Small, low-risk.
6. **Settings refactor** — typed settings value objects (`AnthropicSettings`, `YahooSettings`, `TickerSettings`, `FxSettings`, `DatabaseSettings`) replace the raw key-value `SettingEntry` read path. Per-section update use cases. Touches every config-consuming module — shipped alone to isolate blast radius. (These are VOs, not aggregates — see §11.)

Each phase produces its own implementation plan; this spec covers the design for all six.

## 4. Shared kernel (final inventory)

These are the value objects and strongly typed ids the aggregates share. Phase 1 introduces the **bold** ones (what Portfolio needs); the rest land with their owning aggregate.

### 4.1 Type-shape convention

Pick one C# representation per VO category — uniform across the kernel:

| Category | C# shape | Rationale |
|---|---|---|
| Tiny identity-like wrappers (`Currency`, `Ticker`, all strongly typed ids) | `readonly record struct` | zero allocation, value-type equality, can't accidentally default-construct into an invalid state — `default(Ticker)` is structurally distinct from `Ticker.Empty` only if `Empty` carries an internal flag, which these don't need |
| VOs with derived semantics or computed properties (`Money`, `Price`, `Percentage`, `DateRange`) | `sealed record` (reference type) | structural equality, fewer surprises with `default(T)`, free `with`-expressions for derivations |
| VOs needing an internal `IsSpecified` flag (`Quantity`) | `sealed record` | flag-bearing fields require constructor invariants that struct `default` would bypass |
| VOs with parser/validator construction (`Exchange`, `TimezoneId`, `CurrencyPair`) | `sealed record` | constructor must throw on invalid input — record struct `default(T)` would smuggle invalid values |

### 4.2 Value objects (`TradyStrat.Domain/Shared/`)

- **`Money`** — `sealed record`. Constructor `Money(decimal Amount, Currency Currency)`. `Money.Zero(Currency)` is a real value; `Money.None(Currency)` is the absence sentinel with `IsEmpty = true`. Arithmetic — see §4.5.
- **`Currency`** — `readonly record struct Currency(string Code)`. ISO 4217. `Currency.Parse("USD")`, static accessors `Currency.Eur`, `Currency.Usd`, `Currency.Gbp`. Three-letter validation, uppercase normalization on construction.
- **`Ticker`** — `readonly record struct Ticker(string Value)`. Yahoo symbol. `Ticker.Of("CON3.L")`. Validation: non-empty, no whitespace. No `Empty` — `Ticker` is always required on the entities that hold one.
- **`Quantity`** — `sealed record`. Internal: `(decimal Value, bool IsSpecified)`. `Quantity.Of(5m)` → `IsSpecified = true`; `Quantity.None` → `IsSpecified = false, Value = 0m`. `Quantity.Zero` is `Of(0m)`. Arithmetic — see §4.5.
- **`Price`** — `sealed record Price(Money PerUnit)`. `Price × Quantity → Money`. Distinct type from `Money` so a price and a total cannot be swapped. `Price.None(Currency)` available; `Price.Zero(Currency)` is `new Price(Money.Zero(c))`.
- **`DateRange`** — `sealed record DateRange(DateOnly From, DateOnly To)`. Validates `From ≤ To`. Iterates dates inclusive. Used by `Suggestion.ListForAsync(range)` and `Portfolio.GrowthSeries(range, …)`. Phase 1.
- `Percentage` — `sealed record Percentage(decimal Value)`. Range `[-100..+decimal.MaxValue]`. `Percentage.Empty` for "no value yet" (e.g. progress percentage when there is no goal target set). Added in Phase 5.
- `Conviction` — `readonly record struct Conviction(int Value)`. Range `[1..10]`. **No `None`** — every `Suggestion.From(...)` requires a valid conviction (the AI tool-call schema makes it mandatory). Added in Phase 3.
- `Exchange` — `sealed record Exchange(string Name)`. Wraps Yahoo `fullExchangeName`. Validation: non-empty, trimmed. Added in Phase 4.
- `TimezoneId` — `sealed record TimezoneId(string Id)`. IANA identifier; validated via `TimeZoneInfo.TryFindSystemTimeZoneById`. Added in Phase 4.
- `CurrencyPair` — `sealed record CurrencyPair(Currency Base, Currency Quote)`. `CurrencyPair.Of("EURUSD")`. Throws on `Base == Quote`. Added in Phase 5.

**Aggregate-local VOs:** `PromptFingerprint` (in `Domain/Suggestions/`), `RomanNumeralId` (in `Domain/Goals/`, validates lowercase `i`/`ii`/`iii`/`iv`/`v`/…). These stay inside their aggregate folder, not in `Shared/`, because they're used by exactly one aggregate.

### 4.3 Strongly typed ids

`InstrumentId`, `TradeId`, `SuggestionId`, `GoalId`, `PositionId`. All `readonly record struct Id(int Value)` with a registered EF `ValueConverter`. `Id.New()` returns the "not yet persisted" sentinel (`new Id(0)`). `Id.Singleton` exists where the AR is a singleton: `PortfolioId.Singleton => new(1)`, `GoalId.Singleton => new(1)`.

### 4.4 Empty/None equality semantics

Defined explicitly so consumers don't guess:

| Comparison | Result | Notes |
|---|---|---|
| `Money.None(Eur) == Money.None(Eur)` | `true` | Records have structural equality; both have `IsEmpty = true`, same currency. |
| `Money.None(Eur) == Money.None(Usd)` | `false` | Currency mismatch. Different sentinel per currency. |
| `Money.None(Eur) == Money.Zero(Eur)` | `false` | `IsEmpty` differs. This is the whole point of having both. |
| `Quantity.None == Quantity.Zero` | `false` | `IsSpecified` differs. |
| `Quantity.None.Value` | `0m` | Readable but undefined-meaning; callers should check `IsSpecified` first. |
| `Price.None(Eur) == Price.None(Eur)` | `true` | Wraps `Money.None(Eur)`; same rules. |
| `default(Ticker)` | constructs `Ticker(null)` | Validation throws on use. The shape is `readonly record struct` so `default` is reachable, but anything that reads the value via factory/parser blocks invalid state — defensive code in entity constructors throws on `string.IsNullOrEmpty`. |

### 4.5 Arithmetic surface for `Money`, `Price`, `Quantity`

| Op | Allowed | Returns | Notes |
|---|---|---|---|
| `Money + Money` | same currency | `Money` | throws `CurrencyMismatchException` on mismatch; throws on either operand `IsEmpty` |
| `Money - Money` | same currency | `Money` | same rules; result may be negative |
| `Money * decimal` | yes | `Money` | scalar multiply; preserves currency |
| `Money / decimal` | denominator ≠ 0 | `Money` | throws on `0m` |
| `Money / Money` | same currency, denominator ≠ 0 | `decimal` | ratio for P&L%, progress% |
| `Money / Quantity` | denominator `IsSpecified` and > 0 | `Price` | average-price derivation (cost basis ÷ quantity) |
| `Price * Quantity` | both `IsSpecified` | `Money` | core invariant; either `IsEmpty` propagates to `Money.None(currency)` |
| `Price + Price` | not defined | — | throws or fails to compile — adding two prices has no semantic meaning |
| `Price - Price` | same currency | `Money` | price-delta is a `Money`, not a `Price` |
| `Quantity + Quantity` | both `IsSpecified` | `Quantity` | propagates `IsSpecified`; if either is `None`, result is `None` |
| `Quantity - Quantity` | both `IsSpecified` | `Quantity` | throws if result < 0 |

`Empty`/`None` propagation rule: any binary operator with an `IsEmpty`/`!IsSpecified` operand returns the `Empty` of the result type. Sums over collections (`positions.Sum(p => p.MarketValueEur)`) skip Empty values (treat them as zero in the same currency); the aggregate logs a warning when a position contributes `Money.None`.

### 4.6 Cross-cutting absence rules

- Every VO with optional semantics provides `static Empty` (or `static None`) and an `IsEmpty` predicate.
- VOs where "absent" must be distinguishable from "zero" carry an internal `IsSpecified` flag — *not* equality with `Zero`.
- Optional strings on entities become non-nullable, defaulted to empty string. Check `string.IsNullOrEmpty` at boundaries.
- Collections on entities use `IReadOnlyList<T>` backed by a field initialized to `[]`, never null.

### 4.7 Computed/read records (not full VOs)

`BollingerReading`, `IchimokuReading`, `IndicatorBundle`, `IndicatorReading`, `Citation`, `MarketCitation`, `GrowthPoint`, `CapitalEvent`, `PositionRow`, `PortfolioSnapshot`, `Correctness`, `TradeRecorded`, `TradeDeleted`, `GateDecision`. Structured data, not invariant-protected concepts. They adopt the new VOs internally. Concrete shapes:

- `Citation(string Source, string Url, DateOnly PublishedAt, string Excerpt)` — already record-shaped today; Phase 3 just renames `CitationsJson` away from the AR surface.
- `MarketCitation` — same shape as today; no change beyond folder relocation.
- `GrowthPoint(DateOnly Date, Money Value, Percentage ProgressPct)` — typed (Phase 5).
- `CapitalEvent(DateOnly Date, RomanNumeralId RomanId, string Headline, string Body)` — adopts `RomanNumeralId` (Phase 5).
- `PositionRow` — internal projection used inside `PortfolioSnapshot`; details in §7.

Other domain types not in this list keep their current shape (e.g. `BollingerReading`, `IchimokuReading` adopt `Price`/`Percentage` internally — listed under §10).

**Explicit non-additions:** `Email`, `Result<T>` / discriminated unions, generic `AggregateRoot` base class. None earn their keep here. (`DateRange` is now part of the Phase 1 kernel because both Portfolio and Suggestion need it.)

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
- **Behavior methods return the change they made** as a small record (e.g. `TradeRecorded(TradeId, PositionId, bool CreatedPosition, Money RealizedDelta)`). Useful for logging today; hookable as domain events later without rework. (Coupling to §12 noted in §2.1.)
- **Strategy via method parameter (double-dispatch).** Several AR methods take a service or rule as an argument — `Portfolio.GrowthSeries(DateRange, IFxConverter)`, `Suggestion.WasCorrect(IReadOnlyList<PriceBar>, ICorrectnessRule)`. This is Evans' double-dispatch pattern (DDD pp. 108–110) for letting AR behavior vary by strategy. We use it when the alternative is method explosion (`WasCorrectAgainstFixedThreshold`, `WasCorrectAgainstMovingAverage`, …) or external compute leakage. Each occurrence is justified inline in the phase section that introduces it.

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

Use cases are thin orchestrators. They may load *multiple* aggregates (when a behavior needs cross-aggregate data — e.g., the instrument's currency to construct a `Money` fee for a trade), but they never execute domain rules themselves:

```csharp
public sealed class LogTradeUseCase(
    IPortfolioRepository portfolios,
    IInstrumentRepository instruments,
    IClock clock,
    ILogger<LogTradeUseCase> log)
    : UseCaseBase<LogTradeInput, TradeRecorded>(log)
{
    protected override async Task<TradeRecorded> ExecuteCore(LogTradeInput input, CancellationToken ct)
    {
        // Load both aggregates needed for the behavior.
        var instrument = await instruments.GetAsync(input.InstrumentId, ct)
            ?? throw new InstrumentNotFoundException(input.InstrumentId);
        var portfolio  = await portfolios.GetAsync(ct);

        // Resolve VO-typed inputs at the boundary.
        var quantity  = Quantity.Of(input.QuantityValue);
        var price     = new Price(new Money(input.PriceValue, instrument.Currency));
        var fees      = new Money(input.FeesValue, Currency.Eur);

        // Single domain call. AR enforces invariants.
        var result = portfolio.RecordTrade(
            instrument.Id,
            input.ExecutedOn, input.Side,
            quantity, price, fees,
            input.Note ?? "",
            clock.UtcNow());

        await portfolios.SaveAsync(portfolio, ct);
        return result;
    }
}
```

Two notes on this shape:

1. **Cross-aggregate loads are normal.** Use cases that need data from a second aggregate to construct VOs (e.g., `Instrument.Currency` to construct a `Money`) call its repository first. This is correct DDD — orchestration belongs in Application, and `Portfolio` does not store `Instrument.Currency` (see §2 cross-aggregate references row + §7).
2. **The use case does no validation.** All `*Validation` exceptions originate from `Portfolio.RecordTrade` / `Position.Record` / the VO constructors. The use case's only "rule" is the existence check on `instruments.GetAsync` — which is a *boundary* concern (we don't have the instrument referenced by the input), not a domain rule.

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
Portfolio (AR, singleton, Id = PortfolioId.Singleton (= new(1)))
├── PortfolioId Id
├── List<Position> _positions          ← Portfolio holds no Goal/Instrument data; references by ID only
│
└── Position (child entity)
    ├── PositionId Id
    ├── InstrumentId InstrumentId      ← reference by ID; no ticker/currency snapshot
    ├── List<Lot> _openLots            ← FIFO queue (owned-many)
    ├── Money _realizedPnL             ← cumulative EUR (single-currency for now); starts at Money.Zero(Eur)
    └── List<Trade> _trades            ← full history, ordered by ExecutedOn
        │
        └── Trade (child entity)
            ├── TradeId Id
            ├── DateOnly ExecutedOn
            ├── TradeSide Side
            ├── Quantity Quantity
            ├── Price PricePerShare
            ├── Money Fees
            ├── string Note            ← empty when absent
            └── DateTime CreatedAt
```

`Position` is a child entity, not a separate AR — it has no identity outside the Portfolio (see §2.1 for the aggregate-size trade-off). Only `Portfolio` is loaded/saved through `IPortfolioRepository`. `Trade` is a child entity of `Position`.

**Position does NOT store `Ticker` or `Currency` snapshots.** Read paths resolve names and currencies by passing `IReadOnlyDictionary<InstrumentId, Instrument>` into `Portfolio.Snapshot(...)`. The use case loads both `IPortfolioRepository.GetAsync` and `IInstrumentRepository.ListAsync` and passes them through. This trades a tiny resolve-at-snapshot cost (O(10) positions × O(1) dict lookup) for clean cross-aggregate boundaries.

**Behavior on `Portfolio`:**

- `RecordTrade(InstrumentId, DateOnly executedOn, TradeSide side, Quantity qty, Price pricePerShare, Money fees, string note, DateTime now)` → returns `TradeRecorded(TradeId, PositionId, bool CreatedPosition, Money RealizedDelta)`. Finds or creates the `Position` for the instrument, calls `position.Record(...)`, throws `TradeValidationException` on oversell or mismatched currency (the `Money.Currency` of fees must match the canonical realized-P&L currency — EUR today; price-currency mismatches surface from VO arithmetic).
- `DeleteTrade(TradeId)` → `TradeDeleted(PositionId, Money RealizedDelta)`. FIFO is path-dependent, so a delete replays remaining trades in the affected position from scratch. Throws if the trade doesn't exist.
- `ImportTrades(IReadOnlyList<TradeDraft>)` → applies a batch atomically; rolls back the entire aggregate state on first failure. Used by `ImportTradesCsvUseCase`.
- `Snapshot(IReadOnlyDictionary<InstrumentId, Instrument> instrumentById, IReadOnlyDictionary<InstrumentId, Price> priceByInstrument, Money goalTarget)` → `PortfolioSnapshot` read-model. Caller resolves Instruments, Prices, and the current Goal value, then passes them in. Portfolio does *not* read other repositories.
- `SnapshotAsOf(DateOnly asOf, IReadOnlyDictionary<InstrumentId, Instrument> instrumentById, IReadOnlyDictionary<InstrumentId, Price> priceByInstrument, Money goalTarget)` → time-travel projection that filters trade history before computing.
- `GrowthSeries(DateRange range, IReadOnlyDictionary<InstrumentId, IReadOnlyDictionary<DateOnly, Price>> dailyPriceByInstrument, IReadOnlyDictionary<DateOnly, FxRate> dailyFxByDate, Money goalTarget)` → `IReadOnlyList<GrowthPoint>`. Absorbs today's `GrowthSeriesBuilder`. Caller pre-resolves daily price history + per-day FX rates for the requested range and hands them in — the AR consumes data, not services. This is purer than the Strategy-parameter pattern noted in §5; we use it here because growth-series compute is a single shape (not a strategy variation), so direct data passing wins.

**Behavior on `Position` (internal — only `Portfolio` calls it):**

- `Record(Trade)` — appends to `_trades`, runs FIFO consumption on sells, updates `_openLots` and `_realizedPnL`. Verbatim port of today's `PortfolioService.BuildSnapshot` inner loop (lines 41–69), rewritten against `Money` / `Price` / `Quantity`.
- `Quantity TotalQuantity` — `_openLots.Sum(l => l.Quantity)`.
- `Money CostBasisEur` — `_openLots.Sum(l => l.CostBasisEur)`.

**`PortfolioService` deletion:**

After Phase 2, `TradyStrat.Application/Portfolio/PortfolioService.cs` is **deleted** along with `GrowthSeriesBuilder.cs`. Both `SnapshotAsync` overloads become `Portfolio.Snapshot(...)` / `Portfolio.SnapshotAsOf(...)`. Use cases that took `PortfolioService` are updated to take `IPortfolioRepository`:

- `LoadDashboardUseCase`
- `BuildFocusDerivedSliceUseCase`
- `AiSnapshotService.PortfolioSection`
- Any other current call site (verified by compile-time consumers).

**EF mapping (`TradyStrat.Infrastructure/Data/Configurations/PortfolioConfiguration.cs`):**

- `Portfolio` → new `Portfolios` table, singleton row `Id = 1`. No `Money` columns on the table — `Portfolio` is just `(Id, RowVersion)`. Goal value is read from `IGoalRepository`; instrument values from `IInstrumentRepository`.
- `Position` → `Positions` table, FK to `Portfolios`. Columns: `Id`, `PortfolioId`, `InstrumentId`, `RealizedPnLAmount`, `RealizedPnLCurrency`. No `Ticker` or `InstrumentCurrency` columns. `_realizedPnL` as `OwnsOne<Money>`. `_openLots` as `OwnsMany<Lot>` against the `_openLots` backing field.
- `Trade` → `Trades` table, FK to `Positions`. `Quantity`, `PricePerShare`, `Fees` via value converters / owned types.
- **Schema migration:** existing `Trades.InstrumentId` stays for now (lets old read paths keep working during the cutover window — see §13); new `Trades.PositionId` column added and backfilled. New `Portfolios` and `Positions` tables created. Backfill SQL (in the EF migration `Up`):

  ```sql
  INSERT INTO Portfolios (Id) VALUES (1);

  INSERT INTO Positions (PortfolioId, InstrumentId, RealizedPnLAmount, RealizedPnLCurrency)
    SELECT 1, t.InstrumentId, 0, 'EUR'
    FROM Trades t
    GROUP BY t.InstrumentId;

  UPDATE Trades SET PositionId = (
    SELECT p.Id FROM Positions p WHERE p.InstrumentId = Trades.InstrumentId
  );
  ```

  **Lot rehydration lives in `EfPortfolioRepository`, not on the AR.** When `GetAsync` loads the graph and finds any position with non-empty `_trades` but empty `_openLots`, it invokes a private `RehydrateLots(Portfolio)` helper that replays each position's trades through `position.Record(...)` to derive lots and realized P&L, then immediately calls `SaveAsync` to persist the result. After this one-shot first read, lots are persisted normally and the rehydration branch is dormant. The AR knows nothing about migration state. (See §2.1 for why this lives in the repository.)

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

- `LogTradeUseCase` — loads `IInstrumentRepository.GetAsync(input.InstrumentId)` to resolve the instrument's currency for price construction, then loads `IPortfolioRepository.GetAsync`, calls `portfolio.RecordTrade(...)`, saves. Pattern shown in §5.
- `DeleteTradeUseCase` — `portfolio.DeleteTrade(id)` + save.
- `ImportTradesCsvUseCase` — loads instruments map upfront (so each draft can have its price built with the right currency), then `portfolio.ImportTrades(drafts)` + save.
- `LoadDashboardUseCase` — `IPortfolioRepository`, `IInstrumentRepository`, `IGoalRepository` injected. Loads all three; builds `instrumentById` and resolves goal target; calls `portfolio.Snapshot(instrumentById, priceMap, goal.Target)`.
- `BuildFocusDerivedSliceUseCase` — same shape.
- `AiSnapshotService.PortfolioSection` — same shape.

**Test migration:**

- `PortfolioServiceTests` (in `Application.Tests`) move to `Domain.Tests/Portfolio/PortfolioTests` and rewrite against `Portfolio.RecordTrade` / `Portfolio.Snapshot`. **All FIFO and realized-P&L cases preserved as the regression contract.**
- New `Domain.Tests/Portfolio/PortfolioInvariantsTests` for invariants only catchable at the AR (oversell, mismatched-currency fees, deleting a nonexistent trade, batch-import rollback).
- New `Infrastructure.Tests/Portfolio/EfPortfolioRepositoryTests` covering the eager-load graph + save round-trip + the first-load lot-rehydration path (in the repository, not the AR).

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
- `MarketSnapshotJson : string?` → `MarketSnapshot` owned VO. The VO shape is a typed deserialization of today's JSON: `MarketSnapshot(Money FocusPrice, Percentage Rsi, BollingerReading Bollinger, IchimokuReading Ichimoku, Money Sma50, Money Sma200, IReadOnlyList<MarketCitation> Markets, decimal UsdPerEur)`. Empty form (`MarketSnapshot.Empty`) is all sub-VOs in their Empty state. EF maps via `ValueConverter` round-tripping JSON to/from the existing `MarketSnapshotJson` column (`null` ↔ `MarketSnapshot.Empty`).
- `PromptHash` + `EnvelopeHash?` + `PromptVersionHash?` → single `PromptFingerprint(string PromptHash, string EnvelopeHash, string PromptVersionHash)` owned VO mapped to the existing three columns. Absent components store empty string. Phase 3 has no use case for partial fingerprints; if a future use case needs `IsComplete`, add it then.
- `OrderValueEur : decimal?` (derived) → `Money OrderValue` (derived). `Money.None(MaxPriceHint.Currency)` when either hint is `IsEmpty`.
- `ThinkingText : string?` → `string ThinkingText` (empty when absent).
- `ICorrectnessRule` / `FixedThresholdCorrectness` move to `Domain/Suggestions/`; the AR exposes `suggestion.WasCorrect(IReadOnlyList<PriceBar> forwardBars, ICorrectnessRule rule)` → `Correctness(bool IsCorrect, Money ForwardReturn)`. **Strategy-via-parameter justification:** correctness rules form an open set (fixed-threshold today; future variants like moving-average crossover, conviction-weighted, etc., for replay/backtest experiments). One method per rule would explode the AR surface and require a recompile per new rule. Per §5's double-dispatch convention, the rule is injected as a parameter.
- `Conviction` is non-nullable; the AI tool-call schema makes it mandatory. **No `Conviction.None` on `Suggestion`** — the factory `Suggestion.From(...)` throws if the AI's response is missing a conviction.

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

**Goal aggregate.** `GoalConfig` becomes `Goal` AR (singleton, `Id = GoalId.Singleton`). `TargetEur : decimal` → `Money Target` (always EUR for now; `Money` widens later without schema churn). `TargetDate? : DateOnly?` → `DateOnly TargetDate` with `DateOnly.MinValue` as Empty + `goal.HasDeadline` predicate. Behavior split into two methods so partial updates are explicit:
- `goal.RetargetAmount(Money newTarget, IClock)` — updates target only.
- `goal.RescheduleDeadline(DateOnly newDeadline, IClock)` — updates deadline only. Pass `DateOnly.MinValue` to clear it.

Both methods throw `SettingValidationException` on invariant breach (target ≤ 0, deadline in the past). `GrowthPoint(DateOnly Date, Money Value, Percentage ProgressPct)` becomes typed (today: raw decimal). `CapitalEvent(DateOnly Date, RomanNumeralId RomanId, string Headline, string Body)` adopts `RomanNumeralId`. New `IGoalRepository.GetAsync/SaveAsync` (singleton pattern, like Portfolio). The current `GoalConfig.Default(now)` factory moves to `Goal.Initial(IClock)`.

**MarketData (PriceBar, FxRate, Zone).** These stay close to today's shape — computed read-data, not aggregate roots. Adjustments: `PriceBar` adopts `Ticker` + `Money Open/High/Low/Close` (currency derived from the owning Instrument; the bar itself is single-currency). `FxRate` adopts `CurrencyPair` VO. `Zone` enum unchanged. Current `IRepositoryBase<PriceBar>` / `IRepositoryBase<FxRate>` ports rename to `IPriceBarReadRepository` / `IFxRateReadRepository` (read-only); writes happen via narrow `IPriceFeedWriter` / `IFxRateWriter` ports used only by the hosted services. Spec classes move to Infrastructure (consistent with Phase 2 and 3).

**Indicators.** Already small immutable records — minimal change. Adopt `Price` / `Percentage` VOs internally (`BollingerReading.UpperBand : Price`, `Percentage Rsi`). `IndicatorBundle` becomes non-nullable: each field gets its `.Empty` (`BollingerReading.Empty`, `IchimokuReading.Empty`, `Percentage.Empty` for `Rsi`). **`IndicatorEngine` and `ZoneClassifier` move from Application to Domain** (`Domain/Indicators/Services/`) — they were domain services miscategorized. They consume `IPriceBarReadRepository` (a port; domain services are allowed to take ports) and the pure-compute `Atypical.TechnicalAnalysis` library (a domain-allowed dependency — pure compute, no I/O). Use cases that take `IndicatorEngine` keep their constructor unchanged; only the type's namespace moves.

**New VOs added to kernel in Phase 5:** `Percentage`, `CurrencyPair`, `RomanNumeralId`.

**No DB schema migration** — Goal, PriceBars, FxRates tables unchanged. Only projections change.

**Tests:** `Domain.Tests/Goals/GoalTests`, `Domain.Tests/Shared/CurrencyPairTests`, `Domain.Tests/Shared/PercentageTests`, `Domain.Tests/Shared/RomanNumeralIdTests`, `Domain.Tests/Indicators/IndicatorBundleTests`. Existing tests migrate.

## 11. Phase 6 — Settings refactor

Shipped alone because it touches every config-consuming module.

**Terminology note.** These typed settings types are **value objects, not aggregates** — they have no identity, no lifecycle behavior beyond construction validation, no `*Id`, no repository (they're read in bulk through `ISettingsReader`). Calling them "settings aggregates" would muddle the DDD vocabulary. Throughout this section: *typed settings VO*, *settings section*, *typed config section*. Never "settings aggregate."

**Today:** `SettingEntry(Key, Value, UpdatedAt)` rows in a key-value `Settings` table; `ISettingsReader` returns `string` values keyed by string; each consumer parses its own value. The `UpdateSettingUseCase` is one generic use case taking `(key, value)`.

**Target:** typed settings VOs that own their validation, parsed once at the read boundary.

- `AnthropicSettings(string Model, int MaxTokens, int MaxParallelSuggestions)` — invariants: model non-empty, tokens > 0, parallel ≥ 1.
- `YahooSettings(Uri BaseUrl)` — invariants: absolute URL, https.
- `TickerSettings(Ticker Focus, IReadOnlyList<Ticker> Context)` — invariants: focus non-empty, context distinct, no duplicate of focus.
- `FxSettings(CurrencyPair Pair)`.
- `DatabaseSettings(string Path)`.

`ISettingsReader` returns these typed VOs; the raw key-value table never escapes Infrastructure. `SettingEntry` becomes a private EF row type.

**Per-section update use cases** replace the generic one:

- `UpdateAnthropicSettingsUseCase`
- `UpdateYahooSettingsUseCase`
- `UpdateTickerSettingsUseCase`
- `UpdateFxSettingsUseCase`

Each takes the typed VO as input, validates via the constructor, persists the underlying key-value rows transactionally.

**UI updates:** the Settings page splits its single edit form into one form per section, each posting to its dedicated use case. Form validation moves to the VO constructor (consistent with the rest of the domain).

**Migration:** zero schema change — the rows in the `Settings` table stay; only the read/write paths above them get typed wrappers. Boot-time settings load reads every required key, constructs the typed VOs, and surfaces missing/invalid keys as `SettingValidationException` before the host accepts requests (fail-fast on misconfigured settings).

**Tests:** `Domain.Tests/Settings/AnthropicSettingsTests`, etc. — one test class per typed settings VO covering invariants. `Application.Tests/Settings/Update*UseCaseTests` for each update use case.

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
- **Mediator / CQRS bus.** Use cases stay direct-call (`UseCaseBase<TIn, TOut>`). No MediatR.
- **`Result<T>` / discriminated unions for failure.** Use cases keep throwing typed `*Exception` children of `TradyStratException`. Empty VOs cover absence; exceptions cover failure.
- **Multi-portfolio / multi-user.** `Portfolio` stays singleton. Reaffirms today's README out-of-scope note.
- **Backtesting expansion.** `ReplaySuggestionsUseCase` continues to work post-rework. No new replay capabilities added.
- **Razor view-model rewrites.** UI continues to consume use-case outputs in the same shape. Where empty VOs replace nullables, the use case projects to a presentation DTO that re-introduces nullables for Razor binding convenience. The no-null rule stops at the Application/UI boundary.
- **Spec / repository purism.** Ardalis Specifications stay (moved into Infrastructure). No move to compiled queries or hand-written `DbContext` queries.
- **Splitting `Portfolio` into per-instrument `Position` aggregates.** See §2.1 trade-off table. Revisit triggers documented; not part of this spec.

### 13.1 Known temporary DDD violations

These compromises ship with the rework but should be removed when the named follow-up work lands. Listed separately so they aren't mistaken for permanent design.

- **`PortfolioSnapshot.Shares` / `PortfolioSnapshot.AvgCostEur` legacy scalars.** The domain read model carries two UI-shaped fields populated only when `positions.Count == 1`. They exist because the current dashboard view-model (HeroCapital, PortfolioRail, GrowthChart) reads them. **Removal trigger:** the dashboard view-model rewrite ("Task 14" referenced in the snapshot comment today). When that lands, both fields are deleted from `PortfolioSnapshot`. Domain read models should not know about UI shape.
- **`Trades.InstrumentId` column kept alongside `Trades.PositionId`.** Denormalized to preserve any read path that hasn't been swapped to use `Position`. **Removal trigger:** verify no production read path references `Trades.InstrumentId` directly; then drop in a follow-up migration.
- **`Suggestions.CitationsJson` column kept alongside new `Citations` table.** Dual-written by an EF interceptor for the MCP read path. **Removal trigger:** MCP `query_suggestions` migrated to read the typed `Citations` collection; then drop in a follow-up migration.
- **No analyzer enforcing the no-null rule in Domain.** Enforced by code review per phase. **Removal trigger:** if drift becomes recurrent, add a custom Roslyn analyzer that flags `T?` declarations under `TradyStrat.Domain/` namespaces.

## 14. Risks and mitigations

| Risk | Mitigation |
|---|---|
| FIFO behavioral regression in Phase 2 | Today's `PortfolioServiceTests` move to `Domain.Tests/Portfolio` as the regression contract. Every fixture must pass against `Portfolio.Snapshot` byte-for-byte before Phase 2 merges. |
| EF owned-types + backing fields are subtle (EF can mis-track) | Each phase adds an `Infrastructure.Tests/<Aggregate>/Ef*RepositoryTests` covering full-graph load + save round-trip with at least one mutation per behavior method. |
| Phase 6 (Settings) touches many call sites | Phase ships alone (no other phase concurrent). Worktree allows the change to develop without blocking other work. Test coverage per typed settings VO. |
| Phase 2 migration backfill complexity | `EfPortfolioRepository.GetAsync` runs one-time idempotent lot rehydration when `_openLots` is empty but `_trades` is non-empty; tested with both empty-DB and populated-DB starting states. |
| Empty-VO ↔ null mapping drift between phases | The mapping is contained in EF `ValueConverter` definitions and JSON converters — both have unit tests covering round-trip. The no-null rule is enforced inside Domain by code review; no analyzer added (deferred per §13.1). |
| **`Portfolio` aggregate size grows over trade-history lifetime** | Today: O(hundreds) trades over the goal lifetime — full-graph load < 10 ms. Acceptable. Tracking: monitor `EfPortfolioRepository.GetAsync` latency in logs; trigger §2.1 revisit if it exceeds 100 ms or trade count exceeds 2k. The split into per-instrument `Position` ARs (with events) is the documented escape valve. |
| **Cross-aggregate snapshot resolution becomes a hot path** | `LoadDashboardUseCase` now loads `IPortfolioRepository` + `IInstrumentRepository` + `IGoalRepository` per request. All three are small singletons or small reference data; eager-loading is fast. Monitor: if dashboard load p99 regresses, add a per-request cache in Application (still no cross-aggregate denormalization in Domain). |

## 15. Acceptance criteria (whole rework)

- Every phase merges via its own PR; main is shippable at each phase boundary.
- All existing tests pass at every phase boundary (after their migration to the new layer).
- No user-visible behavior change — dashboard numbers, AI prompts, MCP responses, CSV import results all identical. `Portfolio.Snapshot` output matches `PortfolioService.SnapshotAsync` byte-for-byte against the regression-fixture set.
- `TradyStrat.Application/Portfolio/PortfolioService.cs` and `GrowthSeriesBuilder.cs` are deleted by end of Phase 2.
- `Domain` project has zero references to `Microsoft.EntityFrameworkCore.*`, `Ardalis.Specification.EntityFrameworkCore`, `Anthropic.SDK`, or any HTTP/IO library.
- `Domain` types expose **no** nullable references on aggregate/entity/VO surfaces (verified by code review per phase; repository return types and adapter boundaries excepted).
- Every aggregate root is constructed only via static factory methods; no public constructors.
- **Empty/None round-trip:** every VO that has an `Empty`/`None` form has an `Infrastructure.Tests` round-trip test asserting `Empty → null column → Empty` (no information loss across the boundary):
  - `Money.None(Eur)` round-trip via owned-type mapping.
  - `Quantity.None` round-trip via single-column converter.
  - `Price.None(Eur)` round-trip.
  - `MarketSnapshot.Empty` round-trip via JSON converter (Phase 3).
  - Empty `IReadOnlyList<Citation>` round-trip via owned-many (Phase 3).
- **Domain-service placement:** `IndicatorEngine`, `ZoneClassifier`, `SuggestionGate` (decision portion) all live under `TradyStrat.Domain/*/Services/` by end of Phase 5.
- **Cross-aggregate reference rule:** `grep -r "Ticker " TradyStrat.Domain/Portfolio/` returns no hits (Position holds `InstrumentId` only). Same check for Currency in Portfolio, Goal value in Portfolio.
