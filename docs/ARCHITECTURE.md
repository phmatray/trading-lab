# TradingBot CLI Architecture

## Table of Contents
- [Overview](#overview)
- [Architecture Principles](#architecture-principles)
- [System Architecture](#system-architecture)
- [Layer Details](#layer-details)
- [Data Flow](#data-flow)
- [Technology Stack](#technology-stack)
- [Design Patterns](#design-patterns)
- [Security Architecture](#security-architecture)

## Overview

TradingBot CLI is built using a clean, layered architecture that separates concerns and promotes maintainability, testability, and extensibility. The application follows Domain-Driven Design (DDD) principles and implements various design patterns to ensure robust, production-ready code.

## Architecture Principles

### 1. Separation of Concerns
Each layer has a distinct responsibility:
- **Core**: Domain models and business logic
- **Infrastructure**: Data access and external services
- **Engine**: Trading execution and strategy orchestration
- **Strategies**: Trading strategy implementations
- **Analytics**: Performance analysis and reporting
- **CLI**: User interface and command handling

### 2. Dependency Inversion
- High-level modules don't depend on low-level modules
- Both depend on abstractions (interfaces)
- All dependencies point inward toward the Core

### 3. Single Responsibility
- Each class has one reason to change
- Methods are focused and cohesive
- Clear boundaries between components

### 4. Open/Closed Principle
- Open for extension through interfaces
- Closed for modification of core logic
- Strategy pattern for extensibility

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     TradingBot CLI                          │
│  ┌───────────────────────────────────────────────────────┐  │
│  │         CLI Layer (Spectre.Console)                    │  │
│  │  Commands | Dashboard | Input Validation              │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │         Application Layer (Use Cases)                  │  │
│  │  SignalProcessor | Background Jobs                    │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────┬───────────────┬──────────────────────┐  │
│  │ Strategy      │ Trading       │ Analytics            │  │
│  │ Engine        │ Engine        │ Module               │  │
│  │               │               │                      │  │
│  │ • Indicators  │ • Execution   │ • Metrics           │  │
│  │ • Strategies  │ • Portfolio   │ • Backtesting       │  │
│  │ • Signals     │ • Risk Mgmt   │ • Reports           │  │
│  └───────────────┴───────────────┴──────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │         Core Domain Layer                              │  │
│  │  Models | Interfaces | Enums | Value Objects          │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │         Infrastructure Layer                           │  │
│  │  Repositories | Market Data | Cache | Encryption      │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
   ┌────▼────┐       ┌─────▼──────┐     ┌─────▼──────┐
   │ Yahoo   │       │ Database   │     │ File       │
   │ Finance │       │ (SQLite/   │     │ System     │
   │ API     │       │ PostgreSQL)│     │            │
   └─────────┘       └────────────┘     └────────────┘
```

## Layer Details

### 1. Core Domain Layer (`TradingBot.Core`)

**Responsibility**: Define domain models, interfaces, and business rules

**Components**:
- **Models**: Domain entities (Order, Position, Trade, Candle, Account, Signal, Quote)
- **Interfaces**: Service contracts (IStrategy, IMarketDataService, IOrderExecutionService)
- **Enums**: Type-safe constants (OrderType, OrderStatus, SignalType, OrderSide)
- **Value Objects**: Immutable data structures

**Key Principles**:
- No dependencies on other layers
- Pure business logic
- Framework-agnostic
- Highly testable

**Example**:
```csharp
// Domain Model
public class Order
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public OrderType Type { get; set; }
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal? LimitPrice { get; set; }
    public OrderStatus Status { get; set; }
    // ... more properties
}

// Service Interface
public interface IOrderExecutionService
{
    Task<Order> SubmitOrderAsync(Order order, CancellationToken cancellationToken);
    Task<Order> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken);
    event EventHandler<Order> OrderFilled;
}
```

### 2. Infrastructure Layer (`TradingBot.Infrastructure`)

**Responsibility**: Implement data access and external service integrations

**Components**:

#### Persistence
- **DbContext**: EF Core database context with entity configurations
- **Repositories**: Data access implementations (Order, Position, Trade, Candle, Account)
- **Migrations**: Database schema versioning

#### External Services
- **YahooFinanceService**: Market data provider with Polly resilience
- **HistoricalDataCache**: Database-backed caching layer
- **EncryptionService**: AES-256-GCM encryption for secrets

#### Configuration
- **ConfigurationService**: Thread-safe config management
- **DependencyInjection**: Service registration

**Technology**:
- Entity Framework Core 9
- SQLite (development) / PostgreSQL (production)
- Polly for resilience (retry, circuit breaker)
- Serilog for structured logging

**Example**:
```csharp
public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(TradingBotDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Order>> GetBySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(o => o.Symbol == symbol)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
```

### 3. Strategy Engine (`TradingBot.Strategies`)

**Responsibility**: Implement trading strategies and technical indicators

**Components**:

#### Indicator Library
- **IndicatorLibrary**: Static methods for technical indicators
  - SMA (Simple Moving Average)
  - EMA (Exponential Moving Average)
  - RSI (Relative Strength Index)
  - MACD (Moving Average Convergence Divergence)
  - Bollinger Bands
  - ATR (Average True Range)

#### Base Strategy
- **BaseStrategy**: Abstract base class for all strategies
- Common functionality (enable/disable, validation, logging)

#### Built-in Strategies
- **MomentumStrategy**: Trend-following using RSI, MACD, SMA
- **MeanReversionStrategy**: Counter-trend using Bollinger Bands

**Strategy Architecture**:
```csharp
public abstract class BaseStrategy : IStrategy
{
    public string Name { get; }
    public abstract string Type { get; }
    public IReadOnlyList<string> Symbols { get; }
    public string Timeframe { get; }
    public bool IsEnabled { get; private set; }

    public abstract Task<Signal?> GenerateSignalAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken);

    protected abstract Task<Signal?> ExecuteStrategyLogicAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken);
}
```

### 4. Trading Engine (`TradingBot.Engine`)

**Responsibility**: Execute trades and manage positions

**Components**:

#### Order Execution
- **OrderExecutionService**: Submit, track, and manage orders
- Simulated execution with slippage and commission
- Order lifecycle: Pending → Submitted → Filled/Cancelled/Rejected
- Events: OrderFilled, OrderCancelled, OrderRejected

#### Portfolio Management
- **PortfolioManager**: Track positions and calculate P&L
- Real-time position updates
- Realized and unrealized P&L calculations
- Account equity and margin management

#### Risk Management
- **RiskManager**: Enforce risk limits and controls
- Position sizing validation (1-100% of equity)
- Leverage limits (1-10x)
- Stop-loss and take-profit settings
- Daily loss limits and max drawdown checks

#### Stop-Loss Management
- **StopLossManager**: Fixed and trailing stop-loss orders
- Dynamic stop level updates
- Automatic trigger monitoring

#### Position Sizing
- **PositionSizeCalculator**: Multiple sizing algorithms
  - Fixed amount
  - Fixed percent
  - Risk-based (stop-loss distance)
  - Kelly criterion
  - Volatility-based (ATR)

#### Signal Processing
- **SignalProcessor**: Convert signals to validated orders
- Automatic position sizing based on confidence
- Risk validation before submission
- Auto-generate stop-loss and take-profit orders

#### Strategy Orchestration
- **StrategyEngine**: Manage and execute multiple strategies
- Thread-safe strategy registration
- SignalGenerated event emission
- Background signal generation loop

### 5. Analytics Module (`TradingBot.Analytics`)

**Responsibility**: Analyze performance and generate reports

**Components**:

#### Backtesting
- **BacktestingEngine**: Simulate strategy performance
- Historical data replay
- Transaction cost modeling

#### Performance Metrics
- **MetricsCalculator**: Calculate performance metrics
  - Sharpe ratio
  - Sortino ratio
  - Win rate, profit factor
  - Maximum drawdown
  - Total return, annualized return

#### Visualization
- **EquityCurveGenerator**: Generate equity curves
- **DrawdownAnalyzer**: Analyze drawdown periods
- **BacktestReportGenerator**: Comprehensive HTML reports

#### Background Jobs
- **MarketDataRefreshJob**: Periodic data updates
- **RiskMonitoringJob**: Continuous risk monitoring
- **EndOfDayJob**: Daily processing tasks

### 6. CLI Layer (`TradingBot.Cli`)

**Responsibility**: Provide command-line interface

**Components**:

#### Command Structure
```
tradingbot
├── version
├── strategy [list|enable|disable|start|stop|status]
├── risk [show|set-leverage|set-stoploss|set-takeprofit|...]
├── portfolio [show|history|close]
├── performance [show|export]
├── backtest [run|report]
├── config [show|set|set-apikey]
└── dashboard
```

#### Implementation
- **Spectre.Console.Cli**: Command routing and parsing
- **TypeRegistrar**: DI adapter for Spectre.Cli
- **DashboardRenderer**: Live dashboard with Spectre.Console
- Input validation, error handling, progress bars

## Data Flow

### Signal-to-Order Flow

```
1. Market Data Fetch
   ↓
2. Strategy Execution (StrategyEngine)
   ↓
3. Signal Generation (IStrategy.GenerateSignalAsync)
   ↓
4. Signal Event Emission (StrategyEngine.SignalGenerated)
   ↓
5. Signal Processing (SignalProcessor subscribes to event)
   ↓
6. Position Sizing (PositionSizeCalculator)
   ↓
7. Risk Validation (RiskManager.ValidatePositionSizeAsync)
   ↓
8. Order Creation
   ↓
9. Order Submission (OrderExecutionService)
   ↓
10. Order Execution (simulated)
    ↓
11. Portfolio Update (PortfolioManager)
    ↓
12. Stop-Loss Creation (StopLossManager)
```

### Data Persistence Flow

```
Market Data Request
   ↓
Check Cache (HistoricalDataCache.GetAsync)
   ├─ Cache Hit → Return cached data
   └─ Cache Miss
      ↓
   Fetch from Yahoo Finance (YahooFinanceService)
      ↓
   Store in Cache (HistoricalDataCache.SetAsync)
      ↓
   Return data to caller
```

## Technology Stack

### Core Technologies
- **.NET 9.0**: Runtime and framework
- **C# 13.0**: Programming language
- **Entity Framework Core 9**: ORM for data access

### Database
- **SQLite**: Development and testing
- **PostgreSQL**: Production (supported via connection string)

### External Services
- **Yahoo Finance API**: Real-time and historical market data
- **YahooFinanceApi**: .NET client library

### Resilience
- **Polly**: Retry, circuit breaker, timeout policies
- Rate limiting and fault tolerance

### Logging
- **Serilog**: Structured logging
- Console and file sinks
- Log level configuration

### CLI Framework
- **Spectre.Console**: Terminal UI
- **Spectre.Console.Cli**: Command routing
- Tables, progress bars, interactive prompts

### Testing
- **xUnit**: Test framework
- **FakeItEasy**: Mocking framework
- **Shouldly**: Assertion library
- **Coverlet**: Code coverage

### Code Quality
- **StyleCop Analyzers**: Code style enforcement
- **Microsoft.CodeAnalysis.NetAnalyzers**: Best practices
- **SonarAnalyzer.CSharp**: Security and reliability

## Design Patterns

### 1. Repository Pattern
Abstracts data access logic behind interfaces

```csharp
public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetBySymbolAsync(string symbol, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetOpenOrdersAsync(CancellationToken ct);
}
```

### 2. Strategy Pattern
Encapsulates trading algorithms behind IStrategy interface

```csharp
public interface IStrategy
{
    string Name { get; }
    string Type { get; }
    bool IsEnabled { get; }
    Task<Signal?> GenerateSignalAsync(string symbol, IReadOnlyList<Candle> data, CancellationToken ct);
}
```

### 3. Observer Pattern
Event-driven architecture for signals and order execution

```csharp
public event EventHandler<Signal> SignalGenerated;
public event EventHandler<Order> OrderFilled;
```

### 4. Dependency Injection
Constructor injection for loose coupling

```csharp
public class PortfolioManager : IPortfolioManager
{
    private readonly IPositionRepository _positionRepository;
    private readonly ITradeRepository _tradeRepository;
    private readonly IAccountRepository _accountRepository;

    public PortfolioManager(
        IPositionRepository positionRepository,
        ITradeRepository tradeRepository,
        IAccountRepository accountRepository)
    {
        _positionRepository = positionRepository;
        _tradeRepository = tradeRepository;
        _accountRepository = accountRepository;
    }
}
```

### 5. Factory Pattern
Background job creation and scheduling

### 6. Template Method Pattern
BaseStrategy provides template, subclasses implement specific logic

### 7. Decorator Pattern
Polly wraps HTTP calls with resilience policies

## Security Architecture

### 1. API Key Encryption
- AES-256-GCM encryption
- Machine-specific key derivation
- Rfc2898DeriveBytes with 100,000 iterations
- Never log plaintext secrets

### 2. Input Validation
- All CLI inputs validated
- Order parameters checked before submission
- Risk limits enforced

### 3. Error Handling
- Exceptions logged but not exposed to users
- Graceful degradation
- Clear error messages without sensitive details

### 4. Configuration Security
- Sensitive config in encrypted storage
- Thread-safe configuration access
- Automatic backup before changes

## Scalability Considerations

### Current Design
- Single-process application
- SQLite for development
- Suitable for individual traders

### Future Enhancements
- PostgreSQL for production
- Multiple strategy instances
- Distributed backtesting
- WebSocket for real-time data
- Kubernetes deployment

## Testing Strategy

### Unit Tests
- Test individual components in isolation
- Mock dependencies with FakeItEasy
- Target 80%+ code coverage

### Integration Tests
- Test complete workflows
- Use in-memory database
- Verify end-to-end behavior

### Performance Tests
- Backtest engine throughput
- Indicator calculation speed
- Database query optimization

## Conclusion

TradingBot CLI's architecture prioritizes:
- **Maintainability**: Clean separation of concerns
- **Testability**: Dependency injection and interfaces
- **Extensibility**: Strategy pattern for new algorithms
- **Reliability**: Resilience patterns and error handling
- **Security**: Encryption and input validation

This architecture supports both current requirements and future growth while maintaining code quality and developer productivity.
