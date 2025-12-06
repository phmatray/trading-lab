# TradingStrat - Hexagonal Architecture Trading System

A sophisticated trading strategy backtesting and analysis system built with **Hexagonal Architecture** (Ports & Adapters pattern), featuring ML-powered predictions, comprehensive technical indicators, and full dependency injection.

## 🏗️ Architecture Overview

This project demonstrates a **production-grade hexagonal architecture** implementation, transforming a 3,548-line monolith into a clean, testable, and maintainable system.

```
┌─────────────────────────────────────────────────────────────────┐
│                      Presentation Layer                         │
│  ┌────────────┐  ┌──────────────┐  ┌────────────────┐          │
│  │ ProgramMenu│  │  Presenters  │  │  appsettings   │          │
│  │   (CLI)    │  │  (Spectre)   │  │     .json      │          │
│  └────────────┘  └──────────────┘  └────────────────┘          │
└────────────────────────┬────────────────────────────────────────┘
                         │ Uses
┌────────────────────────▼────────────────────────────────────────┐
│                      Application Layer                          │
│  ┌─────────────────┐  ┌──────────────────────────────────┐     │
│  │   Use Cases     │  │    Application Services          │     │
│  │ • FetchData     │  │ • BacktestEngine                 │     │
│  │ • RunBacktest   │  │ • StrategyFactory                │     │
│  │ • AnalyzePosition│  │ • FeatureEngineering            │     │
│  └─────────────────┘  └──────────────────────────────────┘     │
│           │                                                      │
│           ├──────► Inbound Ports (Use Case Interfaces)          │
│           └──────► Outbound Ports (Infrastructure Interfaces)   │
└────────────────────────┬────────────────────────────────────────┘
                         │ Depends on
┌────────────────────────▼────────────────────────────────────────┐
│                        Domain Layer                             │
│  ┌──────────────┐  ┌────────────────┐  ┌──────────────┐       │
│  │  Entities    │  │  Value Objects │  │  Strategies  │       │
│  │ • Trade      │  │ • TradeSignal  │  │ • MA Cross   │       │
│  │ • Historical │  │ • Thresholds   │  │ • RSI        │       │
│  │   Price      │  │ • Features     │  │ • MACD       │       │
│  └──────────────┘  └────────────────┘  │ • ML-based   │       │
│                                         └──────────────┘       │
│  ┌──────────────────────────────────────────────────────┐      │
│  │           Domain Services                            │      │
│  │ • IIndicatorCalculator (26 technical indicators)    │      │
│  │ • PerformanceCalculator                             │      │
│  └──────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────┘
                         ▲ Implements
┌────────────────────────┴────────────────────────────────────────┐
│                   Infrastructure Layer                          │
│  ┌──────────────────┐  ┌────────────────────────────────┐      │
│  │   Persistence    │  │      External Services         │      │
│  │ • EF Core SQLite │  │ • Yahoo Finance API            │      │
│  │ • Repository     │  │ • ML.NET FastTree              │      │
│  │                  │  │ • CSV/JSON Export              │      │
│  └──────────────────┘  └────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────┘
```

## 🎯 Key Features

### Trading Strategies
- **Moving Average Crossover** - Fast/slow MA with configurable periods
- **RSI Strategy** - Relative Strength Index with overbought/oversold thresholds
- **MACD Strategy** - Moving Average Convergence Divergence
- **ML FastTree** - Machine learning predictions with 26 technical indicators

### Technical Indicators (26 Total)
- **Price-based** (5): Daily Return, Log Return, High-Low Range, Open-Close Range, Price Position
- **Moving Averages** (6): SMA 5/10/20, EMA 12/26, Price to SMA20 ratio
- **Momentum** (4): RSI 14, Momentum 5, ROC 10, Stochastic RSI
- **MACD** (3): MACD line, Signal line, Histogram
- **Volatility** (4): StdDev 10/20, ATR 14, Bollinger Position
- **Volume** (4): Volume Change, Volume MA 10, Volume Ratio, Price-Volume Correlation

### ML-Powered Predictions
- **Algorithm**: FastTree Gradient Boosting (ML.NET)
- **Features**: 26 technical indicators
- **Target**: Next-day return prediction
- **Configurable**: Training bars, thresholds, model parameters

### Data Management
- **Source**: Yahoo Finance API (via YahooQuotesApi)
- **Storage**: SQLite with Entity Framework Core
- **Export**: CSV and JSON formats
- **Deduplication**: Automatic duplicate detection

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- SQLite (included)

### Run the Application

```bash
# Navigate to presentation project
cd src/TradingStrat.Presentation

# Run the application
dotnet run
```

### Configuration

Edit `appsettings.json` to customize:

```json
{
  "Trading": {
    "DefaultTicker": "CON3.L",
    "DefaultIsin": "XS2399367254",
    "Database": {
      "ConnectionString": "Data Source=trading.db"
    },
    "Backtest": {
      "InitialCapital": 10000,
      "CommissionPercentage": 0.001,
      "MinimumCommission": 1.0
    },
    "MachineLearning": {
      "MinTrainingBars": 100,
      "ModelParameters": {
        "NumberOfTrees": 100,
        "LearningRate": 0.1
      }
    }
  }
}
```

## 📁 Project Structure

```
TradingStrat/
├── src/
│   ├── TradingStrat.Domain/              # Core business logic
│   │   ├── Entities/                     # Domain entities
│   │   ├── ValueObjects/                 # Immutable value objects
│   │   ├── Services/                     # Domain services
│   │   │   └── Indicators/               # Technical indicator calculations
│   │   └── Strategies/                   # Trading strategies
│   │
│   ├── TradingStrat.Application/         # Use cases & orchestration
│   │   ├── Ports/
│   │   │   ├── Inbound/                  # Use case interfaces
│   │   │   └── Outbound/                 # Infrastructure interfaces
│   │   ├── UseCases/                     # Use case implementations
│   │   ├── Services/                     # Application services
│   │   ├── Factories/                    # Strategy factory
│   │   └── Configuration/                # Configuration models
│   │
│   ├── TradingStrat.Infrastructure/      # External adapters
│   │   ├── Persistence/
│   │   │   └── EfCore/                   # EF Core repository
│   │   ├── MarketData/                   # Yahoo Finance adapter
│   │   ├── Export/                       # CSV/JSON export
│   │   ├── MachineLearning/              # ML.NET adapter
│   │   └── DependencyInjection/          # Service registration
│   │
│   └── TradingStrat.Presentation/        # CLI interface
│       ├── Console/                      # Menu and presenters
│       ├── DependencyInjection/          # Presentation services
│       └── appsettings.json              # Configuration file
│
└── tests/
    ├── TradingStrat.Domain.Tests/        # Domain unit tests
    ├── TradingStrat.Application.Tests/   # Application unit tests
    │   └── TestDoubles/                  # In-memory test implementations
    └── TradingStrat.Infrastructure.Tests/ # Integration tests
```

## 🧪 Testing

### Run All Tests

```bash
# Run all test projects
dotnet test

# Run specific test project
dotnet test tests/TradingStrat.Domain.Tests
dotnet test tests/TradingStrat.Application.Tests
dotnet test tests/TradingStrat.Infrastructure.Tests
```

### Test Coverage

- **Domain Tests**: Strategy behavior, indicator calculations
- **Application Tests**: Use case orchestration with test doubles
- **Infrastructure Tests**: Database integration with in-memory SQLite

### Test Doubles

```csharp
// In-memory repository for testing
var repository = new InMemoryHistoricalDataRepository();
repository.SeedData("TEST", testPrices);

// Fake market data adapter (no API calls)
var marketData = new FakeMarketDataAdapter();
```

## 🏛️ Architectural Principles

### Hexagonal Architecture Benefits

1. **Independence from Frameworks**: Domain logic has zero external dependencies
2. **Testability**: Easy to test with in-memory implementations
3. **Flexibility**: Swap implementations without changing business logic
4. **Maintainability**: Clear separation of concerns
5. **Scalability**: Easy to add new strategies, indicators, or data sources

### Dependency Flow

```
Presentation → Application → Domain ← Infrastructure
                                ▲
                                │
                     (Infrastructure implements
                      Domain interfaces)
```

### Key Design Patterns

- **Ports & Adapters**: Clean architecture boundaries
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Repository Pattern**: Data access abstraction
- **Strategy Pattern**: Pluggable trading strategies
- **Factory Pattern**: Strategy creation with DI
- **Builder Pattern**: Test data creation

## 🔧 Development

### Adding a New Strategy

1. **Create strategy in Domain**:
```csharp
public class MyStrategy : BaseStrategy
{
    public MyStrategy(IIndicatorCalculator indicatorCalculator)
        : base(indicatorCalculator) { }

    public override TradeSignal GenerateSignal(int currentIndex,
        decimal currentCash, int currentPosition)
    {
        // Your strategy logic
    }
}
```

2. **Register in StrategyFactory**:
```csharp
public IStrategy CreateStrategy(string strategyType,
    Dictionary<string, object>? parameters = null)
{
    return strategyType.ToLowerInvariant() switch
    {
        "mystrategy" => new MyStrategy(_indicatorCalculator),
        // ... other strategies
    };
}
```

### Adding a New Indicator

1. **Add to IIndicatorCalculator interface**:
```csharp
public interface IIndicatorCalculator
{
    decimal[] CalculateMyIndicator(decimal[] prices, int period);
}
```

2. **Implement in IndicatorCalculator**:
```csharp
public decimal[] CalculateMyIndicator(decimal[] prices, int period)
{
    // Calculation logic
}
```

3. **Use in strategies**:
```csharp
var myIndicator = _indicatorCalculator.CalculateMyIndicator(ClosePrices, 14);
```

## 📊 Performance Metrics

The system calculates comprehensive performance metrics:

- Total Return & Annualized Return
- Sharpe Ratio & Volatility
- Win Rate & Profit Factor
- Maximum Drawdown
- Average Win/Loss
- Consecutive Win/Loss Streaks
- Market Exposure

## 🔍 Technical Details

### Technologies Used

- **.NET 10.0** - Latest .NET platform
- **Entity Framework Core 10.0** - ORM with SQLite
- **ML.NET 5.0** - Machine learning framework
- **YahooQuotesApi 7.0.5** - Market data fetching
- **Spectre.Console 0.54.0** - Rich terminal UI
- **CsvHelper 33.1.0** - CSV export
- **xUnit 2.9.3** - Testing framework
- **Moq 4.20.72** - Mocking library
- **FluentAssertions 8.8.0** - Fluent test assertions

### Database Schema

**HistoricalPrices**:
- Unique constraint on (Ticker, DateTime)
- Indexed on ISIN
- Decimal precision for prices (18,6)

**Securities**:
- Unique constraints on Ticker and ISIN
- Metadata for securities

## 📚 Migration from Monolith

The refactoring eliminated several critical issues:

### Issues Resolved

1. ✅ **Removed Reflection Hack**: FeatureEngineering now uses IIndicatorCalculator
2. ✅ **Eliminated Indicator Duplication**: Single source of truth in domain service
3. ✅ **Removed Hardcoded Values**: All configuration in appsettings.json
4. ✅ **Proper Dependency Injection**: No more `new TradingContext()`
5. ✅ **ML Training Separation**: ML model training via IMLModelPort

### Before vs After

**Before** (Monolithic):
```csharp
var context = new TradingContext();  // Hardcoded connection
var repository = new DataRepository(context);
var yahooService = new YahooFinanceService();
const string ticker = "CON3.L";  // Hardcoded
```

**After** (Hexagonal):
```csharp
public ProgramMenu(
    IDataFetchingUseCase dataFetchingUseCase,
    IBacktestUseCase backtestUseCase,
    IOptions<TradingConfiguration> config)  // All injected
{
    var ticker = config.Value.DefaultTicker;  // From config
}
```

## 🤝 Contributing

When adding new features:

1. **Start in Domain**: Define entities, value objects, strategies
2. **Define Ports**: Create interfaces in Application/Ports
3. **Implement Use Cases**: Orchestrate in Application/UseCases
4. **Add Infrastructure**: Implement adapters in Infrastructure
5. **Update Presentation**: Wire up in CLI menu
6. **Write Tests**: Add unit and integration tests

## 📝 License

This project is part of a hexagonal architecture refactoring demonstration.

## 🎓 Learning Resources

This project demonstrates:
- Hexagonal Architecture / Ports & Adapters
- Domain-Driven Design (DDD)
- Dependency Injection
- Clean Architecture
- SOLID Principles
- Test-Driven Development (TDD)
- ML.NET Integration
- Entity Framework Core Best Practices

---

**Built with ❤️ using Hexagonal Architecture principles**
