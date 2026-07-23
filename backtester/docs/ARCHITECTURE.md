# TradingStrat - Architecture Documentation

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Layer Responsibilities](#layer-responsibilities)
3. [Dependency Flow](#dependency-flow)
4. [Port Definitions](#port-definitions)
5. [Design Patterns](#design-patterns)
6. [Data Flow](#data-flow)
7. [Extension Points](#extension-points)

## Architecture Overview

TradingStrat implements **Hexagonal Architecture** (also known as Ports & Adapters), a pattern that creates clear boundaries between business logic and external concerns.

### Core Principles

1. **Domain at the Center**: Business logic has no external dependencies
2. **Dependency Inversion**: All dependencies point inward toward the domain
3. **Ports Define Contracts**: Interfaces define how layers communicate
4. **Adapters Implement Ports**: Infrastructure implements domain interfaces
5. **Testability**: Easy to test with in-memory implementations

## Layer Responsibilities

### 1. Domain Layer (`TradingStrat.Domain`)

**Purpose**: Contains pure business logic with zero external dependencies

**Responsibilities**:
- Define core business entities
- Implement trading strategies
- Calculate technical indicators
- Define business rules and invariants

**Dependencies**: None (only .NET BCL)

**Key Components**:
```
Domain/
├── Entities/                  # Business objects with identity
│   ├── HistoricalPrice.cs    # Price data entity
│   ├── Trade.cs              # Trade execution record
│   ├── BacktestResult.cs     # Backtest execution result
│   └── LiveAnalysisResult.cs # Live prediction result
│
├── ValueObjects/              # Immutable values
│   ├── TradeSignal.cs        # Buy/Sell/Hold signal
│   ├── PredictionThresholds.cs # ML prediction thresholds
│   └── MarketFeatures.cs     # 26 technical indicators
│
├── Services/Indicators/       # Domain services
│   ├── IIndicatorCalculator.cs  # Interface
│   └── IndicatorCalculator.cs   # 26 technical indicators
│
└── Strategies/                # Trading algorithms
    ├── IStrategy.cs           # Strategy interface
    ├── BaseStrategy.cs        # Abstract base with common logic
    ├── MovingAverageCrossoverStrategy.cs
    ├── RSIStrategy.cs
    ├── MACDStrategy.cs
    └── MachineLearningStrategy.cs
```

### 2. Application Layer (`TradingStrat.Application`)

**Purpose**: Orchestrates use cases and coordinates between domain and infrastructure

**Responsibilities**:
- Define use case interfaces (Inbound Ports)
- Define infrastructure interfaces (Outbound Ports)
- Implement business workflows
- Coordinate between domain services and infrastructure

**Dependencies**: Domain layer only

**Key Components**:
```
Application/
├── Ports/
│   ├── Inbound/               # Use case interfaces
│   │   ├── IDataFetchingUseCase.cs
│   │   ├── IBacktestUseCase.cs
│   │   └── ILiveAnalysisUseCase.cs
│   │
│   └── Outbound/              # Infrastructure interfaces
│       ├── IHistoricalDataPort.cs    # Data persistence
│       ├── IMarketDataPort.cs        # External API
│       ├── IMLModelPort.cs           # Machine learning
│       └── IExportPort.cs            # File export
│
├── UseCases/                  # Use case implementations
│   ├── FetchHistoricalDataUseCase.cs
│   ├── RunBacktestUseCase.cs
│   └── AnalyzeCurrentPositionUseCase.cs
│
├── Services/                  # Application services
│   ├── BacktestEngine.cs      # Backtest orchestration
│   ├── FeatureEngineering.cs  # ML feature calculation
│   └── TickerResolver.cs      # ISIN→Ticker resolution
│
└── Factories/
    └── StrategyFactory.cs     # Creates strategies with DI
```

### 3. Infrastructure Layer (`TradingStrat.Infrastructure`)

**Purpose**: Implements technical details and external system integrations

**Responsibilities**:
- Implement outbound ports
- Integrate with databases
- Connect to external APIs
- Handle file I/O
- Manage ML model training

**Dependencies**: Application layer (implements its ports), Domain layer

**Key Components**:
```
Infrastructure/
├── Persistence/EfCore/
│   ├── TradingContext.cs           # EF Core DbContext
│   └── HistoricalDataRepository.cs # IHistoricalDataPort implementation
│
├── MarketData/
│   └── YahooFinanceAdapter.cs      # IMarketDataPort implementation
│
├── MachineLearning/
│   └── MlNetModelAdapter.cs        # IMLModelPort implementation
│
├── Export/
│   └── ExportAdapter.cs            # IExportPort implementation
│
└── DependencyInjection/
    └── InfrastructureServiceRegistration.cs
```

### 4. Presentation Layer (`TradingStrat.Presentation`)

**Purpose**: Handles user interaction (CLI in this case)

**Responsibilities**:
- Display user interface
- Capture user input
- Format output
- Wire up dependency injection

**Dependencies**: Application and Infrastructure layers

**Key Components**:
```
Presentation/
├── Console/
│   ├── ProgramMenu.cs         # Main menu orchestration
│   └── Presenters/            # Output formatting
│       ├── DataSummaryPresenter.cs
│       ├── BacktestPresenter.cs
│       └── AnalysisPresenter.cs
│
├── Program.cs                 # Entry point with DI setup
├── appsettings.json          # Configuration
└── DependencyInjection/
    └── PresentationServiceRegistration.cs
```

## Dependency Flow

```
┌──────────────────────────────────────────────────────┐
│  Presentation Layer                                  │
│  • Depends on: Application, Infrastructure          │
│  • Knows about: All layers (composition root)       │
└────────────────┬─────────────────────────────────────┘
                 │ Uses
┌────────────────▼─────────────────────────────────────┐
│  Application Layer                                   │
│  • Depends on: Domain only                          │
│  • Defines: Ports (interfaces) for infrastructure   │
└────────────────┬─────────────────────────────────────┘
                 │ Depends on
┌────────────────▼─────────────────────────────────────┐
│  Domain Layer                                        │
│  • Depends on: Nothing (pure business logic)        │
│  • No knowledge of: Databases, APIs, UI, etc.       │
└──────────────────────────────────────────────────────┘
                 ▲
                 │ Implements
┌────────────────┴─────────────────────────────────────┐
│  Infrastructure Layer                                │
│  • Depends on: Application (implements ports)       │
│  • Implements: Outbound port interfaces             │
└──────────────────────────────────────────────────────┘
```

## Port Definitions

### Inbound Ports (Application → Presentation)

**Purpose**: Define what the application can do (use cases)

```csharp
// Fetch historical data from external source
public interface IDataFetchingUseCase
{
    Task<DataSummaryResult> ExecuteAsync(
        FetchDataCommand command,
        IProgress<string>? progress = null);
}

// Run strategy backtest
public interface IBacktestUseCase
{
    Task<BacktestResult> ExecuteAsync(
        BacktestCommand command,
        IProgress<BacktestProgress>? progress = null);
}

// Analyze current position with ML
public interface ILiveAnalysisUseCase
{
    Task<LiveAnalysisResult> ExecuteAsync(
        AnalysisCommand command,
        IProgress<string>? progress = null);
}
```

### Outbound Ports (Infrastructure → Application)

**Purpose**: Define what external systems the application needs

```csharp
// Data persistence
public interface IHistoricalDataPort
{
    Task SaveHistoricalDataAsync(string ticker, string? isin,
        IEnumerable<HistoricalPrice> data);
    Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker);
    Task<DataSummaryResult> GetDataSummaryAsync(string ticker);
    // ... other methods
}

// External market data
public interface IMarketDataPort
{
    Task<IReadOnlyList<HistoricalPrice>> FetchHistoricalDataAsync(
        string ticker, DateTime startDate, DateTime endDate);
    Task<HistoricalPrice?> FetchLatestPriceAsync(string ticker);
}

// Machine learning
public interface IMLModelPort
{
    ITransformer TrainModel(IDataView trainingData,
        MLModelConfiguration config);
    float Predict(ITransformer model, MarketFeatures features);
}

// File export
public interface IExportPort
{
    Task ExportToCsvAsync<T>(IEnumerable<T> data, string filePath);
    Task ExportToJsonAsync<T>(T data, string filePath);
}
```

## Design Patterns

### 1. Ports & Adapters (Hexagonal Architecture)

**Purpose**: Isolate business logic from external concerns

**Implementation**:
- **Ports**: Interfaces in Application layer
- **Adapters**: Implementations in Infrastructure layer

**Example**:
```csharp
// Port (Application layer)
public interface IHistoricalDataPort
{
    Task SaveHistoricalDataAsync(string ticker, string? isin,
        IEnumerable<HistoricalPrice> data);
}

// Adapter (Infrastructure layer)
public class HistoricalDataRepository : IHistoricalDataPort
{
    private readonly TradingContext _context;
    // EF Core implementation
}
```

### 2. Strategy Pattern

**Purpose**: Pluggable trading algorithms

**Implementation**:
```csharp
public interface IStrategy
{
    TradeSignal GenerateSignal(int currentIndex,
        decimal currentCash, int currentPosition);
}

// Concrete strategies
public class MovingAverageCrossoverStrategy : IStrategy { }
public class RSIStrategy : IStrategy { }
public class MachineLearningStrategy : IStrategy { }
```

### 3. Factory Pattern

**Purpose**: Create strategies with dependencies

**Implementation**:
```csharp
public class StrategyFactory : IStrategyFactory
{
    private readonly IIndicatorCalculator _indicatorCalculator;

    public IStrategy CreateStrategy(string strategyType,
        Dictionary<string, object>? parameters = null)
    {
        return strategyType switch
        {
            "ma" => new MovingAverageCrossoverStrategy(_indicatorCalculator),
            "rsi" => new RSIStrategy(_indicatorCalculator),
            // ...
        };
    }
}
```

### 4. Repository Pattern

**Purpose**: Abstract data access

**Implementation**:
```csharp
// IHistoricalDataPort is the repository interface
public class HistoricalDataRepository : IHistoricalDataPort
{
    // EF Core implementation details hidden
}
```

### 5. Dependency Injection

**Purpose**: Loose coupling and testability

**Implementation**:
```csharp
// Registration
services.AddScoped<IDataFetchingUseCase, FetchHistoricalDataUseCase>();
services.AddScoped<IHistoricalDataPort, HistoricalDataRepository>();

// Injection
public class ProgramMenu
{
    public ProgramMenu(
        IDataFetchingUseCase dataFetchingUseCase,
        IBacktestUseCase backtestUseCase,
        ILiveAnalysisUseCase liveAnalysisUseCase)
    {
        // All dependencies injected
    }
}
```

## Data Flow

### Example: Fetch Historical Data

```
1. User Action (Presentation)
   ProgramMenu.RunDataFetcherAsync()
   ↓
2. Create Command (Presentation)
   new FetchDataCommand(ticker, isin, startDate, endDate)
   ↓
3. Execute Use Case (Application)
   IDataFetchingUseCase.ExecuteAsync(command)
   ↓
4. Check Latest Date (Application → Infrastructure)
   IHistoricalDataPort.GetLatestDataDateAsync(ticker)
   ↓
5. Fetch External Data (Application → Infrastructure)
   IMarketDataPort.FetchHistoricalDataAsync(ticker, start, end)
   ↓
6. Save Data (Application → Infrastructure)
   IHistoricalDataPort.SaveHistoricalDataAsync(ticker, isin, data)
   ↓
7. Get Summary (Application → Infrastructure)
   IHistoricalDataPort.GetDataSummaryAsync(ticker)
   ↓
8. Return Result (Application → Presentation)
   DataSummaryResult
   ↓
9. Display (Presentation)
   DataSummaryPresenter.Display(result)
```

### Example: Run Backtest

```
1. User Selection (Presentation)
   Select strategy, initial capital, date range
   ↓
2. Create Command (Presentation)
   new BacktestCommand(ticker, strategyType, parameters...)
   ↓
3. Execute Use Case (Application)
   IBacktestUseCase.ExecuteAsync(command)
   ↓
4. Load Historical Data (Application → Infrastructure)
   IHistoricalDataPort.GetHistoricalDataAsync(ticker, start, end)
   ↓
5. Create Strategy (Application)
   IStrategyFactory.CreateStrategy(strategyType, parameters)
   ↓
6. Run Backtest (Application)
   BacktestEngine.RunBacktestAsync(strategy, config)
   ↓
7. Strategy Generates Signals (Domain)
   For each bar: strategy.GenerateSignal(index, cash, position)
   ↓
8. Calculate Performance (Domain)
   PerformanceCalculator.Calculate(trades, equity)
   ↓
9. Return Result (Application → Presentation)
   BacktestResult (metrics, trades, equity curve)
   ↓
10. Display (Presentation)
    BacktestPresenter.DisplayResults(result)
```

## Extension Points

### Adding a New Strategy

1. **Create strategy class** (Domain):
```csharp
public class MyCustomStrategy : BaseStrategy
{
    public MyCustomStrategy(IIndicatorCalculator indicatorCalculator)
        : base(indicatorCalculator) { }

    public override TradeSignal GenerateSignal(...)
    {
        // Implementation
    }
}
```

2. **Register in factory** (Application):
```csharp
return strategyType switch
{
    "custom" => new MyCustomStrategy(_indicatorCalculator),
    // ... other strategies
};
```

### Adding a New Data Source

1. **Implement port** (Infrastructure):
```csharp
public class AlphaVantageAdapter : IMarketDataPort
{
    public async Task<IReadOnlyList<HistoricalPrice>>
        FetchHistoricalDataAsync(...)
    {
        // Alpha Vantage API implementation
    }
}
```

2. **Register service** (Infrastructure):
```csharp
services.AddScoped<IMarketDataPort, AlphaVantageAdapter>();
```

### Adding a New Indicator

1. **Add to interface** (Domain):
```csharp
public interface IIndicatorCalculator
{
    decimal[] CalculateMyIndicator(decimal[] prices, int period);
}
```

2. **Implement** (Domain):
```csharp
public decimal[] CalculateMyIndicator(decimal[] prices, int period)
{
    // Calculation logic
}
```

3. **Use in strategies** (Domain):
```csharp
var indicator = _indicatorCalculator.CalculateMyIndicator(ClosePrices, 14);
```

### Adding a New Export Format

1. **Add method to port** (Application):
```csharp
public interface IExportPort
{
    Task ExportToXmlAsync<T>(T data, string filePath);
}
```

2. **Implement** (Infrastructure):
```csharp
public class ExportAdapter : IExportPort
{
    public async Task ExportToXmlAsync<T>(T data, string filePath)
    {
        // XML serialization implementation
    }
}
```

## Testing Strategy

### Unit Tests (Domain Layer)

Test strategies in isolation with test data:

```csharp
var indicatorCalculator = new IndicatorCalculator();
var strategy = new MovingAverageCrossoverStrategy(indicatorCalculator, 5, 10);
strategy.Initialize(testPrices);
var signal = strategy.GenerateSignal(index, cash, position);
```

### Unit Tests (Application Layer)

Use test doubles for infrastructure:

```csharp
var mockHistoricalDataPort = new Mock<IHistoricalDataPort>();
var mockMarketDataPort = new Mock<IMarketDataPort>();
var useCase = new FetchHistoricalDataUseCase(
    mockHistoricalDataPort.Object,
    mockMarketDataPort.Object,
    mockTickerResolver.Object);
```

### Integration Tests (Infrastructure Layer)

Use real database with in-memory SQLite:

```csharp
var options = new DbContextOptionsBuilder<TradingContext>()
    .UseSqlite("Data Source=:memory:")
    .Options;
var context = new TradingContext(options);
context.Database.OpenConnection();
context.Database.EnsureCreated();
```

## Configuration Management

All configuration in `appsettings.json`:

```json
{
  "Trading": {
    "DefaultTicker": "CON3.L",
    "Database": { "ConnectionString": "Data Source=trading.db" },
    "Backtest": { "InitialCapital": 10000 },
    "MachineLearning": {
      "ModelParameters": { "NumberOfTrees": 100 }
    }
  }
}
```

Loaded via:
```csharp
services.Configure<TradingConfiguration>(
    configuration.GetSection("Trading"));
```

Injected as:
```csharp
public ProgramMenu(IOptions<TradingConfiguration> config)
{
    var ticker = config.Value.DefaultTicker;
}
```

## Conclusion

This hexagonal architecture provides:
- ✅ Clear separation of concerns
- ✅ Easy testability
- ✅ Flexibility to swap implementations
- ✅ Independence from frameworks
- ✅ Maintainability and scalability

The architecture is production-ready and demonstrates enterprise-grade software design principles.
