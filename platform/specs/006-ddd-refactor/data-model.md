# Data Model: DDD Entity Modeling

**Date**: 2025-01-15
**Status**: Complete
**Purpose**: Define domain entities, value objects, and aggregate boundaries for DDD refactoring

---

## 1. Aggregate Boundaries

### 1.1 Aggregate Overview

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│   Order     │     │   Position   │     │   Account   │
│  (Aggregate │     │  (Aggregate  │     │  (Aggregate │
│    Root)    │     │    Root)     │     │    Root)    │
└─────────────┘     └──────────────┘     └─────────────┘
       │                    │                    │
       │                    │                    │
       └────────────────────┴────────────────────┘
              Eventual Consistency via Domain Events
```

**Design Principles**:
- Each aggregate is a consistency boundary
- Aggregates reference each other by ID only
- Domain events maintain eventual consistency
- Aggregates are kept small for performance

---

## 2. Entities (with Identity)

### 2.1 Order Aggregate

**Aggregate Root**: Order
**ID Type**: Guid
**Lifecycle**: Created → Submitted → (Filled | Cancelled | Rejected)
**Invariants**:
- Quantity must be positive
- Limit price must be positive (if specified)
- Stop price must be positive (if specified)
- Status transitions are valid (Pending → Filled, not Filled → Pending)

```csharp
public sealed class Order : EntityBase<Guid>, IAggregateRoot
{
    // Properties
    public required string Symbol { get; set; }
    public required OrderType Type { get; set; }       // SmartEnum
    public required OrderSide Side { get; set; }       // SmartEnum
    public required decimal Quantity { get; set; }
    public decimal? LimitPrice { get; set; }
    public decimal? StopPrice { get; set; }
    public required OrderStatus Status { get; set; }   // SmartEnum
    public required DateTime CreatedAt { get; set; }
    public DateTime? FilledAt { get; set; }
    public decimal FilledQuantity { get; set; }
    public decimal AverageFillPrice { get; set; }
    public decimal Commission { get; set; }
    public required string StrategyName { get; set; }

    // Domain Events
    // - OrderCreatedEvent
    // - OrderSubmittedEvent
    // - OrderFilledEvent
    // - OrderPartiallyFilledEvent
    // - OrderCancelledEvent
    // - OrderRejectedEvent

    // Business Methods
    public void MarkAsFilled(decimal fillPrice, decimal commission, DateTime filledAt)
    {
        if (Status == OrderStatus.Filled)
            throw new InvalidOperationException("Order already filled");

        Status = OrderStatus.Filled;
        FilledQuantity = Quantity;
        AverageFillPrice = fillPrice;
        Commission = commission;
        FilledAt = filledAt;

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

**Database Table**: Orders
**Relationships**:
- References Account by AccountId (string)
- References Position by PositionId (Guid, nullable)

---

### 2.2 Position Aggregate

**Aggregate Root**: Position
**ID Type**: Guid
**Lifecycle**: Opened → Updated → Closed
**Invariants**:
- Quantity must be positive
- Entry price must be positive
- Current price must be positive
- Realized P&L calculated correctly on close

```csharp
public sealed class Position : EntityBase<Guid>, IAggregateRoot
{
    // Properties
    public required string Symbol { get; set; }
    public required OrderSide Side { get; set; }          // SmartEnum
    public required decimal Quantity { get; set; }
    public required decimal EntryPrice { get; set; }
    public required DateTime EntryTime { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime? ExitTime { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal UnrealizedPnL => CalculateUnrealizedPnL();
    public required string StrategyName { get; set; }
    public string? AccountId { get; set; }

    // Domain Events
    // - PositionOpenedEvent
    // - PositionUpdatedEvent
    // - PositionClosedEvent
    // - PositionPriceUpdatedEvent

    // Business Methods
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

        ExitPrice = exitPrice;
        ExitTime = exitTime;
        RealizedPnL = CalculateRealizedPnL(exitPrice);

        RegisterDomainEvent(new PositionClosedEvent(Id, Symbol, RealizedPnL));
    }

    public void IncreaseQuantity(decimal additionalQuantity, decimal newAveragePrice)
    {
        if (additionalQuantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(additionalQuantity));

        Quantity += additionalQuantity;
        EntryPrice = newAveragePrice; // Recalculate average entry

        RegisterDomainEvent(new PositionQuantityIncreasedEvent(Id, Symbol, Quantity));
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

**Database Table**: Positions
**Relationships**:
- References Account by AccountId (string)

---

### 2.3 Account Aggregate

**Aggregate Root**: Account
**ID Type**: string (e.g., "ACC-12345")
**Lifecycle**: Created → Active → Suspended → Closed
**Invariants**:
- Cash balance must not go negative
- Equity = Cash + PositionValue
- Leverage must not exceed max leverage

```csharp
public sealed class Account : EntityBase<string>, IAggregateRoot
{
    // Override Id to use string instead of struct
    public new required string Id { get; set; }

    // Properties
    public required string Name { get; set; }
    public required decimal InitialCapital { get; set; }
    public decimal Cash { get; private set; }
    public decimal Equity { get; private set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsActive { get; set; } = true;

    // Domain Events
    // - AccountCreatedEvent
    // - CashUpdatedEvent
    // - EquityUpdatedEvent
    // - AccountSuspendedEvent

    // Business Methods
    public void DeductCash(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        if (Cash < amount)
            throw new InvalidOperationException($"Insufficient funds. Available: {Cash}, Required: {amount}");

        Cash -= amount;
        LastUpdated = DateTime.UtcNow;

        RegisterDomainEvent(new CashUpdatedEvent(Id, Cash));
    }

    public void AddCash(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        Cash += amount;
        LastUpdated = DateTime.UtcNow;

        RegisterDomainEvent(new CashUpdatedEvent(Id, Cash));
    }

    public void UpdateEquity(decimal totalPositionValue)
    {
        Equity = Cash + totalPositionValue;
        LastUpdated = DateTime.UtcNow;

        RegisterDomainEvent(new EquityUpdatedEvent(Id, Equity));
    }

    public void Suspend(string reason)
    {
        IsActive = false;
        LastUpdated = DateTime.UtcNow;

        RegisterDomainEvent(new AccountSuspendedEvent(Id, reason));
    }
}
```

**Database Table**: Accounts
**Relationships**:
- Has many Positions (eventually consistent)
- Has many Orders (eventually consistent)

---

### 2.4 Trade Entity (Read Model)

**Aggregate Root**: Trade
**ID Type**: Guid
**Lifecycle**: Created (immutable)
**Invariants**: None (read-only historical record)

```csharp
public sealed class Trade : EntityBase<Guid>, IAggregateRoot
{
    // Properties
    public required string Symbol { get; set; }
    public required OrderSide Side { get; set; }         // SmartEnum
    public required decimal Quantity { get; set; }
    public required decimal EntryPrice { get; set; }
    public required DateTime EntryTime { get; set; }
    public required decimal ExitPrice { get; set; }
    public required DateTime ExitTime { get; set; }
    public required decimal RealizedPnL { get; set; }
    public decimal Commission { get; set; }
    public required string StrategyName { get; set; }
    public decimal PnLPercent => (RealizedPnL / (EntryPrice * Quantity)) * 100m;
    public bool IsWinner => RealizedPnL > 0;
    public TimeSpan Duration => ExitTime - EntryTime;

    // No domain events (immutable record)
}
```

**Database Table**: Trades
**Relationships**: None (denormalized read model)

---

### 2.5 Candle Entity (Market Data)

**Aggregate Root**: Candle
**ID Type**: long (auto-increment)
**Lifecycle**: Created (immutable)
**Invariants**:
- Open, High, Low, Close must be positive
- High >= Low
- High >= Open, High >= Close
- Low <= Open, Low <= Close

```csharp
public sealed class Candle : EntityBase<long>, IAggregateRoot
{
    // Properties
    public required string Symbol { get; set; }
    public required TimeFrame Interval { get; set; }    // SmartEnum
    public required DateTime Timestamp { get; set; }
    public required decimal Open { get; set; }
    public required decimal High { get; set; }
    public required decimal Low { get; set; }
    public required decimal Close { get; set; }
    public required long Volume { get; set; }

    // Calculated properties
    public decimal Range => High - Low;
    public decimal Body => Math.Abs(Close - Open);
    public bool IsBullish => Close > Open;
    public bool IsBearish => Close < Open;

    // No domain events (immutable market data)
}
```

**Database Table**: Candles
**Relationships**: None
**Indexes**: Composite index on (Symbol, Interval, Timestamp) for efficient queries

---

## 3. Value Objects (no Identity)

### 3.1 Quote (Market Data)

**Status**: Already a record ✅ (no changes needed)
**Equality**: Attribute-based (all properties)
**Mutability**: Immutable (init-only properties)

```csharp
public sealed record Quote
{
    public required string Symbol { get; init; }
    public required DateTime Timestamp { get; init; }
    public required decimal Price { get; init; }
    public required decimal Bid { get; init; }
    public required decimal Ask { get; init; }
    public required long Volume { get; init; }

    public decimal Spread => Ask - Bid;
    public decimal MidPrice => (Bid + Ask) / 2m;
}
```

**Persistence**: Not persisted (transient market data)

---

### 3.2 SymbolInfo (Reference Data)

**Status**: Already a record ✅ (no changes needed)
**Equality**: Attribute-based
**Mutability**: Immutable

```csharp
public sealed record SymbolInfo
{
    public required string Symbol { get; init; }
    public required string Name { get; init; }
    public required string Exchange { get; init; }
    public required string AssetType { get; init; }
    public required string Currency { get; init; }
    public decimal TickSize { get; init; }
    public decimal LotSize { get; init; }
    public bool IsTradable { get; init; }
}
```

**Persistence**: Cached in memory or external service

---

### 3.3 RiskParameters (Configuration)

**Status**: Already a record ✅ (no changes needed)
**Equality**: Attribute-based
**Mutability**: Immutable

```csharp
public sealed record RiskParameters
{
    public required decimal MaxLeverage { get; init; }
    public required decimal MaxPositionSizePercent { get; init; }
    public required decimal DefaultStopLossPercent { get; init; }
    public required decimal DefaultTakeProfitPercent { get; init; }
    public required decimal MaxDailyLossPercent { get; init; }
    public required decimal MaxDrawdownPercent { get; init; }
}
```

**Persistence**: Part of RiskSettings entity (owned type)

---

### 3.4 EquityPoint (Analytics)

**Status**: Currently duplicated (Models/Analytics/EquityPoint.cs and Models/Portfolio/EquityPoint.cs)
**Action**: Merge into single definition in Models/Analytics/
**Equality**: Attribute-based (Timestamp + Value)
**Mutability**: Immutable

```csharp
// MERGED VERSION (canonical location: Models/Analytics/EquityPoint.cs)
public sealed record EquityPoint
{
    public required DateTime Timestamp { get; init; }
    public required decimal Value { get; init; }
}
```

**Persistence**: Part of PerformanceMetrics or embedded in charts

---

### 3.5 Money (Recommended New Value Object)

**Status**: New
**Purpose**: Type-safe representation of monetary values with currency
**Equality**: Amount + Currency
**Mutability**: Immutable

```csharp
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) =>
        new Money(left.Amount - right.Amount, left.Currency);

    public override string ToString() => $"{Amount:N2} {Currency}";
}
```

**Usage**:
```csharp
// Instead of: decimal cash = 10000m;
// Use: Money cash = new Money(10000m, "USD");
```

**Persistence**: Owned type in Account entity

---

## 4. Duplicate Class Resolution

### 4.1 RiskSettings Consolidation

**Duplicate Locations**:
1. `Models/Risk/RiskSettings.cs` (original)
2. `Models/Configuration/RiskSettings.cs` (web-specific)

**Resolution**: Keep `Models/Configuration/RiskSettings.cs` as canonical version
**Rationale**:
- Web version has database persistence (Id, CreatedAt, LastModified)
- Risk version is configuration-only
- Configuration version is entity (persisted), Risk version could be value object

**Canonical Entity**:
```csharp
// Models/Configuration/RiskSettings.cs (KEEP THIS)
public sealed class RiskSettings : EntityBase<Guid>, IAggregateRoot
{
    public decimal MaxPositionSizePercent { get; set; } = 10m;
    public decimal StopLossPercent { get; set; } = 2m;
    public decimal TakeProfitPercent { get; set; } = 5m;
    public int MaxOpenPositions { get; set; } = 5;
    public decimal MaxDailyLossPercent { get; set; } = 5m;
    public DateTime LastModified { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Migration**:
1. Delete `Models/Risk/RiskSettings.cs`
2. Update all `using TradingBot.Core.Models.Risk;` to `using TradingBot.Core.Models.Configuration;`
3. No database migration needed (same table structure)

---

### 4.2 EquityPoint Consolidation

**Duplicate Locations**:
1. `Models/Analytics/EquityPoint.cs`
2. `Models/Portfolio/EquityPoint.cs`

**Resolution**: Keep `Models/Analytics/EquityPoint.cs` as canonical version
**Rationale**:
- Analytics is the primary use case (equity curves, performance charts)
- Portfolio version is likely redundant

**Canonical Value Object**:
```csharp
// Models/Analytics/EquityPoint.cs (KEEP THIS)
public sealed record EquityPoint
{
    public required DateTime Timestamp { get; init; }
    public required decimal Value { get; init; }
}
```

**Migration**:
1. Delete `Models/Portfolio/EquityPoint.cs`
2. Update all `using TradingBot.Core.Models.Portfolio;` to `using TradingBot.Core.Models.Analytics;` (for EquityPoint only)
3. No database impact (not persisted directly)

---

## 5. Domain Events

### 5.1 Event Naming Convention

Format: `{Entity}{Action}Event`
- OrderFilledEvent
- PositionClosedEvent
- AccountSuspendedEvent

All events extend `DomainEventBase` from Ardalis.SharedKernel.

### 5.2 Order Events

```csharp
// Core/Events/OrderFilledEvent.cs
public sealed class OrderFilledEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public string Symbol { get; }
    public decimal Quantity { get; }
    public decimal AverageFillPrice { get; }
    public decimal Commission { get; }

    public OrderFilledEvent(Guid orderId, string symbol, decimal quantity,
        decimal price, decimal commission)
    {
        OrderId = orderId;
        Symbol = symbol;
        Quantity = quantity;
        AverageFillPrice = price;
        Commission = commission;
    }
}

public sealed class OrderCancelledEvent : DomainEventBase
{
    public Guid OrderId { get; }
    public string Symbol { get; }

    public OrderCancelledEvent(Guid orderId, string symbol)
    {
        OrderId = orderId;
        Symbol = symbol;
    }
}
```

### 5.3 Position Events

```csharp
public sealed class PositionOpenedEvent : DomainEventBase
{
    public Guid PositionId { get; }
    public string Symbol { get; }
    public OrderSide Side { get; }
    public decimal Quantity { get; }
    public decimal EntryPrice { get; }

    public PositionOpenedEvent(Guid positionId, string symbol,
        OrderSide side, decimal quantity, decimal entryPrice)
    {
        PositionId = positionId;
        Symbol = symbol;
        Side = side;
        Quantity = quantity;
        EntryPrice = entryPrice;
    }
}

public sealed class PositionClosedEvent : DomainEventBase
{
    public Guid PositionId { get; }
    public string Symbol { get; }
    public decimal RealizedPnL { get; }

    public PositionClosedEvent(Guid positionId, string symbol, decimal realizedPnL)
    {
        PositionId = positionId;
        Symbol = symbol;
        RealizedPnL = realizedPnL;
    }
}
```

### 5.4 Account Events

```csharp
public sealed class CashUpdatedEvent : DomainEventBase
{
    public string AccountId { get; }
    public decimal NewBalance { get; }

    public CashUpdatedEvent(string accountId, decimal newBalance)
    {
        AccountId = accountId;
        NewBalance = newBalance;
    }
}

public sealed class EquityUpdatedEvent : DomainEventBase
{
    public string AccountId { get; }
    public decimal NewEquity { get; }

    public EquityUpdatedEvent(string accountId, decimal newEquity)
    {
        AccountId = accountId;
        NewEquity = newEquity;
    }
}
```

---

## 6. Entity Relationships

### 6.1 Relationship Diagram

```
Account (string Id)
   │
   ├──> Orders (by AccountId reference)
   │      └──> Order.AccountId: string (FK)
   │
   └──> Positions (by AccountId reference)
          └──> Position.AccountId: string (FK)

Order (Guid Id)
   └──> Position (by PositionId reference)
          └──> Order.PositionId: Guid? (nullable FK)

Position (Guid Id)
   └──> Account (by AccountId reference)
          └──> Position.AccountId: string (FK)

Trade (Guid Id) - Denormalized (no FK relationships)

Candle (long Id) - Market data (no FK relationships)
```

### 6.2 Eventual Consistency Flow

```
1. Order.MarkAsFilled()
   └──> Raises OrderFilledEvent

2. OrderFilledEventHandler receives event
   └──> Updates Position (opens new or increases existing)
   └──> Raises PositionOpenedEvent or PositionQuantityIncreasedEvent

3. PositionEventHandler receives event
   └──> Updates Account.Equity
   └──> Raises EquityUpdatedEvent

4. AccountEventHandler receives event
   └──> Publishes SignalR notification to web clients
```

---

## 7. Database Schema Impact

### 7.1 No Schema Changes Required

**Rationale**:
- EntityBase<TId> uses standard Id property that maps to existing PK columns
- Domain events are not persisted (ignored via `[NotMapped]` or configuration)
- Relationships remain unchanged (FKs by ID reference)

### 7.2 Entity Configuration Updates

All entity configurations must ignore `DomainEvents` property:

```csharp
// Infrastructure/Persistence/Configurations/OrderConfiguration.cs
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        // CRITICAL: Ignore domain events
        builder.Ignore(o => o.DomainEvents);

        // ... other configurations
    }
}
```

---

## Summary

**Entities (Aggregate Roots)**:
- Order (Guid) - Trading orders
- Position (Guid) - Open positions
- Account (string) - Trading accounts
- Trade (Guid) - Historical trades (read model)
- Candle (long) - Market data

**Value Objects**:
- Quote - Real-time market quote
- SymbolInfo - Symbol metadata
- RiskParameters - Risk configuration
- EquityPoint - CONSOLIDATED (single definition)
- Money - NEW (optional, type-safe monetary values)

**Duplicate Resolution**:
- RiskSettings → Keep Models/Configuration/RiskSettings.cs (entity)
- EquityPoint → Keep Models/Analytics/EquityPoint.cs (value object)

**Domain Events**:
- OrderFilledEvent, OrderCancelledEvent
- PositionOpenedEvent, PositionClosedEvent
- CashUpdatedEvent, EquityUpdatedEvent

**Aggregate Boundaries**:
- Separate aggregates for Order, Position, Account
- Eventual consistency via domain events
- No circular references (ID-based relationships)
