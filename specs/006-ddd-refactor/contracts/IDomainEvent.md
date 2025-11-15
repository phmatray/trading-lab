# Domain Events Contract

**Purpose**: Define domain event patterns using Ardalis.SharedKernel with MediatR integration
**Package**: Ardalis.SharedKernel, MediatR

---

## 1. Domain Event Base Classes

### 1.1 DomainEventBase

**Purpose**: Base class for all domain events

```csharp
using Ardalis.SharedKernel;

namespace TradingBot.Core.Events;

public abstract class DomainEventBase : IDomainEvent
{
    // Inherited from IDomainEvent (which extends MediatR.INotification)
    public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
}
```

**Key Characteristics**:
- Extends `IDomainEvent` (marker interface from SharedKernel)
- Automatically implements `INotification` from MediatR
- Immutable (properties set via constructor)
- Records timestamp of occurrence

---

## 2. Event Lifecycle

### 2.1 Event Flow

```
1. Domain Logic
   └──> Entity.RegisterDomainEvent(event)

2. DbContext.SaveChangesAsync()
   └──> IDomainEventDispatcher.DispatchAndClearEvents()

3. MediatR
   └──> INotificationHandler<TEvent>.Handle(event)

4. Event Handler
   └──> Execute business logic (update aggregates, send notifications)
```

### 2.2 Event Registration

Entities inheriting from `EntityBase` can register events:

```csharp
public sealed class Order : EntityBase<Guid>, IAggregateRoot
{
    public void MarkAsFilled(decimal fillPrice, decimal commission, DateTime filledAt)
    {
        // Validate state transition
        if (Status == OrderStatus.Filled)
            throw new InvalidOperationException("Order already filled");

        // Update state
        Status = OrderStatus.Filled;
        FilledQuantity = Quantity;
        AverageFillPrice = fillPrice;
        Commission = commission;
        FilledAt = filledAt;

        // Register domain event
        RegisterDomainEvent(new OrderFilledEvent(
            Id,
            Symbol,
            Quantity,
            AverageFillPrice,
            Commission));
    }
}
```

---

## 3. Domain Event Dispatcher

### 3.1 IDomainEventDispatcher Interface

```csharp
using Ardalis.SharedKernel;

namespace TradingBot.Core.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchAndClearEvents(IEnumerable<IHasDomainEvents> entitiesWithEvents);
}
```

### 3.2 MediatR Implementation

```csharp
using MediatR;
using Microsoft.Extensions.Logging;

namespace TradingBot.Infrastructure.EventDispatching;

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
                    "Dispatching domain event {EventType} at {Timestamp}",
                    domainEvent.GetType().Name,
                    DateTime.UtcNow);

                await _mediator.Publish(domainEvent).ConfigureAwait(false);
            }
        }
    }
}
```

### 3.3 DbContext Integration

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
        if (_dispatcher != null)
        {
            var entitiesWithEvents = ChangeTracker
                .Entries<IHasDomainEvents>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Any())
                .ToArray();

            // Dispatch BEFORE save for strong consistency
            await _dispatcher.DispatchAndClearEvents(entitiesWithEvents);
        }

        return await base.SaveChangesAsync(ct);
    }
}
```

---

## 4. Trading Domain Events

### 4.1 Order Events

```csharp
// Core/Events/OrderFilledEvent.cs
public sealed class OrderFilledEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public string Symbol { get; }
    public decimal Quantity { get; }
    public decimal AverageFillPrice { get; }
    public decimal Commission { get; }

    public OrderFilledEvent(
        Guid orderId,
        string symbol,
        decimal quantity,
        decimal averageFillPrice,
        decimal commission)
    {
        OrderId = orderId;
        Symbol = symbol;
        Quantity = quantity;
        AverageFillPrice = averageFillPrice;
        Commission = commission;
    }
}

// Core/Events/OrderCancelledEvent.cs
public sealed class OrderCancelledEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public string Symbol { get; }
    public string? Reason { get; }

    public OrderCancelledEvent(Guid orderId, string symbol, string? reason = null)
    {
        OrderId = orderId;
        Symbol = symbol;
        Reason = reason;
    }
}

// Core/Events/OrderRejectedEvent.cs
public sealed class OrderRejectedEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public string Symbol { get; }
    public string Reason { get; }

    public OrderRejectedEvent(Guid orderId, string symbol, string reason)
    {
        OrderId = orderId;
        Symbol = symbol;
        Reason = reason;
    }
}
```

### 4.2 Position Events

```csharp
// Core/Events/PositionOpenedEvent.cs
public sealed class PositionOpenedEvent : DomainEventBase
{
    public Guid PositionId { get; }
    public string Symbol { get; }
    public OrderSide Side { get; }
    public decimal Quantity { get; }
    public decimal EntryPrice { get; }

    public PositionOpenedEvent(
        Guid positionId,
        string symbol,
        OrderSide side,
        decimal quantity,
        decimal entryPrice)
    {
        PositionId = positionId;
        Symbol = symbol;
        Side = side;
        Quantity = quantity;
        EntryPrice = entryPrice;
    }
}

// Core/Events/PositionClosedEvent.cs
public sealed class PositionClosedEvent : DomainEventBase
{
    public Guid PositionId { get; }
    public string Symbol { get; }
    public decimal RealizedPnL { get; }
    public decimal RealizedPnLPercent { get; }

    public PositionClosedEvent(
        Guid positionId,
        string symbol,
        decimal realizedPnL,
        decimal realizedPnLPercent)
    {
        PositionId = positionId;
        Symbol = symbol;
        RealizedPnL = realizedPnL;
        RealizedPnLPercent = realizedPnLPercent;
    }
}

// Core/Events/PositionPriceUpdatedEvent.cs
public sealed class PositionPriceUpdatedEvent : DomainEventBase
{
    public Guid PositionId { get; }
    public string Symbol { get; }
    public decimal NewPrice { get; }
    public decimal UnrealizedPnL { get; }

    public PositionPriceUpdatedEvent(
        Guid positionId,
        string symbol,
        decimal newPrice,
        decimal unrealizedPnL)
    {
        PositionId = positionId;
        Symbol = symbol;
        NewPrice = newPrice;
        UnrealizedPnL = unrealizedPnL;
    }
}
```

### 4.3 Account Events

```csharp
// Core/Events/CashUpdatedEvent.cs
public sealed class CashUpdatedEvent : DomainEventBase
{
    public string AccountId { get; }
    public decimal NewBalance { get; }
    public decimal ChangeAmount { get; }

    public CashUpdatedEvent(string accountId, decimal newBalance, decimal changeAmount)
    {
        AccountId = accountId;
        NewBalance = newBalance;
        ChangeAmount = changeAmount;
    }
}

// Core/Events/EquityUpdatedEvent.cs
public sealed class EquityUpdatedEvent : DomainEventBase
{
    public string AccountId { get; }
    public decimal NewEquity { get; }
    public decimal TotalPositionValue { get; }

    public EquityUpdatedEvent(
        string accountId,
        decimal newEquity,
        decimal totalPositionValue)
    {
        AccountId = accountId;
        NewEquity = newEquity;
        TotalPositionValue = totalPositionValue;
    }
}

// Core/Events/AccountSuspendedEvent.cs
public sealed class AccountSuspendedEvent : DomainEventBase
{
    public string AccountId { get; }
    public string Reason { get; }

    public AccountSuspendedEvent(string accountId, string reason)
    {
        AccountId = accountId;
        Reason = reason;
    }
}
```

---

## 5. Event Handlers

### 5.1 Event Handler Pattern

```csharp
using MediatR;
using Microsoft.Extensions.Logging;

namespace TradingBot.Engine.EventHandlers;

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
            "Handling OrderFilledEvent: Order {OrderId} filled - {Symbol} x {Quantity} @ {Price}",
            notification.OrderId,
            notification.Symbol,
            notification.Quantity,
            notification.AverageFillPrice);

        // Business logic: Update or create position
        var spec = new PositionsBySymbolSpec(notification.Symbol);
        var existingPosition = await _positionRepository.FirstOrDefaultAsync(spec, ct);

        if (existingPosition != null)
        {
            // Increase existing position
            var newAvgPrice = CalculateNewAverage(
                existingPosition.Quantity,
                existingPosition.EntryPrice,
                notification.Quantity,
                notification.AverageFillPrice);

            existingPosition.IncreaseQuantity(notification.Quantity, newAvgPrice);
            await _positionRepository.UpdateAsync(existingPosition, ct);
        }
        else
        {
            // Open new position
            var newPosition = new Position
            {
                Id = Guid.NewGuid(),
                Symbol = notification.Symbol,
                Side = DeterminePositionSide(notification.OrderId),
                Quantity = notification.Quantity,
                EntryPrice = notification.AverageFillPrice,
                EntryTime = notification.DateOccurred,
                CurrentPrice = notification.AverageFillPrice,
                StrategyName = DetermineStrategyName(notification.OrderId)
            };

            await _positionRepository.AddAsync(newPosition, ct);
        }

        await _positionRepository.SaveChangesAsync(ct);
    }

    private decimal CalculateNewAverage(
        decimal existingQty,
        decimal existingPrice,
        decimal newQty,
        decimal newPrice)
    {
        return ((existingQty * existingPrice) + (newQty * newPrice)) / (existingQty + newQty);
    }
}
```

### 5.2 Multiple Handlers for Same Event

```csharp
// Handler 1: Update portfolio
public sealed class OrderFilledPortfolioHandler : INotificationHandler<OrderFilledEvent>
{
    public async Task Handle(OrderFilledEvent notification, CancellationToken ct)
    {
        // Update position logic
    }
}

// Handler 2: Send notification
public sealed class OrderFilledNotificationHandler : INotificationHandler<OrderFilledEvent>
{
    private readonly IHubContext<TradingHub, ITradingClient> _hubContext;

    public async Task Handle(OrderFilledEvent notification, CancellationToken ct)
    {
        await _hubContext.Clients.All.OnOrderFilled(
            notification.OrderId,
            notification.Symbol,
            notification.Quantity,
            notification.AverageFillPrice);
    }
}

// Handler 3: Log analytics
public sealed class OrderFilledAnalyticsHandler : INotificationHandler<OrderFilledEvent>
{
    public async Task Handle(OrderFilledEvent notification, CancellationToken ct)
    {
        // Record analytics/metrics
    }
}
```

**Note**: All three handlers execute in sequence when `OrderFilledEvent` is published.

---

## 6. Event Handler Registration

### 6.1 Automatic MediatR Registration

```csharp
// Program.cs or ServiceCollectionExtensions.cs
services.AddMediatR(cfg =>
{
    // Scan assemblies for INotificationHandler implementations
    cfg.RegisterServicesFromAssembly(typeof(OrderFilledEventHandler).Assembly);
});
```

MediatR automatically discovers and registers:
- All `INotificationHandler<TEvent>` implementations
- All domain events extending `DomainEventBase`

### 6.2 Manual Registration (if needed)

```csharp
services.AddTransient<INotificationHandler<OrderFilledEvent>, OrderFilledEventHandler>();
services.AddTransient<INotificationHandler<PositionClosedEvent>, PositionClosedEventHandler>();
```

---

## 7. Event Naming Conventions

### 7.1 Naming Pattern

Format: `{Entity}{Action}Event`

**Examples**:
- ✅ `OrderFilledEvent` (Order + Filled)
- ✅ `PositionClosedEvent` (Position + Closed)
- ✅ `AccountSuspendedEvent` (Account + Suspended)
- ❌ `FilledEvent` (missing entity context)
- ❌ `OrderEvent` (missing action)

### 7.2 Tense

Use **past tense** (event already occurred):
- ✅ `OrderFilledEvent` (filled)
- ✅ `PositionOpenedEvent` (opened)
- ❌ `OrderFillingEvent` (present continuous)
- ❌ `FillOrderEvent` (imperative command)

---

## 8. Best Practices

### 8.1 Event Immutability

Events should be immutable (properties set via constructor):

```csharp
// ✅ Good
public sealed class OrderFilledEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public decimal Price { get; }

    public OrderFilledEvent(Guid orderId, decimal price)
    {
        OrderId = orderId;
        Price = price;
    }
}

// ❌ Bad (mutable properties)
public sealed class OrderFilledEvent : DomainEventBase
{
    public Guid OrderId { get; set; }
    public decimal Price { get; set; }
}
```

### 8.2 Event Content

Include **what happened**, not **how to react**:

```csharp
// ✅ Good (describes what happened)
public sealed class OrderFilledEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public decimal Quantity { get; }
    public decimal AverageFillPrice { get; }
}

// ❌ Bad (prescribes action)
public sealed class UpdatePositionEvent : DomainEventBase
{
    public Guid PositionId { get; }
    public decimal NewQuantity { get; }
}
```

### 8.3 Event Handler Responsibilities

- **Keep handlers focused**: One responsibility per handler
- **No complex logic**: Delegate to domain services
- **Idempotent**: Handle same event multiple times safely
- **Fast execution**: Avoid slow operations (use background jobs for long tasks)

### 8.4 Error Handling

```csharp
public async Task Handle(OrderFilledEvent notification, CancellationToken ct)
{
    try
    {
        // Business logic
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling OrderFilledEvent for Order {OrderId}", notification.OrderId);

        // Decision: Rethrow to rollback transaction, or swallow to continue
        throw; // Rollback entire SaveChanges if critical
    }
}
```

**Important**: If handler throws exception, entire SaveChanges is rolled back (when using before-save dispatch).

---

## 9. Testing Domain Events

### 9.1 Unit Test: Event Registration

```csharp
[Fact]
public void Order_WhenFilled_ShouldRaiseOrderFilledEvent()
{
    // Arrange
    var order = new Order
    {
        Id = Guid.NewGuid(),
        Symbol = "AAPL",
        Quantity = 100m,
        Status = OrderStatus.Pending
    };

    // Act
    order.MarkAsFilled(fillPrice: 150m, commission: 1.50m, DateTime.UtcNow);

    // Assert
    order.DomainEvents.ShouldHaveSingleItem();
    var evt = order.DomainEvents.First();
    evt.ShouldBeOfType<OrderFilledEvent>();

    var orderFilledEvent = (OrderFilledEvent)evt;
    orderFilledEvent.OrderId.ShouldBe(order.Id);
    orderFilledEvent.Symbol.ShouldBe("AAPL");
    orderFilledEvent.Quantity.ShouldBe(100m);
    orderFilledEvent.AverageFillPrice.ShouldBe(150m);
}
```

### 9.2 Integration Test: Event Dispatch

```csharp
[Fact]
public async Task OrderFilledEvent_ShouldUpdatePosition()
{
    // Arrange
    var order = new Order { /* ... */ };
    await _orderRepository.AddAsync(order);

    // Act
    order.MarkAsFilled(150m, 1.50m, DateTime.UtcNow);
    await _orderRepository.SaveChangesAsync(); // Triggers event dispatch

    // Assert
    var spec = new PositionsBySymbolSpec("AAPL");
    var position = await _positionRepository.FirstOrDefaultAsync(spec);

    position.ShouldNotBeNull();
    position.Symbol.ShouldBe("AAPL");
    position.Quantity.ShouldBe(100m);
    position.EntryPrice.ShouldBe(150m);
}
```

### 9.3 Unit Test: Event Handler

```csharp
[Fact]
public async Task OrderFilledEventHandler_ShouldCreateNewPosition()
{
    // Arrange
    var evt = new OrderFilledEvent(
        orderId: Guid.NewGuid(),
        symbol: "AAPL",
        quantity: 100m,
        averageFillPrice: 150m,
        commission: 1.50m);

    var fakeRepository = A.Fake<IRepository<Position>>();
    var handler = new OrderFilledEventHandler(fakeRepository, _logger);

    // Act
    await handler.Handle(evt, CancellationToken.None);

    // Assert
    A.CallTo(() => fakeRepository.AddAsync(
        A<Position>.That.Matches(p =>
            p.Symbol == "AAPL" &&
            p.Quantity == 100m),
        A<CancellationToken>._))
     .MustHaveHappenedOnceExactly();
}
```

---

## 10. Summary

**Key Contracts**:
- `DomainEventBase` - Base class for all domain events
- `IDomainEventDispatcher` - Dispatches events via MediatR
- `INotificationHandler<TEvent>` - Handles specific domain events

**Event Lifecycle**:
1. Entity registers event (via `RegisterDomainEvent()`)
2. DbContext dispatches events before/after SaveChanges
3. MediatR routes event to all registered handlers
4. Handlers execute business logic

**Best Practices**:
- Events are immutable (constructor-only properties)
- Use past tense naming (`OrderFilledEvent`)
- Keep handlers focused and fast
- Test event registration and handling separately

**Next Steps**:
- Create domain events for critical operations
- Implement event handlers for portfolio updates
- Integrate with SignalR for real-time notifications
