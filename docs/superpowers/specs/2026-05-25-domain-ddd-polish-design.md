# Phase 7 — Domain DDD Polish (Seedwork + Domain Events)

**Date:** 2026-05-25
**Status:** Design (awaiting plan)
**Predecessors:** Phases 1–6 of the DDD rework (suggestion AR, instrument AR, goal/market-data/indicators cleanup, settings VOs). This phase completes the arc by polishing primitives the prior phases left implicit.
**Tag on landing:** `phase7-domain-ddd-polish-done`

---

## 1. Motivation

The Domain project (`TradyStrat.Domain`) currently expresses DDD building blocks informally:

- Each aggregate root (AR) re-implements its own private EF ctor, factory methods, and ID handling.
- "Domain events" exist as records (`TradeRecorded`, `TradeDeleted`) but are **returned from aggregate methods**, not collected on the AR and dispatched. They are events in name only — they have no consumer beyond the immediate caller.
- Strongly-typed IDs share a shape (`readonly record struct X(int Value)` with `.New() => new(0)`) but no common interface.
- Several aggregates raise no events at all (`Suggestion`, `Instrument`, `Goal`), so cache invalidation and downstream effects must be wired manually at every call site.
- A few value objects with sentinel flags (`Money.IsEmpty`, `Quantity.IsSpecified`) carry hand-rolled equality.

Phase 7 introduces a small **seedwork kit** (Entity, AggregateRoot, ValueObject, IDomainEvent, IBusinessRule, IStronglyTypedId, IDomainEventDispatcher), migrates the four ARs to inherit from it, and converts the existing "returned event" style into proper **collected-and-dispatched** domain events. Validation and ID encoding remain as they are — the kit is *available*; we don't retrofit working code with it for the sake of consistency.

---

## 2. Scope

### In scope

- New `TradyStrat.Domain/SeedWork/` folder with base types and dispatcher contracts.
- Migration of `Portfolio`, `Suggestion`, `Goal`, `Instrument` to inherit `AggregateRoot<TId>`.
- Migration of `Position`, `Trade` to inherit `Entity<TId>`.
- New `Events/` subfolder under each AR's folder, holding domain event records.
- `Money`, `Price`, `Quantity` migrate to inherit `ValueObject` (because of their sentinel flags); other VOs stay as `record`.
- Existing strongly-typed IDs add `: IStronglyTypedId<int>` marker.
- New `Exceptions/DomainException` intermediate base + `BusinessRuleViolationException`.
- Infrastructure-side `DomainEventDispatcher` + DI registration.
- Application use cases `LogTradeUseCase` and `DeleteTradeUseCase` updated to drain events after `SaveAsync` and dispatch.
- Tests: new SeedWork unit fixtures, existing AR tests adapted to the new event-collection shape, one new integration test proving dispatch flows end-to-end.

### Out of scope

- Retrofitting `IBusinessRule` into existing factory validations. The kit ships, rules land when a real use case introduces one.
- Converting ID structs into records (would require EF value-converter changes; structs work fine and are cheaper).
- New domain events beyond the obvious ones listed in §4.3 (no speculative coverage).
- Any change to `Indicators/`, `MarketData/`, `PriceFeed/`, `Settings/` aggregates/VOs beyond ID interface marker.
- Migrating to MediatR or EF SaveChanges interceptor for dispatch — explicitly chosen against.

---

## 3. SeedWork primitives

Lives in `TradyStrat.Domain/SeedWork/`. Each file ~30 lines, zero external dependencies.

### 3.1 `IDomainEvent.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
```

### 3.2 `DomainEvent.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public abstract record DomainEvent(DateTime OccurredAt) : IDomainEvent;
```

Concrete events are positional records inheriting `DomainEvent`. Immutable, value-equal, carry only what a handler needs.

### 3.3 `Entity<TId>.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public abstract class Entity<TId> where TId : struct
{
    public TId Id { get; protected set; }

    protected Entity() { }                       // EF
    protected Entity(TId id) => Id = id;

    public override bool Equals(object? obj)
        => obj is Entity<TId> other
           && GetType() == other.GetType()
           && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    protected static void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
            throw new Exceptions.BusinessRuleViolationException(rule);
    }
}
```

Entities have identity equality (same type + same Id), not value equality. The `CheckRule` helper is the rule-enforcement seam — currently unused by existing code, available to new rules.

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

    public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
    {
        var snapshot = _events.ToArray();
        _events.Clear();
        return snapshot;
    }
}
```

`DequeueDomainEvents` is a one-shot drain; calling it twice yields the second call empty. Use cases drain *after* `SaveAsync` so dispatch reflects committed state.

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
        => GetEqualityComponents()
            .Aggregate(17, (h, c) => HashCode.Combine(h, c?.GetHashCode() ?? 0));

    public static bool operator ==(ValueObject? a, ValueObject? b) => Equals(a, b);
    public static bool operator !=(ValueObject? a, ValueObject? b) => !Equals(a, b);
}
```

Used by the few VOs with sentinel flags (`Money`, `Price`, `Quantity`) where the `record` synthesized equality would treat `Money.None(EUR)` ≠ `Money.None(EUR)` (it wouldn't, but the `IsEmpty` flag affects equality semantics we want explicit). All other VOs remain `record`s.

### 3.6 `IBusinessRule.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IBusinessRule
{
    bool IsBroken();
    string Message { get; }
}
```

### 3.7 `IStronglyTypedId.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IStronglyTypedId<out TValue> where TValue : struct
{
    TValue Value { get; }
}
```

Pure marker. Existing ID structs add `: IStronglyTypedId<int>` and inherit no behaviour. EF mappings unaffected.

### 3.8 `IDomainEventDispatcher.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct);
}
```

### 3.9 `IDomainEventHandler.cs`

```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent evt, CancellationToken ct);
}
```

---

## 4. Aggregate migrations

### 4.1 `Portfolio : AggregateRoot<PortfolioId>`

- Drop hand-rolled `Id` property (now inherited).
- `RecordTrade(...)` returns a new `TradeRecordedResult(TradeId, PositionId, Money RealizedDelta)` (renamed to disambiguate from the event). The previous `bool CreatedPosition` flag is **dropped from the result** — the `PositionOpened` event now communicates that fact to interested parties. Callers that need to know synchronously can inspect `DomainEvents` before dispatch.
- Internally `RecordTrade` calls `Raise(new TradeRecorded(...))` and, when a new position is opened, additionally `Raise(new PositionOpened(...))`.
- `DeleteTrade(...)` returns `TradeDeletedResult(PositionId, Money RealizedDelta)` and raises `TradeDeleted`.
- `ImportTrades` raises one `TradeRecorded` per imported trade as it goes. The existing rollback path (`_positions` restore on exception) is extended to **clear `_events` before re-throwing** so a partial batch leaves no orphan events on the AR. See §10 risks.
- `Snapshot` / `SnapshotAsOf` / `GrowthSeries` / `RehydrateLots` unchanged.

### 4.2 `Suggestion : AggregateRoot<SuggestionId>`

- Drop hand-rolled `Id`.
- `Suggestion.From(...)` factory raises `SuggestionCreated(SuggestionId, InstrumentId, ForDate, Action, OccurredAt: createdAt)` *after* validation succeeds.
- `WasCorrect` stays a pure query method — does not raise.

### 4.3 `Goal : AggregateRoot<GoalId>`

- Drop hand-rolled `Id`.
- `RetargetAmount(...)` raises `GoalTargetChanged(GoalId, Money OldTarget, Money NewTarget, OccurredAt)`.
- `RescheduleDeadline(...)` raises `GoalDeadlineRescheduled(GoalId, DateOnly OldDeadline, DateOnly NewDeadline, OccurredAt)`.
- `Initial(IClock)` does **not** raise — bootstrap-only construction is not an event.

### 4.4 `Instrument : AggregateRoot<InstrumentId>`

- Drop hand-rolled `Id`.
- `Confirm(IClock)` raises `InstrumentConfirmed(InstrumentId, OccurredAt)`.
- `Rename(string)` raises `InstrumentRenamed(InstrumentId, string OldName, string NewName, OccurredAt)`.
- `Probed(...)` and `Existing(...)` factories do not raise (probing is not yet a domain-meaningful event; rehydration must not raise events).

### 4.5 Entities

- `Position : Entity<PositionId>` — inherits `Id`, equality, `CheckRule`. No event-raising on Position itself; the Portfolio AR observes Position outcomes and raises.
- `Trade : Entity<TradeId>` — same treatment. The `AssignId(TradeId)` internal method stays — it's how the AR assigns portfolio-wide unique trade IDs.

---

## 5. Value objects

### Stay as `record` (synthesized equality is correct)

`Currency`, `Ticker`, `Percentage`, `Conviction`, `CurrencyPair`, `Exchange`, `TimezoneId`, `DateRange`, `Zone`, `PriceBar`, `FxRate`, `Lot`, `Citation`, `MarketCitation`, `PromptFingerprint`, `MarketSnapshot`, `GrowthPoint`, `PositionRow`, `Correctness`, `GateDecision`, `TradeDraft`, `PortfolioSnapshot`, `BollingerReading`, `IchimokuReading`, `IndicatorReading`, `IndicatorBundle`, `IndicatorSeries`, all settings VOs.

### Migrate to inherit `ValueObject`

`Money`, `Price`, `Quantity` migrate from `record` to `sealed class : ValueObject`.

**Motivation:** record-synthesised equality is already correct for these types (`IsEmpty`/`IsSpecified` are auto-property backing fields, so they participate). The real reason to migrate is to **eliminate the `with` expression**:

```csharp
var bad = Money.Of(10m, Currency.Eur) with { Amount = -1m };  // bypasses factory invariants
```

A by-the-book value object only enters valid states through factory methods. Removing `record` removes the `with` escape hatch.

**Mechanics:** drop `record` keyword, override `GetEqualityComponents()` yielding every instance field (`Amount`, `Currency`, `IsEmpty` for Money; `PerUnit` for Price; `Value`, `IsSpecified` for Quantity). Factories (`Of`, `Zero`, `None`, `Empty`) and operators (`+`, `-`, `*`, `/`) are preserved verbatim. `ToString()` overrides preserved.

**Call-site impact:** any external `with` usage on these three types breaks at compile time — verified by grep before this commit lands. Equality, `==`/`!=`, and pattern-matching by type continue to work.

---

## 6. Exceptions

```
TradyStratException (abstract)         ← existing
├── DomainException (abstract)         ← NEW: semantic hint for SeedWork-thrown errors
│   └── BusinessRuleViolationException ← NEW: wraps an IBusinessRule
├── TradeValidationException           ← unchanged, inherits TradyStratException directly
├── SettingValidationException         ← unchanged
├── CurrencyMismatchException          ← unchanged
└── … all other existing subtypes      ← unchanged
```

`BusinessRuleViolationException(IBusinessRule rule)` exposes `public IBusinessRule BrokenRule { get; }` and uses `rule.Message` as its `Message`.

---

## 7. Domain event dispatch infrastructure

### 7.1 `TradyStrat.Infrastructure/SeedWork/DomainEventDispatcher.cs`

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
            foreach (var h in handlers)
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                await (Task)method.Invoke(h, [evt, ct])!;
            }
        }
    }
}
```

Failure policy: handler exceptions bubble. The surrounding use case decides whether to swallow or surface.

### 7.2 DI registration

In the Infrastructure module bootstrap:
```csharp
services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
```
Handlers are registered by feature modules as `services.AddScoped<IDomainEventHandler<TradeRecorded>, MyHandler>()` *when* they exist. No handlers are added in Phase 7 — the infrastructure ships and the first real handler lands in whatever feature needs it.

### 7.3 Use-case pattern

```csharp
public sealed class LogTradeUseCase(
    IPortfolioRepository portfolios,
    IInstrumentRepository instruments,
    IDomainEventDispatcher dispatcher,
    IClock clock,
    ILogger<LogTradeUseCase> log)
    : UseCaseBase<LogTradeInput, TradeRecordedResult>(log)
{
    protected override async Task<TradeRecordedResult> ExecuteCore(LogTradeInput input, CancellationToken ct)
    {
        // … unchanged setup …
        var result = portfolio.RecordTrade(...);
        await portfolios.SaveAsync(portfolio, ct);
        await dispatcher.DispatchAsync(portfolio.DequeueDomainEvents(), ct);
        return result;
    }
}
```

`DeleteTradeUseCase` mirrors the same shape.

---

## 8. Tests

### 8.1 New SeedWork fixtures (`TradyStrat.Domain.Tests/SeedWork/`)

- `EntityEqualityTests` — same type + same Id ⇒ equal; different subtype ⇒ not equal; default Id ⇒ equal-to-self.
- `AggregateRootEventCollectionTests` — `Raise` appends; `DequeueDomainEvents` returns snapshot and clears; subsequent dequeue is empty.
- `ValueObjectEqualityTests` — same components ⇒ equal; different components ⇒ not; `==`/`!=` work; hash codes consistent.
- `BusinessRuleViolationTests` — `CheckRule` throws on broken; passes on satisfied; exception carries the rule.
- `DomainEventDispatcherTests` (in `TradyStrat.Infrastructure.Tests`) — resolves handlers via DI; invokes each handler once per event; bubbles exceptions.

### 8.2 Existing AR test adaptations

Mechanical edits. Examples:
```csharp
// before
var evt = portfolio.RecordTrade(...);
Assert.That(evt.CreatedPosition, Is.True);

// after
var result = portfolio.RecordTrade(...);
var events = portfolio.DequeueDomainEvents();
Assert.That(events.OfType<PositionOpened>().Any(), Is.True);
```

All ~463 existing domain tests stay green throughout the migration.

### 8.3 New integration test (`TradyStrat.Application.Tests`)

`LogTradeUseCase_DispatchesTradeRecorded`: registers a fake `IDomainEventHandler<TradeRecorded>`, calls the use case, asserts the handler received the event and that the trade is persisted.

---

## 9. Migration sequencing (commit shape)

Bundled in a single phase / worktree. Suggested commit boundaries:

1. **SeedWork primitives + tests** — base types compile, fixtures green. No AR changes yet.
2. **Exception hierarchy** — `DomainException`, `BusinessRuleViolationException`.
3. **Strongly-typed ID marker** — all ID structs add `: IStronglyTypedId<int>`.
4. **VO migration** — `Money`, `Price`, `Quantity` inherit `ValueObject`. Domain tests stay green.
5. **Portfolio AR migration** — `Portfolio`/`Position`/`Trade` inherit bases; events move under `Events/`; methods raise; tests adapted.
6. **Suggestion AR migration**.
7. **Goal AR migration**.
8. **Instrument AR migration**.
9. **Infrastructure: `DomainEventDispatcher` + DI** — handler resolution covered by dispatcher unit tests.
10. **Application use-case wiring** — `LogTradeUseCase` and `DeleteTradeUseCase` consume the dispatcher; integration test green.
11. **Tag** — `phase7-domain-ddd-polish-done`. Merge to main.

Each step keeps the full test suite green.

---

## 10. Risks & mitigations

- **EF private ctor regression.** `Entity<TId>`/`AggregateRoot<TId>` expose a `protected` parameterless ctor that EF can call. Verified mentally against each AR's existing private ctor; behaviour identical.
- **`ImportTrades` rollback + events.** Events are added to `_events` as trades are recorded; on failure, the existing `_positions` restore path runs but `_events` is left populated. Mitigation: clear `_events` inside the `catch` block before re-throwing. Codified in the Portfolio migration commit.
- **VO migration changes equality for `Money`/`Price`/`Quantity`.** Risk: a consumer relied on `record`-synthesized equality including some implicit field we forgot. Mitigation: `GetEqualityComponents` enumerates **every** instance field (verified one-by-one in the spec), tests cover round-trip equality.
- **Reflection in dispatcher.** Per-event `MakeGenericType` + `GetMethod` is slow. For the volumes here (handfuls of events per use case) this is fine. If profiling shows it as a hotspot later, swap for cached delegates — out of scope for Phase 7.
- **Memory of stale events.** If a use case forgets to call `DequeueDomainEvents`, events accumulate on an entity until it's evicted from EF's change tracker. Mitigation: the use-case pattern is mandatory; the integration test in §8.3 proves it for `LogTradeUseCase`.

---

## 11. Out-of-scope clarifications

- **No MediatR.** Custom dispatcher only.
- **No EF SaveChanges interceptor.** Explicit dispatch from the use case — keeps timing visible.
- **No retrofit of existing validations to `IBusinessRule`.** Available, not mandatory.
- **No ID type changes.** Structs stay structs.
- **No new aggregates.** Existing four only.
