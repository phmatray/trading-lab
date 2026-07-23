![TradingStrat banner](.github/banner.png)

# TradingStrat - Hexagonal Architecture Trading System

<!-- portfolio-badges:start -->
<!-- Identity -->
[![phmatray - TradingStrat](https://img.shields.io/static/v1?label=phmatray&message=TradingStrat&color=blue&logo=github)](https://github.com/phmatray/TradingStrat)
![Top language](https://img.shields.io/github/languages/top/phmatray/TradingStrat)
[![Stars](https://img.shields.io/github/stars/phmatray/TradingStrat?style=social)](https://github.com/phmatray/TradingStrat/stargazers)
[![Forks](https://img.shields.io/github/forks/phmatray/TradingStrat?style=social)](https://github.com/phmatray/TradingStrat/network/members)

<!-- Activity -->
[![Issues](https://img.shields.io/github/issues/phmatray/TradingStrat)](https://github.com/phmatray/TradingStrat/issues)
[![Pull requests](https://img.shields.io/github/issues-pr/phmatray/TradingStrat)](https://github.com/phmatray/TradingStrat/pulls)
[![Last commit](https://img.shields.io/github/last-commit/phmatray/TradingStrat)](https://github.com/phmatray/TradingStrat/commits)
<!-- portfolio-badges:end -->

<!-- portfolio-toc:start -->

## Table of Contents

- [🏗️ Architecture Overview](#-architecture-overview)
- [🎯 Key Features](#-key-features)
- [🚀 Quick Start](#-quick-start)
- [🧭 New User Workflow](#-new-user-workflow)
- [📁 Project Structure](#-project-structure)
- [🧪 Testing](#-testing)
- [🏛️ Architectural Principles](#-architectural-principles)
- [🔧 Development](#-development)
- [📊 Performance Metrics](#-performance-metrics)
- [🔍 Technical Details](#-technical-details)
- [📚 Migration from Monolith](#-migration-from-monolith)
- [🤝 Contributing](#-contributing)
- [📝 License](#-license)
- [🎓 Learning Resources](#-learning-resources)

<!-- portfolio-toc:end -->



A sophisticated trading strategy backtesting and analysis system built with **Hexagonal Architecture** (Ports & Adapters pattern), featuring ML-powered predictions, comprehensive technical indicators, and full dependency injection.

## 🏗️ Architecture Overview

This project demonstrates a **production-grade hexagonal architecture** implementation, transforming a 3,548-line monolith into a clean, testable, and maintainable system.

```
┌─────────────────────────────────────────────────────────────────┐
│                      Presentation Layer                         │
│  ┌────────────┐  ┌──────────────┐  ┌────────────────┐          │
│  │  Blazor    │  │  Components  │  │  appsettings   │          │
│  │   Server   │  │    & Pages   │  │     .json      │          │
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
# Navigate to web project
cd src/TradingStrat.Web

# Run the application
dotnet run

# Open browser to https://localhost:5218 (or the URL shown in terminal)
```

### Configuration

Edit `src/TradingStrat.Web/appsettings.json` to customize:

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

## 🧭 New User Workflow

This guide walks you through creating, testing, and deploying your first trading strategy in ~5 minutes.

### Option 1: Quick Workflow (Unified Interface)

**Strategy Workspace** (`/workspace`) provides a single-page tabbed interface for the complete workflow:

1. **Navigate to Strategy Workspace** - Click "Strategy Workspace" in the left sidebar
2. **Define Tab** - Create a custom strategy using the visual rule builder
   - Add entry rules (e.g., RSI < 30)
   - Add exit rules (e.g., RSI > 70)
   - Configure position sizing
   - Save your strategy
3. **Test Tab** - Backtest your strategy
   - Select ticker and date range
   - Review performance metrics
   - Analyze equity curve
4. **Optimize Tab** - Fine-tune parameters
   - Select parameters to optimize
   - Choose algorithm (Grid Search or Genetic)
   - Review best parameters
5. **Deploy Tab** - Put your strategy to work
   - Create portfolio with optimized strategy
   - Export configuration
   - Schedule automatic trading (future)

**Context Preserved:** Your strategy, ticker, and parameters carry across tabs automatically - no re-entering data!

### Option 2: Step-by-Step Workflow (Individual Pages)

For users who prefer detailed control at each step:

#### Step 1: Fetch Historical Data (`/data`)
1. Navigate to **Data Management** → **Fetch Data**
2. Enter a ticker symbol (e.g., `AAPL`, `MSFT`, `CON3.L`)
3. Select date range (default: last 2 years)
4. Click **Fetch Data**
5. Verify data coverage in **Data Status** (`/data/status`)

#### Step 2: Create a Custom Strategy (`/strategies/builder`)
1. Navigate to **Strategy Research** → **Strategy Builder**
2. Enter strategy name and description
3. Add entry rules using visual builder:
   - Select indicator (RSI, SMA, MACD, etc.)
   - Set parameters (e.g., RSI Period: 14)
   - Choose comparison operator (e.g., < 30)
   - Add multiple rules with AND/OR logic
4. Add exit rules similarly
5. Configure position sizing (fixed %, risk-based, etc.)
6. Click **Save Strategy**

#### Step 3: Backtest Your Strategy (`/backtest`)
1. Navigate to **Strategy Research** → **Backtest**
2. Select your custom strategy from dropdown
3. Configure:
   - Ticker symbol
   - Initial capital ($10,000 default)
   - Date range
4. Click **Run Backtest**
5. Review results:
   - Total Return, Sharpe Ratio, Max Drawdown
   - Equity curve chart
   - Trade-by-trade breakdown
6. Use **Quick Actions**:
   - Create Portfolio
   - Compare with other strategies
   - Optimize parameters
   - View in archive

#### Step 4: Optimize Parameters (`/strategies/optimize`)
1. Navigate to **Strategy Research** → **Strategy Optimization**
2. Select your custom strategy
3. Configure parameter ranges:
   - RSI Period: Min 10, Max 20, Step 1
   - Enable/disable parameters to optimize
4. Choose optimization algorithm:
   - **Grid Search**: Exhaustive (slower, thorough)
   - **Genetic Algorithm**: Heuristic (faster, good for large spaces)
5. Select objective (Sharpe Ratio, Total Return, etc.)
6. Click **Start Optimization**
7. Review results:
   - Best parameters found
   - Top 5 parameter combinations
   - Performance comparison
8. Use **Quick Actions**:
   - Apply best parameters to strategy
   - Run backtest with best
   - Save as new strategy

#### Step 5: Compare Strategies (`/strategies/compare`)
1. Navigate to **Strategy Research** → **Compare Strategies**
2. Select up to 5 strategies (built-in or custom)
3. Click **Compare**
4. Review comparison matrix:
   - Side-by-side metrics
   - Equity curve overlay
   - "Best" column highlighting
5. Export results to CSV

#### Step 6: Create Portfolio (`/portfolios`)
1. Navigate to **Portfolio** → **Portfolios**
2. Click **Create Portfolio**
3. Enter portfolio name and initial cash
4. Add positions manually or import from backtest results
5. View portfolio dashboard (`/portfolio/{id}`):
   - Current value and allocation
   - Unrealized gains/losses
   - Position details
6. **Rebalance** (`/portfolio/{id}/rebalance`):
   - Set target allocations
   - Calculate rebalancing signals
   - Review buy/sell recommendations
7. **Performance Analytics** (`/portfolio/{id}/performance`):
   - Historical portfolio value
   - Cumulative returns
   - Risk metrics

### Quick Tips

- **Recent Tickers**: Your last 10 tickers are saved for quick selection
- **Backtest Archive** (`/backtests`): All backtest runs are automatically saved with filters and sorting
- **Breadcrumbs**: Use breadcrumb navigation at the top to quickly navigate back
- **Quick Actions**: Buttons after backtests/optimizations enable one-click navigation
- **Context Preservation**: Strategy and ticker selections carry across pages

### Example: 5-Minute RSI Strategy

1. **Fetch Data** (`/data`): `AAPL`, last 2 years → 504 records fetched
2. **Build Strategy** (`/strategies/builder`):
   - Entry: RSI(14) < 30
   - Exit: RSI(14) > 70
   - Position Sizing: 10% of capital
3. **Backtest** (`/backtest`): $10,000, 2 years → +15.3% return, Sharpe 1.42
4. **Optimize** (`/strategies/optimize`): RSI Period 10-20 → Best: 12 (Sharpe 1.67)
5. **Apply & Re-test**: Updated strategy → +18.7% return, Sharpe 1.67 ✅
6. **Create Portfolio** (`/portfolios`): "AAPL RSI Strategy", $10,000 initial

Total time: **~5 minutes** from idea to deployed portfolio!

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
│   └── TradingStrat.Web/                 # Blazor Server web interface
│       ├── Components/                   # Blazor components
│       ├── Pages/                        # Blazor pages
│       ├── Services/                     # Web services
│       └── appsettings.json              # Configuration file
│
└── tests/
    ├── TradingStrat.Domain.Tests/        # Domain unit tests
    ├── TradingStrat.Application.Tests/   # Application unit tests
    │   └── TestDoubles/                  # In-memory test implementations
    ├── TradingStrat.Infrastructure.Tests/ # Integration tests
    └── TradingStrat.UI.Tests/            # Playwright UI tests
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

3. **Add to Web UI**: Update strategy selection in Blazor pages

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
5. **Update Web UI**: Add Blazor pages/components
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
