# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TradingBot is an algorithmic trading platform built with .NET 10, featuring automated strategy execution, risk management, and comprehensive order handling. The solution includes:
- **Web Dashboard**: Modern Blazor Server application with real-time updates via SignalR (single entry point)
- **Clean Architecture**: Layered architecture with SQLite/EF Core for data persistence
- **Domain-Driven Design**: Uses Ardalis.SharedKernel for DDD patterns (aggregates, domain events, repositories)
- **Atomic Design**: Component hierarchy (Atoms → Molecules → Organisms → Pages) with Tb-prefixed components

## Build and Development Commands

### Core Commands

```bash
# Restore dependencies
dotnet restore

# Build the entire solution
dotnet build

# Build with code analyzers enabled
dotnet build /p:RunAnalyzers=true

# Run the Web Dashboard
dotnet run --project src/TradingBot.Web
# Navigate to https://localhost:5001 in your browser
```

### Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests for a specific project
dotnet test tests/TradingBot.Core.Tests

# Run a specific test
dotnet test --filter "FullyQualifiedName~MomentumStrategy"

# Run tests with detailed output
dotnet test --verbosity detailed
```

### Database Management

```bash
# Create a new migration (from Infrastructure project)
dotnet ef migrations add MigrationName --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web

# Update database
dotnet ef database update --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web

# Drop database (use with caution)
dotnet ef database drop --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web
```

## Architecture

### Layered Structure

The solution follows a strict layered architecture with clear dependency flow:

```
TradingBot.Web (Web Presentation - Blazor Server)
    ↓
TradingBot.Engine (Business Logic - Trading Engine)
TradingBot.Strategies (Business Logic - Strategy Implementations)
TradingBot.Analytics (Business Logic - Performance Analysis)
    ↓
TradingBot.Infrastructure (Data Access & External Services)
    ↓
TradingBot.Core (Domain Models & Interfaces)
```

**Dependency Rules:**
- Core has NO dependencies (pure domain models and interfaces, extends Ardalis.SharedKernel)
- Infrastructure depends ONLY on Core
- Engine/Strategies/Analytics depend on Core (and optionally Infrastructure for implementations)
- Web depends on all layers (composition root)
- NO circular dependencies allowed

### Key Projects

**TradingBot.Core**: Domain models and interfaces
- Domain entities extending EntityBase<T> from Ardalis.SharedKernel (Order, Position, Trade, Account, RiskSettings)
- Domain events extending DomainEventBase (OrderFilledEvent, PositionClosedEvent, etc.)
- Core interfaces (IStrategy, IStrategyEngine, IPortfolioManager, IRiskManager, IOrderExecutionService)
- Repository interfaces extending IRepositoryBase and IReadRepositoryBase from SharedKernel
- All enums use SmartEnum pattern (Ardalis.SmartEnum)
- Dependencies: Ardalis.SharedKernel, Ardalis.SmartEnum, MediatR (for domain events)

**TradingBot.Infrastructure**: Data access and external services
- Entity Framework Core 10 with SQLite
- TradingBotDbContext (DbContext with fluent configuration and domain event dispatching)
- Generic repository implementations (EfRepository<T>, EfReadRepository<T>) extending RepositoryBase from SharedKernel
- MediatorDomainEventDispatcher for dispatching domain events via MediatR
- Yahoo Finance integration for market data
- Encryption service for API key storage
- Dependencies: Ardalis.Specification.EntityFrameworkCore

**TradingBot.Engine**: Core trading engine
- StrategyEngine: Manages and executes strategies, emits signals
- SignalProcessor: Converts signals to orders (event-driven)
- OrderExecutionService: Executes orders with simulated slippage/commission
- PortfolioManager: Tracks positions, P&L, and equity
- RiskManager: Validates orders against risk limits
- PositionSizeCalculator: Calculates position sizes based on risk
- StopLossManager: Manages stop-loss and take-profit orders

**TradingBot.Strategies**: Trading strategy implementations
- Base classes for technical indicators (SMA, EMA, RSI, MACD, Bollinger Bands, ATR)
- MomentumStrategy: Moving average crossover strategy
- MeanReversionStrategy: Bollinger Band + RSI strategy
- All strategies implement IStrategy interface

**TradingBot.Analytics**: Performance analytics
- Performance metrics calculation (Sharpe ratio, max drawdown, etc.)
- Equity curve generation
- Trade statistics

**TradingBot.Web**: Blazor Server web dashboard (single application entry point)
- Real-time updates via SignalR (TradingHub)
- Atomic Design component structure (Atoms → Molecules → Organisms → Features)
- All components prefixed with "Tb" (e.g., TbButton, TbCard, TbNavigationSidebar)
- Component organization:
  - **Atoms**: Basic UI elements (TbButton, TbInput, TbLabel, TbBadge, TbIcon, TbSpinner, TbToggle)
  - **Molecules**: Composite components (TbFormField, TbMenuItem, TbToast)
  - **Organisms**: Complex components (TbNavigationSidebar, TbThemeProvider, TbToastContainer, TbSettingsForm)
  - **Features**: Domain-specific components grouped by feature (Dashboard, Portfolio, Strategy, Risk, Performance, Backtest, Charts)
  - **Pages**: Full page components (Index, Portfolio, Strategies, Performance, Backtest, RiskSettings, Settings, Help)
- Services:
  - **DashboardService** (Scoped): Aggregates dashboard data
  - **PortfolioService** (Scoped): Manages positions and trade history with close position capability
  - **PerformanceService** (Scoped): Calculates performance metrics and equity curves
  - **StrategyManagementService** (Scoped): Configures and manages trading strategies with dynamic parameters
  - **BacktestService** (Scoped): Executes backtests asynchronously via background queue
  - **RiskSettingsService** (Singleton): Manages risk settings with validation
  - **ToastService** (Singleton): Toast notifications
  - **UIStateService** (Scoped): UI state management
  - **NavigationService** (Scoped): Navigation helpers
  - **RealtimeUpdateService** (Hosted Service): Publishes real-time updates via SignalR
- Background Workers:
  - **BacktestExecutionWorker**: Processes backtest requests from background queue using Channel-based task queue
  - **IBackgroundTaskQueue**: Channel-based async task queue for long-running operations
- Tailwind CSS for styling (no third-party component libraries)
- WCAG 2.1 Level AA compliant with full keyboard navigation
- Desktop-first design (minimum 1024px width)

### Signal Processing Pipeline

This is a critical architectural flow:

```
1. Strategy executes → generates Signal
2. StrategyEngine.SignalGenerated event fires
3. SignalProcessor (subscriber) receives signal
4. PositionSizeCalculator determines quantity based on risk settings
5. RiskManager validates order against limits
6. OrderExecutionService executes order
7. StopLossManager creates stop-loss/take-profit orders
8. PortfolioManager updates positions and P&L
```

The pipeline is event-driven and asynchronous. Signals are automatically converted to orders without manual intervention.

## Code Quality Standards

### Analyzers and Style

The project uses strict code quality enforcement:
- **StyleCop.Analyzers**: Enforces C# style conventions
- **Roslynator.Analyzers**: Code quality and refactoring suggestions
- **Microsoft.CodeAnalysis.NetAnalyzers**: Security and performance rules
- **TreatWarningsAsErrors**: true (builds fail on any warning)
- **EnforceCodeStyleInBuild**: true
- **Nullable**: enabled (C# nullable reference types required)

### Naming Conventions

- PascalCase for public members
- camelCase with underscore prefix for private fields (\_fieldName)
- Descriptive names that reveal intent
- Async methods must end with "Async" suffix
- Interfaces must start with "I"

### File Headers

All C# files must have copyright headers:
```csharp
// <copyright file="FileName.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>
```

### SmartEnum Pattern

All enums are implemented using Ardalis.SmartEnum for type safety:
```csharp
public sealed class OrderType : SmartEnum<OrderType>
{
    public static readonly OrderType Market = new(1, nameof(Market));
    public static readonly OrderType Limit = new(2, nameof(Limit));
    // ...
}
```

Never use traditional C# enums. Always use SmartEnum.

## Testing Standards

### Test Framework

- **xUnit**: Test framework
- **FakeItEasy**: Mocking framework
- **Shouldly**: Assertion library
- **bUnit**: Blazor component testing

### Test Structure

Follow AAA pattern (Arrange, Act, Assert):
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var service = new Service();

    // Act
    var result = await service.MethodAsync();

    // Assert
    result.ShouldNotBeNull();
}
```

### Test Naming

Format: `MethodName_Scenario_ExpectedBehavior`
- Example: `GenerateSignal_BullishCrossover_ReturnsBuySignal`
- Example: `ValidateOrder_ExceedsRiskLimit_ThrowsException`

### Coverage Requirements

- Minimum 80% code coverage across all projects
- 100% coverage for critical paths (order execution, risk management)
- Run coverage with: `dotnet test --collect:"XPlat Code Coverage"`

## Dependency Injection

The Web application uses Microsoft.Extensions.DependencyInjection. All services are registered in:
- `ServiceCollectionExtensions.AddTradingBotServices()` (Infrastructure project - shared core services)
- `Program.cs` (Web project for web-specific services)

Register dependencies as:
- Singleton: Stateless services, caches, hosted services (RealtimeUpdateService)
- Scoped: DbContext, per-request services, web services (Dashboard, Portfolio, Performance)
- Transient: Stateful services created per use

**Web-specific services**:
- IDashboardService, IPortfolioService, IPerformanceService (Scoped)
- IStrategyManagementService, IBacktestService (Scoped)
- IRiskSettingsService, IToastService (Singleton)
- UIStateService, NavigationService (Scoped)
- RealtimeUpdateService (Hosted Service - publishes updates via SignalR)

## Database

### Schema

- **Orders**: All orders with status tracking (Pending, Filled, Cancelled, Rejected)
- **Positions**: Open positions with real-time P&L
- **Trades**: Closed trade history
- **Candles**: Cached historical market data
- **Accounts**: Account state and equity tracking
- **UserPreferences**: User-specific settings (theme, refresh intervals, notifications)

### Migrations

Always create migrations when changing entity models:
```bash
dotnet ef migrations add MigrationName --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web
dotnet ef database update --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web
```

The database file is `tradingbot.db` in the Web output directory.

## Configuration

Configuration is in `appsettings.json` in the Web project:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=tradingbot.db"
  },
  "TradingBot": {
    "InitialCapital": 100000,
    "DefaultLeverage": 1.0,
    "MaxPositionSize": 10.0,
    "StopLossPercent": 2.0,
    "TakeProfitPercent": 5.0
  }
}
```

Environment variables can override settings with prefix `TRADINGBOT_`.

## Application Usage

### Web Dashboard Usage

The web dashboard provides a comprehensive UI for managing your trading bot:

**Pages**:
- **Dashboard** (`/`): Real-time account summary, active strategies, performance metrics, recent trades
- **Portfolio** (`/portfolio`): Open positions, trade history with filters, position management
- **Strategies** (`/strategies`): Strategy cards with enable/disable toggle, performance by strategy
- **Performance** (`/performance`): Equity curves, trade statistics, comprehensive metrics (Sharpe ratio, max drawdown, win rate)
- **Backtest** (`/backtest`): Run historical backtests, analyze results, view trade lists
- **Risk Settings** (`/risk-settings`): Configure risk limits, position sizing, stop-loss/take-profit
- **Settings** (`/settings`): User preferences (theme, refresh intervals, notifications)
- **Help** (`/help`): Documentation and keyboard shortcuts

**Real-time Updates**:
The dashboard uses SignalR for live updates. The `RealtimeUpdateService` hosted service publishes:
- Portfolio updates (positions, P&L)
- Trade notifications
- Strategy status changes
- Account balance updates

**Component Development**:
When creating new web components, follow the Atomic Design pattern:
1. Create atoms for basic UI elements (prefix with "Tb")
2. Compose molecules from atoms
3. Build organisms from molecules
4. Use organisms in feature-specific components
5. Assemble pages from feature components

Example component structure:
```
TradingBot.Web/Components/
├── Atoms/          # Basic UI elements (TbButton, TbInput, etc.)
├── Molecules/      # Composite components (TbFormField, TbMenuItem)
├── Organisms/      # Complex components (TbNavigationSidebar, TbThemeProvider)
├── Features/       # Domain components grouped by feature
│   ├── Dashboard/
│   ├── Portfolio/
│   ├── Strategy/
│   └── ...
├── Pages/          # Full page components
└── Layout/         # Layout components (MainLayout)
```

## Custom Slash Commands

This repository uses the SpecKit framework for feature specification and implementation. Available commands:

- `/speckit.specify`: Create/update feature specifications
- `/speckit.plan`: Generate implementation plan
- `/speckit.tasks`: Generate actionable task list
- `/speckit.implement`: Execute implementation tasks
- `/speckit.checklist`: Generate custom checklists
- `/speckit.clarify`: Identify underspecified areas
- `/speckit.analyze`: Cross-artifact consistency analysis
- `/speckit.constitution`: Update project constitution

## Important Patterns and Practices

### Async/Await

- Use async/await for all I/O operations (database, HTTP, file system)
- Always pass CancellationToken through async methods
- Never use `.Result` or `.Wait()` - always await

### Error Handling

- Use exceptions for exceptional cases (InvalidOperationException, ArgumentException)
- Return result types for expected failures (success/failure patterns)
- Log errors with structured logging (Serilog)

### Resource Management

- Implement IDisposable for unmanaged resources
- Use `using` statements or declarations
- Dispose DbContext, HttpClient, streams properly

### Event-Driven Architecture

The system uses events for decoupling:
- `StrategyEngine.SignalGenerated` → SignalProcessor listens
- This pattern allows multiple subscribers without tight coupling

### Domain Events

The application implements Domain-Driven Design (DDD) patterns using **Ardalis.SharedKernel** for domain events:

**Event Infrastructure**:
- All domain events extend `DomainEventBase` from Ardalis.SharedKernel
- Events are dispatched via **MediatR** before SaveChangesAsync completes
- Event handlers are registered in DI container as MediatR notification handlers
- Events enable eventual consistency between aggregates

**Key Domain Events**:
- `OrderFilledEvent`: Raised when an order is filled
- `OrderCancelledEvent`: Raised when an order is cancelled
- `PositionOpenedEvent`: Raised when a position is opened
- `PositionClosedEvent`: Raised when a position is closed
- `PositionPriceUpdatedEvent`: Raised when position price updates
- `CashUpdatedEvent`: Raised when account cash changes
- `EquityUpdatedEvent`: Raised when account equity changes
- `AccountSuspendedEvent`: Raised when account is suspended

**Event Dispatching Flow**:
```csharp
// 1. Entity raises domain event
public void MarkAsFilled(decimal fillPrice, decimal commission, DateTime filledAt)
{
    Status = OrderStatus.Filled;
    RegisterDomainEvent(new OrderFilledEvent(Id, Symbol, Quantity, fillPrice, commission));
}

// 2. DbContext dispatches events before save
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    await _domainEventDispatcher.DispatchAndClearEvents(entitiesWithEvents);
    return await base.SaveChangesAsync(cancellationToken);
}

// 3. MediatR notifies registered handlers
public class OrderFilledEventHandler : INotificationHandler<OrderFilledEvent>
{
    public async Task Handle(OrderFilledEvent notification, CancellationToken cancellationToken)
    {
        // Update position, account, send notifications, etc.
    }
}
```

**Usage Guidelines**:
- Domain events are raised within aggregate roots (entities implementing `IAggregateRoot`)
- Events represent business-significant state changes
- Keep events focused on a single concern (OrderFilled, not OrderFilledAndPositionOpened)
- Event handlers should be idempotent (safe to process multiple times)
- Use events to maintain eventual consistency between aggregates
- Never modify aggregates directly from event handlers without proper transactional boundaries

### Ardalis.SharedKernel Usage Patterns

**EntityBase<TId>**: Base class for all entities with identity
```csharp
public sealed class Order : EntityBase<Guid>, IAggregateRoot
{
    // Id property inherited from EntityBase<Guid>
    // DomainEvents collection inherited for event registration

    public required string Symbol { get; set; }
    public required OrderStatus Status { get; set; }

    public void MarkAsFilled(decimal fillPrice, decimal commission, DateTime filledAt)
    {
        Status = OrderStatus.Filled;
        RegisterDomainEvent(new OrderFilledEvent(Id, Symbol, Quantity, fillPrice, commission));
    }
}
```

**IAggregateRoot**: Marker interface for aggregate roots
```csharp
// All entities that are aggregate roots implement this interface
public sealed class Position : EntityBase<Guid>, IAggregateRoot { }
public sealed class Account : IAggregateRoot  // Manual implementation for non-Guid IDs
{
    public required string Id { get; set; }
    private readonly List<DomainEventBase> _domainEvents = new();
    public IEnumerable<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();

    protected void RegisterDomainEvent(DomainEventBase domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

**IRepository and IReadRepository**: Repository interfaces
```csharp
// Core interfaces extend SharedKernel interfaces
public interface IRepository<T> : IRepositoryBase<T> where T : class, IAggregateRoot { }
public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class { }

// Infrastructure implementations extend RepositoryBase
public class EfRepository<T> : RepositoryBase<T>, IRepository<T>
    where T : class, IAggregateRoot
{
    public EfRepository(TradingBotDbContext dbContext) : base(dbContext) { }
}
```

**Specifications Pattern**: Query encapsulation using Ardalis.Specification
```csharp
// Define specifications for common queries
public class PendingOrdersSpec : Specification<Order>
{
    public PendingOrdersSpec()
    {
        Query.Where(o => o.Status == OrderStatus.Pending)
             .OrderBy(o => o.CreatedAt);
    }
}

// Use in repositories
var pendingOrders = await _orderRepository.ListAsync(new PendingOrdersSpec());
```

**Entity Configuration**:
```csharp
// Always ignore DomainEvents property in EF Core configurations
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        // CRITICAL: Ignore domain events collection
        builder.Ignore(o => o.DomainEvents);

        // Other configurations...
    }
}
```

**Non-Guid Aggregate Roots**:
For entities with non-Guid IDs (string, long), manually implement IAggregateRoot:
```csharp
public sealed class Account : IAggregateRoot
{
    // Manual Id property (string instead of Guid)
    public required string Id { get; set; }

    // Manual domain events implementation
    private readonly List<DomainEventBase> _domainEvents = new();
    public IEnumerable<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();

    protected void RegisterDomainEvent(DomainEventBase domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### Repository Pattern

All data access goes through repository interfaces:
- IOrderRepository, IPositionRepository, ITradeRepository, etc.
- All repository interfaces extend `IRepositoryBase<T>` from Ardalis.SharedKernel
- Generic repositories (`EfRepository<T>`, `EfReadRepository<T>`) extend `RepositoryBase<T>`
- Use **Specifications** pattern for complex queries instead of exposing IQueryable
- Never access DbContext directly from business logic
- Repositories are in Infrastructure, interfaces in Core

## CI/CD

GitHub Actions workflows (`.github/workflows/`):

**ci.yml**: Runs on all pushes and PRs
- Build solution
- Run tests with coverage
- Upload to Codecov
- Run code quality analysis
- Security vulnerability scan

**release.yml**: Deployment pipeline for releases

## Special Notes

### SmartEnum Usage

When working with enums, always use SmartEnum pattern. Example:
```csharp
// BAD - traditional enum
public enum OrderStatus { Pending, Filled }

// GOOD - SmartEnum
public sealed class OrderStatus : SmartEnum<OrderStatus>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Filled = new(2, nameof(Filled));

    private OrderStatus(int value, string name) : base(value, name) { }
}
```

### Entity Framework Context

- TradingBotDbContext is in Infrastructure project
- Uses fluent API configuration (no data annotations)
- All entities have configurations in `Persistence/Configurations/`

### Market Data Integration

- Yahoo Finance is the data provider
- Historical data is cached in the database (Candles table)
- Quote data is fetched on-demand
- Rate limiting is handled by the service

### Risk Management Flow

Before any order executes:
1. PositionSizeCalculator determines quantity
2. RiskManager.ValidateOrderAsync checks limits
3. Only then does OrderExecutionService execute

This sequence is critical for safety and cannot be bypassed.

### Blazor Component Testing

Use bUnit 2.0.66 for testing Blazor components. **Important API Changes**:
- Use `BunitContext` instead of `Bunit.TestContext`
- Use `ctx.Render<T>()` instead of `ctx.RenderComponent<T>()`

```csharp
using Bunit;

public class TbButtonTests
{
    [Fact]
    public void TbButton_RendersCorrectly()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbButton>(parameters => parameters
            .Add(p => p.Text, "Click me")
            .Add(p => p.Variant, "primary"));

        // Assert
        cut.Find("button").TextContent.ShouldBe("Click me");
        cut.Find("button").ClassList.ShouldContain("bg-blue-600");
    }
}
```

### SignalR Hub Testing

Test SignalR hubs by mocking the hub context:
```csharp
var hubContext = A.Fake<IHubContext<TradingHub>>();
var service = new RealtimeUpdateService(hubContext, portfolioManager);
await service.StartAsync(CancellationToken.None);
// Verify hub methods were called
```

## Common Gotchas

1. **EF Core Tracking**: Be careful with tracked entities. Use `.AsNoTracking()` for read-only queries.
2. **Async Deadlocks**: Never mix sync and async code (no `.Result` or `.Wait()`).
3. **Nullable References**: Compiler enforces null safety. Use `!` operator only when certain, prefer null checks.
4. **SmartEnum Equality**: Use `.Equals()` or `==` for SmartEnum comparison, not `Equals()` on name strings.
5. **Signal Processing**: Signals are fire-and-forget events. Don't await signal generation expecting order completion.
6. **Test Isolation**: Each test must be independent. Use separate DbContext instances or in-memory databases.
7. **Migration Conflicts**: Always pull latest before creating migrations to avoid conflicts.
8. **Blazor Component Naming**: All custom components must be prefixed with "Tb" (e.g., TbButton, not Button).
9. **Import Consolidation**: Each page/component should have a single `_Imports.razor` file at the appropriate level (avoid duplicate imports).
10. **SignalR Connection Management**: Always dispose SignalR connections properly. Use `await connection.DisposeAsync()` in Blazor components.
11. **Blazor Render Modes**: The web app uses Interactive Server render mode. Be aware of circuit reconnection handling.
12. **Tailwind CSS**: Use utility classes directly in components. No custom CSS unless absolutely necessary. Follow atomic design principles.
13. **Domain Events**: Always ignore the `DomainEvents` property in EF Core entity configurations (use `builder.Ignore(e => e.DomainEvents)`).
14. **Aggregate Boundaries**: Never navigate from one aggregate root to another via object references. Use ID references and repositories.
15. **Event Ordering**: Domain events are dispatched in the order they were registered. Design handlers to be idempotent.
16. **Non-Guid IDs**: Entities with non-Guid IDs (string, long) must manually implement IAggregateRoot and domain event management.

## Project Constitution

Refer to `.specify/memory/constitution.md` for comprehensive project standards including:
- Code quality principles
- Testing standards (80% coverage minimum, 100% for critical paths)
- Performance requirements (API <200ms p95, page load <1.5s FCP)
- Security standards (encryption, input validation, audit trails)
- DevOps and CI/CD practices

This constitution takes precedence for architectural decisions and coding standards.

## Active Technologies
- **Framework**: C# / .NET 10 (LangVersion 14)
- **Web**: ASP.NET Core Blazor Server with Interactive Server render mode
- **Real-time Communication**: SignalR with MessagePack protocol
- **Styling**: Tailwind CSS (no third-party component libraries)
- **Icons**: Heroicons
- **Charts**: Blazor-ApexCharts 6.0.2
- **Database**: SQLite via Entity Framework Core 10
- **DDD Patterns**: Ardalis.SharedKernel (entities, aggregates, domain events, repositories)
- **Domain Events**: MediatR (event dispatching and handling)
- **Specifications**: Ardalis.Specification (query encapsulation)
- **Testing**: xUnit 3.2, bUnit 2.0.66, FakeItEasy 8.3, Shouldly 4.3
- **Logging**: Serilog 4.3.0 with structured logging
- **Code Analysis**: StyleCop, Roslynator, SonarAnalyzer, Microsoft.CodeAnalysis.NetAnalyzers
- **Enums**: Ardalis.SmartEnum (type-safe enum pattern)
- C# / .NET 10 (LangVersion 14) + ASP.NET Core Blazor Server, Entity Framework Core 10, Ardalis.SharedKernel (DDD patterns), MediatR (domain events), SignalR (real-time updates), Ardalis.SmartEnum (type-safe enums), Yahoo Finance API (market data) (007-weekly-cash-managed-strategy)
- SQLite via Entity Framework Core 10 with fluent API configuration (007-weekly-cash-managed-strategy)

## Recent Changes
- **2025-01-15**: DDD refactoring complete - removed CLI application, eliminated duplicate classes, implemented DDD patterns with Ardalis.SharedKernel (spec 006)
- **2025-01-12**: Upgraded to .NET 10 and updated all NuGet packages
- **2025-01-08**: Component refactoring with Atomic Design pattern and Tb-prefix (spec 004)
- **Previous**: Added Blazor Server web dashboard with real-time updates (spec 002-003)
