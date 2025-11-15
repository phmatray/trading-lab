# Aggregate Root Pattern Contract

**Purpose**: Define aggregate root patterns and consistency boundaries for DDD
**Package**: Ardalis.SharedKernel

---

## 1. IAggregateRoot Interface

### 1.1 Marker Interface

```csharp
namespace Ardalis.SharedKernel;

/// <summary>
/// Marker interface to identify aggregate roots.
/// Apply this to domain entities that form a consistency boundary.
/// </summary>
public interface IAggregateRoot
{
    // Empty marker interface
}
```

**Purpose**:
- Identifies the root entity of an aggregate
- Enforces repository pattern (only aggregate roots get repositories)
- Documents aggregate boundaries in code

---

## 2. Aggregate Root Principles

### 2.1 Definition

An aggregate is a **cluster of domain objects** that can be treated as a **single unit for data changes**.

**Key characteristics**:
1. **Consistency boundary**: Invariants enforced within the aggregate
2. **Transaction boundary**: All changes committed together or not at all
3. **Single root**: External entities reference only the aggregate root
4. **Small scope**: Keep aggregates as small as possible for performance

### 2.2 Rules

1. **External references by ID only**:
   ```csharp
   // ✅ Good: Reference by ID
   public class Order : EntityBase<Guid>, IAggregateRoot
   {
       public string AccountId { get; set; } // ID reference
   }

   // ❌ Bad: Direct navigation property
   public class Order : EntityBase<Guid>, IAggregateRoot
   {
       public Account Account { get; set; } // Tight coupling
   }
   ```

2. **Repositories only for roots**:
   ```csharp
   // ✅ Good: Repository for aggregate root
   services.AddScoped<IRepository<Order>, EfRepository<Order>>();

   // ❌ Bad: Repository for child entity
   services.AddScoped<IRepository<OrderLine>, EfRepository<OrderLine>>();
   ```

3. **Modify via root methods**:
   ```csharp
   // ✅ Good: Modify through aggregate root
   order.MarkAsFilled(price, commission, timestamp);

   // ❌ Bad: Direct property mutation
   order.Status = OrderStatus.Filled;
   order.FilledQuantity = order.Quantity;
   ```

---

## 3. Trading Domain Aggregates

### 3.1 Order Aggregate

**Aggregate Root**: Order
**Consistency Boundary**: Order state and transitions
**Invariants**:
- Quantity must be positive
- Valid state transitions (Pending → Filled, not Filled → Pending)
- Price constraints (limit price ≥ 0 if specified)

```csharp
public sealed class Order : EntityBase<Guid>, IAggregateRoot
{
    // State
    public required OrderStatus Status { get; set; }
    public required decimal Quantity { get; set; }

    // External references (by ID only)
    public string? AccountId { get; set; }
    public Guid? PositionId { get; set; }

    // Business methods enforce invariants
    public void MarkAsFilled(decimal fillPrice, decimal commission, DateTime filledAt)
    {
        // Validate state transition
        if (Status == OrderStatus.Filled)
            throw new InvalidOperationException("Order already filled");

        // Validate business rules
        if (fillPrice <= 0)
            throw new ArgumentException("Fill price must be positive", nameof(fillPrice));

        // Update state
        Status = OrderStatus.Filled;
        FilledQuantity = Quantity;
        AverageFillPrice = fillPrice;
        Commission = commission;
        FilledAt = filledAt;

        // Raise domain event
        RegisterDomainEvent(new OrderFilledEvent(Id, Symbol, Quantity, fillPrice, commission));
    }

    public void Cancel()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot cancel order in {Status} status");

        Status = OrderStatus.Cancelled;
        RegisterDomainEvent(new OrderCancelledEvent(Id, Symbol));
    }
}
```

**Why separate aggregate**:
- Independent lifecycle (created → filled/cancelled)
- No mandatory child entities
- State changes are atomic (single order at a time)

---

### 3.2 Position Aggregate

**Aggregate Root**: Position
**Consistency Boundary**: Position state, P&L calculations
**Invariants**:
- Quantity must be positive
- Entry price must be positive
- Realized P&L calculated correctly on close

```csharp
public sealed class Position : EntityBase<Guid>, IAggregateRoot
{
    // State
    public required decimal Quantity { get; set; }
    public required decimal EntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }

    // External references (by ID only)
    public string? AccountId { get; set; }

    // Calculated properties
    public decimal UnrealizedPnL => CalculateUnrealizedPnL();

    // Business methods
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("Price must be positive", nameof(newPrice));

        CurrentPrice = newPrice;
        RegisterDomainEvent(new PositionPriceUpdatedEvent(Id, Symbol, newPrice, UnrealizedPnL));
    }

    public void Close(decimal exitPrice, DateTime exitTime)
    {
        if (ExitTime.HasValue)
            throw new InvalidOperationException("Position already closed");

        if (exitPrice <= 0)
            throw new ArgumentException("Exit price must be positive", nameof(exitPrice));

        ExitPrice = exitPrice;
        ExitTime = exitTime;
        RealizedPnL = CalculateRealizedPnL(exitPrice);

        RegisterDomainEvent(new PositionClosedEvent(Id, Symbol, RealizedPnL));
    }

    private decimal CalculateUnrealizedPnL()
    {
        if (ExitTime.HasValue) return 0;

        return Side == OrderSide.Buy
            ? (CurrentPrice - EntryPrice) * Quantity
            : (EntryPrice - CurrentPrice) * Quantity;
    }

    private decimal CalculateRealizedPnL(decimal exitPrice)
    {
        return Side == OrderSide.Buy
            ? (exitPrice - EntryPrice) * Quantity
            : (EntryPrice - exitPrice) * Quantity;
    }
}
```

**Why separate aggregate**:
- Independent lifecycle (opened → closed)
- Can exist without orders (imported positions)
- P&L calculations are self-contained

---

### 3.3 Account Aggregate

**Aggregate Root**: Account
**Consistency Boundary**: Cash balance, equity, account status
**Invariants**:
- Cash balance cannot go negative
- Equity = Cash + PositionValue
- Leverage must not exceed limits

```csharp
public sealed class Account : EntityBase<string>, IAggregateRoot
{
    public new required string Id { get; set; } // Override for string ID

    // State
    private decimal _cash;
    private decimal _equity;

    public decimal Cash => _cash;
    public decimal Equity => _equity;
    public bool IsActive { get; private set; } = true;

    // Business methods
    public void DeductCash(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        if (_cash < amount)
            throw new InvalidOperationException(
                $"Insufficient funds. Available: {_cash}, Required: {amount}");

        _cash -= amount;
        LastUpdated = DateTime.UtcNow;

        RegisterDomainEvent(new CashUpdatedEvent(Id, _cash, -amount));
    }

    public void AddCash(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        _cash += amount;
        LastUpdated = DateTime.UtcNow;

        RegisterDomainEvent(new CashUpdatedEvent(Id, _cash, amount));
    }

    public void UpdateEquity(decimal totalPositionValue)
    {
        _equity = _cash + totalPositionValue;
        LastUpdated = DateTime.UtcNow;

        RegisterDomainEvent(new EquityUpdatedEvent(Id, _equity, totalPositionValue));
    }

    public void Suspend(string reason)
    {
        if (!IsActive)
            throw new InvalidOperationException("Account already suspended");

        IsActive = false;
        LastUpdated = DateTime.UtcNow;

        RegisterDomainEvent(new AccountSuspendedEvent(Id, reason));
    }
}
```

**Why separate aggregate**:
- Central consistency point for account-level invariants
- Independent lifecycle
- Reduces contention (positions/orders update separately)

---

### 3.4 Trade Aggregate (Read Model)

**Aggregate Root**: Trade
**Consistency Boundary**: None (immutable record)
**Invariants**: None (read-only historical data)

```csharp
public sealed class Trade : EntityBase<Guid>, IAggregateRoot
{
    // Immutable properties (set once on creation)
    public required string Symbol { get; init; }
    public required OrderSide Side { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal EntryPrice { get; init; }
    public required DateTime EntryTime { get; init; }
    public required decimal ExitPrice { get; init; }
    public required DateTime ExitTime { get; init; }
    public required decimal RealizedPnL { get; init; }
    public required string StrategyName { get; init; }

    // Calculated properties
    public decimal PnLPercent => (RealizedPnL / (EntryPrice * Quantity)) * 100m;
    public bool IsWinner => RealizedPnL > 0;
    public TimeSpan Duration => ExitTime - EntryTime;

    // No domain events (immutable)
    // No business methods (read-only)
}
```

**Why separate aggregate**:
- Denormalized read model
- No state changes after creation
- Query-optimized (no joins required)

---

## 4. Eventual Consistency Between Aggregates

### 4.1 Consistency Strategy

```
Order (fills) ──[OrderFilledEvent]──> Position (updates)
                                          │
                                          └──[PositionUpdatedEvent]──> Account (equity recalc)
```

**Pattern**:
1. Order aggregate raises `OrderFilledEvent`
2. Event handler updates Position aggregate
3. Position raises `PositionUpdatedEvent`
4. Event handler updates Account aggregate

**Benefits**:
- No locking across aggregates
- Independent transaction boundaries
- Scalable (concurrent order processing)

### 4.2 Example Flow

```csharp
// Step 1: Order filled (synchronous in same transaction)
order.MarkAsFilled(price, commission, timestamp);
await _orderRepository.SaveChangesAsync(); // Dispatches OrderFilledEvent

// Step 2: Event handler (in same transaction via before-save dispatch)
public async Task Handle(OrderFilledEvent evt, CancellationToken ct)
{
    var position = await _positionRepository.FirstOrDefaultAsync(spec, ct);

    if (position == null)
    {
        position = new Position { /* ... */ };
        await _positionRepository.AddAsync(position, ct);
    }
    else
    {
        position.IncreaseQuantity(evt.Quantity, evt.AverageFillPrice);
        await _positionRepository.UpdateAsync(position, ct);
    }

    await _positionRepository.SaveChangesAsync(ct); // Dispatches PositionUpdatedEvent
}

// Step 3: Account update (in separate transaction or after-save)
public async Task Handle(PositionUpdatedEvent evt, CancellationToken ct)
{
    var account = await _accountRepository.GetByIdAsync(evt.AccountId, ct);
    var totalPositionValue = await _positionRepository.GetTotalValueAsync(evt.AccountId, ct);

    account.UpdateEquity(totalPositionValue);
    await _accountRepository.UpdateAsync(account, ct);
    await _accountRepository.SaveChangesAsync(ct);
}
```

---

## 5. Aggregate Design Decisions

### 5.1 When to Create New Aggregate

**Create separate aggregate if**:
- ✅ Has independent lifecycle
- ✅ Can be consistently modified in isolation
- ✅ Does not require immediate consistency with other entities
- ✅ Has clear transactional boundary

**Keep in same aggregate if**:
- ❌ Must be consistent with parent at all times
- ❌ No independent identity (value object)
- ❌ Lifecycle fully dependent on parent
- ❌ Always modified together

### 5.2 Order vs Position vs Account Decision

| Criterion | Order | Position | Account | Separate? |
|-----------|-------|----------|---------|-----------|
| Independent lifecycle | ✅ | ✅ | ✅ | ✅ |
| Immediate consistency required | ❌ | ❌ | ❌ | ✅ |
| High contention | ✅ | ✅ | ✅ | ✅ (reduce locking) |
| Different update frequency | ✅ | ✅ | ❌ | ✅ |

**Conclusion**: Separate aggregates with eventual consistency

---

## 6. Common Anti-Patterns

### 6.1 Anti-Pattern: Large Aggregates

```csharp
// ❌ Bad: Aggregate too large
public class Account : EntityBase<string>, IAggregateRoot
{
    public List<Order> Orders { get; set; } // Loaded every time
    public List<Position> Positions { get; set; } // Loaded every time
    public List<Trade> Trades { get; set; } // Loaded every time
}
```

**Problem**:
- Loads hundreds of orders/positions unnecessarily
- High memory consumption
- Slow queries
- Contention (many concurrent updates)

**Solution**: Reference by ID, use eventual consistency
```csharp
// ✅ Good: Reference by ID
public class Account : EntityBase<string>, IAggregateRoot
{
    // No navigation properties
    // Orders/Positions reference Account by AccountId
}
```

### 6.2 Anti-Pattern: Modifying Multiple Aggregates in Single Transaction

```csharp
// ❌ Bad: Updating multiple aggregates directly
public async Task ExecuteTradeAsync(Order order, Position position, Account account)
{
    order.MarkAsFilled(price, commission, timestamp);
    position.IncreaseQuantity(order.Quantity, price);
    account.DeductCash(order.Quantity * price);

    await _orderRepository.SaveChangesAsync();
    await _positionRepository.SaveChangesAsync();
    await _accountRepository.SaveChangesAsync();
}
```

**Problem**:
- Violates aggregate boundaries
- Creates coupling
- Risk of partial failures

**Solution**: Use domain events for cross-aggregate updates
```csharp
// ✅ Good: Update one aggregate, let events handle others
public async Task ExecuteTradeAsync(Order order)
{
    order.MarkAsFilled(price, commission, timestamp);
    await _orderRepository.SaveChangesAsync(); // Raises OrderFilledEvent

    // Event handlers update Position and Account
}
```

### 6.3 Anti-Pattern: Exposing Aggregate Internals

```csharp
// ❌ Bad: Public setters allow bypassing invariants
public class Order : EntityBase<Guid>, IAggregateRoot
{
    public OrderStatus Status { get; set; } // Anyone can set
    public decimal FilledQuantity { get; set; } // Bypass validation
}

// External code bypasses invariants
order.Status = OrderStatus.Filled; // No validation!
order.FilledQuantity = -100; // Invalid state!
```

**Solution**: Encapsulate state changes in methods
```csharp
// ✅ Good: Encapsulated state changes
public class Order : EntityBase<Guid>, IAggregateRoot
{
    private OrderStatus _status;
    private decimal _filledQuantity;

    public OrderStatus Status => _status;
    public decimal FilledQuantity => _filledQuantity;

    public void MarkAsFilled(decimal fillPrice, decimal commission, DateTime filledAt)
    {
        // Validate state transition
        if (_status == OrderStatus.Filled)
            throw new InvalidOperationException("Order already filled");

        // Update state with validation
        _status = OrderStatus.Filled;
        _filledQuantity = Quantity;

        RegisterDomainEvent(new OrderFilledEvent(/* ... */));
    }
}
```

---

## 7. Testing Aggregate Invariants

### 7.1 Test Invariant Enforcement

```csharp
[Fact]
public void Order_MarkAsFilled_WhenAlreadyFilled_ShouldThrow()
{
    // Arrange
    var order = new Order
    {
        Id = Guid.NewGuid(),
        Symbol = "AAPL",
        Quantity = 100m,
        Status = OrderStatus.Filled
    };

    // Act & Assert
    Should.Throw<InvalidOperationException>(() =>
        order.MarkAsFilled(150m, 1.50m, DateTime.UtcNow));
}

[Fact]
public void Position_UpdatePrice_WithNegativePrice_ShouldThrow()
{
    // Arrange
    var position = new Position { /* ... */ };

    // Act & Assert
    Should.Throw<ArgumentException>(() => position.UpdatePrice(-100m));
}

[Fact]
public void Account_DeductCash_WhenInsufficientFunds_ShouldThrow()
{
    // Arrange
    var account = new Account { Cash = 1000m };

    // Act & Assert
    Should.Throw<InvalidOperationException>(() => account.DeductCash(2000m));
}
```

### 7.2 Test Aggregate Consistency

```csharp
[Fact]
public void Position_WhenClosed_ShouldCalculateCorrectPnL()
{
    // Arrange
    var position = new Position
    {
        Symbol = "AAPL",
        Side = OrderSide.Buy,
        Quantity = 100m,
        EntryPrice = 140m,
        CurrentPrice = 150m
    };

    // Act
    position.Close(exitPrice: 155m, DateTime.UtcNow);

    // Assert
    position.RealizedPnL.ShouldBe(1500m); // (155 - 140) * 100
    position.ExitTime.ShouldNotBeNull();
    position.ExitPrice.ShouldBe(155m);
}
```

---

## 8. Summary

**IAggregateRoot Marker**:
- Applied to root entities only
- Enforces repository pattern
- Documents consistency boundaries

**Trading Domain Aggregates**:
- Order - Order state and transitions
- Position - Position state and P&L
- Account - Cash balance and equity
- Trade - Immutable read model

**Key Principles**:
- Reference other aggregates by ID only
- Modify via root methods (encapsulation)
- Use domain events for cross-aggregate updates
- Keep aggregates small (single responsibility)

**Anti-Patterns to Avoid**:
- Large aggregates with many children
- Modifying multiple aggregates in single transaction
- Exposing internal state via public setters

**Next Steps**:
- Apply `IAggregateRoot` to Order, Position, Account
- Enforce invariants in business methods
- Use domain events for eventual consistency
