# Quickstart Guide: Weekly Cash-Managed Trading Strategy

**Feature**: Weekly Cash-Managed Trading Strategy
**Branch**: `007-weekly-cash-managed-strategy`
**Target Audience**: Developers implementing this feature

## Feature Overview

The Weekly Cash-Managed Trading Strategy is an automated trading system that buys ETP (Exchange-Traded Product) shares when the underlying asset (e.g., COIN) is above its 20-day moving average (MA20) and sells when it stays below MA20 for 2+ consecutive days. The strategy maintains a healthy cash buffer (15-25% of total equity) through configurable buy/sell ratios (default 5% buy, 10% sell weekly) and includes an optional breakout rule to accelerate buying during strong momentum periods. It integrates with the existing DDD architecture, executes on a weekly schedule (default Friday), and provides real-time dashboard updates via SignalR.

## Prerequisites

### Required Knowledge

Before working on this feature, you should understand:

- **Domain-Driven Design (DDD)**: Aggregate roots, domain events, repositories, value objects
- **Ardalis.SharedKernel**: `EntityBase<T>`, `IAggregateRoot`, `DomainEventBase`, repository patterns
- **Entity Framework Core 10**: Fluent API configuration, migrations, in-memory databases for testing
- **ASP.NET Core Blazor Server**: Razor components, component lifecycle, parameter binding
- **SignalR**: Real-time communication, hub methods, MessagePack serialization
- **MediatR**: Domain event dispatching, notification handlers
- **Async/Await**: Proper async patterns, `CancellationToken` usage
- **Testing**: xUnit, FakeItEasy (mocking), Shouldly (assertions), bUnit (Blazor component testing)

### Development Environment Setup

1. **Install .NET 10 SDK**: Ensure you have .NET 10 SDK installed
   ```bash
   dotnet --version  # Should output 10.x.x
   ```

2. **Clone and checkout the feature branch**:
   ```bash
   cd /Users/phmatray/Repositories/github-phm/TradingBot
   git checkout 007-weekly-cash-managed-strategy
   ```

3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

4. **Verify build**:
   ```bash
   dotnet build
   ```

5. **Run existing tests** to ensure baseline passes:
   ```bash
   dotnet test
   ```

6. **Set up the database**:
   ```bash
   dotnet ef database update --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web
   ```

## Project Structure

### Core Layer (`src/TradingBot.Core/`)

**Domain Entities** - Create these in `Entities/`:
- `WeeklyCashManagedStrategy.cs` - Aggregate root extending `EntityBase<Guid>`, implements `IAggregateRoot`
  - Manages strategy configuration (cash ratios, buy/sell ratios, symbol mappings)
  - Tracks state (days_below_ma20, last_execution_timestamp, is_enabled)
  - Raises domain events (StrategyConfiguredEvent, StrategyExecutedEvent, StrategyDisabledEvent)

**Value Objects** - Create these in `ValueObjects/`:
- `StrategyConfiguration.cs` - Immutable configuration value object (MIN_CASH_RATIO, MAX_CASH_RATIO, WEEKLY_BUY_RATIO, WEEKLY_SELL_RATIO)
- `BreakoutRuleConfig.cs` - Optional breakout rule parameters (price threshold, volume multiplier, buy ratio multiplier)

**Domain Events** - Create these in `Events/`:
- `StrategyConfiguredEvent.cs` - Raised when strategy is configured
- `StrategyExecutedEvent.cs` - Raised when weekly routine executes
- `StrategyDisabledEvent.cs` - Raised when strategy is disabled
- `MA20UpdatedEvent.cs` - Raised when daily MA20 calculation completes

**Repository Interfaces** - Create these in `Interfaces/`:
- `IWeeklyCashManagedStrategyRepository.cs` - Extends `IRepositoryBase<WeeklyCashManagedStrategy>`
- `IMA20IndicatorService.cs` - Service for MA20 calculations
- `IWeeklyRoutineExecutor.cs` - Service for weekly routine execution

### Infrastructure Layer (`src/TradingBot.Infrastructure/`)

**Persistence Configuration** - Create in `Persistence/Configurations/`:
- `WeeklyCashManagedStrategyConfiguration.cs` - EF Core fluent configuration
  - Map all properties to database columns
  - Configure value object conversions (JSON for BreakoutRuleConfig)
  - **CRITICAL**: Ignore DomainEvents property: `builder.Ignore(e => e.DomainEvents);`

**Repository Implementations** - Create in `Persistence/Repositories/`:
- `WeeklyCashManagedStrategyRepository.cs` - Extends `EfRepository<WeeklyCashManagedStrategy>`

**Services** - Create in `Services/`:
- `MA20IndicatorService.cs` - Implements sliding window MA20 calculation (O(1) performance)
  - Maintains 20-day sliding window with running sum
  - Handles weekend/holiday gaps by detecting timestamp differences
  - Validates minimum 20 days of historical data before activation

### Engine Layer (`src/TradingBot.Engine/`)

**Weekly Routine Logic** - Create in `WeeklyRoutine/`:
- `WeeklyRoutineExecutor.cs` - Orchestrates weekly execution flow
  - Checks if today is execution day (default Friday)
  - Calculates current equity and cash ratio
  - Executes buy logic (if COIN > MA20 and cash_ratio > MIN_CASH_RATIO)
  - Executes sell logic (if days_below_ma20 >= 2)
  - Calls CashBufferManager for rebalancing
  - All orders go through OrderExecutionService and RiskManager

- `CashBufferManager.cs` - Manages cash buffer adjustments
  - If cash_ratio < MIN_CASH_RATIO: Sell WEEKLY_SELL_RATIO of position
  - If cash_ratio > MAX_CASH_RATIO: Buy with excess cash (if COIN > MA20)

- `BreakoutDetector.cs` - Detects breakout conditions (optional)
  - Checks weekly price increase > 10% (configurable)
  - Validates volume > 1.5x average (configurable)
  - Returns accelerated buy ratio multiplier (default 2x)

### Web Layer (`src/TradingBot.Web/`)

**Blazor Components** - Create in `Components/Features/WeeklyCashStrategy/`:
- `StrategyConfigurationForm.razor` - Form for configuring strategy parameters
  - Input fields for cash ratios, buy/sell ratios, symbol mappings
  - Validation with error messages (MIN_CASH_RATIO < MAX_CASH_RATIO, values in [0,1])
  - Enable/disable toggle
  - Breakout rule configuration section

- `StrategyStateCard.razor` - Real-time state display card
  - Shows current cash ratio, position size, days_below_ma20
  - Displays current COIN price, ETP price, MA20 value
  - Next scheduled execution time
  - Updates via SignalR

- `StrategyDetailsPanel.razor` - Detailed metrics panel
  - Historical equity curve for this strategy
  - Trade history (buy/sell events)
  - Performance metrics (Sharpe ratio, max drawdown specific to this strategy)

**Services** - Create in `Services/`:
- `WeeklyCashStrategyService.cs` (Scoped) - Web-layer service for UI interactions
  - ConfigureStrategyAsync, EnableStrategyAsync, DisableStrategyAsync
  - GetStrategyStateAsync, GetStrategyHistoryAsync

**Background Workers** - Create in `BackgroundWorkers/`:
- `WeeklyRoutineWorker.cs` - Hosted service (BackgroundService)
  - Uses `PeriodicTimer` for daily checks (runs every 24 hours)
  - Uses NCrontab for weekly schedule parsing (e.g., "0 16 * * FRI" for 4 PM Friday)
  - Checks ITradingCalendar to skip market holidays
  - Invokes IWeeklyRoutineExecutor when schedule matches

**SignalR Hub** - Extend `Hubs/TradingHub.cs`:
- Add hub method: `SendStrategyStateUpdate(Guid strategyId, StrategyStateDto state)`
- Broadcast to all connected clients when strategy state changes

### Test Projects

**Unit Tests** - `tests/TradingBot.Core.Tests/`:
- `WeeklyCashManagedStrategyTests.cs` - Entity behavior tests
- `BreakoutRuleConfigTests.cs` - Value object validation tests
- `StrategyConfigurationTests.cs` - Value object validation tests

**Infrastructure Tests** - `tests/TradingBot.Infrastructure.Tests/`:
- `WeeklyCashManagedStrategyRepositoryTests.cs` - Repository persistence tests (in-memory SQLite)
- `MA20IndicatorServiceTests.cs` - MA20 calculation accuracy tests (0.01% precision)

**Engine Tests** - `tests/TradingBot.Engine.Tests/`:
- `WeeklyRoutineExecutorTests.cs` - Buy/sell logic, cash buffer adjustment (100% coverage)
- `CashBufferManagerTests.cs` - Rebalancing logic tests
- `BreakoutDetectorTests.cs` - Breakout rule detection tests

**Web Tests** - `tests/TradingBot.Web.Tests/`:
- `StrategyConfigurationFormTests.cs` - Blazor component tests using bUnit
- `WeeklyCashStrategyServiceTests.cs` - Service integration tests

## Implementation Steps (High-Level)

### Step 1: Create Domain Entities in Core

1. Create `WeeklyCashManagedStrategy : EntityBase<Guid>, IAggregateRoot`
   - Add properties: `StrategyConfiguration`, `BreakoutRuleConfig`, `DaysBelowMA20`, `LastExecutionTimestamp`, `IsEnabled`
   - Add domain event registration methods: `Configure()`, `Execute()`, `Disable()`
   - Follow SmartEnum pattern for any enums (e.g., `ExecutionDay`)

2. Create value objects: `StrategyConfiguration`, `BreakoutRuleConfig`
   - Immutable records with validation in constructors
   - Throw `ArgumentException` for invalid values

3. Create domain events extending `DomainEventBase`
   - `StrategyConfiguredEvent`, `StrategyExecutedEvent`, `StrategyDisabledEvent`, `MA20UpdatedEvent`

4. Create repository interfaces extending `IRepositoryBase<T>`

### Step 2: Add EF Core Configuration and Migration

1. Create `WeeklyCashManagedStrategyConfiguration : IEntityTypeConfiguration<WeeklyCashManagedStrategy>`
   - Map all properties using fluent API
   - Convert value objects to JSON: `.HasConversion(...)`
   - **CRITICAL**: `builder.Ignore(e => e.DomainEvents);`

2. Add `DbSet<WeeklyCashManagedStrategy>` to `TradingBotDbContext`

3. Create migration:
   ```bash
   dotnet ef migrations add AddWeeklyCashManagedStrategy --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web
   ```

4. Review migration file, then apply:
   ```bash
   dotnet ef database update --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web
   ```

### Step 3: Implement MA20 Calculation Service

1. Create `MA20IndicatorService : IMA20IndicatorService` in Infrastructure
   - Implement sliding window with `Queue<decimal>` (max size 20)
   - Maintain running sum: `sum += newCandle.Close; sum -= oldestCandle.Close;`
   - Detect gaps: `if ((newCandle.Date - lastCandle.Date).Days > 1)` recalculate from last 20 candles
   - Return `MA20Indicator` value object with current value, calculation date, and confidence level

2. Write unit tests:
   - Verify accuracy within 0.01% against LINQ baseline
   - Test gap handling (weekend, holidays)
   - Test insufficient data scenario (< 20 days)

### Step 4: Implement Weekly Routine Executor

1. Create `WeeklyRoutineExecutor : IWeeklyRoutineExecutor` in Engine
   - Inject dependencies: `IWeeklyCashManagedStrategyRepository`, `IMA20IndicatorService`, `IOrderExecutionService`, `IRiskManager`, `IPortfolioManager`
   - Implement `ExecuteAsync(Guid strategyId, CancellationToken cancellationToken)`
   - Flow:
     1. Load strategy from repository
     2. Calculate current equity (cash + position value)
     3. Calculate current cash ratio
     4. Check buy conditions: COIN > MA20, cash_ratio > MIN_CASH_RATIO
     5. If buy: Calculate amount = min(WEEKLY_BUY_RATIO × equity, available_cash)
     6. Check sell conditions: days_below_ma20 >= 2, position size > 0
     7. If sell: Calculate quantity = WEEKLY_SELL_RATIO × position_size
     8. Execute orders via OrderExecutionService (goes through RiskManager)
     9. Call CashBufferManager for rebalancing
     10. Update strategy state (LastExecutionTimestamp)
     11. Raise StrategyExecutedEvent

2. Create `CashBufferManager` with `AdjustCashBufferAsync()` method

3. Create `BreakoutDetector` with `DetectBreakoutAsync()` method (optional)

4. Write comprehensive unit tests (100% coverage for buy/sell logic):
   - Test all acceptance scenarios from spec.md User Stories 2, 3, 4
   - Use FakeItEasy to mock dependencies
   - Verify correct order amounts, risk validation, domain event raising

### Step 5: Create Blazor Components

1. Create `StrategyConfigurationForm.razor`:
   - Use existing Tb-prefixed atomic components (TbButton, TbFormField, TbInput, TbToggle)
   - Implement two-way binding with `@bind-Value`
   - Add validation with `DataAnnotationsValidator` and `ValidationMessage`
   - On submit: Call `WeeklyCashStrategyService.ConfigureStrategyAsync()`
   - Display success/error toast using `IToastService`

2. Create `StrategyStateCard.razor`:
   - Display real-time state using `@code { private StrategyStateDto? _state; }`
   - Implement SignalR connection in `OnInitializedAsync()`
   - Subscribe to hub method: `hubConnection.On<StrategyStateDto>("ReceiveStrategyStateUpdate", OnStateUpdated);`
   - Update UI via `StateHasChanged()` when data arrives
   - Dispose connection in `DisposeAsync()`

3. Create `StrategyDetailsPanel.razor`:
   - Show detailed metrics using existing chart components (Blazor-ApexCharts)
   - Use Tailwind CSS for layout (no custom CSS)
   - Follow atomic design principles (compose from molecules/atoms)

4. Write bUnit tests using `BunitContext` (NOT `Bunit.TestContext`):
   ```csharp
   using var ctx = new BunitContext();
   var cut = ctx.Render<StrategyConfigurationForm>(parameters => parameters
       .Add(p => p.StrategyId, Guid.NewGuid()));
   ```

### Step 6: Add Background Worker

1. Create `WeeklyRoutineWorker : BackgroundService` in Web
   - Inject: `IWeeklyRoutineExecutor`, `IWeeklyCashManagedStrategyRepository`, `ITradingCalendar`, `ILogger`
   - In `ExecuteAsync()`:
     - Create `PeriodicTimer` with 24-hour interval
     - On each tick:
       - Check if market is open (ITradingCalendar)
       - Check if today matches weekly schedule (NCrontab)
       - If match: Load all enabled strategies and execute via `IWeeklyRoutineExecutor`
       - Handle exceptions, log errors, continue execution

2. Register worker in `Program.cs`:
   ```csharp
   builder.Services.AddHostedService<WeeklyRoutineWorker>();
   ```

3. Add NCrontab package if not present:
   ```bash
   dotnet add src/TradingBot.Web package NCrontab
   ```

### Step 7: Write Tests

1. **Unit Tests** (70% of total tests):
   - Test entity behavior (domain event raising, validation)
   - Test value object validation
   - Test MA20 calculation accuracy
   - Test weekly routine buy/sell logic with various scenarios
   - Use FakeItEasy for all dependencies

2. **Integration Tests** (20% of total tests):
   - Test repository persistence with in-memory SQLite
   - Test end-to-end weekly routine execution with real DbContext
   - Test domain event dispatching via MediatR

3. **Component Tests** (10% of total tests):
   - Test Blazor form validation and submission
   - Test SignalR real-time updates
   - Use bUnit for component rendering

4. **Coverage Verification**:
   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```
   - Ensure 80% minimum coverage overall
   - Ensure 100% coverage for buy logic, sell logic, cash buffer adjustment

## Building and Running

### Build Commands

```bash
# Clean build
dotnet clean
dotnet build

# Build with code analyzers (StyleCop, Roslynator, SonarAnalyzer)
dotnet build /p:RunAnalyzers=true

# Treat warnings as errors (project already configured)
dotnet build
```

### Database Migration Commands

```bash
# Create migration (after modifying entities)
dotnet ef migrations add MigrationName --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web

# Apply migration to database
dotnet ef database update --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web

# Rollback to previous migration
dotnet ef database update PreviousMigrationName --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web

# Drop database (CAUTION: deletes all data)
dotnet ef database drop --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web
```

### Run the Web Application

```bash
# Run web dashboard
dotnet run --project src/TradingBot.Web

# Navigate to: https://localhost:5001
```

### Configure a Strategy

1. **Navigate to Strategies page** (`/strategies`)
2. **Click "Add Weekly Cash Strategy" button**
3. **Fill in configuration form**:
   - Min Cash Ratio: `0.15` (15%)
   - Max Cash Ratio: `0.25` (25%)
   - Weekly Buy Ratio: `0.05` (5% of equity)
   - Weekly Sell Ratio: `0.10` (10% of position)
   - ETP Symbol: `BTCW` (or your desired ETP)
   - Underlying Symbol: `COIN` (or your desired asset)
   - Execution Day: `Friday`
   - Enable Breakout Rule: `false` (optional)
4. **Click "Save and Enable"**
5. **Verify strategy appears in active strategies list**
6. **Navigate to Strategy Details** to see current state (cash ratio, MA20, next execution time)

## Testing

### Run Unit Tests

```bash
# Run all tests
dotnet test

# Run tests for specific project
dotnet test tests/TradingBot.Core.Tests
dotnet test tests/TradingBot.Engine.Tests

# Run specific test class
dotnet test --filter "FullyQualifiedName~WeeklyCashManagedStrategyTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~ExecuteAsync_WhenCoinAboveMA20AndSufficientCash_CreatesBuyOrder"
```

### Run Integration Tests

```bash
# Run integration tests (slower, uses in-memory database)
dotnet test tests/TradingBot.Infrastructure.Tests

# Run with detailed output
dotnet test tests/TradingBot.Infrastructure.Tests --verbosity detailed
```

### Run Tests with Coverage

```bash
# Generate code coverage report
dotnet test --collect:"XPlat Code Coverage"

# Coverage report is in: tests/**/TestResults/*/coverage.cobertura.xml
# Use tools like ReportGenerator to view HTML report
```

### Example Test Scenarios

**Scenario 1: Buy Order Execution**
```csharp
[Fact]
public async Task ExecuteAsync_WhenCoinAboveMA20AndSufficientCash_CreatesBuyOrder()
{
    // Arrange
    var strategy = CreateStrategy(minCashRatio: 0.15m, weeklyBuyRatio: 0.05m);
    var coinPrice = 150m;
    var ma20 = 140m; // COIN > MA20
    var totalEquity = 100000m;
    var cashRatio = 0.20m; // Above minimum

    var ma20Service = A.Fake<IMA20IndicatorService>();
    A.CallTo(() => ma20Service.CalculateAsync("COIN", A<CancellationToken>._))
        .Returns(new MA20Indicator(ma20, DateTime.UtcNow));

    var executor = new WeeklyRoutineExecutor(ma20Service, /* other deps */);

    // Act
    await executor.ExecuteAsync(strategy.Id, CancellationToken.None);

    // Assert
    A.CallTo(() => orderExecutionService.ExecuteOrderAsync(
        A<Order>.That.Matches(o =>
            o.OrderSide == OrderSide.Buy &&
            o.Symbol == "BTCW" &&
            o.Quantity == Math.Floor(5000m / etpPrice)), // 5% of 100k equity
        A<CancellationToken>._))
    .MustHaveHappenedOnceExactly();
}
```

**Scenario 2: Sell Order on MA20 Breakdown**
```csharp
[Fact]
public async Task ExecuteAsync_WhenDaysBelowMA20GreaterThanTwo_CreatesSellOrder()
{
    // Arrange
    var strategy = CreateStrategy(weeklySellRatio: 0.10m);
    strategy.IncrementDaysBelowMA20(); // Day 1
    strategy.IncrementDaysBelowMA20(); // Day 2

    var coinPrice = 130m;
    var ma20 = 140m; // COIN < MA20
    var positionSize = 100m; // 100 shares held

    var ma20Service = A.Fake<IMA20IndicatorService>();
    A.CallTo(() => ma20Service.CalculateAsync("COIN", A<CancellationToken>._))
        .Returns(new MA20Indicator(ma20, DateTime.UtcNow));

    var executor = new WeeklyRoutineExecutor(ma20Service, /* other deps */);

    // Act
    await executor.ExecuteAsync(strategy.Id, CancellationToken.None);

    // Assert
    A.CallTo(() => orderExecutionService.ExecuteOrderAsync(
        A<Order>.That.Matches(o =>
            o.OrderSide == OrderSide.Sell &&
            o.Symbol == "BTCW" &&
            o.Quantity == 10m), // 10% of 100 shares
        A<CancellationToken>._))
    .MustHaveHappenedOnceExactly();
}
```

**Scenario 3: Cash Buffer Rebalancing**
```csharp
[Fact]
public async Task AdjustCashBufferAsync_WhenCashRatioBelowMinimum_SellsToRebuildBuffer()
{
    // Arrange
    var strategy = CreateStrategy(minCashRatio: 0.15m, weeklySellRatio: 0.10m);
    var currentCashRatio = 0.12m; // Below minimum
    var positionSize = 100m;

    var cashBufferManager = new CashBufferManager(/* deps */);

    // Act
    await cashBufferManager.AdjustCashBufferAsync(strategy, currentCashRatio, positionSize, CancellationToken.None);

    // Assert
    A.CallTo(() => orderExecutionService.ExecuteOrderAsync(
        A<Order>.That.Matches(o =>
            o.OrderSide == OrderSide.Sell &&
            o.Quantity == 10m), // 10% of position
        A<CancellationToken>._))
    .MustHaveHappenedOnceExactly();
}
```

## Common Pitfalls

### 1. Domain Events Not Dispatching

**Problem**: Domain events are raised in the entity but never handled.

**Solution**:
- Ensure `builder.Ignore(e => e.DomainEvents);` is in EF Core configuration
- Verify `TradingBotDbContext` calls `_domainEventDispatcher.DispatchAndClearEvents()` in `SaveChangesAsync`
- Register event handlers in DI container as `INotificationHandler<TEvent>`
- Check that `MediatorDomainEventDispatcher` is registered in DI

**Debug**:
```csharp
// Add logging in domain event handler
public class StrategyExecutedEventHandler : INotificationHandler<StrategyExecutedEvent>
{
    private readonly ILogger<StrategyExecutedEventHandler> _logger;

    public async Task Handle(StrategyExecutedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("StrategyExecutedEvent received for strategy {StrategyId}", notification.StrategyId);
        // Handler logic...
    }
}
```

### 2. MA20 Calculation with Insufficient Data

**Problem**: Strategy activates but MA20 calculation fails because less than 20 days of historical data exists.

**Solution**:
- Add validation in `WeeklyCashManagedStrategy.Enable()` method
- Call `IMA20IndicatorService.ValidateDataAvailabilityAsync(symbol)` before allowing activation
- Return user-friendly error: "Cannot activate strategy: Minimum 20 days of historical data required for MA20 calculation."

**Example**:
```csharp
public async Task<Result> EnableAsync(IMA20IndicatorService ma20Service, CancellationToken cancellationToken)
{
    var dataAvailable = await ma20Service.ValidateDataAvailabilityAsync(
        Configuration.UnderlyingSymbol,
        minimumDays: 20,
        cancellationToken);

    if (!dataAvailable)
    {
        return Result.Failure("Insufficient historical data (minimum 20 days required)");
    }

    IsEnabled = true;
    RegisterDomainEvent(new StrategyEnabledEvent(Id));
    return Result.Success();
}
```

### 3. Time Zone Handling for Weekly Execution

**Problem**: Weekly routine executes at wrong time due to UTC vs local time zone confusion.

**Solution**:
- **Always use UTC** for scheduling: `DateTime.UtcNow`, `DateTimeOffset.UtcNow`
- Store execution timestamps in UTC in database
- Use NCrontab with UTC timezone for cron expressions
- Convert to user's local timezone only for display in UI

**Example**:
```csharp
// CORRECT: Use UTC for scheduling
var nextExecution = CrontabSchedule.Parse("0 21 * * FRI").GetNextOccurrence(DateTime.UtcNow); // 9 PM UTC (4 PM EST)

// WRONG: Do NOT use local time for scheduling
// var nextExecution = CrontabSchedule.Parse("0 16 * * FRI").GetNextOccurrence(DateTime.Now); // Timezone-dependent
```

### 4. SignalR Connection Management in Blazor

**Problem**: SignalR connection not disposed properly, causing memory leaks or "connection closed" errors.

**Solution**:
- Implement `IAsyncDisposable` in Blazor components
- Dispose connection in `DisposeAsync()` method
- Handle connection closed event and attempt reconnection

**Example**:
```csharp
@implements IAsyncDisposable

@code {
    private HubConnection? _hubConnection;

    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/tradinghub"))
            .WithAutomaticReconnect()
            .AddMessagePackProtocol()
            .Build();

        _hubConnection.On<StrategyStateDto>("ReceiveStrategyStateUpdate", OnStateUpdated);

        await _hubConnection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
```

### 5. Race Condition in Concurrent Weekly Execution

**Problem**: Multiple instances of WeeklyRoutineWorker execute simultaneously (e.g., in load-balanced scenario) causing duplicate orders.

**Solution**:
- Add distributed lock using database-based pessimistic locking
- OR use `IDistributedLock` abstraction with Redis/SQL Server implementation
- OR ensure single instance deployment for hosted services

**Example** (simple database lock):
```csharp
public async Task ExecuteAsync(Guid strategyId, CancellationToken cancellationToken)
{
    await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

    var strategy = await _repository.GetByIdAsync(strategyId, cancellationToken);

    // Check if already executed today
    if (strategy.LastExecutionTimestamp.Date == DateTime.UtcNow.Date)
    {
        _logger.LogInformation("Strategy {StrategyId} already executed today, skipping", strategyId);
        return;
    }

    // Execute routine...

    await transaction.CommitAsync(cancellationToken);
}
```

### 6. Incorrect Order Quantity Rounding

**Problem**: Buy/sell quantities are fractional (e.g., 10.7 shares) but broker only accepts whole shares.

**Solution**:
- Round buy quantities UP: `Math.Ceiling(quantity)` (to ensure full investment)
- Round sell quantities DOWN: `Math.Floor(quantity)` (to avoid selling more than owned)
- Handle case where rounded quantity is 0 (skip order)

**Example**:
```csharp
var buyAmount = strategy.Configuration.WeeklyBuyRatio * totalEquity;
var rawQuantity = buyAmount / currentEtpPrice; // e.g., 10.7 shares

var quantity = Math.Ceiling(rawQuantity); // 11 shares (buy slightly more)

if (quantity < 1m)
{
    _logger.LogInformation("Calculated buy quantity {Quantity} is less than 1 share, skipping order", rawQuantity);
    return;
}
```

## Debugging Tips

### How to Debug Weekly Routine Execution

1. **Set breakpoints** in `WeeklyRoutineExecutor.ExecuteAsync()` at key decision points:
   - Before buy logic: Check `coinPrice > ma20` and `cashRatio > MIN_CASH_RATIO`
   - Before sell logic: Check `daysBelowMA20 >= 2`
   - After cash buffer adjustment: Check final cash ratio

2. **Use manual trigger** for testing (don't wait for Friday):
   ```csharp
   // Add temporary test endpoint in Web project
   [HttpPost("api/debug/trigger-weekly-routine")]
   public async Task<IActionResult> TriggerWeeklyRoutine([FromQuery] Guid strategyId)
   {
       await _weeklyRoutineExecutor.ExecuteAsync(strategyId, CancellationToken.None);
       return Ok("Weekly routine executed");
   }
   ```

3. **Check structured logs** for decision rationale:
   ```bash
   # View logs in console (Serilog)
   # Logs are written to console and file (if configured)

   # Example log output:
   # [INF] Weekly routine executing for strategy {StrategyId}
   # [INF] COIN price: 150.00, MA20: 140.00, COIN > MA20: True
   # [INF] Cash ratio: 0.20, Min cash ratio: 0.15, Eligible for buy: True
   # [INF] Calculated buy amount: $5,000.00, Buy quantity: 33 shares
   # [INF] Order executed: OrderId={OrderId}, Symbol=BTCW, Quantity=33
   ```

### How to Inspect Strategy State

1. **Use database browser** (e.g., DB Browser for SQLite):
   - Open `tradingbot.db` file
   - Query `WeeklyCashManagedStrategies` table
   - Check `DaysBelowMA20`, `LastExecutionTimestamp`, `IsEnabled` columns

2. **Add debug endpoint** to query strategy state:
   ```csharp
   [HttpGet("api/debug/strategy-state/{strategyId}")]
   public async Task<IActionResult> GetStrategyState(Guid strategyId)
   {
       var strategy = await _repository.GetByIdAsync(strategyId, CancellationToken.None);

       return Ok(new
       {
           strategy.Id,
           strategy.IsEnabled,
           strategy.DaysBelowMA20,
           strategy.LastExecutionTimestamp,
           strategy.Configuration
       });
   }
   ```

3. **Use Blazor DevTools** to inspect real-time SignalR messages:
   - Open browser DevTools (F12)
   - Navigate to Network tab → WS (WebSockets)
   - Filter for `tradinghub` connection
   - View `ReceiveStrategyStateUpdate` messages in real-time

### Log File Locations

Logs are written by Serilog (configured in `appsettings.json`):

1. **Console logs**: Visible in terminal when running `dotnet run`
2. **File logs** (if configured):
   - Path: `logs/tradingbot-.log` (rolling daily files)
   - Example: `logs/tradingbot-20250116.log`

3. **Log levels**:
   - `Information`: Normal execution flow (weekly routine executed, orders placed)
   - `Warning`: Non-critical issues (insufficient cash, risk limit exceeded)
   - `Error`: Exceptions, failures (MA20 calculation error, order rejection)

4. **Useful log queries**:
   ```bash
   # View all weekly routine executions
   grep "Weekly routine executing" logs/tradingbot-*.log

   # View all buy orders
   grep "Buy order executed" logs/tradingbot-*.log

   # View all errors
   grep "ERR" logs/tradingbot-*.log
   ```

## References

### Feature Documentation

- **[spec.md](./spec.md)**: Complete feature specification with user stories, requirements, success criteria
- **[research.md](./research.md)**: Technical decisions for scheduling, MA20 calculation, state persistence, breakout rules, SignalR updates, test data
- **[plan.md](./plan.md)**: Implementation plan with project structure, technical context, constitution check

### CLAUDE.md Sections

- **[Architecture](../CLAUDE.md#architecture)**: Layered structure, dependency rules, key projects
- **[Code Quality Standards](../CLAUDE.md#code-quality-standards)**: Analyzers, naming conventions, SmartEnum pattern
- **[Testing Standards](../CLAUDE.md#testing-standards)**: Test framework, AAA pattern, coverage requirements
- **[Dependency Injection](../CLAUDE.md#dependency-injection)**: Service registration, lifetimes (Singleton/Scoped/Transient)
- **[Database](../CLAUDE.md#database)**: Schema, migrations, DbContext usage
- **[Domain Events](../CLAUDE.md#domain-events)**: Event infrastructure, key events, dispatching flow, usage guidelines
- **[Ardalis.SharedKernel Patterns](../CLAUDE.md#ardalis-sharedkernel-usage-patterns)**: EntityBase, IAggregateRoot, repositories, specifications
- **[Blazor Component Testing](../CLAUDE.md#blazor-component-testing)**: bUnit usage, BunitContext vs Bunit.TestContext
- **[Common Gotchas](../CLAUDE.md#common-gotchas)**: EF Core tracking, async deadlocks, domain events, SignalR connection management

### External Resources

- [Ardalis.SharedKernel Documentation](https://github.com/ardalis/Ardalis.SharedKernel)
- [EF Core 10 Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [bUnit Documentation](https://bunit.dev/)
- [NCrontab Documentation](https://github.com/atifaziz/NCrontab)

---

**Happy Coding!** If you encounter issues not covered here, consult the team or add to this guide for future developers.
