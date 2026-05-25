# Phase 7 — Domain DDD Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land seedwork base types (Entity, AggregateRoot, ValueObject, IDomainEvent, IBusinessRule), migrate the four ARs to inherit them, convert "returned events" into collected-and-dispatched domain events with repository-owned drain, and migrate `Money`/`Price`/`Quantity` to `ValueObject` for structural closure.

**Architecture:** New `TradyStrat.Domain/SeedWork/` folder holds zero-dependency primitives. Each AR raises events via a protected `Raise(IDomainEvent)`; the repository's `SaveAsync`/`AddAsync` drains them via `DequeueDomainEvents()` *after* `SaveChangesAsync` commits, and returns them so use cases dispatch through `IDomainEventDispatcher` (resolved via DI in Infrastructure). Per-AR `Events/` subfolders hold concrete event records.

**Tech Stack:** .NET 10, EF Core 10 (SQLite), xUnit v3, Shouldly, Microsoft.Extensions.DependencyInjection.

**Spec:** [`docs/superpowers/specs/2026-05-25-domain-ddd-polish-design.md`](../specs/2026-05-25-domain-ddd-polish-design.md).

**Tag on landing:** `phase7-domain-ddd-polish-done`.

---

## File Structure

### New files
```
TradyStrat.Domain/SeedWork/
├── IDomainEvent.cs              ← Guid EventId + DateTime OccurredAt
├── DomainEvent.cs               ← abstract record base
├── Entity.cs                    ← Entity<TId> with transient-id short-circuit
├── AggregateRoot.cs             ← AggregateRoot<TId> with Raise/Dequeue/Clear/CheckRule
├── ValueObject.cs               ← abstract class with GetEqualityComponents
├── IBusinessRule.cs
├── IDomainEventDispatcher.cs
└── IDomainEventHandler.cs

TradyStrat.Domain/Exceptions/
└── BusinessRuleViolationException.cs

TradyStrat.Domain/Portfolio/Events/
├── PortfolioCreated.cs
├── TradeRecorded.cs             ← MOVED from Portfolio/TradeRecorded.cs
├── TradeDeleted.cs              ← MOVED from Portfolio/TradeDeleted.cs
└── PositionOpened.cs

TradyStrat.Domain/Suggestions/Events/
└── SuggestionCreated.cs

TradyStrat.Domain/Goals/Events/
├── GoalCreated.cs
├── GoalTargetChanged.cs
└── GoalDeadlineRescheduled.cs

TradyStrat.Domain/Instruments/Events/
├── InstrumentProbed.cs
├── InstrumentConfirmed.cs
└── InstrumentRenamed.cs

TradyStrat.Infrastructure/SeedWork/
└── DomainEventDispatcher.cs

TradyStrat.Domain.Tests/SeedWork/
├── EntityEqualityTests.cs
├── AggregateRootEventCollectionTests.cs
├── ValueObjectEqualityTests.cs
├── BusinessRuleViolationTests.cs
└── DomainEventTests.cs

TradyStrat.Infrastructure.Tests/SeedWork/
├── DomainEventDispatcherTests.cs
└── MoneyPriceQuantityRoundTripTests.cs

TradyStrat.Application.Tests/Trades/
└── LogTradeUseCaseDispatchTests.cs
```

### Modified files
```
TradyStrat.Domain/Shared/Money.cs              ← record → sealed class : ValueObject
TradyStrat.Domain/Shared/Price.cs              ← record → sealed class : ValueObject
TradyStrat.Domain/Shared/Quantity.cs           ← record → sealed class : ValueObject
TradyStrat.Domain/Portfolio/Portfolio.cs       ← inherit AggregateRoot<PortfolioId>; Raise events
TradyStrat.Domain/Portfolio/Position.cs        ← inherit Entity<PositionId>
TradyStrat.Domain/Portfolio/Trade.cs           ← inherit Entity<TradeId>
TradyStrat.Domain/Suggestions/Suggestion.cs    ← inherit AggregateRoot<SuggestionId>; raise SuggestionCreated
TradyStrat.Domain/Goals/Goal.cs                ← inherit AggregateRoot<GoalId>; raise Goal* events
TradyStrat.Domain/Instruments/Instrument.cs    ← inherit AggregateRoot<InstrumentId>; raise Instrument* events
TradyStrat.Application/Portfolio/IPortfolioRepository.cs
TradyStrat.Application/Goals/IGoalRepository.cs
TradyStrat.Application/AiSuggestion/ISuggestionRepository.cs
TradyStrat.Application/Settings/IInstrumentRepository.cs
TradyStrat.Infrastructure/Portfolio/EfPortfolioRepository.cs
TradyStrat.Infrastructure/Goals/EfGoalRepository.cs
TradyStrat.Infrastructure/AiSuggestion/EfSuggestionRepository.cs
TradyStrat.Infrastructure/Settings/EfInstrumentRepository.cs
TradyStrat.Application/Trades/UseCases/LogTradeUseCase.cs
TradyStrat.Application/Trades/UseCases/DeleteTradeUseCase.cs
TradyStrat.Application/Trades/UseCases/ImportTradesCsvUseCase.cs
TradyStrat.Application/AiSuggestion/UseCases/*.cs           (3 files using suggestions.AddAsync)
TradyStrat.Infrastructure/<bootstrap>                       (DI registration)
TradyStrat.Domain.Tests/Portfolio/*.cs                      (assertion edits)
TradyStrat.Domain.Tests/Suggestions/SuggestionFactoryTests.cs
TradyStrat.Domain.Tests/Goals/GoalTests.cs
TradyStrat.Domain.Tests/Instruments/InstrumentTests.cs
```

### Deleted files
```
TradyStrat.Domain/Portfolio/TradeRecorded.cs       ← moved into Events/, restructured
TradyStrat.Domain/Portfolio/TradeDeleted.cs        ← moved into Events/, restructured
```

---

## Task 1: Create worktree and verify baseline

**Files:**
- Create: `.claude/worktrees/phase7-ddd-polish/` (git worktree)

- [ ] **Step 1: Create the worktree off main**

Run from `/Users/philippe/repo/gh-phmatray/TradyStrat`:
```bash
git worktree add .claude/worktrees/phase7-ddd-polish -b phase7-ddd-polish main
```
Expected: `Preparing worktree (new branch 'phase7-ddd-polish')` followed by `HEAD is now at <sha> ...`.

- [ ] **Step 2: Verify baseline build is green**

```bash
cd .claude/worktrees/phase7-ddd-polish
dotnet build TradyStrat.slnx -c Debug
```
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Verify baseline tests are green and record the count**

```bash
dotnet test TradyStrat.slnx --nologo -c Debug | tail -5
```
Expected: `Passed: N` where N is the baseline (≈463). Record N for comparison after each subsequent task.

> **All subsequent steps run from the worktree root: `.claude/worktrees/phase7-ddd-polish/`**

---

## Task 2: SeedWork primitives + tests (TDD)

**Files:**
- Create: `TradyStrat.Domain/SeedWork/IDomainEvent.cs`
- Create: `TradyStrat.Domain/SeedWork/DomainEvent.cs`
- Create: `TradyStrat.Domain/SeedWork/Entity.cs`
- Create: `TradyStrat.Domain/SeedWork/AggregateRoot.cs`
- Create: `TradyStrat.Domain/SeedWork/ValueObject.cs`
- Create: `TradyStrat.Domain/SeedWork/IBusinessRule.cs`
- Create: `TradyStrat.Domain/SeedWork/IDomainEventDispatcher.cs`
- Create: `TradyStrat.Domain/SeedWork/IDomainEventHandler.cs`
- Create: `TradyStrat.Domain/Exceptions/BusinessRuleViolationException.cs`
- Test: `TradyStrat.Domain.Tests/SeedWork/EntityEqualityTests.cs`
- Test: `TradyStrat.Domain.Tests/SeedWork/AggregateRootEventCollectionTests.cs`
- Test: `TradyStrat.Domain.Tests/SeedWork/ValueObjectEqualityTests.cs`
- Test: `TradyStrat.Domain.Tests/SeedWork/BusinessRuleViolationTests.cs`
- Test: `TradyStrat.Domain.Tests/SeedWork/DomainEventTests.cs`

- [ ] **Step 1: Write the failing Entity equality test**

Create `TradyStrat.Domain.Tests/SeedWork/EntityEqualityTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.SeedWork;

public class EntityEqualityTests
{
    private sealed class Foo(InstrumentId id) : Entity<InstrumentId>(id) { }
    private sealed class Bar(InstrumentId id) : Entity<InstrumentId>(id) { }

    [Fact]
    public void Two_persisted_entities_with_same_type_and_id_are_equal()
    {
        var a = new Foo(new InstrumentId(7));
        var b = new Foo(new InstrumentId(7));
        a.Equals(b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Different_subtype_with_same_id_is_not_equal()
    {
        var a = new Foo(new InstrumentId(7));
        var b = new Bar(new InstrumentId(7));
        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void Two_transient_entities_are_not_equal_to_each_other()
    {
        // Default sentinel must never make two unsaved aggregates equal.
        var a = new Foo(InstrumentId.New());
        var b = new Foo(InstrumentId.New());
        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void Transient_entity_is_equal_to_itself()
    {
        var a = new Foo(InstrumentId.New());
        a.Equals(a).ShouldBeTrue();
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter EntityEqualityTests
```
Expected: build failure (type `Entity<>` and `IDomainEvent` don't exist yet).

- [ ] **Step 3: Create `IDomainEvent.cs`**

Create `TradyStrat.Domain/SeedWork/IDomainEvent.cs`:
```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IDomainEvent
{
    Guid     EventId    { get; }
    DateTime OccurredAt { get; }
}
```

- [ ] **Step 4: Create `DomainEvent.cs`**

Create `TradyStrat.Domain/SeedWork/DomainEvent.cs`:
```csharp
namespace TradyStrat.Domain.SeedWork;

public abstract record DomainEvent : IDomainEvent
{
    public Guid     EventId    { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; }
}
```

- [ ] **Step 5: Create `Entity.cs`**

Create `TradyStrat.Domain/SeedWork/Entity.cs`:
```csharp
using System.Runtime.CompilerServices;

namespace TradyStrat.Domain.SeedWork;

public abstract class Entity<TId> where TId : struct
{
    public TId Id { get; protected set; }

    protected Entity() { }                   // EF
    protected Entity(TId id) => Id = id;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (GetType() != other.GetType()) return false;
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

- [ ] **Step 6: Create `IBusinessRule.cs`**

Create `TradyStrat.Domain/SeedWork/IBusinessRule.cs`:
```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IBusinessRule
{
    bool IsBroken();
    string Message { get; }
}
```

- [ ] **Step 7: Create `BusinessRuleViolationException.cs`**

Create `TradyStrat.Domain/Exceptions/BusinessRuleViolationException.cs`:
```csharp
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Exceptions;

public sealed class BusinessRuleViolationException(IBusinessRule rule)
    : TradyStratException(rule.Message)
{
    public IBusinessRule BrokenRule { get; } = rule;
}
```

- [ ] **Step 8: Create `AggregateRoot.cs`**

Create `TradyStrat.Domain/SeedWork/AggregateRoot.cs`:
```csharp
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.SeedWork;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : struct
{
    private readonly List<IDomainEvent> _events = new();

    protected AggregateRoot() { }                    // EF
    protected AggregateRoot(TId id) : base(id) { }

    public IReadOnlyList<IDomainEvent> DomainEvents => _events;

    protected void Raise(IDomainEvent evt) => _events.Add(evt);

    protected static void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
            throw new BusinessRuleViolationException(rule);
    }

    public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
    {
        var snapshot = _events.ToArray();
        _events.Clear();
        return snapshot;
    }

    internal void ClearDomainEvents() => _events.Clear();
}
```

- [ ] **Step 9: Create `ValueObject.cs`**

Create `TradyStrat.Domain/SeedWork/ValueObject.cs`:
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

- [ ] **Step 10: Create `IDomainEventDispatcher.cs`**

Create `TradyStrat.Domain/SeedWork/IDomainEventDispatcher.cs`:
```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct);
}
```

- [ ] **Step 11: Create `IDomainEventHandler.cs`**

Create `TradyStrat.Domain/SeedWork/IDomainEventHandler.cs`:
```csharp
namespace TradyStrat.Domain.SeedWork;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent evt, CancellationToken ct);
}
```

- [ ] **Step 12: Re-run the Entity test**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter EntityEqualityTests
```
Expected: 4 tests pass.

- [ ] **Step 13: Write the AggregateRoot event-collection tests**

Create `TradyStrat.Domain.Tests/SeedWork/AggregateRootEventCollectionTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.SeedWork;

public class AggregateRootEventCollectionTests
{
    private sealed record FooHappened(DateTime OccurredAt) : DomainEvent
    { public DateTime OccurredAt { get; init; } = OccurredAt; }

    private sealed class TestAr : AggregateRoot<InstrumentId>
    {
        public TestAr(InstrumentId id) : base(id) { }
        public void Do(DateTime at) => Raise(new FooHappened(at));
    }

    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Raise_appends_to_DomainEvents()
    {
        var ar = new TestAr(new InstrumentId(1));
        ar.Do(_now);
        ar.DomainEvents.Count.ShouldBe(1);
        ar.DomainEvents[0].ShouldBeOfType<FooHappened>();
    }

    [Fact]
    public void DequeueDomainEvents_returns_snapshot_and_clears()
    {
        var ar = new TestAr(new InstrumentId(1));
        ar.Do(_now);
        ar.Do(_now);

        var drained = ar.DequeueDomainEvents();
        drained.Count.ShouldBe(2);
        ar.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Dequeue_twice_yields_empty_on_second_call()
    {
        var ar = new TestAr(new InstrumentId(1));
        ar.Do(_now);
        ar.DequeueDomainEvents();
        ar.DequeueDomainEvents().ShouldBeEmpty();
    }
}
```

- [ ] **Step 14: Run AggregateRoot tests**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter AggregateRootEventCollectionTests
```
Expected: 3 tests pass.

- [ ] **Step 15: Write the ValueObject equality tests**

Create `TradyStrat.Domain.Tests/SeedWork/ValueObjectEqualityTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Domain.SeedWork;
using Xunit;

namespace TradyStrat.Domain.Tests.SeedWork;

public class ValueObjectEqualityTests
{
    private sealed class Pair(int a, string b) : ValueObject
    {
        public int A { get; } = a;
        public string B { get; } = b;
        protected override IEnumerable<object?> GetEqualityComponents()
        { yield return A; yield return B; }
    }

    private sealed class OtherPair(int a, string b) : ValueObject
    {
        public int A { get; } = a;
        public string B { get; } = b;
        protected override IEnumerable<object?> GetEqualityComponents()
        { yield return A; yield return B; }
    }

    [Fact]
    public void Same_components_are_equal()
    {
        new Pair(1, "x").ShouldBe(new Pair(1, "x"));
    }

    [Fact]
    public void Different_components_are_not_equal()
    {
        new Pair(1, "x").ShouldNotBe(new Pair(2, "x"));
        new Pair(1, "x").ShouldNotBe(new Pair(1, "y"));
    }

    [Fact]
    public void Different_subtype_with_same_components_is_not_equal()
    {
        ValueObject p = new Pair(1, "x");
        ValueObject o = new OtherPair(1, "x");
        p.ShouldNotBe(o);
    }

    [Fact]
    public void Equal_values_have_equal_hash_codes()
    {
        new Pair(1, "x").GetHashCode().ShouldBe(new Pair(1, "x").GetHashCode());
    }

    [Fact]
    public void Equality_operators_match_Equals()
    {
        var p = new Pair(1, "x");
        var q = new Pair(1, "x");
        (p == q).ShouldBeTrue();
        (p != q).ShouldBeFalse();
    }
}
```

- [ ] **Step 16: Run ValueObject tests**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter ValueObjectEqualityTests
```
Expected: 5 tests pass.

- [ ] **Step 17: Write the BusinessRuleViolation tests**

Create `TradyStrat.Domain.Tests/SeedWork/BusinessRuleViolationTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.SeedWork;

public class BusinessRuleViolationTests
{
    private sealed class AlwaysBroken : IBusinessRule
    {
        public bool IsBroken() => true;
        public string Message => "broken";
    }
    private sealed class NeverBroken : IBusinessRule
    {
        public bool IsBroken() => false;
        public string Message => "ok";
    }

    private sealed class TestAr : AggregateRoot<InstrumentId>
    {
        public TestAr(InstrumentId id) : base(id) { }
        public void EnforceBroken() => CheckRule(new AlwaysBroken());
        public void EnforceOk()     => CheckRule(new NeverBroken());
    }

    [Fact]
    public void CheckRule_throws_when_rule_is_broken()
    {
        var ar = new TestAr(new InstrumentId(1));
        var ex = Should.Throw<BusinessRuleViolationException>(() => ar.EnforceBroken());
        ex.Message.ShouldBe("broken");
        ex.BrokenRule.ShouldBeOfType<AlwaysBroken>();
    }

    [Fact]
    public void CheckRule_passes_when_rule_is_satisfied()
    {
        var ar = new TestAr(new InstrumentId(1));
        Should.NotThrow(ar.EnforceOk);
    }
}
```

- [ ] **Step 18: Run BusinessRuleViolation tests**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter BusinessRuleViolationTests
```
Expected: 2 tests pass.

- [ ] **Step 19: Write the DomainEvent tests**

Create `TradyStrat.Domain.Tests/SeedWork/DomainEventTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Domain.SeedWork;
using Xunit;

namespace TradyStrat.Domain.Tests.SeedWork;

public class DomainEventTests
{
    private sealed record FooHappened(int X, DateTime OccurredAt) : DomainEvent
    { public DateTime OccurredAt { get; init; } = OccurredAt; }

    [Fact]
    public void EventId_is_assigned_a_fresh_guid()
    {
        var e1 = new FooHappened(1, DateTime.UtcNow);
        var e2 = new FooHappened(1, DateTime.UtcNow);
        e1.EventId.ShouldNotBe(Guid.Empty);
        e1.EventId.ShouldNotBe(e2.EventId);
    }

    [Fact]
    public void OccurredAt_round_trips()
    {
        var at = new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);
        var e = new FooHappened(1, at);
        e.OccurredAt.ShouldBe(at);
    }
}
```

- [ ] **Step 20: Run all SeedWork tests + full suite**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter "FullyQualifiedName~SeedWork"
dotnet test TradyStrat.slnx --nologo
```
Expected: SeedWork tests pass; full suite stays at baseline N (or N + new SeedWork tests = baseline + 14).

- [ ] **Step 21: Commit**

```bash
git add TradyStrat.Domain/SeedWork TradyStrat.Domain/Exceptions/BusinessRuleViolationException.cs TradyStrat.Domain.Tests/SeedWork
git commit -m "$(cat <<'EOF'
feat(domain): add SeedWork primitives — Entity, AggregateRoot, ValueObject, IDomainEvent — Phase 7 Task 2

Introduces the zero-dependency seedwork kit: Entity<TId> with
transient-identity short-circuit, AggregateRoot<TId> with event
collection + CheckRule, ValueObject with canonical HashCode builder,
IDomainEvent + DomainEvent base (with Guid EventId), IBusinessRule,
IDomainEventDispatcher + IDomainEventHandler contracts, and
BusinessRuleViolationException. No AR migrations yet — the kit is
available but unused.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Migrate Money/Price/Quantity to ValueObject + EF round-trip test

**Files:**
- Modify: `TradyStrat.Domain/Shared/Money.cs`
- Modify: `TradyStrat.Domain/Shared/Price.cs`
- Modify: `TradyStrat.Domain/Shared/Quantity.cs`
- Test: `TradyStrat.Infrastructure.Tests/SeedWork/MoneyPriceQuantityRoundTripTests.cs`

- [ ] **Step 1: Confirm there are zero `with` expressions on these types**

```bash
grep -rn "Money\.[A-Z][a-zA-Z]*(.*).*with {\|Price\.[A-Z][a-zA-Z]*(.*).*with {\|Quantity\.[A-Z][a-zA-Z]*(.*).*with {" --include="*.cs" . | grep -v -E "(bin|obj|worktrees)"
```
Expected: no output. If any are found, refactor them to use the appropriate factory before continuing.

- [ ] **Step 2: Rewrite `Money.cs` from `record` to `sealed class : ValueObject`**

Replace the entire content of `TradyStrat.Domain/Shared/Money.cs`:
```csharp
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Shared;

public sealed class Money : ValueObject
{
    public decimal  Amount   { get; }
    public Currency Currency { get; }
    public bool     IsEmpty  { get; }

    private Money() { }   // EF
    private Money(decimal amount, Currency currency, bool isEmpty)
    {
        Amount = amount;
        Currency = currency;
        IsEmpty = isEmpty;
    }

    public static Money Of(decimal amount, Currency currency) => new(amount, currency, isEmpty: false);
    public static Money Zero(Currency currency)               => new(0m, currency, isEmpty: false);
    public static Money None(Currency currency)               => new(0m, currency, isEmpty: true);

    private static void RequireSpecified(Money m, string op)
    {
        if (m.IsEmpty)
            throw new InvalidOperationException($"Cannot perform '{op}' on Money.None({m.Currency}).");
    }

    private static void RequireMatchingCurrency(Money a, Money b, string op)
    {
        if (a.Currency != b.Currency)
            throw new CurrencyMismatchException(
                $"Cannot {op} {a.Currency} and {b.Currency}.");
    }

    public static Money operator +(Money a, Money b)
    {
        RequireSpecified(a, "+"); RequireSpecified(b, "+");
        RequireMatchingCurrency(a, b, "add");
        return Of(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        RequireSpecified(a, "-"); RequireSpecified(b, "-");
        RequireMatchingCurrency(a, b, "subtract");
        return Of(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money m, decimal scalar)
    {
        RequireSpecified(m, "*");
        return Of(m.Amount * scalar, m.Currency);
    }

    public static Money operator *(decimal scalar, Money m) => m * scalar;

    public static Money operator /(Money m, decimal scalar)
    {
        RequireSpecified(m, "/");
        if (scalar == 0m) throw new DivideByZeroException();
        return Of(m.Amount / scalar, m.Currency);
    }

    public static decimal operator /(Money a, Money b)
    {
        RequireSpecified(a, "/"); RequireSpecified(b, "/");
        RequireMatchingCurrency(a, b, "divide");
        if (b.Amount == 0m) throw new DivideByZeroException();
        return a.Amount / b.Amount;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
        yield return IsEmpty;
    }

    public override string ToString() => IsEmpty ? $"None({Currency})" : $"{Amount} {Currency}";
}
```

- [ ] **Step 3: Run Money tests**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter MoneyTests
```
Expected: all Money tests pass. If any fail because of `record`-specific syntax (e.g. `with { Amount = ... }`), fix the test to use factory methods.

- [ ] **Step 4: Rewrite `Price.cs`**

Replace the entire content of `TradyStrat.Domain/Shared/Price.cs`:
```csharp
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Shared;

public sealed class Price : ValueObject
{
    public Money    PerUnit  { get; private set; } = Money.Zero(Currency.Eur);
    public Currency Currency => PerUnit.Currency;
    public bool     IsEmpty  => PerUnit.IsEmpty;

    private Price() { }   // EF
    private Price(Money perUnit) => PerUnit = perUnit;

    public static Price Of(Money perUnit)        => new(perUnit);
    public static Price None(Currency currency)  => new(Money.None(currency));
    public static Price Zero(Currency currency)  => new(Money.Zero(currency));

    public static Money operator *(Price p, Quantity q)
    {
        if (p.IsEmpty || !q.IsSpecified)
            return Money.None(p.Currency);
        return p.PerUnit * q.Value;
    }

    public static Money operator -(Price a, Price b)
    {
        if (a.IsEmpty || b.IsEmpty)
            return Money.None(a.Currency);
        return a.PerUnit - b.PerUnit;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PerUnit;
    }

    public override string ToString() => IsEmpty ? $"None({Currency})" : PerUnit.ToString();
}
```

- [ ] **Step 5: Run Price tests**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter PriceTests
```
Expected: all pass.

- [ ] **Step 6: Rewrite `Quantity.cs`**

Replace the entire content of `TradyStrat.Domain/Shared/Quantity.cs`:
```csharp
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Shared;

public sealed class Quantity : ValueObject
{
    public decimal Value       { get; }
    public bool    IsSpecified { get; }

    private Quantity() { }   // EF
    private Quantity(decimal value, bool isSpecified) { Value = value; IsSpecified = isSpecified; }

    public static Quantity Of(decimal value)
    {
        if (value < 0m) throw new ArgumentException($"Quantity must be non-negative: {value}.", nameof(value));
        return new Quantity(value, isSpecified: true);
    }

    public static Quantity Zero => Of(0m);
    public static Quantity None { get; } = new(0m, isSpecified: false);

    public static Quantity operator +(Quantity a, Quantity b)
    {
        if (!a.IsSpecified || !b.IsSpecified) return None;
        return Of(a.Value + b.Value);
    }

    public static Quantity operator -(Quantity a, Quantity b)
    {
        if (!a.IsSpecified || !b.IsSpecified) return None;
        return Of(a.Value - b.Value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return IsSpecified;
    }

    public override string ToString()
        => IsSpecified ? Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "None";
}
```

- [ ] **Step 7: Run Quantity tests + full Domain suite**

```bash
dotnet test TradyStrat.Domain.Tests --nologo
```
Expected: all pass at baseline + 14 (from Task 2).

- [ ] **Step 8: Write the EF round-trip integration test**

Create `TradyStrat.Infrastructure.Tests/SeedWork/MoneyPriceQuantityRoundTripTests.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Data;
using TradyStrat.TestKit.Specifications;
using Xunit;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Infrastructure.Tests.SeedWork;

public class MoneyPriceQuantityRoundTripTests
{
    private static readonly DateTime _now = new(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Trade_round_trips_through_EF_with_money_price_quantity_intact()
    {
        await using var db = InMemoryDb.Create();

        var p = PortfolioAr.Empty(PortfolioId.Singleton);
        p.RecordTrade(
            instrumentId:   new InstrumentId(42),
            executedOn:     new DateOnly(2026, 1, 15),
            side:           TradeSide.Buy,
            quantity:       Quantity.Of(7.5m),
            pricePerShare:  Price.Of(Money.Of(123.45m, Currency.Eur)),
            fees:           Money.Of(0.99m, Currency.Eur),
            note:           "rt",
            now:            _now);
        db.Portfolios.Add(p);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Reload through a fresh context
        await using var db2 = InMemoryDb.Create(db.Database.GetDbConnection());
        var reloaded = await db2.Portfolios
            .Include("_positions._openLots")
            .Include("_positions._trades")
            .SingleAsync(TestContext.Current.CancellationToken);
        var trade = reloaded.Positions[0].Trades[0];

        trade.Quantity.Value.ShouldBe(7.5m);
        trade.Quantity.IsSpecified.ShouldBeTrue();
        trade.PricePerShare.PerUnit.Amount.ShouldBe(123.45m);
        trade.PricePerShare.PerUnit.Currency.ShouldBe(Currency.Eur);
        trade.PricePerShare.PerUnit.IsEmpty.ShouldBeFalse();
        trade.Fees.Amount.ShouldBe(0.99m);
        trade.Fees.Currency.ShouldBe(Currency.Eur);
    }
}
```

> **Note:** `InMemoryDb.Create(connection)` may not exist with that overload — check `TradyStrat.TestKit/Specifications/InMemoryDb.cs` and adapt the call to whatever the helper exposes for reopening a context against the same SQLite in-memory connection. If the helper only exposes `InMemoryDb.Create()` without sharing, change the test to keep a single `AppDbContext` reference and clear its change tracker (`db.ChangeTracker.Clear()`) before re-querying.

- [ ] **Step 9: Run the round-trip test**

```bash
dotnet test TradyStrat.Infrastructure.Tests --nologo --filter MoneyPriceQuantityRoundTripTests
```
Expected: pass. If it fails because of an EF mapping change, do **not** change EF configurations — the test should pass without touching `TradeConfiguration` / `PositionConfiguration` because the private parameterless ctor on each VO is preserved.

- [ ] **Step 10: Run the full suite**

```bash
dotnet test TradyStrat.slnx --nologo
```
Expected: all pass at baseline + 14 + 1 = baseline + 15.

- [ ] **Step 11: Commit**

```bash
git add TradyStrat.Domain/Shared/Money.cs TradyStrat.Domain/Shared/Price.cs TradyStrat.Domain/Shared/Quantity.cs TradyStrat.Infrastructure.Tests/SeedWork
git commit -m "$(cat <<'EOF'
refactor(domain): migrate Money/Price/Quantity from record to ValueObject — Phase 7 Task 3

Structural closure against future init-property drift: dropping the
`record` keyword removes the `with` syntax entirely. Equality is
preserved via GetEqualityComponents yielding every instance field.
The private parameterless ctor stays so EF owned-entity materialisation
is unaffected. New EF round-trip test guards the mapping.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Portfolio AR migration

**Files:**
- Modify: `TradyStrat.Domain/Portfolio/Portfolio.cs`
- Modify: `TradyStrat.Domain/Portfolio/Position.cs`
- Modify: `TradyStrat.Domain/Portfolio/Trade.cs`
- Delete: `TradyStrat.Domain/Portfolio/TradeRecorded.cs`
- Delete: `TradyStrat.Domain/Portfolio/TradeDeleted.cs`
- Create: `TradyStrat.Domain/Portfolio/Events/PortfolioCreated.cs`
- Create: `TradyStrat.Domain/Portfolio/Events/TradeRecorded.cs`
- Create: `TradyStrat.Domain/Portfolio/Events/TradeDeleted.cs`
- Create: `TradyStrat.Domain/Portfolio/Events/PositionOpened.cs`
- Modify: `TradyStrat.Domain.Tests/Portfolio/PortfolioBehaviorTests.cs` (assertion edits)
- Modify: any other Portfolio tests that interrogate result fields

- [ ] **Step 1: Create the four event records**

Create `TradyStrat.Domain/Portfolio/Events/PortfolioCreated.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio.Events;

public sealed record PortfolioCreated(PortfolioId PortfolioId, DateTime OccurredAt) : DomainEvent
{ public DateTime OccurredAt { get; init; } = OccurredAt; }
```

Create `TradyStrat.Domain/Portfolio/Events/TradeRecorded.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio.Events;

public sealed record TradeRecorded(
    TradeId    TradeId,
    PositionId PositionId,
    Money      RealizedDelta,
    DateTime   OccurredAt) : DomainEvent
{ public DateTime OccurredAt { get; init; } = OccurredAt; }
```

Create `TradyStrat.Domain/Portfolio/Events/TradeDeleted.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio.Events;

public sealed record TradeDeleted(
    PositionId PositionId,
    Money      RealizedDelta,
    DateTime   OccurredAt) : DomainEvent
{ public DateTime OccurredAt { get; init; } = OccurredAt; }
```

Create `TradyStrat.Domain/Portfolio/Events/PositionOpened.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio.Events;

public sealed record PositionOpened(
    PositionId   PositionId,
    InstrumentId InstrumentId,
    DateTime     OccurredAt) : DomainEvent
{ public DateTime OccurredAt { get; init; } = OccurredAt; }
```

- [ ] **Step 2: Delete the old result records**

```bash
git rm TradyStrat.Domain/Portfolio/TradeRecorded.cs TradyStrat.Domain/Portfolio/TradeDeleted.cs
```

- [ ] **Step 3: Update `Trade.cs` to inherit `Entity<TradeId>`**

Replace `TradyStrat.Domain/Portfolio/Trade.cs`:
```csharp
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed class Trade : Entity<TradeId>
{
    public DateOnly  ExecutedOn    { get; private set; }
    public TradeSide Side          { get; private set; }
    public Quantity  Quantity      { get; private set; } = Quantity.None;
    public Price     PricePerShare { get; private set; } = Price.None(Currency.Eur);
    public Money     Fees          { get; private set; } = Money.Zero(Currency.Eur);
    public string    Note          { get; private set; } = "";
    public DateTime  CreatedAt     { get; private set; }

    private Trade() { }   // EF

    private Trade(
        DateOnly executedOn, TradeSide side, Quantity quantity,
        Price pricePerShare, Money fees, string note, DateTime now)
        : base(TradeId.New())
    {
        ExecutedOn    = executedOn;
        Side          = side;
        Quantity      = quantity;
        PricePerShare = pricePerShare;
        Fees          = fees;
        Note          = note;
        CreatedAt     = now;
    }

    public static Trade Create(
        DateOnly executedOn, TradeSide side, Quantity quantity,
        Price pricePerShare, Money fees, string note, DateTime now)
    {
        if (!quantity.IsSpecified || quantity.Value <= 0m)
            throw new TradeValidationException("Quantity must be positive.");
        if (pricePerShare.IsEmpty || pricePerShare.PerUnit.Amount <= 0m)
            throw new TradeValidationException("Price per share must be positive.");
        if (fees.IsEmpty || fees.Amount < 0m)
            throw new TradeValidationException("Fees cannot be negative.");

        return new Trade(executedOn, side, quantity, pricePerShare, fees, note ?? "", now);
    }

    public bool IsBuy => Side == TradeSide.Buy;

    public Money Gross => PricePerShare * Quantity;
    public Money Net   => Side == TradeSide.Buy ? Gross + Fees : Gross - Fees;

    internal void AssignId(TradeId id) => Id = id;
}
```

- [ ] **Step 4: Update `Position.cs` to inherit `Entity<PositionId>`**

Replace `TradyStrat.Domain/Portfolio/Position.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed class Position : Entity<PositionId>
{
    public InstrumentId InstrumentId { get; private set; }

    private readonly List<Lot>   _openLots = new();
    private readonly List<Trade> _trades   = new();

    private Money _realizedPnL = Money.Zero(Currency.Eur);

    public IReadOnlyList<Lot>   OpenLots    => _openLots;
    public IReadOnlyList<Trade> Trades      => _trades;
    public Money                RealizedPnL => _realizedPnL;

    public Quantity TotalQuantity
    {
        get
        {
            var sum = Quantity.Zero;
            foreach (var lot in _openLots)
                sum += lot.Quantity;
            return sum;
        }
    }

    public Money CostBasis
    {
        get
        {
            var sum = Money.Zero(Currency.Eur);
            foreach (var lot in _openLots)
                sum += lot.CostBasis;
            return sum;
        }
    }

    private Position() { }   // EF

    private Position(InstrumentId instrumentId) : base(PositionId.New())
    {
        InstrumentId = instrumentId;
    }

    public static Position OpenFor(InstrumentId instrumentId) => new(instrumentId);

    public Money Record(Trade trade)
    {
        var realizedBefore = _realizedPnL;
        if (trade.Id == TradeId.New())
            trade.AssignId(new TradeId(_trades.Count + 1));
        _trades.Add(trade);

        if (trade.IsBuy)
        {
            var grossPlusFees = trade.PricePerShare * trade.Quantity + trade.Fees;
            var unitCost = grossPlusFees / trade.Quantity.Value;
            _openLots.Add(new Lot(trade.ExecutedOn, trade.Quantity, unitCost));
            return Money.Zero(Currency.Eur);
        }

        var remaining = trade.Quantity.Value;
        var totalSellQty = trade.Quantity.Value;
        while (remaining > 0m)
        {
            if (_openLots.Count == 0)
                throw new Exceptions.TradeValidationException(
                    $"Sell on {trade.ExecutedOn} for instrument {InstrumentId} exceeds open lots.");

            var head = _openLots[0];
            var consumed = Math.Min(head.Quantity.Value, remaining);

            var pricePnL = (trade.PricePerShare.PerUnit - head.UnitCost) * consumed;
            var feeShare = trade.Fees * (consumed / totalSellQty);
            _realizedPnL = _realizedPnL + pricePnL - feeShare;

            if (consumed == head.Quantity.Value)
                _openLots.RemoveAt(0);
            else
                _openLots[0] = head.WithQuantity(Quantity.Of(head.Quantity.Value - consumed));

            remaining -= consumed;
        }

        return _realizedPnL - realizedBefore;
    }

    internal void ClearAllForReplay()
    {
        _openLots.Clear();
        _trades.Clear();
        _realizedPnL = Money.Zero(Currency.Eur);
    }

    internal void RestoreState(IEnumerable<Lot> lots, IEnumerable<Trade> trades, Money realized)
    {
        _openLots.Clear(); _openLots.AddRange(lots);
        _trades.Clear();   _trades.AddRange(trades);
        _realizedPnL = realized;
    }
}
```

- [ ] **Step 5: Rewrite `Portfolio.cs` to inherit `AggregateRoot<PortfolioId>` and raise events**

Replace `TradyStrat.Domain/Portfolio/Portfolio.cs`:
```csharp
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Portfolio.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed class Portfolio : AggregateRoot<PortfolioId>
{
    private readonly List<Position> _positions = new();
    public IReadOnlyList<Position> Positions => _positions;

    private Portfolio() { }   // EF
    private Portfolio(PortfolioId id) : base(id) { }

    /// <summary>
    /// Creates an empty portfolio AR. The <paramref name="now"/> parameter
    /// stamps the PortfolioCreated event — pass clock.UtcNow() at call sites.
    /// </summary>
    public static Portfolio Empty(PortfolioId id, DateTime now)
    {
        var p = new Portfolio(id);
        p.Raise(new PortfolioCreated(id, now));
        return p;
    }

    /// <summary>Rehydration overload — does not raise. Use only from EF-shaped paths and tests that need a bare AR.</summary>
    public static Portfolio Empty(PortfolioId id) => new(id);

    public TradeRecorded RecordTrade(
        InstrumentId instrumentId,
        DateOnly executedOn, TradeSide side,
        Quantity quantity, Price pricePerShare, Money fees, string note, DateTime now)
    {
        var trade = Trade.Create(executedOn, side, quantity, pricePerShare, fees, note, now);

        var position = _positions.FirstOrDefault(p => p.InstrumentId == instrumentId);
        var created = position is null;
        if (position is null)
        {
            position = Position.OpenFor(instrumentId);
            _positions.Add(position);
        }

        trade.AssignId(new TradeId(NextTradeIdValue()));
        var realizedDelta = position.Record(trade);

        if (created)
            Raise(new PositionOpened(position.Id, instrumentId, now));

        var evt = new TradeRecorded(trade.Id, position.Id, realizedDelta, now);
        Raise(evt);
        return evt;
    }

    private int NextTradeIdValue()
    {
        var max = 0;
        foreach (var p in _positions)
            foreach (var t in p.Trades)
                if (t.Id.Value > max) max = t.Id.Value;
        return max + 1;
    }

    public TradeDeleted DeleteTrade(TradeId tradeId, DateTime now)
    {
        Position? target = null;
        foreach (var p in _positions)
            if (p.Trades.Any(t => t.Id == tradeId)) { target = p; break; }
        if (target is null)
            throw new TradeValidationException($"Trade {tradeId} not found.");

        var realizedBefore = target.RealizedPnL;
        var remaining = target.Trades.Where(t => t.Id != tradeId)
                                     .OrderBy(t => t.ExecutedOn)
                                     .ToList();
        target.ClearAllForReplay();
        foreach (var t in remaining) target.Record(t);

        var evt = new TradeDeleted(target.Id, target.RealizedPnL - realizedBefore, now);
        Raise(evt);
        return evt;
    }

    public IReadOnlyList<TradeRecorded> ImportTrades(
        IReadOnlyList<TradeDraft> drafts, DateTime now)
    {
        var savedPositions = _positions.ToList();
        var saved = _positions.ToDictionary(p => p,
            p => (lots: p.OpenLots.ToList(),
                  trades: p.Trades.ToList(),
                  realized: p.RealizedPnL));

        var results = new List<TradeRecorded>(drafts.Count);
        try
        {
            foreach (var d in drafts)
                results.Add(RecordTrade(d.InstrumentId, d.ExecutedOn, d.Side,
                    d.Quantity, d.PricePerShare, d.Fees, d.Note, now));
            return results;
        }
        catch
        {
            _positions.Clear();
            _positions.AddRange(savedPositions);
            foreach (var p in _positions)
            {
                var (lots, trades, realized) = saved[p];
                p.RestoreState(lots, trades, realized);
            }
            ClearDomainEvents();   // discard events from the failed batch
            throw;
        }
    }

    public PortfolioSnapshot Snapshot(
        IReadOnlyDictionary<InstrumentId, Instrument> instrumentById,
        IReadOnlyDictionary<InstrumentId, Price>      priceByInstrument,
        Money goalTarget)
        => BuildSnapshot(_positions, instrumentById, priceByInstrument, goalTarget);

    public PortfolioSnapshot SnapshotAsOf(
        DateOnly asOf,
        IReadOnlyDictionary<InstrumentId, Instrument> instrumentById,
        IReadOnlyDictionary<InstrumentId, Price>      priceByInstrument,
        Money goalTarget)
    {
        var tempPortfolio = new Portfolio(Id);   // rehydration shape — no event
        foreach (var pos in _positions)
        {
            var tradesInWindow = pos.Trades
                .Where(t => t.ExecutedOn <= asOf)
                .OrderBy(t => t.ExecutedOn);
            foreach (var t in tradesInWindow)
                tempPortfolio.RecordTrade(
                    pos.InstrumentId, t.ExecutedOn, t.Side,
                    t.Quantity, t.PricePerShare, t.Fees, t.Note, t.CreatedAt);
        }
        tempPortfolio.ClearDomainEvents();   // discard events from snapshot replay
        return BuildSnapshot(tempPortfolio._positions, instrumentById, priceByInstrument, goalTarget);
    }

    public bool RehydrateLots()
    {
        var any = false;
        foreach (var pos in _positions)
        {
            if (pos.Trades.Count == 0 || pos.OpenLots.Count > 0) continue;
            var ordered = pos.Trades.OrderBy(t => t.ExecutedOn).ToList();
            pos.ClearAllForReplay();
            foreach (var t in ordered) pos.Record(t);
            any = true;
        }
        return any;
    }

    public IReadOnlyList<GrowthPoint> GrowthSeries(
        IReadOnlyDictionary<InstrumentId, IReadOnlyList<PriceBar>> barsByInstrument)
    {
        var allTrades = _positions.SelectMany(p => p.Trades.Select(t => (p.InstrumentId, t)))
                                  .OrderBy(x => x.t.ExecutedOn)
                                  .ToList();
        if (allTrades.Count == 0) return [];

        var allBarDates = barsByInstrument.Values
            .SelectMany(bars => bars.Select(b => b.Date))
            .Distinct()
            .OrderBy(d => d)
            .ToList();
        if (allBarDates.Count == 0) return [];

        var firstTradeDate = allTrades[0].t.ExecutedOn;
        var points = new List<GrowthPoint>(allBarDates.Count + 1)
        {
            new(firstTradeDate.AddDays(-1), Money.Zero(Currency.Eur), Percentage.Empty),
        };

        var sharesByInstrument = new Dictionary<InstrumentId, decimal>();
        var tradesByDate = allTrades
            .GroupBy(x => x.t.ExecutedOn)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var date in allBarDates)
        {
            if (tradesByDate.TryGetValue(date, out var todays))
                foreach (var (iid, t) in todays)
                {
                    var delta = t.IsBuy ? t.Quantity.Value : -t.Quantity.Value;
                    sharesByInstrument[iid] = sharesByInstrument.GetValueOrDefault(iid) + delta;
                }

            var totalValue = 0m;
            foreach (var (iid, bars) in barsByInstrument)
            {
                var bar = bars.FirstOrDefault(b => b.Date == date);
                if (bar is null) continue;
                var shares = sharesByInstrument.GetValueOrDefault(iid);
                totalValue += shares * bar.Close;
            }
            points.Add(new GrowthPoint(date, Money.Of(totalValue, Currency.Eur), Percentage.Empty));
        }

        return points;
    }

    private static PortfolioSnapshot BuildSnapshot(
        List<Position> positions,
        IReadOnlyDictionary<InstrumentId, Instrument> instrumentById,
        IReadOnlyDictionary<InstrumentId, Price>      priceByInstrument,
        Money goalTarget)
    {
        var rows = new List<PositionRow>(positions.Count);

        foreach (var pos in positions)
        {
            var hasInst  = instrumentById.TryGetValue(pos.InstrumentId, out var inst);
            var hasPrice = priceByInstrument.TryGetValue(pos.InstrumentId, out var price);

            var ticker   = hasInst  ? inst!.Ticker        : "?";
            var currency = hasInst  ? inst!.Currency.Code : "?";

            var qty = pos.TotalQuantity;
            var costBasis = pos.CostBasis;
            var marketValue = hasPrice
                ? (price! * qty)
                : Money.Zero(Currency.Eur);
            var unrealised = marketValue - costBasis;

            rows.Add(new PositionRow(
                InstrumentId: pos.InstrumentId,
                Ticker:        ticker,
                Currency:      currency,
                Quantity:      qty,
                CostBasisEur:  costBasis,
                MarketValueEur: marketValue,
                UnrealizedPnLEur: unrealised,
                RealizedPnLEur:   pos.RealizedPnL));
        }

        var totalValue  = rows.Aggregate(Money.Zero(Currency.Eur), (a, r) => a + r.MarketValueEur);
        var totalCost   = rows.Aggregate(Money.Zero(Currency.Eur), (a, r) => a + r.CostBasisEur);
        var totalUnreal = totalValue - totalCost;
        var totalReal   = rows.Aggregate(Money.Zero(Currency.Eur), (a, r) => a + r.RealizedPnLEur);

        var pct = goalTarget.Amount == 0m
            ? 0m
            : totalValue.Amount / goalTarget.Amount * 100m;

        var legacyShares = rows.Count == 1 ? rows[0].Quantity.Value : 0m;
        var legacyAvgCost = (rows.Count == 1 && legacyShares > 0m)
            ? Money.Of(rows[0].CostBasisEur.Amount / legacyShares, Currency.Eur)
            : Money.Zero(Currency.Eur);

        return new PortfolioSnapshot(
            Positions:        rows,
            CurrentValueEur:  totalValue,
            CostBasisEur:     totalCost,
            UnrealizedPnLEur: totalUnreal,
            RealizedPnLEur:   totalReal,
            ProgressPct:      pct,
            Shares:           legacyShares,
            AvgCostEur:       legacyAvgCost);
    }
}
```

- [ ] **Step 6: Build the solution to surface compile errors**

```bash
dotnet build TradyStrat.slnx -c Debug 2>&1 | tail -30
```
Expected: errors in test files and call sites referencing the old `TradeRecorded.CreatedPosition` field or the old `DeleteTrade(TradeId)` 1-arg signature.

- [ ] **Step 7: Update `PortfolioBehaviorTests.cs` — adapt to events**

Open `TradyStrat.Domain.Tests/Portfolio/PortfolioBehaviorTests.cs` and make the following edits:

- Add `using TradyStrat.Domain.Portfolio.Events;` at the top.
- Replace every `result.CreatedPosition.ShouldBeTrue();` with `portfolio.DomainEvents.OfType<PositionOpened>().Any().ShouldBeTrue();`.
- Replace every `result.CreatedPosition.ShouldBeFalse();` with `portfolio.DomainEvents.OfType<PositionOpened>().Count(e => e.PositionId == /* relevant id */).ShouldBe(0);` — or, simpler, drain events between calls: `portfolio.DequeueDomainEvents();` after the first `RecordTrade` so the second call's events are inspected in isolation.
- Replace `PortfolioAr.Empty(PortfolioId.Singleton)` with `PortfolioAr.Empty(PortfolioId.Singleton, _now)` to use the new event-raising overload (or with `PortfolioAr.Empty(PortfolioId.Singleton)` plus a separate `portfolio.DequeueDomainEvents()` to discard the PortfolioCreated when the test isn't asserting it).
- Replace every call to `portfolio.DeleteTrade(tradeId)` with `portfolio.DeleteTrade(tradeId, _now)`.

> The exact diff varies per test; the rule is mechanical: drain events when irrelevant, assert their presence when relevant.

- [ ] **Step 8: Run the Portfolio test suite**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter "FullyQualifiedName~Portfolio"
```
Expected: all pass. If any test fails, edit it per the rule in Step 7. Do not modify production code to make a test pass — the production behaviour is correct; the test must adapt.

- [ ] **Step 9: Adapt other Portfolio-aware tests**

Run a build of the whole solution to catch remaining call sites:
```bash
dotnet build TradyStrat.slnx -c Debug 2>&1 | grep -E "error CS" | head -20
```
Expected: any remaining errors are in `TradyStrat.Application/Trades/UseCases/` files or tests. **Do not touch the use cases yet** (that's Task 9). For now, in `LogTradeUseCase.cs`, `DeleteTradeUseCase.cs`, `ImportTradesCsvUseCase.cs`:
- The use-case base's `TOutput` was `TradeRecorded` / `TradeDeleted`. Those types now live in `TradyStrat.Domain.Portfolio.Events`. Add `using TradyStrat.Domain.Portfolio.Events;` to each file.
- `DeleteTradeUseCase` calls `portfolio.DeleteTrade(new TradeId(input.Id))` — change to `portfolio.DeleteTrade(new TradeId(input.Id), clock.UtcNow())` and inject `IClock` (existing parameter on most use cases; add if missing).
- The return shape is unchanged: `TradeRecorded` / `TradeDeleted` records, just from a different namespace. Field shapes are different (no `CreatedPosition`); update any consumer that read it. Use `grep` to confirm there are no remaining `.CreatedPosition` references:
```bash
grep -rn "\.CreatedPosition" --include="*.cs" . | grep -v -E "(bin|obj)"
```
Expected: no output.

- [ ] **Step 10: Run the full suite**

```bash
dotnet test TradyStrat.slnx --nologo 2>&1 | tail -5
```
Expected: all pass.

- [ ] **Step 11: Commit**

```bash
git add TradyStrat.Domain/Portfolio TradyStrat.Domain.Tests/Portfolio TradyStrat.Application/Trades
git commit -m "$(cat <<'EOF'
refactor(domain): migrate Portfolio AR to AggregateRoot<PortfolioId> with events — Phase 7 Task 4

Portfolio/Position/Trade now inherit Entity/AggregateRoot. The old
returned-event records (TradeRecorded, TradeDeleted) move to
Portfolio/Events/ and inherit DomainEvent. New events: PortfolioCreated
(raised by Empty(id, now)) and PositionOpened (raised when RecordTrade
creates a new position). ImportTrades rollback now calls
ClearDomainEvents() before re-throwing. DeleteTrade gains a `now`
parameter for OccurredAt stamping. The `bool CreatedPosition` flag is
replaced by the presence of PositionOpened in DomainEvents.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Suggestion AR migration

**Files:**
- Modify: `TradyStrat.Domain/Suggestions/Suggestion.cs`
- Create: `TradyStrat.Domain/Suggestions/Events/SuggestionCreated.cs`
- Modify: `TradyStrat.Domain.Tests/Suggestions/SuggestionFactoryTests.cs`

- [ ] **Step 1: Create the SuggestionCreated event**

Create `TradyStrat.Domain/Suggestions/Events/SuggestionCreated.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Suggestions.Events;

public sealed record SuggestionCreated(
    SuggestionId     SuggestionId,
    InstrumentId     InstrumentId,
    DateOnly         ForDate,
    SuggestionAction Action,
    DateTime         OccurredAt) : DomainEvent
{ public DateTime OccurredAt { get; init; } = OccurredAt; }
```

- [ ] **Step 2: Update `Suggestion.cs` to inherit `AggregateRoot<SuggestionId>` and raise**

Edit `TradyStrat.Domain/Suggestions/Suggestion.cs`:

Replace the class declaration line:
```csharp
public sealed class Suggestion
{
    public SuggestionId      Id            { get; private set; }
```
with:
```csharp
public sealed class Suggestion : AggregateRoot<SuggestionId>
{
```
(Delete the `public SuggestionId Id { get; private set; }` line — it's inherited.)

Add `using TradyStrat.Domain.SeedWork;` and `using TradyStrat.Domain.Suggestions.Events;` to the file's using block.

Replace the private constructor's `Id = SuggestionId.New();` line with a call to the base ctor — i.e. change:
```csharp
private Suggestion(
    InstrumentId instrumentId, DateOnly forDate, SuggestionAction action,
    Quantity quantityHint, Price maxPriceHint, Conviction conviction,
    string rationale, IReadOnlyList<Citation> citations,
    MarketSnapshot snapshot, PromptFingerprint fingerprint, string thinkingText,
    DateTime createdAt)
{
    // Id is DB-assigned via ValueGeneratedOnAdd. SuggestionId.New() returns
    // the zero sentinel — EF rewrites it on insert.
    Id           = SuggestionId.New();
    InstrumentId = instrumentId;
```
to:
```csharp
private Suggestion(
    InstrumentId instrumentId, DateOnly forDate, SuggestionAction action,
    Quantity quantityHint, Price maxPriceHint, Conviction conviction,
    string rationale, IReadOnlyList<Citation> citations,
    MarketSnapshot snapshot, PromptFingerprint fingerprint, string thinkingText,
    DateTime createdAt)
    : base(SuggestionId.New())
{
    InstrumentId = instrumentId;
```

At the end of the `From(...)` static factory, *after* the `return new Suggestion(...)` line is reached, raise the event. Concretely, change:
```csharp
return new Suggestion(
    instrumentId, forDate, action, quantityHint, maxPriceHint, conviction,
    rationale, citations ?? [], snapshot, fingerprint, thinkingText ?? "",
    createdAt);
```
to:
```csharp
var s = new Suggestion(
    instrumentId, forDate, action, quantityHint, maxPriceHint, conviction,
    rationale, citations ?? [], snapshot, fingerprint, thinkingText ?? "",
    createdAt);
s.Raise(new Events.SuggestionCreated(s.Id, instrumentId, forDate, action, createdAt));
return s;
```

> **Note:** `Raise` is `protected` on `AggregateRoot<TId>`. Calling it on a Suggestion instance from within a static method on the same class works because the call site is inside the class's own scope. If the compiler complains, expose a private helper method `private void RaiseCreated(...)` and call it from the factory.

- [ ] **Step 3: Add a `protected internal void RaiseFromFactory` helper if needed**

If Step 2 yielded `CS0122: 'AggregateRoot<TId>.Raise(IDomainEvent)' is inaccessible due to its protection level`, add a private instance method to `Suggestion`:
```csharp
private void RaiseCreated(SuggestionCreated evt) => Raise(evt);
```
and call `s.RaiseCreated(...)` from the factory. The original `protected` access is preserved for subclasses.

- [ ] **Step 4: Build and run Suggestion tests**

```bash
dotnet build TradyStrat.slnx -c Debug 2>&1 | tail -5
dotnet test TradyStrat.Domain.Tests --nologo --filter "FullyQualifiedName~Suggestion"
```
Expected: build green; suggestion tests pass.

- [ ] **Step 5: Add a SuggestionCreated assertion to `SuggestionFactoryTests.cs`**

Open `TradyStrat.Domain.Tests/Suggestions/SuggestionFactoryTests.cs` and add a new `[Fact]`:
```csharp
[Fact]
public void From_raises_SuggestionCreated_event()
{
    var s = Suggestion.From(
        instrumentId:   new InstrumentId(7),
        forDate:        new DateOnly(2026, 5, 25),
        action:         SuggestionAction.Acquire,
        quantityHint:   Quantity.None,
        maxPriceHint:   Price.None(Currency.Eur),
        conviction:     Conviction.Of(3),
        rationale:      "test",
        citations:      [],
        snapshot:       MarketSnapshot.Empty,
        fingerprint:    PromptFingerprint.Of("h", null, null),
        thinkingText:   "",
        createdAt:      new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));

    var evt = s.DomainEvents.OfType<SuggestionCreated>().ShouldHaveSingleItem();
    evt.InstrumentId.ShouldBe(new InstrumentId(7));
    evt.Action.ShouldBe(SuggestionAction.Acquire);
    evt.OccurredAt.ShouldBe(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));
}
```
Add `using TradyStrat.Domain.Suggestions.Events;` to the using block.

- [ ] **Step 6: Run the new test**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter "From_raises_SuggestionCreated_event"
```
Expected: pass.

- [ ] **Step 7: Run full suite**

```bash
dotnet test TradyStrat.slnx --nologo 2>&1 | tail -5
```
Expected: all pass.

- [ ] **Step 8: Commit**

```bash
git add TradyStrat.Domain/Suggestions TradyStrat.Domain.Tests/Suggestions
git commit -m "$(cat <<'EOF'
refactor(domain): migrate Suggestion AR to AggregateRoot<SuggestionId> — Phase 7 Task 5

Suggestion inherits AggregateRoot<SuggestionId>. The From factory now
raises SuggestionCreated after validation. The Id property is inherited
from Entity<TId>; the private ctor passes it to base.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Goal AR migration

**Files:**
- Modify: `TradyStrat.Domain/Goals/Goal.cs`
- Create: `TradyStrat.Domain/Goals/Events/GoalCreated.cs`
- Create: `TradyStrat.Domain/Goals/Events/GoalTargetChanged.cs`
- Create: `TradyStrat.Domain/Goals/Events/GoalDeadlineRescheduled.cs`
- Modify: `TradyStrat.Domain.Tests/Goals/GoalTests.cs`

- [ ] **Step 1: Create the three event records**

Create `TradyStrat.Domain/Goals/Events/GoalCreated.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Goals.Events;

public sealed record GoalCreated(GoalId GoalId, Money Target, DateTime OccurredAt) : DomainEvent
{ public DateTime OccurredAt { get; init; } = OccurredAt; }
```

Create `TradyStrat.Domain/Goals/Events/GoalTargetChanged.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Goals.Events;

public sealed record GoalTargetChanged(
    GoalId   GoalId,
    Money    OldTarget,
    Money    NewTarget,
    DateTime OccurredAt) : DomainEvent
{ public DateTime OccurredAt { get; init; } = OccurredAt; }
```

Create `TradyStrat.Domain/Goals/Events/GoalDeadlineRescheduled.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Goals.Events;

public sealed record GoalDeadlineRescheduled(
    GoalId   GoalId,
    DateOnly OldDeadline,
    DateOnly NewDeadline,
    DateTime OccurredAt) : DomainEvent
{ public DateTime OccurredAt { get; init; } = OccurredAt; }
```

- [ ] **Step 2: Rewrite `Goal.cs`**

Replace `TradyStrat.Domain/Goals/Goal.cs`:
```csharp
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Goals.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed class Goal : AggregateRoot<GoalId>
{
    public Money    Target     { get; private set; } = Money.Zero(Currency.Eur);
    public DateOnly TargetDate { get; private set; } = DateOnly.MinValue;
    public DateTime UpdatedAt  { get; private set; }

    public bool HasDeadline => TargetDate != DateOnly.MinValue;

    private Goal() { }   // EF
    private Goal(GoalId id, Money target, DateOnly targetDate, DateTime updatedAt)
        : base(id)
    {
        Target     = target;
        TargetDate = targetDate;
        UpdatedAt  = updatedAt;
    }

    public static Goal Initial(IClock clock)
    {
        var now = clock.UtcNow();
        var target = Money.Of(1_000_000m, Currency.Eur);
        var g = new Goal(GoalId.Singleton, target, DateOnly.MinValue, now);
        g.Raise(new GoalCreated(GoalId.Singleton, target, now));
        return g;
    }

    public static Goal Existing(GoalId id, Money target, DateOnly targetDate, DateTime updatedAt)
        => new(id, target, targetDate, updatedAt);

    public void RetargetAmount(Money newTarget, IClock clock)
    {
        if (newTarget.IsEmpty || newTarget.Amount <= 0m)
            throw new SettingValidationException(
                $"Target must be positive (was {newTarget}).");
        var oldTarget = Target;
        Target = newTarget;
        var now = clock.UtcNow();
        UpdatedAt = now;
        Raise(new GoalTargetChanged(Id, oldTarget, newTarget, now));
    }

    public void RescheduleDeadline(DateOnly newDeadline, IClock clock)
    {
        if (newDeadline != DateOnly.MinValue)
        {
            var today = clock.TodayLocal();
            if (newDeadline < today)
                throw new SettingValidationException(
                    $"Deadline must be today or later (was {newDeadline:O}, today is {today:O}).");
        }
        var oldDeadline = TargetDate;
        TargetDate = newDeadline;
        var now = clock.UtcNow();
        UpdatedAt = now;
        Raise(new GoalDeadlineRescheduled(Id, oldDeadline, newDeadline, now));
    }
}
```

- [ ] **Step 3: Run the Goal tests**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter "FullyQualifiedName~GoalTests"
```
Expected: all existing tests pass.

- [ ] **Step 4: Add three new event-assertion tests**

In `TradyStrat.Domain.Tests/Goals/GoalTests.cs`, add `using TradyStrat.Domain.Goals.Events;` and append:
```csharp
[Fact]
public void Initial_raises_GoalCreated()
{
    var clock = new TradyStrat.TestKit.Time.FakeClock(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));
    var g = Goal.Initial(clock);
    var evt = g.DomainEvents.OfType<GoalCreated>().ShouldHaveSingleItem();
    evt.Target.ShouldBe(Money.Of(1_000_000m, Currency.Eur));
    evt.OccurredAt.ShouldBe(clock.UtcNow());
}

[Fact]
public void RetargetAmount_raises_GoalTargetChanged_with_old_and_new()
{
    var clock = new TradyStrat.TestKit.Time.FakeClock(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));
    var g = Goal.Initial(clock);
    g.DequeueDomainEvents();   // discard GoalCreated

    g.RetargetAmount(Money.Of(2_000_000m, Currency.Eur), clock);

    var evt = g.DomainEvents.OfType<GoalTargetChanged>().ShouldHaveSingleItem();
    evt.OldTarget.ShouldBe(Money.Of(1_000_000m, Currency.Eur));
    evt.NewTarget.ShouldBe(Money.Of(2_000_000m, Currency.Eur));
}

[Fact]
public void RescheduleDeadline_raises_GoalDeadlineRescheduled_with_old_and_new()
{
    var clock = new TradyStrat.TestKit.Time.FakeClock(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));
    var g = Goal.Initial(clock);
    g.DequeueDomainEvents();

    var newDate = new DateOnly(2030, 1, 1);
    g.RescheduleDeadline(newDate, clock);

    var evt = g.DomainEvents.OfType<GoalDeadlineRescheduled>().ShouldHaveSingleItem();
    evt.OldDeadline.ShouldBe(DateOnly.MinValue);
    evt.NewDeadline.ShouldBe(newDate);
}
```

> **Note:** confirm the FakeClock namespace and ctor signature via `cat TradyStrat.TestKit/Time/FakeClock.cs`. Adapt the constructor call if it differs.

- [ ] **Step 5: Run Goal tests and full suite**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter "FullyQualifiedName~GoalTests"
dotnet test TradyStrat.slnx --nologo 2>&1 | tail -5
```
Expected: pass.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat.Domain/Goals TradyStrat.Domain.Tests/Goals
git commit -m "$(cat <<'EOF'
refactor(domain): migrate Goal AR to AggregateRoot<GoalId> with events — Phase 7 Task 6

Goal inherits AggregateRoot<GoalId>. Initial raises GoalCreated;
RetargetAmount raises GoalTargetChanged (with OldTarget captured before
mutation); RescheduleDeadline raises GoalDeadlineRescheduled (with
OldDeadline captured). Existing is rehydration — does not raise.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: Instrument AR migration

**Files:**
- Modify: `TradyStrat.Domain/Instruments/Instrument.cs`
- Create: `TradyStrat.Domain/Instruments/Events/InstrumentProbed.cs`
- Create: `TradyStrat.Domain/Instruments/Events/InstrumentConfirmed.cs`
- Create: `TradyStrat.Domain/Instruments/Events/InstrumentRenamed.cs`
- Modify: `TradyStrat.Domain.Tests/Instruments/InstrumentTests.cs`

- [ ] **Step 1: Create the three event records**

Create `TradyStrat.Domain/Instruments/Events/InstrumentProbed.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Instruments.Events;

public sealed record InstrumentProbed(
    string   Ticker,
    Currency Currency,
    Exchange Exchange,
    DateTime OccurredAt) : DomainEvent
{ public DateTime OccurredAt { get; init; } = OccurredAt; }
```

Create `TradyStrat.Domain/Instruments/Events/InstrumentConfirmed.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Instruments.Events;

public sealed record InstrumentConfirmed(InstrumentId InstrumentId, DateTime OccurredAt) : DomainEvent
{ public DateTime OccurredAt { get; init; } = OccurredAt; }
```

Create `TradyStrat.Domain/Instruments/Events/InstrumentRenamed.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Instruments.Events;

public sealed record InstrumentRenamed(
    InstrumentId InstrumentId,
    string       OldName,
    string       NewName,
    DateTime     OccurredAt) : DomainEvent
{ public DateTime OccurredAt { get; init; } = OccurredAt; }
```

- [ ] **Step 2: Rewrite `Instrument.cs`**

Replace `TradyStrat.Domain/Instruments/Instrument.cs`:
```csharp
using TradyStrat.Domain.Instruments.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed class Instrument : AggregateRoot<InstrumentId>
{
    public string         Ticker    { get; private set; } = "";
    public string         Name      { get; private set; } = "";
    public Currency       Currency  { get; private set; } = Currency.Eur;
    public Exchange       Exchange  { get; private set; } = Exchange.Of("UNKNOWN");
    public TimezoneId     Timezone  { get; private set; } = TimezoneId.Of("UTC");
    public InstrumentKind Kind      { get; private set; }
    public DateTime       AddedAt   { get; private set; }

    private Instrument() { }   // EF

    private Instrument(
        InstrumentId id, string ticker, string name,
        Currency currency, Exchange exchange, TimezoneId timezone,
        InstrumentKind kind, DateTime addedAt)
        : base(id)
    {
        Ticker    = ticker;
        Name      = name;
        Currency  = currency;
        Exchange  = exchange;
        Timezone  = timezone;
        Kind      = kind;
        AddedAt   = addedAt;
    }

    public static Instrument Probed(
        string ticker, string name,
        Currency currency, Exchange exchange, TimezoneId timezoneId,
        InstrumentKind kind,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("Ticker is required.", nameof(ticker));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        var normalisedTicker = ticker.Trim().ToUpperInvariant();
        var inst = new Instrument(
            id:       InstrumentId.New(),
            ticker:   normalisedTicker,
            name:     name.Trim(),
            currency: currency,
            exchange: exchange,
            timezone: timezoneId,
            kind:     kind,
            addedAt:  default);
        inst.Raise(new InstrumentProbed(normalisedTicker, currency, exchange, now));
        return inst;
    }

    public static Instrument Existing(
        InstrumentId id, string ticker, string name,
        Currency currency, Exchange exchange, TimezoneId timezoneId,
        InstrumentKind kind, DateTime addedAt)
        => new(id, ticker, name, currency, exchange, timezoneId, kind, addedAt);

    public void Confirm(IClock clock)
    {
        if (AddedAt != default)
            throw new InvalidOperationException(
                $"Instrument '{Ticker}' is already confirmed (AddedAt = {AddedAt:O}).");
        var now = clock.UtcNow();
        AddedAt = now;
        Raise(new InstrumentConfirmed(Id, now));
    }

    public void Rename(string newName, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name must not be empty.", nameof(newName));
        var trimmed = newName.Trim();
        if (trimmed == Name) return;
        var oldName = Name;
        Name = trimmed;
        Raise(new InstrumentRenamed(Id, oldName, trimmed, clock.UtcNow()));
    }
}
```

> **Note:** `Probed` now requires a `DateTime now` parameter and `Rename` now takes an `IClock`. Existing callers must adapt — see Step 4.

- [ ] **Step 3: Build to find broken call sites**

```bash
dotnet build TradyStrat.slnx -c Debug 2>&1 | grep "error CS" | head -20
```
Expected: errors in any code that calls `Instrument.Probed(...)` or `instrument.Rename(name)` without the new parameters.

- [ ] **Step 4: Fix each broken call site**

Update each call site to pass `clock.UtcNow()` (for `Probed`) or `clock` (for `Rename`). Most call sites are in Infrastructure providers (`Yahoo*Probe`) and CLI commands. The change is mechanical: thread `IClock` through to the call.

For tests, change e.g.:
```csharp
Instrument.Probed("MSFT", "Microsoft", Currency.Usd, Exchange.Of("NASDAQ"), TimezoneId.Of("America/New_York"), InstrumentKind.Stock);
```
to:
```csharp
Instrument.Probed("MSFT", "Microsoft", Currency.Usd, Exchange.Of("NASDAQ"), TimezoneId.Of("America/New_York"), InstrumentKind.Stock, _now);
```
where `_now` is a test-fixture DateTime constant.

- [ ] **Step 5: Add three event-assertion tests to `InstrumentTests.cs`**

Open `TradyStrat.Domain.Tests/Instruments/InstrumentTests.cs`, add `using TradyStrat.Domain.Instruments.Events;` and append:
```csharp
[Fact]
public void Probed_raises_InstrumentProbed()
{
    var inst = Instrument.Probed(
        "msft", "Microsoft",
        Currency.Usd, Exchange.Of("NASDAQ"), TimezoneId.Of("America/New_York"),
        InstrumentKind.Stock,
        new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));

    var evt = inst.DomainEvents.OfType<InstrumentProbed>().ShouldHaveSingleItem();
    evt.Ticker.ShouldBe("MSFT");
    evt.Currency.ShouldBe(Currency.Usd);
}

[Fact]
public void Confirm_raises_InstrumentConfirmed()
{
    var clock = new TradyStrat.TestKit.Time.FakeClock(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));
    var inst = Instrument.Probed("MSFT", "Microsoft", Currency.Usd, Exchange.Of("NASDAQ"),
        TimezoneId.Of("America/New_York"), InstrumentKind.Stock, clock.UtcNow());
    inst.DequeueDomainEvents();

    inst.Confirm(clock);

    inst.DomainEvents.OfType<InstrumentConfirmed>().ShouldHaveSingleItem()
        .OccurredAt.ShouldBe(clock.UtcNow());
}

[Fact]
public void Rename_raises_InstrumentRenamed_with_old_and_new_names()
{
    var clock = new TradyStrat.TestKit.Time.FakeClock(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));
    var inst = Instrument.Probed("MSFT", "Microsoft", Currency.Usd, Exchange.Of("NASDAQ"),
        TimezoneId.Of("America/New_York"), InstrumentKind.Stock, clock.UtcNow());
    inst.DequeueDomainEvents();

    inst.Rename("Microsoft Corp", clock);

    var evt = inst.DomainEvents.OfType<InstrumentRenamed>().ShouldHaveSingleItem();
    evt.OldName.ShouldBe("Microsoft");
    evt.NewName.ShouldBe("Microsoft Corp");
}
```

- [ ] **Step 6: Run Instrument tests + full suite**

```bash
dotnet test TradyStrat.Domain.Tests --nologo --filter "FullyQualifiedName~InstrumentTests"
dotnet test TradyStrat.slnx --nologo 2>&1 | tail -5
```
Expected: all pass.

- [ ] **Step 7: Commit**

```bash
git add TradyStrat.Domain/Instruments TradyStrat.Domain.Tests/Instruments
# also git add any provider/CLI files modified for the new signatures
git add TradyStrat.Infrastructure TradyStrat.Cli TradyStrat.Application 2>/dev/null
git commit -m "$(cat <<'EOF'
refactor(domain): migrate Instrument AR to AggregateRoot<InstrumentId> — Phase 7 Task 7

Instrument inherits AggregateRoot<InstrumentId>. Probed raises
InstrumentProbed (gains a `now` parameter), Confirm raises
InstrumentConfirmed, Rename raises InstrumentRenamed (gains IClock and
captures OldName before mutating). Existing is rehydration — does not
raise. Callers (provider probes, CLI commands) thread IClock through.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: Repository contract — return events from save/add methods

**Files:**
- Modify: `TradyStrat.Application/Portfolio/IPortfolioRepository.cs`
- Modify: `TradyStrat.Application/Goals/IGoalRepository.cs`
- Modify: `TradyStrat.Application/AiSuggestion/ISuggestionRepository.cs`
- Modify: `TradyStrat.Application/Settings/IInstrumentRepository.cs`
- Modify: `TradyStrat.Infrastructure/Portfolio/EfPortfolioRepository.cs`
- Modify: `TradyStrat.Infrastructure/Goals/EfGoalRepository.cs`
- Modify: `TradyStrat.Infrastructure/AiSuggestion/EfSuggestionRepository.cs`
- Modify: `TradyStrat.Infrastructure/Settings/EfInstrumentRepository.cs`

- [ ] **Step 1: Update `IPortfolioRepository.cs`**

Replace `TradyStrat.Application/Portfolio/IPortfolioRepository.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Application.Portfolio;

public interface IPortfolioRepository
{
    Task<PortfolioAr> GetAsync(CancellationToken ct);

    /// <summary>
    /// Persists the AR's pending changes and returns the drained domain
    /// events for the caller to dispatch. After this call the AR's
    /// DomainEvents is empty.
    /// </summary>
    Task<IReadOnlyList<IDomainEvent>> SaveAsync(PortfolioAr portfolio, CancellationToken ct);
}
```

- [ ] **Step 2: Update `EfPortfolioRepository.cs`**

Replace `TradyStrat.Infrastructure/Portfolio/EfPortfolioRepository.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Portfolio;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Data;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Infrastructure.Portfolio;

public sealed class EfPortfolioRepository(AppDbContext db) : IPortfolioRepository
{
    public async Task<PortfolioAr> GetAsync(CancellationToken ct)
    {
        var portfolio = await db.Portfolios
            .Include("_positions._openLots")
            .Include("_positions._trades")
            .SingleOrDefaultAsync(p => p.Id == PortfolioId.Singleton, ct);

        if (portfolio is null)
        {
            // First-creation path: stamp the PortfolioCreated event.
            // We don't have IClock here; pass DateTime.UtcNow.
            portfolio = PortfolioAr.Empty(PortfolioId.Singleton, DateTime.UtcNow);
            db.Portfolios.Add(portfolio);
            await db.SaveChangesAsync(ct);
            portfolio.DequeueDomainEvents();   // discard bootstrap event — never dispatched
            return portfolio;
        }

        if (portfolio.RehydrateLots())
            await db.SaveChangesAsync(ct);

        return portfolio;
    }

    public async Task<IReadOnlyList<IDomainEvent>> SaveAsync(PortfolioAr portfolio, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        return portfolio.DequeueDomainEvents();
    }
}
```

> **Note on the bootstrap PortfolioCreated**: the first-creation path in `GetAsync` raises and immediately drains the event without dispatching. This is deliberate — the singleton-portfolio bootstrap is not a domain-meaningful event for any handler. If a handler later needs it, fold that handler into the same path.

- [ ] **Step 3: Update `IGoalRepository.cs`**

Replace `TradyStrat.Application/Goals/IGoalRepository.cs`:
```csharp
using TradyStrat.Domain;
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Application.Goals;

public interface IGoalRepository
{
    Task<Goal> GetAsync(CancellationToken ct);
    Task<IReadOnlyList<IDomainEvent>> SaveAsync(Goal goal, CancellationToken ct);
}
```

- [ ] **Step 4: Update `EfGoalRepository.cs`**

Replace `TradyStrat.Infrastructure/Goals/EfGoalRepository.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Goals;
using TradyStrat.Domain;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Goals;

public sealed class EfGoalRepository(AppDbContext db, IClock clock) : IGoalRepository
{
    public async Task<Goal> GetAsync(CancellationToken ct)
    {
        var goal = await db.Goals.SingleOrDefaultAsync(ct);
        if (goal is not null) return goal;

        var initial = Goal.Initial(clock);
        db.Goals.Add(initial);
        await db.SaveChangesAsync(ct);
        initial.DequeueDomainEvents();   // discard bootstrap GoalCreated
        return initial;
    }

    public async Task<IReadOnlyList<IDomainEvent>> SaveAsync(Goal goal, CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        return goal.DequeueDomainEvents();
    }
}
```

- [ ] **Step 5: Update `ISuggestionRepository.cs`**

Replace `TradyStrat.Application/AiSuggestion/ISuggestionRepository.cs`:
```csharp
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion;

public interface ISuggestionRepository
{
    Task<Suggestion?> GetForAsync(InstrumentId instrumentId, DateOnly date, CancellationToken ct);

    Task<IReadOnlyList<Suggestion>> ListForAsync(InstrumentId instrumentId, DateRange range, CancellationToken ct);

    Task<Suggestion?> LatestForAsync(InstrumentId instrumentId, CancellationToken ct);

    Task<IReadOnlyList<Suggestion>> QueryAsync(
        InstrumentId? instrumentId,
        DateRange? range,
        SuggestionAction? action,
        int take,
        CancellationToken ct);

    Task<Suggestion?> PriorToAsync(InstrumentId instrumentId, DateOnly before, CancellationToken ct);

    Task<IReadOnlyList<Suggestion>> RecentForAsync(
        InstrumentId instrumentId, DateOnly asOf, int count, CancellationToken ct);

    /// <summary>Persists the new Suggestion and returns its drained domain events.</summary>
    Task<IReadOnlyList<IDomainEvent>> AddAsync(Suggestion suggestion, CancellationToken ct);

    /// <summary>Removes the suggestion. Returns no events (Suggestion has no removed-event today).</summary>
    Task RemoveAsync(Suggestion suggestion, CancellationToken ct);
}
```

- [ ] **Step 6: Update `EfSuggestionRepository.cs`**

In `TradyStrat.Infrastructure/AiSuggestion/EfSuggestionRepository.cs`, change the `AddAsync` method (keep everything else):
```csharp
public async Task<IReadOnlyList<IDomainEvent>> AddAsync(Suggestion suggestion, CancellationToken ct)
{
    db.Suggestions.Add(suggestion);
    await db.SaveChangesAsync(ct);
    return suggestion.DequeueDomainEvents();
}
```
Add `using TradyStrat.Domain.SeedWork;` to the file's usings.

- [ ] **Step 7: Update `IInstrumentRepository.cs`**

Replace `TradyStrat.Application/Settings/IInstrumentRepository.cs`:
```csharp
using TradyStrat.Domain;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.Settings;

public interface IInstrumentRepository
{
    Task<Instrument?> GetAsync(InstrumentId id, CancellationToken ct);

    Task<Instrument?> FindByTickerAsync(string ticker, CancellationToken ct);

    Task<IReadOnlyList<Instrument>> ListAsync(CancellationToken ct);

    /// <summary>Throws DuplicateInstrumentException when Ticker already exists. Returns drained domain events.</summary>
    Task<IReadOnlyList<IDomainEvent>> AddAsync(Instrument instrument, CancellationToken ct);
}
```

- [ ] **Step 8: Update `EfInstrumentRepository.cs`**

In `TradyStrat.Infrastructure/Settings/EfInstrumentRepository.cs`, change `AddAsync`:
```csharp
public async Task<IReadOnlyList<IDomainEvent>> AddAsync(Instrument instrument, CancellationToken ct)
{
    var dup = await FindByTickerAsync(instrument.Ticker, ct);
    if (dup is not null)
        throw new DuplicateInstrumentException(
            $"Instrument '{instrument.Ticker}' is already tracked.");

    db.Instruments.Add(instrument);
    await db.SaveChangesAsync(ct);
    return instrument.DequeueDomainEvents();
}
```
Add `using TradyStrat.Domain.SeedWork;`.

- [ ] **Step 9: Build the solution — surface broken use-case call sites**

```bash
dotnet build TradyStrat.slnx -c Debug 2>&1 | grep "error CS" | head -20
```
Expected: errors in `LogTradeUseCase.cs`, `DeleteTradeUseCase.cs`, `ImportTradesCsvUseCase.cs`, `BackfillSuggestionsUseCase.cs`, `ForceRefetchSuggestionUseCase.cs`, `GetTodaysSuggestionUseCase.cs`, and any place that calls `IInstrumentRepository.AddAsync`. These get fixed in Task 9.

- [ ] **Step 10: Do NOT commit yet — Task 9 wires the use cases. Verify build is in expected state**

The build is intentionally red at the use-case layer. Task 9 fixes it. Do not commit a broken build.

---

## Task 9: Application use-case wiring + dispatcher

**Files:**
- Create: `TradyStrat.Infrastructure/SeedWork/DomainEventDispatcher.cs`
- Modify: Infrastructure DI bootstrap (one file — see Step 1)
- Modify: `TradyStrat.Application/Trades/UseCases/LogTradeUseCase.cs`
- Modify: `TradyStrat.Application/Trades/UseCases/DeleteTradeUseCase.cs`
- Modify: `TradyStrat.Application/Trades/UseCases/ImportTradesCsvUseCase.cs`
- Modify: `TradyStrat.Application/AiSuggestion/UseCases/BackfillSuggestionsUseCase.cs`
- Modify: `TradyStrat.Application/AiSuggestion/UseCases/ForceRefetchSuggestionUseCase.cs`
- Modify: `TradyStrat.Application/AiSuggestion/UseCases/GetTodaysSuggestionUseCase.cs`
- (Any callers of `IInstrumentRepository.AddAsync` similarly need 3-line updates)

- [ ] **Step 1: Locate the Infrastructure DI bootstrap**

```bash
grep -rn "AddScoped<IPortfolioRepository" --include="*.cs" /Users/philippe/repo/gh-phmatray/TradyStrat/TradyStrat.Infrastructure 2>/dev/null | head -3
```
Expected: one or two file matches identifying where Infrastructure services are registered. Call it `<INF_BOOTSTRAP>`. Open it.

- [ ] **Step 2: Create `DomainEventDispatcher.cs`**

Create `TradyStrat.Infrastructure/SeedWork/DomainEventDispatcher.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Infrastructure.SeedWork;

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
                if (h is null) continue;
                var task = (Task?)method.Invoke(h, [evt, ct]) ?? Task.CompletedTask;
                await task;
            }
        }
    }
}
```

- [ ] **Step 3: Register the dispatcher in DI**

In `<INF_BOOTSTRAP>`, add (next to the other `AddScoped` calls):
```csharp
services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
```
Add `using TradyStrat.Domain.SeedWork;` and `using TradyStrat.Infrastructure.SeedWork;` if not already present.

- [ ] **Step 4: Wire `LogTradeUseCase.cs`**

Replace `TradyStrat.Application/Trades/UseCases/LogTradeUseCase.cs`:
```csharp
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Portfolio.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.Trades.UseCases;

public sealed record LogTradeInput(
    int InstrumentId, DateOnly ExecutedOn, TradeSide Side,
    decimal Quantity, decimal PricePerShare, decimal FeesEur, string? Note);

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
        var instrument = await instruments.GetAsync(new InstrumentId(input.InstrumentId), ct)
            ?? throw new InstrumentNotFoundException($"Instrument {input.InstrumentId} not found.");
        var portfolio = await portfolios.GetAsync(ct);

        var quantity = Quantity.Of(input.Quantity);
        var price    = Price.Of(Money.Of(input.PricePerShare, instrument.Currency));
        var fees     = Money.Of(input.FeesEur, Currency.Eur);

        var result = portfolio.RecordTrade(
            instrument.Id,
            input.ExecutedOn, input.Side,
            quantity, price, fees,
            input.Note ?? "",
            clock.UtcNow());

        var events = await portfolios.SaveAsync(portfolio, ct);
        await dispatcher.DispatchAsync(events, ct);
        return result;
    }
}
```

- [ ] **Step 5: Wire `DeleteTradeUseCase.cs`**

Replace `TradyStrat.Application/Trades/UseCases/DeleteTradeUseCase.cs`:
```csharp
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.Trades.UseCases;

public sealed record DeleteTradeInput(int Id);

public sealed class DeleteTradeUseCase(
    IPortfolioRepository portfolios,
    IDomainEventDispatcher dispatcher,
    IClock clock,
    ILogger<DeleteTradeUseCase> log)
    : UseCaseBase<DeleteTradeInput, TradeDeleted>(log)
{
    protected override async Task<TradeDeleted> ExecuteCore(DeleteTradeInput input, CancellationToken ct)
    {
        var portfolio = await portfolios.GetAsync(ct);
        var result = portfolio.DeleteTrade(new TradeId(input.Id), clock.UtcNow());
        var events = await portfolios.SaveAsync(portfolio, ct);
        await dispatcher.DispatchAsync(events, ct);
        return result;
    }
}
```

- [ ] **Step 6: Wire `ImportTradesCsvUseCase.cs`**

Open the file at line 63 (existing `await portfolios.SaveAsync(portfolio, ct);`). Change it to:
```csharp
var events = await portfolios.SaveAsync(portfolio, ct);
await dispatcher.DispatchAsync(events, ct);
```
Add `IDomainEventDispatcher dispatcher` to the primary ctor parameters and `using TradyStrat.Domain.SeedWork;` to the usings.

- [ ] **Step 7: Wire each Suggestion use case**

For each of `BackfillSuggestionsUseCase.cs`, `ForceRefetchSuggestionUseCase.cs`, `GetTodaysSuggestionUseCase.cs`:

- Add `IDomainEventDispatcher dispatcher` to the primary ctor parameters.
- Add `using TradyStrat.Domain.SeedWork;` to the usings.
- Locate each `await suggestions.AddAsync(...)` call. Change:
```csharp
await suggestions.AddAsync(suggestion, ct);
```
to:
```csharp
var events = await suggestions.AddAsync(suggestion, ct);
await dispatcher.DispatchAsync(events, ct);
```

- [ ] **Step 8: Wire `IInstrumentRepository.AddAsync` callers**

Find them:
```bash
grep -rn "instruments\.AddAsync\|\.AddAsync.*Instrument" --include="*.cs" /Users/philippe/repo/gh-phmatray/TradyStrat/TradyStrat.Application /Users/philippe/repo/gh-phmatray/TradyStrat/TradyStrat.Cli /Users/philippe/repo/gh-phmatray/TradyStrat/TradyStrat 2>/dev/null | head -10
```
Apply the same 3-line edit pattern (inject `IDomainEventDispatcher`, capture events, dispatch).

- [ ] **Step 9: Build the solution**

```bash
dotnet build TradyStrat.slnx -c Debug 2>&1 | tail -5
```
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 10: Run the full test suite**

```bash
dotnet test TradyStrat.slnx --nologo 2>&1 | tail -5
```
Expected: all pass. Test doubles for repositories (if any exist for unit tests) may need their `AddAsync`/`SaveAsync` updated to return an empty event list — apply mechanically as the compiler points them out. The simplest valid implementation is `=> Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>())`.

- [ ] **Step 11: Commit Tasks 8+9 together**

```bash
git add TradyStrat.Application TradyStrat.Infrastructure
git commit -m "$(cat <<'EOF'
refactor: repository owns event drain; use cases dispatch via IDomainEventDispatcher — Phase 7 Tasks 8 & 9

IPortfolioRepository / IGoalRepository / ISuggestionRepository /
IInstrumentRepository: SaveAsync / AddAsync now return
IReadOnlyList<IDomainEvent>, drained from the AR after SaveChangesAsync
commits. Use cases (LogTradeUseCase, DeleteTradeUseCase,
ImportTradesCsvUseCase, three Suggestion use cases, Instrument add
callers) inject IDomainEventDispatcher and dispatch the returned events
through it. Infrastructure ships DomainEventDispatcher (reflection-based
handler resolution); registered scoped in DI. No handlers ship — the
infrastructure lands; first handler arrives with the feature that needs
it.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 10: Dispatcher unit tests + integration test

**Files:**
- Test: `TradyStrat.Infrastructure.Tests/SeedWork/DomainEventDispatcherTests.cs`
- Test: `TradyStrat.Application.Tests/Trades/LogTradeUseCaseDispatchTests.cs`

- [ ] **Step 1: Write the DomainEventDispatcher unit tests**

Create `TradyStrat.Infrastructure.Tests/SeedWork/DomainEventDispatcherTests.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Infrastructure.SeedWork;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.SeedWork;

public class DomainEventDispatcherTests
{
    private sealed record EventA(DateTime OccurredAt) : DomainEvent
    { public DateTime OccurredAt { get; init; } = OccurredAt; }
    private sealed record EventB(DateTime OccurredAt) : DomainEvent
    { public DateTime OccurredAt { get; init; } = OccurredAt; }

    private sealed class HandlerA : IDomainEventHandler<EventA>
    {
        public List<EventA> Received { get; } = new();
        public Task HandleAsync(EventA evt, CancellationToken ct)
        { Received.Add(evt); return Task.CompletedTask; }
    }

    private sealed class HandlerAFails : IDomainEventHandler<EventA>
    {
        public Task HandleAsync(EventA evt, CancellationToken ct)
            => throw new InvalidOperationException("boom");
    }

    private sealed class CatchAll : IDomainEventHandler<IDomainEvent>
    {
        public List<IDomainEvent> Received { get; } = new();
        public Task HandleAsync(IDomainEvent evt, CancellationToken ct)
        { Received.Add(evt); return Task.CompletedTask; }
    }

    private static IServiceProvider BuildSp(Action<IServiceCollection> register)
    {
        var sc = new ServiceCollection();
        register(sc);
        return sc.BuildServiceProvider();
    }

    [Fact]
    public async Task Dispatches_to_concrete_handler()
    {
        var handler = new HandlerA();
        var sp = BuildSp(sc => sc.AddSingleton<IDomainEventHandler<EventA>>(handler));
        var d = new DomainEventDispatcher(sp, NullLogger<DomainEventDispatcher>.Instance);

        await d.DispatchAsync([new EventA(DateTime.UtcNow)], TestContext.Current.CancellationToken);

        handler.Received.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Skips_unregistered_event_type()
    {
        var sp = BuildSp(_ => { });
        var d = new DomainEventDispatcher(sp, NullLogger<DomainEventDispatcher>.Instance);

        await Should.NotThrowAsync(() =>
            d.DispatchAsync([new EventA(DateTime.UtcNow)], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Bubbles_handler_exceptions()
    {
        var sp = BuildSp(sc => sc.AddSingleton<IDomainEventHandler<EventA>, HandlerAFails>());
        var d = new DomainEventDispatcher(sp, NullLogger<DomainEventDispatcher>.Instance);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            d.DispatchAsync([new EventA(DateTime.UtcNow)], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Catch_all_handler_for_IDomainEvent_is_NOT_invoked_for_concrete_events()
    {
        var catchAll = new CatchAll();
        var sp = BuildSp(sc => sc.AddSingleton<IDomainEventHandler<IDomainEvent>>(catchAll));
        var d = new DomainEventDispatcher(sp, NullLogger<DomainEventDispatcher>.Instance);

        await d.DispatchAsync([new EventA(DateTime.UtcNow)], TestContext.Current.CancellationToken);

        catchAll.Received.ShouldBeEmpty();
    }

    [Fact]
    public async Task Invokes_each_handler_once_per_event()
    {
        var h1 = new HandlerA();
        var h2 = new HandlerA();
        var sp = BuildSp(sc =>
        {
            sc.AddSingleton<IDomainEventHandler<EventA>>(h1);
            sc.AddSingleton<IDomainEventHandler<EventA>>(h2);
        });
        var d = new DomainEventDispatcher(sp, NullLogger<DomainEventDispatcher>.Instance);

        await d.DispatchAsync([new EventA(DateTime.UtcNow), new EventA(DateTime.UtcNow)],
            TestContext.Current.CancellationToken);

        h1.Received.Count.ShouldBe(2);
        h2.Received.Count.ShouldBe(2);
    }
}
```

- [ ] **Step 2: Run the dispatcher tests**

```bash
dotnet test TradyStrat.Infrastructure.Tests --nologo --filter DomainEventDispatcherTests
```
Expected: 5 tests pass.

- [ ] **Step 3: Write the LogTradeUseCase dispatch integration test**

Create `TradyStrat.Application.Tests/Trades/LogTradeUseCaseDispatchTests.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings;
using TradyStrat.Application.Trades.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Portfolio.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Infrastructure.Portfolio;
using TradyStrat.Infrastructure.Settings;
using TradyStrat.Infrastructure.SeedWork;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Application.Tests.Trades;

public class LogTradeUseCaseDispatchTests
{
    private sealed class TradeRecordedSpy : IDomainEventHandler<TradeRecorded>
    {
        public List<TradeRecorded> Received { get; } = new();
        public Task HandleAsync(TradeRecorded evt, CancellationToken ct)
        { Received.Add(evt); return Task.CompletedTask; }
    }

    [Fact]
    public async Task LogTrade_persists_and_dispatches_TradeRecorded()
    {
        await using var db = InMemoryDb.Create();

        // Seed the instrument
        db.Instruments.Add(Instrument.Existing(
            id:        new InstrumentId(42),
            ticker:    "MSFT",
            name:      "Microsoft",
            currency:  Currency.Usd,
            exchange:  Exchange.Of("NASDAQ"),
            timezoneId: TimezoneId.Of("America/New_York"),
            kind:      InstrumentKind.Stock,
            addedAt:   new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var spy = new TradeRecordedSpy();
        var sc = new ServiceCollection();
        sc.AddSingleton<AppDbContext>(db);
        sc.AddSingleton<IPortfolioRepository, EfPortfolioRepository>();
        sc.AddSingleton<IInstrumentRepository, EfInstrumentRepository>();
        sc.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc)));
        sc.AddSingleton<IDomainEventHandler<TradeRecorded>>(spy);
        sc.AddSingleton<IDomainEventDispatcher>(sp =>
            new DomainEventDispatcher(sp, NullLogger<DomainEventDispatcher>.Instance));
        var sp = sc.BuildServiceProvider();

        var uc = new LogTradeUseCase(
            sp.GetRequiredService<IPortfolioRepository>(),
            sp.GetRequiredService<IInstrumentRepository>(),
            sp.GetRequiredService<IDomainEventDispatcher>(),
            sp.GetRequiredService<IClock>(),
            NullLogger<LogTradeUseCase>.Instance);

        var result = await uc.ExecuteAsync(new LogTradeInput(
            InstrumentId: 42,
            ExecutedOn:   new DateOnly(2026, 5, 25),
            Side:         TradeSide.Buy,
            Quantity:     5m,
            PricePerShare:100m,
            FeesEur:      0m,
            Note:         "t1"),
            TestContext.Current.CancellationToken);

        // Persisted
        var portfolio = await sp.GetRequiredService<IPortfolioRepository>().GetAsync(TestContext.Current.CancellationToken);
        portfolio.Positions.ShouldHaveSingleItem().Trades.ShouldHaveSingleItem();
        // Dispatched
        spy.Received.ShouldHaveSingleItem().TradeId.ShouldBe(result.TradeId);
        // Drained
        portfolio.DomainEvents.ShouldBeEmpty();
    }
}
```

> **Note:** confirm `InMemoryDb.Create()` returns an `AppDbContext` with the same lifetime semantics expected by `EfPortfolioRepository`. If the helper produces a context that is disposed on enumeration end, restructure the test to keep the context alive for the whole scope (`await using` in outer scope).

- [ ] **Step 4: Run the integration test**

```bash
dotnet test TradyStrat.Application.Tests --nologo --filter LogTradeUseCaseDispatchTests
```
Expected: 1 test passes.

- [ ] **Step 5: Run the full suite**

```bash
dotnet test TradyStrat.slnx --nologo 2>&1 | tail -5
```
Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat.Infrastructure.Tests/SeedWork TradyStrat.Application.Tests/Trades
git commit -m "$(cat <<'EOF'
test: DomainEventDispatcher unit tests + LogTradeUseCase dispatch integration — Phase 7 Task 10

Five dispatcher unit tests cover: concrete-handler dispatch, missing
handler is a no-op, handler exceptions bubble, catch-all IDomainEvent
handlers are NOT matched for concrete events, and N-handlers-per-event
fan-out. New Application integration test proves
LogTradeUseCase persists + dispatches + drains.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 11: Final verification + tag + merge

- [ ] **Step 1: Run the full test suite and confirm baseline + new tests**

```bash
dotnet test TradyStrat.slnx --nologo 2>&1 | tail -5
```
Expected: `Passed: M` where M = baseline N + 14 (SeedWork) + 1 (round-trip) + ~12 (event assertions across ARs) + 5 (dispatcher) + 1 (LogTrade dispatch integration). All passing. Exact count varies based on which AR tests pre-existed.

- [ ] **Step 2: Build the full solution in Release**

```bash
dotnet build TradyStrat.slnx -c Release 2>&1 | tail -5
```
Expected: `Build succeeded`, 0 errors, 0 warnings (or only pre-existing warnings).

- [ ] **Step 3: Run a representative CLI smoke test**

```bash
dotnet run --project TradyStrat.Cli -- --help
```
Expected: Spectre.Console help output renders. (Per the project's CLI convention — see [[feedback_cli_tooling]].)

- [ ] **Step 4: Tag the phase**

From the worktree root:
```bash
git tag phase7-domain-ddd-polish-done
git log --oneline -15
```
Expected: tag created at `HEAD`. Recent log shows the Phase 7 commits.

- [ ] **Step 5: Push the branch and tag** (only if user approves)

This is a shared-state action — confirm with the user before running:
```bash
git push origin phase7-ddd-polish
git push origin phase7-domain-ddd-polish-done
```

- [ ] **Step 6: Merge to main** (only if user approves)

```bash
cd /Users/philippe/repo/gh-phmatray/TradyStrat
git checkout main
git merge --no-ff phase7-ddd-polish -m "Merge Phase 7: Domain DDD polish — seedwork + collected domain events"
dotnet test TradyStrat.slnx --nologo 2>&1 | tail -3
```
Expected: merge clean, tests green on main.

- [ ] **Step 7: Clean up the worktree**

```bash
git worktree remove .claude/worktrees/phase7-ddd-polish
git branch -d phase7-ddd-polish
```

- [ ] **Step 8: Update auto-memory**

Create `/Users/philippe/.claude/projects/-Users-philippe-repo-gh-phmatray-TradyStrat/memory/tradystrat_phase7_landed.md`:
```markdown
---
name: tradystrat-phase7-landed
description: Phase 7 Domain DDD polish landed — seedwork primitives + collected-and-dispatched domain events with repository-owned drain
metadata:
  type: project
---

Phase 7 (Domain DDD polish) completed 2026-05-25. Tag
`phase7-domain-ddd-polish-done`.

**Why:** Final phase of the DDD-rework arc started in Phase 1. Polishes
seedwork (Entity, AggregateRoot, ValueObject, IDomainEvent) and
converts returned-event records to collected-and-dispatched events.

**How to apply:**
- New ARs inherit `AggregateRoot<TId>` from `TradyStrat.Domain.SeedWork`.
- New events inherit `DomainEvent` (carries `Guid EventId` + UTC `OccurredAt`).
- Every new-creation factory raises a `*Created` event; rehydration never raises.
- Every mutating method captures `OldX` before mutating and raises a `*Changed` event.
- Repositories' `SaveAsync`/`AddAsync` return `IReadOnlyList<IDomainEvent>` —
  callers feed it to `IDomainEventDispatcher.DispatchAsync`.
- Handlers register against the concrete event closed-generic;
  catch-all `IDomainEventHandler<IDomainEvent>` is NOT matched by the dispatcher.
- Post-commit handler failures are unrecoverable today (no outbox);
  handlers must be in-process and idempotent on `EventId` until an
  outbox lands.

Closes DDD-rework arc (Phases 1–7). Links: [[tradystrat_phase6_landed]].
```

Then append to `MEMORY.md`:
```
- [Phase 7 Domain DDD polish landed](tradystrat_phase7_landed.md) — seedwork + collected domain events + repo-owned drain; completed 2026-05-25; tag phase7-domain-ddd-polish-done
```

---

## Notes for the executing agent

- **Run every test command and verify expected output before moving on.** Don't trust "should work."
- **Don't change EF configurations** unless a test explicitly requires it. The migration is designed to be EF-mapping-neutral.
- **If a test from the pre-existing suite fails after a refactor, the test is wrong, not the production code** (excepting the deliberately-rewritten assertions in Tasks 4–7). Adapt the test to the new event shape.
- **Don't squash commits.** Each task's commit boundary is the review checkpoint.
- **Per [[feedback_collaboration_style]]**: terse replies, isolated worktree, this plan is `/effort max` territory.
- **Per [[feedback_cli_tooling]]**: any new CLI surface uses Spectre.Console.Cli (none planned in Phase 7).
- **Per [[feedback_no_null_in_domain]]**: no nulls in domain; events carry typed values or sentinels.

When in doubt, re-read [`docs/superpowers/specs/2026-05-25-domain-ddd-polish-design.md`](../specs/2026-05-25-domain-ddd-polish-design.md) — it's the source of truth.
