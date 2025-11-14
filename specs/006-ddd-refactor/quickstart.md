# DDD Refactoring Quickstart Guide

**Purpose**: Developer guide for implementing DDD patterns with Ardalis.SharedKernel
**Audience**: Developers working on the TradingBot codebase

---

## 1. Quick Reference

### 1.1 When to Use What

| Pattern | Use When | Example |
|---------|----------|---------|
| `EntityBase<Guid>` | Domain entity with identity | Order, Position, Account |
| `ValueObject` | Attribute-based equality, immutable | Money, PriceRange (optional - records often sufficient) |
| C# `record` | Value object with simple equality | Quote, SymbolInfo, RiskParameters |
| `IAggregateRoot` | Root of consistency boundary | Order, Position, Account |
| `DomainEventBase` | Something significant happened | OrderFilledEvent, PositionClosedEvent |
| `Specification<T>` | Reusable query logic | PendingOrdersSpec, OpenPositionsSpec |

---

## 2. Package Installation

```bash
# Core project
dotnet add src/TradingBot.Core package Ardalis.SharedKernel

# Infrastructure project
dotnet add src/TradingBot.Infrastructure package Ardalis.Specification.EntityFrameworkCore
dotnet add src/TradingBot.Infrastructure package MediatR
```

---

## 3. Converting an Entity to DDD Pattern

### 3.1 Before (Plain Class)

```csharp
public sealed class Order
{
    public required Guid Id { get; set; }
    public required string Symbol { get; set; }
    public required OrderStatus Status { get; set; }
    public required DateTime CreatedAt { get; set; }

    // No invariant enforcement
    // No domain events
}
```

### 3.2 After (DDD Entity)

```csharp
public sealed class Order : EntityBase<Guid>, IAggregateRoot
{
    // Id inherited from EntityBase<Guid>
    // DomainEvents collection inherited from HasDomainEventsBase

    public required string Symbol { get; set; }
    private OrderStatus _status; // Private backing field
    public OrderStatus Status => _status; // Read-only property

    public required DateTime CreatedAt { get; set; }

    // Business method enforces invariants
    public void MarkAsFilled(decimal fillPrice, decimal commission, DateTime filledAt)
    {
        // Validate state transition
        if (_status == OrderStatus.Filled)
            throw new InvalidOperationException("Order already filled");

        if (fillPrice <= 0)
            throw new ArgumentException("Price must be positive", nameof(fillPrice));

        // Update state
        _status = OrderStatus.Filled;
        FilledAt = filledAt;

        // Raise domain event
        RegisterDomainEvent(new OrderFilledEvent(Id, Symbol, fillPrice, commission));
    }
}
```

### 3.3 EF Core Configuration

```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id); // Standard PK mapping

        // CRITICAL: Ignore domain events (not persisted)
        builder.Ignore(o => o.DomainEvents);

        // Configure SmartEnum conversions
        builder.Property(o => o.Status)
            .HasConversion(
                v => v.Value,
                v => OrderStatus.FromValue(v));

        // Other configurations...
    }
}
```

---

## 4. Creating a Domain Event

### 4.1 Event Class

```csharp
// Core/Events/OrderFilledEvent.cs
public sealed class OrderFilledEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public string Symbol { get; }
    public decimal AverageFillPrice { get; }
    public decimal Commission { get; }

    public OrderFilledEvent(
        Guid orderId,
        string symbol,
        decimal averageFillPrice,
        decimal commission)
    {
        OrderId = orderId;
        Symbol = symbol;
        AverageFillPrice = averageFillPrice;
        Commission = commission;
    }
}
```

### 4.2 Event Handler

```csharp
// Engine/EventHandlers/OrderFilledEventHandler.cs
public sealed class OrderFilledEventHandler : INotificationHandler<OrderFilledEvent>
{
    private readonly IRepository<Position> _positionRepository;
    private readonly ILogger<OrderFilledEventHandler> _logger;

    public OrderFilledEventHandler(
        IRepository<Position> positionRepository,
        ILogger<OrderFilledEventHandler> logger)
    {
        _positionRepository = positionRepository;
        _logger = logger;
    }

    public async Task Handle(OrderFilledEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Order {OrderId} filled: {Symbol} x {Quantity} @ {Price}",
            notification.OrderId,
            notification.Symbol,
            notification.AverageFillPrice);

        // Update or create position
        var spec = new PositionsBySymbolSpec(notification.Symbol);
        var position = await _positionRepository.FirstOrDefaultAsync(spec, ct);

        if (position != null)
        {
            // Update existing position
            position.IncreaseQuantity(notification.Quantity, notification.AverageFillPrice);
            await _positionRepository.UpdateAsync(position, ct);
        }
        else
        {
            // Create new position
            var newPosition = new Position
            {
                Id = Guid.NewGuid(),
                Symbol = notification.Symbol,
                // ... other properties
            };
            await _positionRepository.AddAsync(newPosition, ct);
        }

        await _positionRepository.SaveChangesAsync(ct);
    }
}
```

---

## 5. Setting Up Domain Event Dispatcher

### 5.1 Dispatcher Implementation

```csharp
// Infrastructure/EventDispatching/MediatorDomainEventDispatcher.cs
public sealed class MediatorDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediatorDomainEventDispatcher> _logger;

    public MediatorDomainEventDispatcher(
        IMediator mediator,
        ILogger<MediatorDomainEventDispatcher> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task DispatchAndClearEvents(IEnumerable<IHasDomainEvents> entitiesWithEvents)
    {
        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToArray();
            entity.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                _logger.LogInformation(
                    "Dispatching {EventType}",
                    domainEvent.GetType().Name);

                await _mediator.Publish(domainEvent);
            }
        }
    }
}
```

### 5.2 DbContext Integration

```csharp
public class TradingBotDbContext : DbContext
{
    private readonly IDomainEventDispatcher? _dispatcher;

    public TradingBotDbContext(
        DbContextOptions<TradingBotDbContext> options,
        IDomainEventDispatcher? dispatcher = null)
        : base(options)
    {
        _dispatcher = dispatcher;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Dispatch events BEFORE saving (strong consistency)
        if (_dispatcher != null)
        {
            var entitiesWithEvents = ChangeTracker
                .Entries<IHasDomainEvents>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Any())
                .ToArray();

            await _dispatcher.DispatchAndClearEvents(entitiesWithEvents);
        }

        return await base.SaveChangesAsync(ct);
    }
}
```

### 5.3 DI Registration

```csharp
// Infrastructure/ServiceCollectionExtensions.cs
public static IServiceCollection AddDomainEvents(this IServiceCollection services)
{
    // MediatR for event dispatching
    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(OrderFilledEventHandler).Assembly);
    });

    // Domain event dispatcher
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    return services;
}

// Web/Program.cs or CLI/Program.cs
builder.Services.AddDomainEvents();
```

---

## 6. Creating a Specification

### 6.1 Simple Specification

```csharp
// Core/Specifications/OrderSpecifications.cs
public sealed class PendingOrdersSpec : Specification<Order>
{
    public PendingOrdersSpec()
    {
        Query.Where(o => o.Status == OrderStatus.Pending)
             .OrderBy(o => o.CreatedAt);
    }
}
```

### 6.2 Parameterized Specification

```csharp
public sealed class OrdersBySymbolSpec : Specification<Order>
{
    public OrdersBySymbolSpec(string symbol, DateTime? startDate = null)
    {
        Query.Where(o => o.Symbol == symbol);

        if (startDate.HasValue)
        {
            Query.Where(o => o.CreatedAt >= startDate.Value);
        }

        Query.OrderByDescending(o => o.CreatedAt);
    }
}
```

### 6.3 Projection Specification

```csharp
public record OrderSummaryDto(
    Guid Id,
    string Symbol,
    decimal Quantity,
    decimal AverageFillPrice,
    DateTime FilledAt);

public sealed class OrderSummarySpec : Specification<Order, OrderSummaryDto>
{
    public OrderSummarySpec(string symbol)
    {
        Query.Where(o => o.Symbol == symbol && o.Status == OrderStatus.Filled)
             .Select(o => new OrderSummaryDto(
                 o.Id,
                 o.Symbol,
                 o.Quantity,
                 o.AverageFillPrice,
                 o.FilledAt!.Value));
    }
}
```

### 6.4 Using Specifications

```csharp
public class OrderService
{
    private readonly IReadRepository<Order> _orderReadRepository;
    private readonly IRepository<Order> _orderRepository;

    // Query with specification
    public async Task<List<Order>> GetPendingOrdersAsync(CancellationToken ct)
    {
        var spec = new PendingOrdersSpec();
        return await _orderReadRepository.ListAsync(spec, ct);
    }

    // Query with projection
    public async Task<List<OrderSummaryDto>> GetOrderSummaryAsync(string symbol, CancellationToken ct)
    {
        var spec = new OrderSummarySpec(symbol);
        return await _orderReadRepository.ListAsync(spec, ct);
    }

    // Command with specification
    public async Task CancelOrderAsync(Guid orderId, CancellationToken ct)
    {
        var spec = new OrderByIdSpec(orderId);
        var order = await _orderRepository.FirstOrDefaultAsync(spec, ct);

        if (order == null)
            throw new InvalidOperationException($"Order {orderId} not found");

        order.Cancel(); // Raises OrderCancelledEvent

        await _orderRepository.UpdateAsync(order, ct);
        await _orderRepository.SaveChangesAsync(ct); // Dispatches events
    }
}
```

---

## 7. Repository Setup

### 7.1 Generic Repository Implementation

```csharp
// Infrastructure/Persistence/Repositories/EfRepository.cs
using Ardalis.Specification.EntityFrameworkCore;

public class EfRepository<T> : RepositoryBase<T>, IRepository<T>
    where T : class, IAggregateRoot
{
    public EfRepository(TradingBotDbContext dbContext)
        : base(dbContext)
    {
    }
}

public class EfReadRepository<T> : RepositoryBase<T>, IReadRepository<T>
    where T : class, IAggregateRoot
{
    public EfReadRepository(TradingBotDbContext dbContext)
        : base(dbContext)
    {
    }
}
```

### 7.2 DI Registration

```csharp
public static IServiceCollection AddRepositories(this IServiceCollection services)
{
    // Generic repositories
    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
    services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));

    return services;
}
```

---

## 8. Common Tasks

### 8.1 Add New Domain Entity

1. **Create entity class**:
```csharp
public sealed class MyEntity : EntityBase<Guid>, IAggregateRoot
{
    public required string Name { get; set; }

    public void DoSomething()
    {
        // Business logic
        RegisterDomainEvent(new MyEntityChangedEvent(Id));
    }
}
```

2. **Create EF configuration**:
```csharp
public class MyEntityConfiguration : IEntityTypeConfiguration<MyEntity>
{
    public void Configure(EntityTypeBuilder<MyEntity> builder)
    {
        builder.ToTable("MyEntities");
        builder.HasKey(e => e.Id);
        builder.Ignore(e => e.DomainEvents); // CRITICAL
    }
}
```

3. **Add DbSet to context**:
```csharp
public DbSet<MyEntity> MyEntities => Set<MyEntity>();
```

4. **Create migration**:
```bash
dotnet ef migrations add AddMyEntity --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web
```

### 8.2 Add Domain Event

1. **Create event class**:
```csharp
public sealed class MyEntityChangedEvent : DomainEventBase
{
    public Guid EntityId { get; }

    public MyEntityChangedEvent(Guid entityId)
    {
        EntityId = entityId;
    }
}
```

2. **Create event handler**:
```csharp
public sealed class MyEntityChangedEventHandler : INotificationHandler<MyEntityChangedEvent>
{
    public async Task Handle(MyEntityChangedEvent notification, CancellationToken ct)
    {
        // Handle event
    }
}
```

3. **Register in entity method**:
```csharp
public void DoSomething()
{
    // Business logic
    RegisterDomainEvent(new MyEntityChangedEvent(Id));
}
```

That's it! MediatR auto-discovers and registers handlers.

### 8.3 Add Specification

1. **Create specification**:
```csharp
public sealed class MyEntitiesByNameSpec : Specification<MyEntity>
{
    public MyEntitiesByNameSpec(string name)
    {
        Query.Where(e => e.Name.Contains(name))
             .OrderBy(e => e.Name);
    }
}
```

2. **Use in service**:
```csharp
var spec = new MyEntitiesByNameSpec("test");
var entities = await _repository.ListAsync(spec, ct);
```

---

## 9. Testing Patterns

### 9.1 Test Entity Invariants

```csharp
[Fact]
public void Order_MarkAsFilled_WhenAlreadyFilled_ShouldThrow()
{
    // Arrange
    var order = new Order { Status = OrderStatus.Filled };

    // Act & Assert
    Should.Throw<InvalidOperationException>(() =>
        order.MarkAsFilled(100m, 1m, DateTime.UtcNow));
}
```

### 9.2 Test Event Registration

```csharp
[Fact]
public void Order_MarkAsFilled_ShouldRaiseOrderFilledEvent()
{
    // Arrange
    var order = new Order { Status = OrderStatus.Pending };

    // Act
    order.MarkAsFilled(100m, 1m, DateTime.UtcNow);

    // Assert
    order.DomainEvents.ShouldHaveSingleItem();
    order.DomainEvents.First().ShouldBeOfType<OrderFilledEvent>();
}
```

### 9.3 Test Event Handler

```csharp
[Fact]
public async Task OrderFilledEventHandler_ShouldUpdatePosition()
{
    // Arrange
    var fakeRepo = A.Fake<IRepository<Position>>();
    var handler = new OrderFilledEventHandler(fakeRepo, _logger);
    var evt = new OrderFilledEvent(Guid.NewGuid(), "AAPL", 100m, 150m);

    // Act
    await handler.Handle(evt, CancellationToken.None);

    // Assert
    A.CallTo(() => fakeRepo.AddAsync(
        A<Position>.That.Matches(p => p.Symbol == "AAPL"),
        A<CancellationToken>._))
     .MustHaveHappenedOnceExactly();
}
```

### 9.4 Test Specification

```csharp
[Fact]
public async Task PendingOrdersSpec_ReturnsOnlyPendingOrders()
{
    // Arrange
    await _repository.AddRangeAsync(new[]
    {
        new Order { Status = OrderStatus.Pending },
        new Order { Status = OrderStatus.Filled },
        new Order { Status = OrderStatus.Pending }
    });
    await _repository.SaveChangesAsync();

    // Act
    var spec = new PendingOrdersSpec();
    var results = await _repository.ListAsync(spec);

    // Assert
    results.Count.ShouldBe(2);
    results.ShouldAllBe(o => o.Status == OrderStatus.Pending);
}
```

---

## 10. Troubleshooting

### 10.1 Domain Events Not Firing

**Problem**: Events registered but handler not called

**Checklist**:
- ✅ Is `IDomainEventDispatcher` injected into DbContext?
- ✅ Is `SaveChangesAsync` overridden in DbContext?
- ✅ Is MediatR registered (`services.AddMediatR(...)`)?
- ✅ Is handler assembly scanned by MediatR?

**Fix**:
```csharp
// Verify in Program.cs
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(OrderFilledEventHandler).Assembly);
});

services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();
```

### 10.2 EF Core Tries to Persist Events

**Problem**: Error about `DomainEvents` column not found

**Checklist**:
- ✅ Is `builder.Ignore(e => e.DomainEvents)` in entity configuration?
- ✅ Is configuration applied in `OnModelCreating`?

**Fix**:
```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Ignore(o => o.DomainEvents); // Add this
    }
}

// In DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingBotDbContext).Assembly);
}
```

### 10.3 Specification Not Working

**Problem**: Specification returns no results or wrong results

**Checklist**:
- ✅ Is `Ardalis.Specification.EntityFrameworkCore` package installed?
- ✅ Does repository extend `RepositoryBase<T>`?
- ✅ Is specification query correct (check with `.ToQueryString()`)?

**Debug**:
```csharp
// Log generated SQL
var spec = new PendingOrdersSpec();
var query = _context.Orders.WithSpecification(spec);
var sql = query.ToQueryString();
_logger.LogDebug("Generated SQL: {Sql}", sql);
```

### 10.4 Events Fire Multiple Times

**Problem**: Event handler called multiple times for same event

**Checklist**:
- ✅ Is `ClearDomainEvents()` called after dispatch?
- ✅ Are multiple SaveChanges calls triggering re-dispatch?

**Fix**:
```csharp
public async Task DispatchAndClearEvents(IEnumerable<IHasDomainEvents> entities)
{
    foreach (var entity in entities)
    {
        var events = entity.DomainEvents.ToArray();
        entity.ClearDomainEvents(); // CRITICAL - call immediately

        foreach (var evt in events)
        {
            await _mediator.Publish(evt);
        }
    }
}
```

---

## 11. Checklist: Converting Existing Entity

```
Step 1: Update Entity Class
[ ] Extend EntityBase<TId> (e.g., EntityBase<Guid>)
[ ] Implement IAggregateRoot
[ ] Remove Id property (inherited from EntityBase)
[ ] Add business methods for state changes
[ ] Register domain events in business methods

Step 2: Update EF Configuration
[ ] Add builder.Ignore(e => e.DomainEvents)
[ ] Verify Id mapping (should auto-map)
[ ] Keep existing SmartEnum conversions
[ ] Test with dotnet ef migrations add Test

Step 3: Create Domain Events
[ ] Create event class extending DomainEventBase
[ ] Add immutable properties via constructor
[ ] Create event handler implementing INotificationHandler<TEvent>
[ ] Test event registration and dispatch

Step 4: Update Tests
[ ] Test invariant enforcement
[ ] Test event registration
[ ] Test event handlers
[ ] Verify existing tests still pass

Step 5: Build & Verify
[ ] dotnet build (zero warnings)
[ ] dotnet test (all tests pass)
[ ] Manual smoke test of affected functionality
```

---

## 12. Summary

**Key Takeaways**:
- Entities extend `EntityBase<TId>` and implement `IAggregateRoot`
- Domain events extend `DomainEventBase`
- Event handlers implement `INotificationHandler<TEvent>`
- Specifications extend `Specification<T>` or `Specification<T, TResult>`
- Always `builder.Ignore(e => e.DomainEvents)` in EF configurations

**Resources**:
- [research.md](./research.md) - Detailed integration guide
- [data-model.md](./data-model.md) - Entity modeling patterns
- [contracts/IRepository.md](./contracts/IRepository.md) - Repository patterns
- [contracts/IDomainEvent.md](./contracts/IDomainEvent.md) - Event patterns
- [contracts/IAggregateRoot.md](./contracts/IAggregateRoot.md) - Aggregate patterns

**Next Steps**:
- Review `/speckit.tasks` output for implementation tasks
- Start with Phase 1 (entity conversion)
- Add domain events in Phase 2
- Adopt specifications incrementally in Phase 3
