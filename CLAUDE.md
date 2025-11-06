# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TradingBot CLI is an algorithmic trading platform built with .NET 9, featuring automated strategy execution, risk management, and comprehensive order handling. The solution uses a clean layered architecture with SQLite/EF Core for data persistence and Spectre.Console for the CLI interface.

## Build and Development Commands

### Core Commands

```bash
# Restore dependencies
dotnet restore

# Build the entire solution
dotnet build

# Build with code analyzers enabled
dotnet build /p:RunAnalyzers=true

# Run the CLI application
dotnet run --project src/TradingBot.Cli

# Run with configuration
dotnet run --project src/TradingBot.Cli -- strategy list
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
dotnet ef migrations add MigrationName --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Cli

# Update database
dotnet ef database update --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Cli

# Drop database (use with caution)
dotnet ef database drop --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Cli
```

## Architecture

### Layered Structure

The solution follows a strict layered architecture with clear dependency flow:

```
TradingBot.Cli (Presentation)
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
- Core has NO dependencies (pure domain models and interfaces)
- Infrastructure depends ONLY on Core
- Engine/Strategies/Analytics depend on Core (and optionally Infrastructure for implementations)
- CLI depends on all layers (composition root)
- NO circular dependencies allowed

### Key Projects

**TradingBot.Core**: Domain models and interfaces
- Domain entities (Order, Position, Trade, Signal, Account, Candle)
- Core interfaces (IStrategy, IStrategyEngine, IPortfolioManager, IRiskManager, IOrderExecutionService)
- All enums use SmartEnum pattern (Ardalis.SmartEnum)
- NO external dependencies except SmartEnum

**TradingBot.Infrastructure**: Data access and external services
- Entity Framework Core 9 with SQLite
- TradingBotDbContext (DbContext with fluent configuration)
- Repository pattern implementations
- Yahoo Finance integration for market data
- Encryption service for API key storage

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

**TradingBot.Cli**: Command-line interface
- Spectre.Console.Cli for commands
- Commands: strategy (list/enable/disable), config (show/set/set-apikey)
- Dependency injection composition root
- Serilog for structured logging

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
- **Spectre.Console.Testing**: CLI testing with TestConsole

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

The CLI uses Microsoft.Extensions.DependencyInjection. All services are registered in:
- `ServiceCollectionExtensions.AddTradingBotServices()` (Infrastructure project)
- `Program.cs` (CLI project for CLI-specific services)

Register dependencies as:
- Singleton: Stateless services, caches
- Scoped: DbContext, per-request services
- Transient: Stateful services created per use

## Database

### Schema

- **Orders**: All orders with status tracking (Pending, Filled, Cancelled, Rejected)
- **Positions**: Open positions with real-time P&L
- **Trades**: Closed trade history
- **Candles**: Cached historical market data
- **Accounts**: Account state and equity tracking

### Migrations

Always create migrations when changing entity models:
```bash
dotnet ef migrations add MigrationName --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Cli
dotnet ef database update --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Cli
```

The database file is `tradingbot.db` in the CLI output directory.

## Configuration

Configuration is in `appsettings.json` in the CLI project:

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

## CLI Usage

The CLI uses Spectre.Console.Cli with the following command structure:

```bash
# Strategy commands
dotnet run --project src/TradingBot.Cli -- strategy list
dotnet run --project src/TradingBot.Cli -- strategy enable momentum
dotnet run --project src/TradingBot.Cli -- strategy disable momentum

# Config commands
dotnet run --project src/TradingBot.Cli -- config show
dotnet run --project src/TradingBot.Cli -- config set MaxPositionSize 15.0
dotnet run --project src/TradingBot.Cli -- config set-apikey YahooFinance "key"
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

### Repository Pattern

All data access goes through repository interfaces:
- IOrderRepository, IPositionRepository, ITradeRepository, etc.
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

### CLI Testing

Use Spectre.Console.Testing for testing CLI commands:
```csharp
var console = new TestConsole();
var command = new MyCommand();
await command.ExecuteAsync(context, settings, console, CancellationToken.None);
var output = console.Output;
```

Commands must accept `IAnsiConsole` parameter for testability.

## Common Gotchas

1. **EF Core Tracking**: Be careful with tracked entities. Use `.AsNoTracking()` for read-only queries.
2. **Async Deadlocks**: Never mix sync and async code (no `.Result` or `.Wait()`).
3. **Nullable References**: Compiler enforces null safety. Use `!` operator only when certain, prefer null checks.
4. **SmartEnum Equality**: Use `.Equals()` or `==` for SmartEnum comparison, not `Equals()` on name strings.
5. **Signal Processing**: Signals are fire-and-forget events. Don't await signal generation expecting order completion.
6. **Test Isolation**: Each test must be independent. Use separate DbContext instances or in-memory databases.
7. **Migration Conflicts**: Always pull latest before creating migrations to avoid conflicts.

## Project Constitution

Refer to `.specify/memory/constitution.md` for comprehensive project standards including:
- Code quality principles
- Testing standards (80% coverage minimum, 100% for critical paths)
- Performance requirements (API <200ms p95, page load <1.5s FCP)
- Security standards (encryption, input validation, audit trails)
- DevOps and CI/CD practices

This constitution takes precedence for architectural decisions and coding standards.

## Active Technologies
- C# / .NET 9 + ASP.NET Core Blazor Server, Tailwind CSS, SignalR, bUnit (testing), existing TradingBot layers (Core, Infrastructure, Engine, Analytics, Strategies) (002-blazor-server-app)
- SQLite via Entity Framework Core 9 (shared with CLI application) (002-blazor-server-app)

## Recent Changes
- 002-blazor-server-app: Added C# / .NET 9 + ASP.NET Core Blazor Server, Tailwind CSS, SignalR, bUnit (testing), existing TradingBot layers (Core, Infrastructure, Engine, Analytics, Strategies)
