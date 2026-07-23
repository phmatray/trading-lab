# Phase 7 — Domain DDD Polish (Seedwork + Domain Events)

**Date:** 2026-05-25
**Status:** Design v2 (post-review; awaiting plan)
**Predecessors:** Phases 1–6 of the DDD rework (suggestion AR, instrument AR, goal/market-data/indicators cleanup, settings VOs). This phase completes the arc by polishing primitives the prior phases left implicit.
**Tag on landing:** `phase7-domain-ddd-polish-done`
**Revision notes:** v2 incorporates a max-effort DDD review. Material changes from v1: (a) repository owns event drain via `SaveAsync` return value; (b) all factory-creation methods raise `*Created` events uniformly; (c) `IStronglyTypedId`, `DomainException` intermediate, `TradeRecordedResult`/`TradeDeletedResult`, and entity-level `CheckRule` are all dropped; (d) `Entity<TId>` gains a transient-identity short-circuit; (e) `IDomainEvent` gains `Guid EventId`; (f) honest motivation and EF round-trip test added for `Money`/`Price`/`Quantity` migration.

---

## 1. Motivation

The Domain project (`TradyStrat.Domain`) currently expresses DDD building blocks informally:

- Each aggregate root (AR) re-implements its own private EF ctor, factory methods, and ID handling.
- "Domain events" exist as records (`TradeRecorded`, `TradeDeleted`) but are **returned from aggregate methods**, not collected on the AR and dispatched. They are events in name only — they have no consumer beyond the immediate caller.
- Strongly-typed IDs share a shape (`readonly record struct X(int Value)` with `.New() => new(0)`) but no common interface.
- Several aggregates raise no events at all (`Suggestion`, `Instrument`, `Goal`), so cache invalidation and downstream effects must be wired manually at every call site.
- A few value objects with sentinel flags (`Money.IsEmpty`, `Quantity.IsSpecified`) carry hand-rolled equality.

Phase 7 introduces a small **seedwork kit** (Entity, AggregateRoot, ValueObject, IDomainEvent, IBusinessRule, IDomainEventDispatcher), migrates the four ARs to inherit from it, and converts the existing "returned event" style into proper **collected-and-dispatched** domain events whose drain is owned by the repository. Validation and ID encoding remain as they are — the kit is *available*; we don't retrofit working code with it for the sake of consistency.

---

## 2. Scope

### In scope

- New `TradyStrat.Domain/SeedWork/` folder with base types and dispatcher contracts.
- Migration of `Portfolio`, `Suggestion`, `Goal`, `Instrument` to inherit `AggregateRoot<TId>`.
- Migration of `Position`, `Trade` to inherit `Entity<TId>`.
- New `Events/` subfolder under each AR's folder, holding domain event records — including `*Created` events for every new-creation factory (uniform rule, see §4).
- `Money`, `Price`, `Quantity` migrate to inherit `ValueObject` — motivation is structural closure (see §5); equality and EF round-trip behaviour verified by tests.
- New `BusinessRuleViolationException` inheriting `TradyStratException` directly (no `DomainException` intermediate).
- Repository contract change: `SaveAsync` returns the drained `IReadOnlyList<IDomainEvent>` for every event-bearing repository (`IPortfolioRepository`, `IGoalRepository`, `IInstrumentRepository`, `ISuggestionRepository`). Persistence + drain happen as one act.
- Infrastructure-side `DomainEventDispatcher` + DI registration.
- Application use cases (`LogTradeUseCase`, `DeleteTradeUseCase`, plus any goal/instrument/suggestion command use cases that hit `SaveAsync`) updated to consume the returned event list and dispatch.
- Tests: new SeedWork unit fixtures, existing AR tests adapted to the new event-collection shape, EF round-trip integration test for migrated VOs, one new integration test proving dispatch flows end-to-end.

### Out of scope

- Retrofitting `IBusinessRule` into existing factory validations. The kit ships, rules land when a real use case introduces one.
- Strongly-typed-ID marker interface (`IStronglyTypedId<T>`) — zero consumer in Phase 7; ID structs stay as they are.
- Converting ID structs into records (would require EF value-converter changes; structs work fine and are cheaper).
- New domain events beyond those listed in §4 (no speculative coverage).
- Any change to `Indicators/`, `MarketData/`, `PriceFeed/`, `Settings/` aggregates/VOs.
- Migrating to MediatR or EF SaveChanges interceptor for dispatch — explicitly chosen against.
- Same-transaction handler dispatch / outbox table. Phase 7 dispatches *after* `SaveAsync` commits; handler failure post-commit is unrecoverable in this phase. When the first handler crosses an I/O boundary, introduce an outbox.

---

## 3. SeedWork primitives

Lives in `TradyStrat.Domain/SeedWork/`. Each file ~30 lines, zero external dependencies.

### 3.1 `IDomainEvent.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IDomainEvent
{
    Guid     EventId    { get; }
    DateTime OccurredAt { get; }   // UTC by convention; raised via clock.UtcNow()
}
```

`EventId` enables future idempotency at the handler boundary and outbox keying without a schema change to existing events. `OccurredAt` is UTC — every `Raise(...)` call site must source it from `IClock.UtcNow()` (no `DateTime.UtcNow` shortcuts).

### 3.2 `DomainEvent.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public abstract record DomainEvent : IDomainEvent
{
    public Guid     EventId    { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; }
}
```

Concrete events declare positional ctor parameters for `OccurredAt` plus their payload; `EventId` defaults to a fresh GUID per instance. Concrete shape:

```csharp
public sealed record TradeRecorded(
    TradeId    TradeId,
    PositionId PositionId,
    Money      RealizedDelta,
    DateTime   OccurredAt) : DomainEvent { public DateTime OccurredAt { get; init; } = OccurredAt; }
```
*(The init-property re-declaration is the C# idiom for positional records inheriting an abstract record with init properties — verified to compile in C# 12.)*

### 3.3 `Entity<TId>.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public abstract class Entity<TId> where TId : struct
{
    public TId Id { get; protected set; }

    protected Entity() { }                       // EF
    protected Entity(TId id) => Id = id;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (GetType() != other.GetType()) return false;
        // Transient entities (Id == default) only equal themselves by reference.
        if (EqualityComparer<TId>.Default.Equals(Id, default)
            || EqualityComparer<TId>.Default.Equals(other.Id, default))
            return ReferenceEquals(this, other);
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
        => EqualityComparer<TId>.Default.Equals(Id, default)
            ? RuntimeHelpers.GetHashCode(this)
            : HashCode.Combine(GetType(), Id);
}
```

Entities have identity equality (same type + same persisted Id), not value equality. The transient-identity short-circuit prevents two unsaved aggregates (each with the zero-sentinel `Id`) from being mistakenly considered equal — a canonical DDD requirement (Evans, Vernon). `CheckRule` lives on `AggregateRoot<TId>` only (see §3.4); invariants are an AR-boundary concern.

### 3.4 `AggregateRoot.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : struct
{
    private readonly List<IDomainEvent> _events = new();

    protected AggregateRoot() { }                // EF
    protected AggregateRoot(TId id) : base(id) { }

    public IReadOnlyList<IDomainEvent> DomainEvents => _events;

    protected void Raise(IDomainEvent evt) => _events.Add(evt);

    protected static void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
            throw new Exceptions.BusinessRuleViolationException(rule);
    }

    public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
    {
        var snapshot = _events.ToArray();
        _events.Clear();
        return snapshot;
    }

    internal void ClearDomainEvents() => _events.Clear();   // for rollback in batch operations
}
```

`DequeueDomainEvents` is a one-shot drain called by the repository's `SaveAsync` (see §7). `ClearDomainEvents` is internal — only `Portfolio.ImportTrades` uses it on rollback (§4.1). Direct call by use cases is forbidden by convention.

### 3.5 `ValueObject.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
        => other is not null
           && GetType() == other.GetType()
           && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override bool Equals(object? obj) => obj is ValueObject vo && Equals(vo);

    public override int GetHashCode()
    {
        var hc = new HashCode();
        foreach (var c in GetEqualityComponents()) hc.Add(c);
        return hc.ToHashCode();
    }

    public static bool operator ==(ValueObject? a, ValueObject? b) => Equals(a, b);
    public static bool operator !=(ValueObject? a, ValueObject? b) => !Equals(a, b);
}
```

Used by `Money`, `Price`, `Quantity` (see §5). All other VOs remain `record`s. `GetHashCode` uses the canonical `HashCode` builder — no order-fragile manual aggregation.

### 3.6 `IBusinessRule.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IBusinessRule
{
    bool IsBroken();
    string Message { get; }
}
```

### 3.7 `IDomainEventDispatcher.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct);
}
```

### 3.8 `IDomainEventHandler.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent evt, CancellationToken ct);
}
```

---

## 4. Aggregate migrations

**Uniform rule for creation events:** every new-creation factory raises a `*Created` event. Rehydration factories (`Existing`, EF private ctor) never raise. Pure query methods never raise. This rule is mechanical and removes the 1-of-4 inconsistency the review caught.

### 4.1 `Portfolio : AggregateRoot<PortfolioId>`

- Drop hand-rolled `Id` property (now inherited).
- `Portfolio.Empty(PortfolioId id)` raises `PortfolioCreated(PortfolioId, OccurredAt: clock.UtcNow())`. Signature gains an `IClock` parameter (or callers pass `DateTime now`); existing call sites are seed/bootstrap and trivial to update.
- `RecordTrade(...)` returns the `TradeRecorded` event itself (`Task<TradeRecorded>` at the use-case layer). Internally it raises `TradeRecorded(TradeId, PositionId, Money RealizedDelta, OccurredAt)` and, when a new position is opened, additionally `PositionOpened(PositionId, InstrumentId, OccurredAt)`. The previous `bool CreatedPosition` result field is replaced by the presence of `PositionOpened` in `DomainEvents` — callers that need it synchronously inspect the AR's `DomainEvents` *before* the repository drains.
- `DeleteTrade(...)` returns the raised `TradeDeleted(PositionId, Money RealizedDelta, OccurredAt)` event.
- `ImportTrades` raises one `TradeRecorded` per imported trade. On exception the existing `_positions` restore runs **and** `ClearDomainEvents()` runs **before re-throwing**, so a partial batch leaves no orphan events. (`ClearDomainEvents` is `internal` so only this method can call it.)
- `Snapshot` / `SnapshotAsOf` / `GrowthSeries` / `RehydrateLots` unchanged. `RehydrateLots` is rehydration → must not raise.
- **Result-type cleanup:** the previous `TradeRecorded` and `TradeDeleted` records (in `Portfolio/`) move to `Portfolio/Events/` and inherit `DomainEvent`. There is no `*Result` type — use-case return signatures stay `Task<TradeRecorded>` / `Task<TradeDeleted>` because the event records carry exactly what callers need (TradeId, PositionId, RealizedDelta).

### 4.2 `Suggestion : AggregateRoot<SuggestionId>`

- Drop hand-rolled `Id`.
- `Suggestion.From(...)` factory raises `SuggestionCreated(SuggestionId, InstrumentId, ForDate, Action, OccurredAt: createdAt)` *after* validation succeeds. The DB-assigned-Id timing is preserved: the event holds whatever `Id` the AR has when raised; if the use case needs the post-save Id, it reads it back from the AR after `SaveAsync` (Phase 7 doesn't change Id-assignment timing).
- `WasCorrect` stays a pure query method — does not raise.
- Mutations to `_citations` are not exposed today; if a future `AddCitation`/`RemoveCitation` method lands, it must raise a corresponding event per the convention in §11.

### 4.3 `Goal : AggregateRoot<GoalId>`

- Drop hand-rolled `Id`.
- `Goal.Initial(IClock)` raises `GoalCreated(GoalId, Money Target, OccurredAt)`.
- `RetargetAmount(...)` captures `OldTarget` **before** mutation, then raises `GoalTargetChanged(GoalId, Money OldTarget, Money NewTarget, OccurredAt)`.
- `RescheduleDeadline(...)` captures `OldDeadline` before mutation, then raises `GoalDeadlineRescheduled(GoalId, DateOnly OldDeadline, DateOnly NewDeadline, OccurredAt)`.
- `Goal.Existing(...)` is rehydration → does not raise.

### 4.4 `Instrument : AggregateRoot<InstrumentId>`

- Drop hand-rolled `Id`.
- `Instrument.Probed(...)` raises `InstrumentProbed(Ticker, Currency, Exchange, OccurredAt)` (no `InstrumentId` field — probed instruments have the zero sentinel until persisted).
- `Confirm(IClock)` raises `InstrumentConfirmed(InstrumentId, OccurredAt)`.
- `Rename(string)` captures `OldName` **before** mutation, then raises `InstrumentRenamed(InstrumentId, string OldName, string NewName, OccurredAt)`.
- `Instrument.Existing(...)` is rehydration → does not raise.

### 4.5 Entities

- `Position : Entity<PositionId>` — inherits `Id` and equality. **No `CheckRule`** (moved to `AggregateRoot` per the review). No event-raising on Position itself; the Portfolio AR observes Position outcomes and raises.
- `Trade : Entity<TradeId>` — same treatment. The `AssignId(TradeId)` internal method stays — it's how the AR assigns portfolio-wide unique trade IDs.
- `Lot` stays a `record` (VO). It has a DB-side surrogate key on EF's owned-many mapping (`PositionConfiguration` sets `lots.Property<int>("Id").ValueGeneratedOnAdd(); lots.HasKey("Id")`), but that's EF change-tracking plumbing, not domain identity. The domain treats Lot as a value.

---

## 5. Value objects

### Stay as `record` (synthesised equality is correct, no operator-on-sentinel branching)

`Currency`, `Ticker`, `Percentage`, `Conviction`, `CurrencyPair`, `Exchange`, `TimezoneId`, `DateRange`, `Zone`, `PriceBar`, `FxRate`, `Lot`, `Citation`, `MarketCitation`, `PromptFingerprint`, `MarketSnapshot`, `GrowthPoint`, `PositionRow`, `Correctness`, `GateDecision`, `TradeDraft`, `PortfolioSnapshot`, `BollingerReading`, `IchimokuReading`, `IndicatorReading`, `IndicatorBundle`, `IndicatorSeries`, all settings VOs.

`Percentage` carries an `IsEmpty` sentinel but doesn't overload `+`/`-`/`*` to branch on it, so it has no equality nuance and stays a record.

### Migrate to inherit `ValueObject`

`Money`, `Price`, `Quantity` migrate from `record` to `sealed class : ValueObject`.

**Motivation (honest version — review v2):**

1. The three VOs overload arithmetic operators (`+`, `-`, `*`, `/`) that **branch on the sentinel flag** (`IsEmpty`/`IsSpecified`) — propagating "empty" through expressions. This semantic is currently encoded inside the operators; record-synthesised equality already includes the sentinel field, so equality is correct today.
2. **Structural closure against future drift.** Records are *capable* of being mutated via `with` *if* someone adds `init` setters later. Today `Money.Amount { get; }`, `Quantity.Value { get; }`, and `Price` (with `private set;`) don't expose `init`, so `var bad = Money.Of(10m, Currency.Eur) with { Amount = -1m };` **does not compile.** That's a fragile guarantee — a casual future edit that changes `{ get; }` to `{ get; init; }` re-opens the door. Class form removes the `with` syntax entirely, making the closure structural rather than incidental.
3. **Documentation value.** `: ValueObject` makes the type's role unmistakable in the file header. Records can be DTOs, events, or VOs; the marker disambiguates.

**Mechanics:** drop `record` keyword, declare `sealed class : ValueObject`, override `GetEqualityComponents()` yielding every instance field (`Amount`, `Currency`, `IsEmpty` for Money; `PerUnit` for Price; `Value`, `IsSpecified` for Quantity). Factories (`Of`, `Zero`, `None`, `Empty`) and operators preserved verbatim. `ToString()` overrides preserved. The existing private parameterless constructor (e.g., `private Money() { }`) **is preserved** so EF's owned-entity materialisation continues to work.

**Equality round-trip check (semantic-neutrality proof):**
- Today `Money.None(EUR) ≠ Money.Zero(EUR)` (record equality: `IsEmpty` differs). After migration: `≠` (component equality: `IsEmpty` in `GetEqualityComponents`). ✓
- Today `Money.None(EUR) == Money.None(EUR)`. After migration: `==`. ✓
- Today `Quantity.None ≠ Quantity.Zero`. After migration: `≠`. ✓

**Call-site impact:** any external `with` usage on these three types breaks at compile time. Grep confirms zero call sites use `with` on Money/Price/Quantity today. Equality, `==`/`!=`, and pattern-matching by type continue to work.

**EF round-trip risk (review v2 finding #4):** these three VOs are mapped as owned-entities in multiple EF configurations (Trade, Position, Lot, Goal, Suggestion). The class form must keep the private parameterless ctor EF uses for materialisation. Verification is a dedicated integration test (§8.3 `Money_RoundTripsThroughEf`) that round-trips a Trade through `SaveAsync` + reload and asserts the materialised `Money`/`Price`/`Quantity` deep-equal the originals.

---

## 6. Exceptions

```
TradyStratException (abstract)          ← existing
├── BusinessRuleViolationException      ← NEW: wraps an IBusinessRule
├── TradeValidationException            ← unchanged
├── SettingValidationException          ← unchanged
├── CurrencyMismatchException           ← unchanged
└── … all other existing subtypes       ← unchanged
```

The `DomainException` intermediate base proposed in v1 is **dropped** (review v2 finding #12): it had one subclass while every validation exception bypassed it. `BusinessRuleViolationException` inherits `TradyStratException` directly.

```csharp
namespace TradyStrat.Domain.Exceptions;

public sealed class BusinessRuleViolationException(IBusinessRule rule)
    : TradyStratException(rule.Message)
{
    public IBusinessRule BrokenRule { get; } = rule;
}
```

---

## 7. Domain event dispatch infrastructure

### 7.1 Repository contract change — `SaveAsync` returns the drained events

To make "persist + drain" one indivisible act (so use cases cannot forget to drain), every event-bearing repository's `SaveAsync` signature changes:

```csharp
// before
Task SaveAsync(Portfolio portfolio, CancellationToken ct);

// after
Task<IReadOnlyList<IDomainEvent>> SaveAsync(Portfolio portfolio, CancellationToken ct);
```

Implementation pattern (EF):
```csharp
public async Task<IReadOnlyList<IDomainEvent>> SaveAsync(Portfolio portfolio, CancellationToken ct)
{
    await _db.SaveChangesAsync(ct);
    return portfolio.DequeueDomainEvents();
}
```

Applies to `IPortfolioRepository`, `IGoalRepository`, `IInstrumentRepository`, `ISuggestionRepository`. The drain runs **after** `SaveChangesAsync` succeeds — events represent committed state. If `SaveChangesAsync` throws, the events remain on the AR; the surrounding request scope dies with the failed request, so they're collected. (For singletons that's a hypothetical leak; today all event-bearing ARs are request-scoped via EF tracking — verified in §10.)

This trade-off is deliberate (see §11): repository-owned drain wins over caller-orchestrated drain because the caller cannot forget.

### 7.2 `TradyStrat.Infrastructure/SeedWork/DomainEventDispatcher.cs`

```csharp
public sealed class DomainEventDispatcher(IServiceProvider sp, ILogger<DomainEventDispatcher> log)
    : IDomainEventDispatcher
{
    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct)
    {
        foreach (var evt in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(evt.GetType());
            var handlers = sp.GetServices(handlerType);
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
            foreach (var h in handlers)
            {
                var task = (Task?)method.Invoke(h, [evt, ct]) ?? Task.CompletedTask;
                await task;
            }
        }
    }
}
```

**Handler resolution constraint:** lookup is by the *concrete* event type. A handler registered as `IDomainEventHandler<IDomainEvent>` (catch-all) will not match a `TradeRecorded` dispatch. This is intentional for Phase 7 — register per concrete event. If catch-all becomes a real need, extend the dispatcher to also resolve handlers registered against base/marker interfaces; out of scope here.

**Performance:** per-event `MakeGenericType` + `Invoke` is slow under load. For the volumes here (handfuls of events per use case) it's fine. Swap for cached `DynamicMethod` / source-generated dispatch if profiling later shows it as a hotspot — out of scope.

### 7.3 DI registration

In the Infrastructure module bootstrap:
```csharp
services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
```
Handlers are registered by feature modules as `services.AddScoped<IDomainEventHandler<TradeRecorded>, MyHandler>()` *when they exist*. No handlers ship in Phase 7 — the infrastructure lands and the first real handler arrives with whatever feature needs it.

### 7.4 Use-case pattern

```csharp
public sealed class LogTradeUseCase(
    IPortfolioRepository portfolios,
    IInstrumentRepository instruments,
    IDomainEventDispatcher dispatcher,
    IClock clock,
    ILogger<LogTradeUseCase> log)
    : UseCaseBase<LogTradeInput, TradeRecorded>(log)
{
    protected override async Task<TradeRecorded> ExecuteCore(LogTradeInput input, CancellationToken ct)
    {
        // … unchanged setup …
        var tradeRecorded = portfolio.RecordTrade(...);          // returns the raised TradeRecorded event
        var events = await portfolios.SaveAsync(portfolio, ct);  // persist + drain
        await dispatcher.DispatchAsync(events, ct);              // dispatch committed events
        return tradeRecorded;
    }
}
```

`DeleteTradeUseCase` mirrors the same shape, returning `TradeDeleted`. Goal/Instrument/Suggestion command use cases follow the same `(mutate → SaveAsync → DispatchAsync)` triplet.

### 7.5 Failure semantics & deferred outbox

- **Pre-commit failure** (`SaveChangesAsync` throws): events remain on the AR, request scope dies, no dispatch. Safe.
- **Post-commit handler failure** (`DispatchAsync` throws after `SaveAsync` succeeded): the database is committed, but the handler's work is not. **Phase 7 has no recovery path.** The exception bubbles to the use-case caller. The first handler that crosses an I/O boundary (sending email, writing to a queue, calling an external API) must arrive with an outbox: append-event-row-inside-the-transaction, drain-via-background-worker. Until then, all handlers in this codebase must be in-process and idempotent on `EventId` (§3.1) — and Phase 7 ships *no* handlers, so this constraint is satisfied trivially.
- The dispatcher does **not** swallow handler exceptions.

---

## 8. Tests

### 8.1 New SeedWork fixtures (`TradyStrat.Domain.Tests/SeedWork/`)

- `EntityEqualityTests` — same type + same persisted Id ⇒ equal; different subtype ⇒ not equal; **two distinct transient entities with `Id == default` ⇒ NOT equal** (reference-equality short-circuit, review finding #1); transient entity equal-to-self.
- `AggregateRootEventCollectionTests` — `Raise` appends; `DequeueDomainEvents` returns snapshot and clears; subsequent dequeue is empty; `ClearDomainEvents` is internal and unreachable from tests outside the assembly.
- `ValueObjectEqualityTests` — same components ⇒ equal; different components ⇒ not; `==`/`!=` work; `GetHashCode` stable for equal values and uses `System.HashCode` builder (review finding #2).
- `BusinessRuleViolationTests` — `AggregateRoot.CheckRule` throws on broken; passes on satisfied; exception carries the rule.
- `DomainEventDispatcherTests` (`TradyStrat.Infrastructure.Tests`) — resolves concrete-event handlers via DI; invokes each handler once per event; bubbles exceptions; **catch-all `IDomainEventHandler<IDomainEvent>` is NOT matched** (documents the §7.2 constraint).

### 8.2 Existing AR test adaptations

Mechanical edits — done in the same commit as each AR migration so the suite is green *after* each step's edits (not automatically). Examples:
```csharp
// before
var evt = portfolio.RecordTrade(...);
evt.CreatedPosition.ShouldBeTrue();

// after
var tradeRecorded = portfolio.RecordTrade(...);
portfolio.DomainEvents.OfType<PositionOpened>().Any().ShouldBeTrue();
```

`PortfolioBehaviorTests` (and any peer tests interrogating result-record fields that the new event shape doesn't carry) get rewritten per AR migration commit. Each commit ends with `dotnet test` green.

### 8.3 EF round-trip integration test (`TradyStrat.Infrastructure.Tests`)

`MoneyPriceQuantity_RoundTripsThroughEf`: lands in the VO-migration commit. Creates a Trade with non-trivial `Money`/`Price`/`Quantity` values (positive, zero, and `None`/`Empty` variants), persists via `SaveChangesAsync`, opens a fresh `DbContext`, reloads the Trade, asserts every field deep-equals the original. Covers review finding #4.

### 8.4 Dispatch integration test (`TradyStrat.Application.Tests`)

`LogTradeUseCase_PersistsAndDispatchesTradeRecorded`: registers a fake `IDomainEventHandler<TradeRecorded>`, calls the use case, asserts (a) the trade is persisted, (b) the handler received exactly one `TradeRecorded` event with matching `TradeId`, (c) the AR's `DomainEvents` is empty post-call (drained by the repository).

---

## 9. Migration sequencing (commit shape)

Bundled in a single phase / worktree. Each commit ends with `dotnet test` green.

1. **SeedWork primitives + tests** — `Entity<TId>` (with transient-identity short-circuit), `AggregateRoot<TId>`, `ValueObject` (canonical `HashCode` builder), `IDomainEvent` (with `EventId`), `DomainEvent` base, `IBusinessRule`, `IDomainEventDispatcher`, `IDomainEventHandler`. Unit fixtures per §8.1.
2. **Exception** — `BusinessRuleViolationException : TradyStratException`.
3. **VO migration** — `Money`, `Price`, `Quantity` → `sealed class : ValueObject`. EF round-trip test (§8.3) lands here. Domain tests adapted.
4. **Portfolio AR migration** — `Portfolio`/`Position`/`Trade` inherit bases; `Portfolio/TradeRecorded.cs`/`TradeDeleted.cs` move to `Portfolio/Events/` and inherit `DomainEvent`; new `PortfolioCreated` and `PositionOpened` events added; `Portfolio.Empty` raises; `ImportTrades` rollback calls `ClearDomainEvents`. AR tests adapted (§8.2).
5. **Suggestion AR migration** — inherit `AggregateRoot<SuggestionId>`; new `Events/SuggestionCreated.cs`; `Suggestion.From` raises. Tests adapted.
6. **Goal AR migration** — inherit `AggregateRoot<GoalId>`; new `Events/GoalCreated.cs`, `GoalTargetChanged.cs`, `GoalDeadlineRescheduled.cs`; `Initial`/`RetargetAmount`/`RescheduleDeadline` raise (with OldX captured before mutation). Tests adapted.
7. **Instrument AR migration** — inherit `AggregateRoot<InstrumentId>`; new `Events/InstrumentProbed.cs`, `InstrumentConfirmed.cs`, `InstrumentRenamed.cs`; `Probed`/`Confirm`/`Rename` raise (with OldName captured before mutation). Tests adapted.
8. **Repository contract change** — `IPortfolioRepository.SaveAsync` (and Goal/Instrument/Suggestion peers) now return `Task<IReadOnlyList<IDomainEvent>>`. EF implementations drain after `SaveChangesAsync`. Test doubles in `TradyStrat.TestKit` updated.
9. **Infrastructure: `DomainEventDispatcher` + DI** — concrete dispatcher, DI registration, unit tests per §8.1 last bullet.
10. **Application use-case wiring** — `LogTradeUseCase`, `DeleteTradeUseCase`, plus any Goal/Instrument/Suggestion command use cases that call `SaveAsync`, consume the new return value and dispatch. Integration test (§8.4) green.
11. **Tag** — `phase7-domain-ddd-polish-done`. Merge to main.

---

## 10. Risks & mitigations

- **EF private ctor regression.** `Entity<TId>`/`AggregateRoot<TId>` expose a `protected` parameterless ctor that EF can call. Existing `private Foo() { }` ctors on each AR remain. Verified against each AR.
- **EF owned-entity round-trip for migrated VOs.** `Money`/`Price`/`Quantity` are mapped as owned-entities in `TradeConfiguration`, `PositionConfiguration`, `LotConfiguration`, `GoalConfiguration`, `SuggestionConfiguration`. Risk: dropping `record` changes change-detection behaviour for EF's snapshot path. Mitigation: the dedicated round-trip test (§8.3) — and the private parameterless ctor on each VO class is preserved verbatim. The owned-entity `OwnsOne(...)` lambdas in EF configurations require no change.
- **`ImportTrades` rollback + events.** Events accumulate on the AR as trades are recorded. On failure the `_positions` restore path runs **and** `AggregateRoot.ClearDomainEvents()` runs **before re-throwing**. `ClearDomainEvents` is `internal` so the boundary is tight.
- **Transient-entity equality.** With `EqualityComparer<TId>.Default.Equals(default, default) == true`, two unsaved ARs could be mistakenly treated as equal. Mitigated by the `Id == default` short-circuit in `Entity<TId>.Equals` (§3.3) and explicit test coverage (§8.1).
- **Post-commit handler failure.** Persisted state diverges from un-run handler effects. **Phase 7 has no recovery path** (no outbox, no retry). Mitigated by shipping zero handlers in Phase 7 and documenting the constraint (§7.5).
- **Reflection in dispatcher.** Per-event `MakeGenericType` + `Invoke` is slow under load. For the volumes here (handfuls of events per use case) this is fine. Swap to cached delegates if profiling later flags it.
- **Stale events on the AR.** With repository-owned drain (§7.1), the only path that bypasses the drain is direct `db.SaveChangesAsync()` calls that skip `IPortfolioRepository.SaveAsync`. Convention forbids them; the test suite has no such bypass today (verified by grep).
- **EF change-tracker churn from new `GetHashCode`.** `ValueObject`'s new hash differs from `record`'s synthesised hash for the same logical value. EF's change detection uses property-by-property comparison (not hash), so this is not load-bearing — but any `Dictionary<Money, …>` in user code would re-bucket. Grep confirms zero such usages today.

---

## 11. Out-of-scope clarifications & conventions

**Explicitly deferred:**
- **No MediatR.** Custom dispatcher only.
- **No EF SaveChanges interceptor.** Repository owns the drain (§7.1); dispatch is explicit in the use case.
- **No outbox / same-transaction handler atomicity.** Phase 7 dispatches post-commit. The first handler crossing an I/O boundary triggers an outbox introduction (likely Phase 8).
- **No `IStronglyTypedId<T>` marker.** Dropped per review finding #11 — no consumer in Phase 7.
- **No `DomainException` intermediate.** Dropped per review finding #12.
- **No retrofit of existing validations to `IBusinessRule`.** Available, not mandatory.
- **No ID type changes.** Structs stay structs.
- **No new aggregates.** Existing four only.

**Conventions established by Phase 7 (binding on all future code):**
- Every new-creation factory raises a `*Created` event. Rehydration factories never raise. Queries never raise.
- Every mutating method on an AR raises a corresponding `*Changed` event. Captures `OldX` before mutating when the delta is meaningful for handlers.
- `OccurredAt` is UTC, sourced from `IClock.UtcNow()`.
- Use cases call `(mutate → repository.SaveAsync → dispatcher.DispatchAsync)` in that order. Never bypass the repository.
- Handlers are registered against the concrete event closed-generic (`IDomainEventHandler<TradeRecorded>`), not against `IDomainEvent`.
- Handlers must be idempotent on `EventId` until an outbox lands.
