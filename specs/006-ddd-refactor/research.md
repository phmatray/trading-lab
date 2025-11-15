# Phase 0 Research: Ardalis.SharedKernel DDD Integration

**Date**: 2025-01-15
**Status**: Complete

## Executive Summary

This research validates the feasibility of integrating Ardalis.SharedKernel into the TradingBot application for Domain-Driven Design (DDD) pattern adoption. Key findings:

✅ **EF Core 10 Compatibility**: EntityBase works seamlessly with Entity Framework Core 10 using standard property mapping
✅ **Domain Events**: MediatR integration provides robust event dispatching with before/after save hooks
✅ **Aggregate Identification**: Order, Position, and Account are independent aggregates with eventual consistency
✅ **Value Objects**: Existing records (Quote, SymbolInfo, RiskParameters) already follow value object patterns
✅ **Specifications**: Ardalis.Specification.EntityFrameworkCore provides type-safe query composition

**Recommendation**: Proceed with phased adoption starting with structural changes, then domain events, finally specifications.

---

## 1. Ardalis.SharedKernel Overview

**Package**: Ardalis.SharedKernel (NuGet)
**Purpose**: DDD base classes for Clean Architecture
**Key Components**:
- `EntityBase<TId>` - Base class for entities with identity
- `ValueObject` - Base class for immutable value objects
- `DomainEventBase` - Base class for domain events
- `IAggregateRoot` - Marker interface for aggregate roots
- `IRepository<T>` / `IReadRepository<T>` - Repository abstractions

---

## 2. Entity Framework Core 10 Integration

### 2.1 EntityBase<TId> Configuration

**ID Type Selection**:
- **Guid**: Recommended for TradingBot (globally unique, client-generated)
- **long**: High-volume scenarios with database-generated IDs
- **int**: Simple domains with auto-increment
- **string**: Custom ID formats (e.g., Account "ACC-12345")

**EF Core Mapping**:
```csharp
public sealed class Order : EntityBase<Guid>, IAggregateRoot
{
    // Id inherited from EntityBase<Guid>
    public required string Symbol { get; set; }
    // ...
}

// Entity configuration
builder.HasKey(o => o.Id); // Standard primary key
builder.Ignore(o => o.DomainEvents); // Don't persist events
```

**Compatibility with Current Schema**: ✅
- Existing `Guid Id` properties map directly to `EntityBase<Guid>.Id`
- No schema migration required
- SmartEnum conversions preserved

### 2.2 Domain Events Collection

All entities inheriting from `EntityBase` get a `DomainEvents` collection:

```csharp
[NotMapped]
public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
```

**Critical**: Always ignore this property in EF Core configurations:
```csharp
builder.Ignore(e => e.DomainEvents);
```

---

## 3. Domain Event Patterns

### 3.1 Event Lifecycle

1. **Registration**: Entity registers events during state changes
   ```csharp
   order.MarkAsFilled(price, commission, timestamp);
   // Internally calls: RegisterDomainEvent(new OrderFilledEvent(...))
   ```

2. **Dispatch**: DbContext dispatches events before/after SaveChanges
   ```csharp
   await _dbContext.SaveChangesAsync(); // Triggers event dispatch
   ```

3. **Handling**: MediatR routes events to registered handlers
   ```csharp
   public class OrderFilledEventHandler : INotificationHandler<OrderFilledEvent>
   {
       public async Task Handle(OrderFilledEvent evt, CancellationToken ct)
       {
           // Update portfolio positions
       }
   }
   ```

4. **Clearing**: Events cleared after dispatch to prevent re-firing

### 3.2 Before vs After SaveChanges

**Before SaveChanges (Recommended for TradingBot)**:
- ✅ Single transaction (all-or-nothing)
- ✅ Strong consistency for position updates
- ✅ Same DbContext scope
- ❌ Slower SaveChanges (handlers run synchronously)
- ❌ Handler failures rollback entire transaction

**After SaveChanges**:
- ✅ Faster saves
- ✅ Suitable for notifications (SignalR)
- ❌ Eventual consistency required
- ❌ Cannot rollback if handler fails

**Decision**: Use **before-save** for critical business logic (portfolio updates, risk checks), **after-save** for UI notifications.

### 3.3 MediatR Integration

```csharp
// Install package
dotnet add package MediatR

// Dispatcher implementation
public sealed class MediatorDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public async Task DispatchAndClearEvents(IEnumerable<IHasDomainEvents> entities)
    {
        foreach (var entity in entities)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                await _mediator.Publish(domainEvent);
            }
            entity.ClearDomainEvents();
        }
    }
}

// DbContext integration
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var entities = ChangeTracker.Entries<IHasDomainEvents>()
        .Select(e => e.Entity)
        .Where(e => e.DomainEvents.Any())
        .ToArray();

    await _dispatcher.DispatchAndClearEvents(entities);
    return await base.SaveChangesAsync(ct);
}
```

---

## 4. Aggregate Root Identification

### 4.1 Aggregate Criteria

An aggregate should:
1. Enforce invariants (business rules)
2. Define a transactional boundary
3. Have a clear lifecycle
4. Be kept small (performance)

### 4.2 Trading Domain Analysis

| Entity | Aggregate Root? | Reasoning |
|--------|----------------|-----------|
| **Order** | ✅ YES | Independent lifecycle, enforces order invariants (quantity > 0, valid prices) |
| **Position** | ✅ YES | Independent lifecycle, enforces position invariants (P&L calculations) |
| **Account** | ✅ YES | Consistency point for equity/cash, enforces account limits |
| **Trade** | ✅ YES | Immutable read model, no invariants to enforce |
| **Candle** | ✅ YES | Independent market data, immutable once created |
| **Signal** | ❌ NO | Transient (not persisted) |

**Key Decision**: Order, Position, and Account are **separate aggregates** with **eventual consistency** via domain events:

```
Order (Filled Event) ──> Position (Updated via handler)
                    └──> Account (Balance updated via handler)
```

This prevents:
- Deadlocks from concurrent order processing
- Large transaction scopes
- Tight coupling

---

## 5. Value Object Classification

### 5.1 Entity vs Value Object Decision

| Characteristic | Entity | Value Object |
|---------------|--------|--------------|
| Identity | Has unique ID | No identity |
| Mutability | Mutable | Immutable |
| Equality | ID-based | Attribute-based |
| Persistence | Own table | Owned/embedded |

### 5.2 Trading Domain Classification

**Entities (have identity)**:
- Order (Guid Id)
- Position (Guid Id)
- Account (string AccountId)
- Trade (Guid Id)
- Candle (composite: Symbol + Timestamp)

**Value Objects (no identity, immutable)**:
- Quote ✅ (already a record)
- SymbolInfo ✅ (already a record)
- RiskParameters ✅ (already a record)
- Money (recommended: amount + currency)
- Quantity (recommended: validated positive decimal)
- PriceRange (recommended: high/low pair)

**Current Code Status**: Existing records are already value objects! C# records provide structural equality and immutability.

### 5.3 Value Object Base Class (Optional)

Can extend `ValueObject` for additional methods:

```csharp
public sealed record Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Currency mismatch");
        return new Money(Amount + other.Amount, Currency);
    }
}
```

**Decision**: Keep existing records as-is (C# record pattern sufficient), only extend `ValueObject` if custom operators/methods needed.

---

## 6. Repository Pattern with Specifications

### 6.1 Ardalis.Specification Package

```bash
dotnet add package Ardalis.Specification
dotnet add package Ardalis.Specification.EntityFrameworkCore
```

**Benefits**:
- Type-safe query composition
- Reusable query logic
- Unit-testable queries
- Cleaner service layer

### 6.2 Specification Examples

```csharp
// Simple filter
public sealed class PendingOrdersSpec : Specification<Order>
{
    public PendingOrdersSpec()
    {
        Query.Where(o => o.Status == OrderStatus.Pending)
             .OrderBy(o => o.CreatedAt);
    }
}

// With projection
public sealed class OrderSummarySpec : Specification<Order, OrderSummaryDto>
{
    public OrderSummarySpec(string symbol)
    {
        Query.Where(o => o.Symbol == symbol && o.Status == OrderStatus.Filled)
             .Select(o => new OrderSummaryDto(
                 o.Id,
                 o.Symbol,
                 o.Quantity,
                 o.AverageFillPrice));
    }
}

// Paginated
public sealed class PaginatedOrdersSpec : Specification<Order>
{
    public PaginatedOrdersSpec(int skip, int take)
    {
        Query.OrderByDescending(o => o.CreatedAt)
             .Skip(skip)
             .Take(take);
    }
}
```

### 6.3 Repository Implementation

```csharp
using Ardalis.Specification.EntityFrameworkCore;

public class EfRepository<T> : RepositoryBase<T>, IRepository<T>
    where T : class, IAggregateRoot
{
    public EfRepository(TradingBotDbContext dbContext)
        : base(dbContext)
    {
    }
}

// DI registration
services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));
```

**Usage**:
```csharp
public async Task<List<Order>> GetPendingOrdersAsync(CancellationToken ct)
{
    var spec = new PendingOrdersSpec();
    return await _repository.ListAsync(spec, ct);
}
```

---

## 7. Migration Strategy

### 7.1 Phased Adoption (Recommended)

**Phase 1: Foundation** (Low Risk)
- Add Ardalis.SharedKernel package
- Convert Order, Position, Account to `EntityBase<Guid>, IAggregateRoot`
- Update entity configurations to ignore `DomainEvents`
- No domain events yet, just structure
- **Time**: 2-3 hours
- **Risk**: Low (structural only, no behavior change)

**Phase 2: Domain Events** (Medium Risk)
- Install MediatR
- Implement `MediatorDomainEventDispatcher`
- Create critical events (OrderFilled, PositionOpened, PositionClosed)
- Update DbContext to dispatch before SaveChanges
- Create event handlers for portfolio updates
- **Time**: 1-2 days
- **Risk**: Medium (new async behavior, test carefully)

**Phase 3: Specifications** (Optional)
- Add Ardalis.Specification.EntityFrameworkCore
- Create specifications for common queries
- Gradually replace LINQ with specifications
- **Time**: 1-2 days
- **Risk**: Low (incremental, can coexist with LINQ)

**Phase 4: Full Repository Pattern** (Optional)
- Replace custom `IRepository<T>` with Ardalis `IRepositoryBase<T>`
- Refactor all queries to specifications
- **Time**: 2-3 days
- **Risk**: Medium (large refactor, thorough testing required)

### 7.2 Testing Strategy

**Unit Tests for Domain Events**:
```csharp
[Fact]
public void Order_WhenFilled_ShouldRaiseOrderFilledEvent()
{
    // Arrange
    var order = new Order { /* ... */ };

    // Act
    order.MarkAsFilled(100m, 1m, DateTime.UtcNow);

    // Assert
    order.DomainEvents.ShouldHaveSingleItem();
    order.DomainEvents.First().ShouldBeOfType<OrderFilledEvent>();
}
```

**Integration Tests for Event Handlers**:
```csharp
[Fact]
public async Task OrderFilledEvent_ShouldUpdatePortfolio()
{
    // Arrange
    var order = new Order { /* ... */ };
    await _repository.AddAsync(order);

    // Act
    order.MarkAsFilled(100m, 1m, DateTime.UtcNow);
    await _repository.SaveChangesAsync(); // Triggers events

    // Assert
    var position = await _positionRepository.FirstOrDefaultAsync(
        new PositionsBySymbolSpec(order.Symbol));
    position.ShouldNotBeNull();
}
```

---

## 8. Common Gotchas

### 8.1 Domain Events Not Cleared
❌ **Problem**: Events accumulate and fire multiple times
✅ **Solution**: Always call `ClearDomainEvents()` after dispatch

### 8.2 Forgetting `[NotMapped]` on DomainEvents
❌ **Problem**: EF tries to persist events collection
✅ **Solution**: Always ignore in entity configuration:
```csharp
builder.Ignore(e => e.DomainEvents);
```

### 8.3 Using Events for Synchronous Validation
❌ **Problem**: Domain events are async, can't prevent state change
✅ **Solution**: Use direct method calls for validation, events for notifications

### 8.4 Entity Tracking Issues
❌ **Problem**: EF tracks entities from specifications, causing update conflicts
✅ **Solution**: Use `.AsNoTracking()` in read-only specifications:
```csharp
Query.Where(o => o.Symbol == symbol).AsNoTracking();
```

### 8.5 Repository Interface Constraints
❌ **Problem**: `IRepository<T>` requires `IAggregateRoot` constraint
✅ **Solution**: Only create repositories for aggregate roots

---

## 9. Performance Considerations

### 9.1 Domain Events Overhead
- Pre-save dispatch adds ~10-50ms depending on handler count
- Keep handlers fast (<100ms)
- Use background jobs for long-running operations

### 9.2 Specification Overhead
- Adds ~5-10% overhead vs raw LINQ
- Benefits: reusability, testability, maintainability
- Use `.AsNoTracking()` for read-only queries

### 9.3 Repository Pattern
- Generic repositories have slight overhead vs DbSet<T>
- Benefits: consistent API, easier testing, specification support
- Use read repositories for queries, write repositories for commands

---

## 10. Decisions and Rationale

### 10.1 Use EntityBase<Guid>
**Decision**: All entities extend `EntityBase<Guid>`
**Rationale**:
- Current schema uses Guid IDs
- Globally unique (distributed scenarios)
- Client-side generation
- No migration required

### 10.2 Before-Save Event Dispatch
**Decision**: Dispatch critical events before SaveChanges
**Rationale**:
- Strong consistency for position updates
- Single transaction (all-or-nothing)
- Simpler error handling

### 10.3 Keep Existing Records as Value Objects
**Decision**: Don't extend `ValueObject` for Quote, SymbolInfo, RiskParameters
**Rationale**:
- C# records already provide value semantics
- No custom operators needed
- Less code to maintain

### 10.4 Gradual Specification Adoption
**Decision**: Adopt specifications incrementally, not wholesale replacement
**Rationale**:
- Lower risk (can coexist with LINQ)
- Start with complex queries
- Measure performance impact

---

## 11. Next Steps

1. ✅ Research complete
2. ⏭️ Create data-model.md (Phase 1)
3. ⏭️ Create contracts/ (Phase 1)
4. ⏭️ Create quickstart.md (Phase 1)
5. ⏭️ Generate tasks.md (Phase 2)

---

## 12. References

- [Ardalis.SharedKernel GitHub](https://github.com/ardalis/Ardalis.SharedKernel)
- [Ardalis.Specification Documentation](https://github.com/ardalis/Specification)
- [DDD Aggregate Design](https://www.martinfowler.com/bliki/DDD_Aggregate.html)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [EF Core Value Conversions](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions)
