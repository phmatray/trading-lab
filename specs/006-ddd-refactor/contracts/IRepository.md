# Repository Interfaces Contract

**Purpose**: Define repository pattern contracts using Ardalis.SharedKernel abstractions
**Package**: Ardalis.SharedKernel, Ardalis.Specification.EntityFrameworkCore

---

## 1. Base Repository Interfaces

### 1.1 IRepository<T>

**Purpose**: Write repository for aggregate roots (add, update, delete operations)

```csharp
using Ardalis.Specification;

namespace TradingBot.Core.Interfaces;

public interface IRepository<T> : IRepositoryBase<T>
    where T : class, IAggregateRoot
{
    // Inherits from IRepositoryBase<T>:
    // - Task<T> AddAsync(T entity, CancellationToken ct = default)
    // - Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    // - Task UpdateAsync(T entity, CancellationToken ct = default)
    // - Task DeleteAsync(T entity, CancellationToken ct = default)
    // - Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    // - Task<int> SaveChangesAsync(CancellationToken ct = default)

    // Plus all read methods from IReadRepositoryBase<T>
}
```

**Usage Example**:
```csharp
public class OrderService
{
    private readonly IRepository<Order> _orderRepository;

    public async Task ExecuteOrderAsync(Order order, CancellationToken ct)
    {
        order.MarkAsFilled(price, commission, DateTime.UtcNow);
        await _orderRepository.UpdateAsync(order, ct);
        await _orderRepository.SaveChangesAsync(ct); // Dispatches domain events
    }
}
```

---

### 1.2 IReadRepository<T>

**Purpose**: Read-only repository for queries (no write operations)

```csharp
public interface IReadRepository<T> : IReadRepositoryBase<T>
    where T : class, IAggregateRoot
{
    // Inherits from IReadRepositoryBase<T>:

    // By ID
    // Task<T?> GetByIdAsync<TId>(TId id, CancellationToken ct = default)

    // Single result with specification
    // Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken ct = default)
    // Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> spec, CancellationToken ct = default)

    // List results
    // Task<List<T>> ListAsync(CancellationToken ct = default)
    // Task<List<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default)
    // Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> spec, CancellationToken ct = default)

    // Count and existence
    // Task<int> CountAsync(ISpecification<T> spec, CancellationToken ct = default)
    // Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken ct = default)
}
```

**Usage Example**:
```csharp
public class OrderQueryService
{
    private readonly IReadRepository<Order> _orderReadRepository;

    public async Task<List<Order>> GetPendingOrdersAsync(CancellationToken ct)
    {
        var spec = new PendingOrdersSpec();
        return await _orderReadRepository.ListAsync(spec, ct);
    }

    public async Task<int> CountOrdersBySymbolAsync(string symbol, CancellationToken ct)
    {
        var spec = new OrdersBySymbolSpec(symbol);
        return await _orderReadRepository.CountAsync(spec, ct);
    }
}
```

---

## 2. Specification Pattern

### 2.1 Specification<T>

**Purpose**: Encapsulate query logic in reusable, testable specifications

```csharp
using Ardalis.Specification;

public sealed class PendingOrdersSpec : Specification<Order>
{
    public PendingOrdersSpec()
    {
        Query.Where(o => o.Status == OrderStatus.Pending)
             .OrderBy(o => o.CreatedAt);
    }
}

public sealed class OrdersBySymbolSpec : Specification<Order>
{
    public OrdersBySymbolSpec(string symbol)
    {
        Query.Where(o => o.Symbol == symbol)
             .OrderByDescending(o => o.CreatedAt);
    }
}

public sealed class PaginatedOrdersSpec : Specification<Order>
{
    public PaginatedOrdersSpec(int skip, int take, string? symbol = null)
    {
        Query.OrderByDescending(o => o.CreatedAt)
             .Skip(skip)
             .Take(take);

        if (!string.IsNullOrEmpty(symbol))
        {
            Query.Where(o => o.Symbol == symbol);
        }
    }
}
```

### 2.2 Specification<T, TResult> (Projection)

**Purpose**: Query with projection to DTO/view model

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

**Usage**:
```csharp
var spec = new OrderSummarySpec("AAPL");
var summaries = await _orderReadRepository.ListAsync(spec, ct);
// Returns List<OrderSummaryDto> directly
```

---

## 3. Repository Implementations

### 3.1 Generic EF Repository

```csharp
using Ardalis.Specification.EntityFrameworkCore;

namespace TradingBot.Infrastructure.Persistence.Repositories;

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

### 3.2 DI Registration

```csharp
// Infrastructure/ServiceCollectionExtensions.cs
public static IServiceCollection AddRepositories(this IServiceCollection services)
{
    // Generic repositories
    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
    services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));

    return services;
}
```

---

## 4. Aggregate-Specific Repository Interfaces

**Current Interfaces** (will be updated to extend SharedKernel interfaces):

### 4.1 IOrderRepository

```csharp
public interface IOrderRepository : IRepository<Order>
{
    // Inherits all base repository methods
    // Add domain-specific methods if needed
    // (Most queries should use specifications instead)
}
```

### 4.2 IPositionRepository

```csharp
public interface IPositionRepository : IRepository<Position>
{
    // Inherits all base repository methods
    Task<List<Position>> GetOpenPositionsAsync(CancellationToken ct = default);
}
```

### 4.3 IAccountRepository

```csharp
public interface IAccountRepository : IRepository<Account>
{
    // Inherits all base repository methods
    Task<Account?> GetByAccountIdAsync(string accountId, CancellationToken ct = default);
}
```

### 4.4 ITradeRepository

```csharp
public interface ITradeRepository : IRepository<Trade>
{
    // Inherits all base repository methods
}
```

### 4.5 ICandleRepository

```csharp
public interface ICandleRepository : IRepository<Candle>
{
    // Inherits all base repository methods
    Task<List<Candle>> GetHistoricalDataAsync(
        string symbol,
        TimeFrame interval,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);
}
```

---

## 5. Specification Examples for Trading Domain

### 5.1 Order Specifications

```csharp
// Core/Specifications/OrderSpecifications.cs

public sealed class OpenOrdersSpec : Specification<Order>
{
    public OpenOrdersSpec()
    {
        Query.Where(o => o.Status == OrderStatus.Pending)
             .OrderBy(o => o.CreatedAt);
    }
}

public sealed class FilledOrdersByStrategySpec : Specification<Order>
{
    public FilledOrdersByStrategySpec(string strategyName, DateTime? startDate = null)
    {
        Query.Where(o => o.StrategyName == strategyName && o.Status == OrderStatus.Filled);

        if (startDate.HasValue)
        {
            Query.Where(o => o.CreatedAt >= startDate.Value);
        }

        Query.OrderByDescending(o => o.FilledAt);
    }
}

public sealed class OrderByIdSpec : SingleResultSpecification<Order>
{
    public OrderByIdSpec(Guid orderId)
    {
        Query.Where(o => o.Id == orderId);
    }
}
```

### 5.2 Position Specifications

```csharp
// Core/Specifications/PositionSpecifications.cs

public sealed class OpenPositionsSpec : Specification<Position>
{
    public OpenPositionsSpec()
    {
        Query.OrderBy(p => p.Symbol);
    }
}

public sealed class PositionsBySymbolSpec : Specification<Position>
{
    public PositionsBySymbolSpec(string symbol)
    {
        Query.Where(p => p.Symbol == symbol);
    }
}

public sealed class PositionsInProfitSpec : Specification<Position>
{
    public PositionsInProfitSpec()
    {
        Query.Where(p =>
            (p.Side == OrderSide.Buy && p.CurrentPrice > p.EntryPrice) ||
            (p.Side == OrderSide.Sell && p.CurrentPrice < p.EntryPrice));
    }
}
```

### 5.3 Trade Specifications with Projections

```csharp
public record TradeStatisticsDto(
    int TotalTrades,
    int WinningTrades,
    int LosingTrades,
    decimal WinRate,
    decimal TotalPnL,
    decimal AveragePnL);

public sealed class TradeStatisticsSpec : Specification<Trade>
{
    public TradeStatisticsSpec(string? strategyName = null, DateTime? startDate = null)
    {
        if (!string.IsNullOrEmpty(strategyName))
        {
            Query.Where(t => t.StrategyName == strategyName);
        }

        if (startDate.HasValue)
        {
            Query.Where(t => t.EntryTime >= startDate.Value);
        }
    }
}
```

---

## 6. Best Practices

### 6.1 Specification Naming

- Use descriptive names: `PendingOrdersSpec`, not `OrderSpec1`
- Include filter criteria in name: `OrdersBySymbolSpec`, `PositionsInProfitSpec`
- Use `Spec` suffix for consistency

### 6.2 Repository Usage

- Use `IReadRepository<T>` for queries (read-only)
- Use `IRepository<T>` for commands (write operations)
- Prefer specifications over custom repository methods
- Keep repositories focused on aggregate roots only

### 6.3 Query Performance

- Use `.AsNoTracking()` for read-only queries:
  ```csharp
  Query.Where(o => o.Symbol == symbol).AsNoTracking();
  ```

- Include related entities when needed:
  ```csharp
  Query.Include(o => o.Position).ThenInclude(p => p.Account);
  ```

- Add caching for frequently used specifications:
  ```csharp
  Query.WithCacheKey($"PendingOrders_{strategyName}");
  ```

### 6.4 Testing Specifications

```csharp
[Fact]
public async Task PendingOrdersSpec_ReturnsOnlyPendingOrders()
{
    // Arrange
    var orders = new List<Order>
    {
        new Order { Status = OrderStatus.Pending, /* ... */ },
        new Order { Status = OrderStatus.Filled, /* ... */ },
        new Order { Status = OrderStatus.Pending, /* ... */ }
    };

    await _repository.AddRangeAsync(orders);
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

## 7. Migration Path

### 7.1 Current State

Existing custom repository interfaces:
- `IOrderRepository`, `IPositionRepository`, etc.
- Custom LINQ queries in services

### 7.2 Target State

Repositories extending Ardalis.SharedKernel:
- `IRepository<Order>`, `IReadRepository<Order>`
- Specifications for all queries
- Generic `EfRepository<T>` implementation

### 7.3 Migration Steps

1. **Add packages**:
   ```bash
   dotnet add src/TradingBot.Core package Ardalis.SharedKernel
   dotnet add src/TradingBot.Infrastructure package Ardalis.Specification.EntityFrameworkCore
   ```

2. **Update Core interfaces**:
   ```csharp
   // Before
   public interface IOrderRepository
   {
       Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
       Task<List<Order>> GetAllAsync(CancellationToken ct);
       Task AddAsync(Order order, CancellationToken ct);
   }

   // After
   public interface IOrderRepository : IRepository<Order>
   {
       // Base methods inherited, add domain-specific if needed
   }
   ```

3. **Create specifications**:
   ```csharp
   // Move queries from repositories to specifications
   // Before (in repository):
   public async Task<List<Order>> GetPendingOrdersAsync(CancellationToken ct)
   {
       return await _context.Orders
           .Where(o => o.Status == OrderStatus.Pending)
           .ToListAsync(ct);
   }

   // After (specification):
   public class PendingOrdersSpec : Specification<Order>
   {
       public PendingOrdersSpec()
       {
           Query.Where(o => o.Status == OrderStatus.Pending);
       }
   }
   ```

4. **Update repository implementations**:
   ```csharp
   // Before (custom implementation)
   public class OrderRepository : IOrderRepository
   {
       private readonly TradingBotDbContext _context;
       // ... manual CRUD methods
   }

   // After (use generic base)
   public class OrderRepository : EfRepository<Order>, IOrderRepository
   {
       public OrderRepository(TradingBotDbContext context) : base(context) { }
   }
   ```

5. **Update service layer**:
   ```csharp
   // Before
   var orders = await _orderRepository.GetPendingOrdersAsync(ct);

   // After
   var spec = new PendingOrdersSpec();
   var orders = await _orderRepository.ListAsync(spec, ct);
   ```

---

## Summary

**Key Contracts**:
- `IRepository<T>` - Write operations for aggregate roots
- `IReadRepository<T>` - Read-only queries
- `Specification<T>` - Encapsulated query logic
- `Specification<T, TResult>` - Queries with projections

**Benefits**:
- Type-safe, reusable queries
- Testable query logic
- Clean service layer
- Consistent API across all aggregates

**Next Steps**:
- Create specifications for common queries
- Update existing repositories to extend SharedKernel interfaces
- Refactor service layer to use specifications
