# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TradingStrat is a trading strategy backtesting and analysis system built using **Hexagonal Architecture** (Ports & Adapters pattern). The system was refactored from a 3,548-line monolith into a clean, testable architecture with strict dependency boundaries.

**Target Framework:** .NET 10.0

## Essential Commands

### Running the Application
```bash
# Run the Web application
cd src/TradingStrat.Web
dotnet run

# Open browser to https://localhost:5218 (or the URL shown in terminal)
```

### Testing
```bash
# Run all tests (122+ total: 46 Domain + 37 Application + 8 Infrastructure + 31+ UI)
dotnet test

# Run specific test project
dotnet test tests/TradingStrat.Domain.Tests          # 46 tests (strategies, valuation, rebalancing, performance)
dotnet test tests/TradingStrat.Application.Tests     # 37 tests (use cases with test doubles)
dotnet test tests/TradingStrat.Infrastructure.Tests  # 8 tests (repository, adapters)
dotnet test tests/TradingStrat.UI.Tests              # 31+ tests (E2E with Playwright)

# Run a single test class
dotnet test --filter "FullyQualifiedName~RSIStrategyTests"
dotnet test --filter "FullyQualifiedName~PortfoliosPageTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName~RSIStrategyTests.GenerateSignal_WhenRSIOversold_ReturnsBuySignal"
```

### UI Testing with Playwright
```bash
# Install Playwright browsers (first time only)
pwsh tests/TradingStrat.UI.Tests/bin/Debug/net10.0/playwright.ps1 install

# Run UI tests (31+ E2E tests covering Home, Portfolios, Dashboard, Rebalancing, Performance)
dotnet test tests/TradingStrat.UI.Tests

# Run specific test class
dotnet test --filter "FullyQualifiedName~HomePageTests"
dotnet test --filter "FullyQualifiedName~PortfoliosPageTests"
dotnet test --filter "FullyQualifiedName~PortfolioDashboardPageTests"
dotnet test --filter "FullyQualifiedName~RebalancingPageTests"
dotnet test --filter "FullyQualifiedName~PerformanceAnalyticsPageTests"

# Debug tests with visible browser
TEST_HEADLESS=false dotnet test tests/TradingStrat.UI.Tests
```

### Building
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/TradingStrat.Domain
```

## Architecture

### Hexagonal Architecture Layers

The codebase follows strict **Dependency Rule**: Presentation → Application → Domain ← Infrastructure

```
┌─────────────────────────────────────────────────────────┐
│ Presentation (Blazor Server Web Application)           │
│  └─ Depends on: Application (Use Case interfaces)      │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│ Application (Use Cases + Orchestration)                │
│  ├─ Ports/Inbound: Use case interfaces                 │
│  ├─ Ports/Outbound: Infrastructure interfaces          │
│  └─ Depends on: Domain only                            │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│ Domain (Business Logic - ZERO external dependencies)   │
│  ├─ Entities: HistoricalPrice, Trade, Security,        │
│  │             Portfolio, Position, CashTransaction     │
│  ├─ Value Objects: TradeSignal, MLFeatures,            │
│  │                  PortfolioSnapshot, RebalancingPlan │
│  ├─ Strategies: RSI, MACD, MA Crossover, ML FastTree   │
│  └─ Services: IIndicatorCalculator, PerformanceCalc,   │
│                PortfolioValuation, Rebalancing         │
└─────────────────────────────────────────────────────────┘
                     ▲
┌────────────────────┴────────────────────────────────────┐
│ Infrastructure (Adapters - implements Domain/App ports)│
│  ├─ Persistence: EF Core + SQLite                      │
│  ├─ MarketData: Yahoo Finance API adapter              │
│  ├─ MachineLearning: ML.NET FastTree adapter           │
│  └─ Export: CSV/JSON exporters                         │
└─────────────────────────────────────────────────────────┘
```

### Key Architectural Patterns

**Ports & Adapters:**
- **Inbound Ports** (`Application/Ports/Inbound`): Use case interfaces (IDataFetchingUseCase, IBacktestUseCase, ILiveAnalysisUseCase)
- **Outbound Ports** (`Application/Ports/Outbound`): Infrastructure interfaces (IHistoricalDataPort, IMarketDataPort, IMLModelPort)
- **Adapters** (`Infrastructure`): Concrete implementations of outbound ports

**Dependency Injection:**
- All dependencies injected via Microsoft.Extensions.DependencyInjection
- Service registration in `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
- Configuration bound from `appsettings.json` to strongly-typed classes

**Strategy Pattern:**
- All trading strategies inherit from `BaseStrategy` in Domain
- Factory creates strategies with DI-injected dependencies via `StrategyFactory`
- Strategies use `IIndicatorCalculator` domain service (single source of truth for 26 technical indicators)

## Critical Architecture Rules

### Domain Layer Purity
**The Domain layer MUST have ZERO external dependencies.** Only .NET standard library types are allowed.
- ❌ Never add NuGet packages to TradingStrat.Domain
- ❌ Never reference Application, Infrastructure, or Presentation from Domain
- ✅ Domain defines interfaces that Infrastructure implements
- ✅ Keep business logic pure and testable

### Dependency Flow
When adding new features:
1. **Start in Domain** - Define entities, value objects, strategies, domain services
2. **Define Ports** - Create interfaces in Application/Ports (Inbound for use cases, Outbound for infrastructure)
3. **Implement Use Cases** - Orchestrate domain logic in Application/UseCases
4. **Add Adapters** - Implement infrastructure in Infrastructure layer
5. **Wire Up Presentation** - Add Blazor pages/components in Web project

### Test Doubles
The Application.Tests project contains **TestDoubles/** with in-memory implementations:
- `InMemoryHistoricalDataRepository`: No database required for testing
- `FakeMarketDataAdapter`: No API calls, returns deterministic test data
- Use these instead of mocking when testing use cases

## Testing Libraries

**Current Stack:**
- **FakeItEasy** (v8.3.0) - Mocking library for fakes/stubs
- **Shouldly** (v4.2.1) - Assertion library with fluent syntax
- **xUnit** (v2.9.3) - Testing framework

**Common Patterns:**

```csharp
// FakeItEasy - creating fakes
var fake = A.Fake<ITickerResolver>();

// FakeItEasy - setup
A.CallTo(() => fake.GetAllTickersForIsin("XS2399367254"))
    .Returns(new List<string> { "CON3.L", "3COI.DE" });

// FakeItEasy - verification
A.CallTo(() => fake.GetAllTickersForIsin("XS2399367254"))
    .MustHaveHappenedOnceExactly();

// Shouldly - assertions
result.ShouldNotBeNull();
result.Ticker.ShouldBe("TEST");
result.TotalRecords.ShouldBeGreaterThan(0);
savedData.ShouldNotBeEmpty();

// Shouldly - exception assertions
var ex = Should.Throw<ArgumentException>(act);
ex.Message.ShouldContain("expected text");

var ex = await Should.ThrowAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync(command));
ex.Message.ShouldContain("No historical data");

// Shouldly - collection assertions
savedData.Count.ShouldBe(2);
savedData.ShouldAllBe(p => p.Ticker == "TEST");
foreach (var p in savedData)
{
    p.Ticker.ShouldBe("TEST");
    p.ISIN.ShouldBe("TEST_ISIN");
}
```

## UI Testing Architecture

**Test Infrastructure:**
- **WebApplicationFixture:** Hosts Blazor Server app on Kestrel (Playwright requires real HTTP server, not TestServer)
- **PlaywrightFixture:** Manages Chromium/Firefox/WebKit browser lifecycle
- **BaseTest:** Base class providing browser context and page per test (isolation)
- **Page Object Model:** Encapsulates page-specific selectors and interactions
- **Test Configuration:** Environment-based config for CI/CD flexibility

**Test Coverage (Current):**
- HomePageTests: 13 tests covering navigation, content, Blazor connection, console errors
- PortfoliosPageTests: Tests for portfolio list, creation, deletion
- PortfolioDashboardPageTests: Tests for portfolio metrics, positions, refresh
- RebalancingPageTests: Tests for rebalancing calculator
- PerformanceAnalyticsPageTests: Tests for performance charts and metrics
- DataStatusPageTests: 8 tests for data coverage display and refresh
- BacktestArchivePageTests: 10 tests for backtest history, filtering, sorting
- StrategyComparisonPageTests: 12 tests for multi-strategy comparison
- StrategyWorkspacePageTests: 15 tests for tabbed workflow interface
- **Future:** BacktestPageTests, LiveAnalysisPageTests, StrategyBuilderPageTests

**Key Practices:**
- Use `main .grid a[href='/data']` instead of just `a[href='/data']` to avoid matching navigation menu links (Playwright strict mode)
- Always call `await Page.WaitForBlazorAsync()` after navigation to ensure SignalR connection is established
- Filter acceptable console errors (favicon 404, sourcemaps) in error tests
- Each test gets its own browser context for complete isolation
- Screenshots automatically saved on test failure for debugging

## Application Navigation Structure

The application features a reorganized navigation system designed for strategy developers and researchers, with logical grouping of related functionality.

### Navigation Groups (Left Sidebar)

**1. Workspace (Primary Entry Points)**
- `/` - Dashboard (overview, quick stats, recent activity)
- `/workspace` - Strategy Workspace (unified Define → Test → Optimize → Deploy interface)

**2. Strategy Research (Analysis & Development)**
- `/strategies/library` - Strategy Library (browse built-in and custom strategies)
- `/strategies/builder` - Strategy Builder (create/edit custom strategies with visual rule editor)
- `/strategies/compare` - Compare Strategies (side-by-side comparison of multiple strategies)
- `/strategies/optimize` - Strategy Optimization (parameter optimization with grid search/genetic algorithms)
- `/backtest` - Backtest (test strategies on historical data)
- `/backtests` - Backtest Archive (history of all backtest runs with filtering/sorting)
- `/analysis` - Live Analysis (real-time market analysis)

**3. Data Management**
- `/data` - Fetch Data (import historical market data from Yahoo Finance)
- `/data/status` - Data Status (data coverage overview with gap detection)

**4. Portfolio**
- `/portfolios` - Portfolios (list/create/manage portfolios)
- `/portfolio/{id}` - Portfolio Dashboard (positions, metrics, current value)
- `/portfolio/{id}/rebalance` - Rebalancing (calculate rebalancing signals)
- `/portfolio/{id}/performance` - Performance Analytics (historical performance metrics)

**5. System**
- `/settings` - Settings (application configuration)

### Quick Actions

Quick action buttons appear after completing backtests or optimizations to enable seamless workflows:

**Backtest Page Quick Actions:**
- Create Portfolio - Navigate to `/portfolios`
- Compare Strategies - Navigate to `/strategies/compare`
- Optimize Parameters - Navigate to `/strategies/optimize` with current strategy
- View in Archive - Navigate to `/backtests`

**Strategy Optimization Quick Actions:**
- Apply Best Parameters - Update strategy with optimized values
- Run Backtest with Best - Test optimized strategy
- Create Portfolio - Navigate to `/portfolios`
- Compare Variations - Navigate to `/strategies/compare`
- Save as New Strategy - Clone strategy with optimized parameters

### Context Preservation

The application preserves execution context via `AppStateService` (persisted to localStorage):

- **BacktestContext**: Saved after each backtest (ticker, strategy, parameters, config, results)
- **OptimizationContext**: Saved after optimization (custom strategy ID, best parameters, objective value)
- **RecentTickers**: Tracks last 10 tickers used (most recent first)

This enables quick actions to pre-populate forms with relevant context when navigating between pages.

### Breadcrumb Navigation

Hierarchical breadcrumbs are displayed on key pages for context and easy navigation:

- Strategy Builder: Dashboard → Strategy Library → Strategy Builder (or strategy name in edit mode)
- Strategy Optimization: Dashboard → Strategy Library → Strategy Optimization
- Portfolio Pages: Dashboard → Portfolios → {Portfolio Name} → {Action}

## Configuration

All configuration is in `src/TradingStrat.Web/appsettings.json`:

**Key Settings:**
- `Trading.DefaultTicker` / `Trading.DefaultIsin` - Default security to trade
- `Trading.Database.ConnectionString` - SQLite database location
- `Trading.Backtest.InitialCapital` - Starting capital for backtests
- `Trading.Backtest.CommissionPercentage` / `MinimumCommission` - Trading costs
- `Trading.Backtest.DefaultPeriodYears` - Default backtest period (2 years)
- `Trading.MachineLearning.MinTrainingBars` - Minimum data required for ML training (100)
- `Trading.MachineLearning.ModelParameters` - FastTree configuration (100 trees, 0.1 learning rate)
- `Trading.Export.OutputDirectory` - Where to save CSV/JSON exports

Configuration is strongly-typed and injected via `IOptions<TradingConfiguration>`.

### Configuring API Keys (AI Trading Assistant)

The AI Trading Assistant requires an Anthropic API key. For security, API keys should **never** be committed to source control.

**Development (Recommended - User Secrets):**
```bash
# Initialize user secrets (first time only)
dotnet user-secrets init --project src/TradingStrat.Web

# Set the API key
dotnet user-secrets set "Trading:Assistant:ApiKey" "sk-ant-api03-..." --project src/TradingStrat.Web

# Verify it's set
dotnet user-secrets list --project src/TradingStrat.Web
```

**Development (Environment Variable):**
```bash
# Set environment variable (use double underscores for nested config)
export Trading__Assistant__ApiKey="sk-ant-api03-..."

# Run the application
dotnet run --project src/TradingStrat.Web
```

**Production (Docker/Container):**
```bash
docker run -e Trading__Assistant__ApiKey="sk-ant-..." tradingstrat-web
```

**Production (Systemd Service):**
```ini
[Service]
Environment="Trading__Assistant__ApiKey=sk-ant-..."
ExecStart=/usr/bin/dotnet TradingStrat.Web.dll
```

**Configuration Priority (Highest Last):**
1. appsettings.json (base configuration)
2. appsettings.{Environment}.json (environment-specific)
3. User Secrets (development only)
4. Environment Variables (all environments)
5. Command-line arguments

## Key Domain Concepts

### IIndicatorCalculator - Single Source of Truth
The domain service `IIndicatorCalculator` provides **26 technical indicators**. This eliminates the previous duplication where indicators were calculated in multiple places.

**Categories:**
- Price-based (5): DailyReturn, LogReturn, HighLowRange, OpenCloseRange, PricePosition
- Moving Averages (6): SMA 5/10/20, EMA 12/26, PriceToSMA20Ratio
- Momentum (4): RSI 14, Momentum 5, ROC 10, StochasticRSI
- MACD (3): MACD line, Signal line, Histogram
- Volatility (4): StdDev 10/20, ATR 14, BollingerPosition
- Volume (4): VolumeChange, VolumeMA10, VolumeRatio, PriceVolumeCorrelation

**Usage in Strategies:**
```csharp
public class MyStrategy : BaseStrategy
{
    public MyStrategy(IIndicatorCalculator indicatorCalculator)
        : base(indicatorCalculator) { }

    public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
    {
        var rsi = _indicatorCalculator.CalculateRSI(ClosePrices, 14);
        var sma = _indicatorCalculator.CalculateSMA(ClosePrices, 20);
        // Use indicators to generate trading signals
    }
}
```

### Trading Strategies
All strategies inherit from `BaseStrategy` and implement `GenerateSignal()`:
- **Moving Average Crossover** (ma): Fast/slow SMA crossover
- **RSI Strategy** (rsi): Relative Strength Index with overbought/oversold thresholds
- **MACD Strategy** (macd): MACD line crossing signal line
- **ML FastTree** (ml): Machine learning predictions using 26 technical indicators

**Strategy Registration:**
Strategies are created via `StrategyFactory.CreateStrategy(string strategyType, Dictionary<string, object>? parameters)`. The factory handles dependency injection and parameter binding.

### Custom Strategy Builder

The custom strategy builder allows users to create trading strategies through a visual rule-based UI without writing code. Strategies are persisted to the database and can be optimized using grid search or genetic algorithms.

**Architecture:**
- **Domain Layer**: `CustomStrategy` entity, `StrategyDefinition` value object, `CustomRuleBasedStrategy` interpreter
- **Application Layer**: `ICustomStrategyManagementUseCase` for CRUD operations, `IOptimizeStrategyParametersUseCase` for parameter optimization
- **Infrastructure Layer**: `CustomStrategyRepository` for persistence to SQLite
- **Web Layer**: StrategyBuilder, StrategyLibrary, and StrategyOptimization Blazor pages

**Key Features:**
- Create entry/exit rules using 26 technical indicators
- Compare indicators to constants, price, or other indicators (e.g., SMA20 > SMA50)
- Combine rules with AND/OR logic
- Multiple position sizing modes (fixed %, fixed quantity, risk-based)
- Save/load/edit/delete/clone custom strategies
- Optimize strategy parameters using Grid Search or Genetic Algorithms
- Integration with existing backtest workflow

**Value Objects:**
```csharp
public sealed record StrategyDefinition(
    List<StrategyRule> EntryRules,
    List<StrategyRule> ExitRules,
    PositionSizingMode SizingMode,
    Dictionary<string, decimal> SizingParameters
);

public sealed record StrategyRule(
    string IndicatorName,                          // e.g., "RSI", "SMA"
    Dictionary<string, object> IndicatorParameters, // e.g., {"Period": 14}
    ComparisonOperator Operator,                   // >, <, CrossesAbove, etc.
    RuleValueType ValueType,                       // Constant, Indicator, Price
    decimal? ConstantValue,                        // For comparing to number
    string? SecondIndicatorName,                   // For comparing two indicators
    Dictionary<string, object>? SecondIndicatorParameters,
    LogicalOperator LogicalOperator                // AND/OR with next rule
);
```

**Comparison Operators:**
- `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `Equal`
- `CrossesAbove`: Detects indicator[i] > value && indicator[i-1] <= value
- `CrossesBelow`: Detects indicator[i] < value && indicator[i-1] >= value

**Web Pages:**
- `/strategies/library` - Browse built-in strategies and custom strategies (tabs)
- `/strategies/builder` - Create/edit custom strategies with visual rule editor
- `/strategies/builder/{id}` - Edit existing strategy
- `/strategies/optimize` - Optimize custom strategy parameters

**Parameter Optimization:**

The optimization system supports two algorithms:

1. **Grid Search (Exhaustive)**:
   - User defines min, max, step for each parameter
   - Explores all parameter combinations
   - Returns best parameter set based on objective (Sharpe ratio, total return, etc.)
   - Example: 3 parameters × 10 steps each = 1,000 backtests

2. **Genetic Algorithm**:
   - Population-based optimization
   - Tournament selection, crossover, mutation
   - Configurable population size (default 50), generations (default 100), mutation rate (default 0.1)
   - Elitism preserves best solutions
   - Faster than grid search for large parameter spaces

**Optimization Objectives:**
- `MaximizeTotalReturn`: Maximize total return percentage
- `MaximizeSharpeRatio`: Maximize risk-adjusted return (default)
- `MinimizeDrawdown`: Minimize maximum drawdown
- `MaximizeWinRate`: Maximize percentage of profitable trades
- `MaximizeProfitFactor`: Maximize ratio of gross profit to gross loss

**Usage Example:**
```csharp
// Create a custom strategy via use case
CreateCustomStrategyCommand command = new(
    Name: "My RSI Strategy",
    Description: "Buy when RSI < 30, sell when RSI > 70",
    Category: "Momentum",
    Author: "User",
    Definition: new StrategyDefinition(
        EntryRules: new List<StrategyRule>
        {
            new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.LessThan,
                RuleValueType.Constant, ConstantValue: 30, null, null, LogicalOperator.None)
        },
        ExitRules: new List<StrategyRule>
        {
            new("RSI", new() { ["Period"] = 14 }, ComparisonOperator.GreaterThan,
                RuleValueType.Constant, ConstantValue: 70, null, null, LogicalOperator.None)
        },
        SizingMode: PositionSizingMode.FixedPercentage,
        SizingParameters: new() { ["Percentage"] = 0.1m }
    )
);

CustomStrategyResult result = await customStrategyUseCase.CreateStrategyAsync(command);

// Optimize strategy parameters
OptimizeParametersCommand optimizeCommand = new(
    CustomStrategyId: result.Id,
    Type: OptimizationType.GridSearch,
    ParameterRanges: new()
    {
        ["Period"] = new ParameterRange(Min: 10, Max: 20, Step: 1)
    },
    Objective: OptimizationObjective.MaximizeSharpeRatio,
    BacktestSettings: new BacktestConfig("AAPL", DateTime.Today.AddYears(-2), DateTime.Today, 10000m)
);

OptimizationResult optimizationResult = await optimizeUseCase.ExecuteAsync(optimizeCommand, progress);
// Best parameters: {"Period": 14}, Best Sharpe: 1.85
```

**Performance:**
- Custom strategies pre-calculate all indicators during `Initialize()` for efficiency
- Indicator results cached in `Dictionary<cacheKey, decimal[]>` to avoid recalculation
- Overhead vs. compiled strategies: <5%
- Grid search optimization: ~100 backtests/second on modern CPU
- Supports parallel backtest execution for faster optimization

### Entity Framework Core
**Database:** SQLite (connection string in appsettings.json)
**Context:** `TradingContext` in Infrastructure/Persistence/EfCore
**Migrations:** Managed via EF Core migrations

**Key Tables:**
- `HistoricalPrices`: Ticker, DateTime (unique constraint), OHLC data, Volume
- `Securities`: Ticker (unique), ISIN (unique), metadata
- `Portfolios`: Id (PK), Name (indexed), Cash (18,2 precision), CreatedAt, LastUpdated
- `Positions`: Id (PK), PortfolioId (FK, cascade delete), Ticker, Quantity, EntryPrice (18,6), EntryDate, Notes, unique index on (PortfolioId, Ticker)
- `CashTransactions`: Id (PK), PortfolioId (FK, cascade delete), Type, Amount (18,2), TransactionDate, Notes, index on (PortfolioId, TransactionDate)
- `CustomStrategies`: Id (PK), Name, Description, Author, Category, DefinitionJson (TEXT), CreatedAt, LastUpdatedAt, TimesUsed, LastBacktestReturn, LastBacktestDate, indexes on Category and (Author, CreatedAt)

**Important:** Never instantiate `TradingContext` directly. Always use repository ports (`IHistoricalDataPort`, `IPortfolioPort`) injected via DI.

### Walk-Forward Backtesting
The ML strategy uses walk-forward optimization:
1. Train model on historical data (minimum 100 bars)
2. Test on forward period
3. Roll window forward and retrain

This prevents look-ahead bias common in traditional backtesting.

## Portfolio Management System

The portfolio management system provides multi-asset position tracking, rebalancing, and performance analytics following hexagonal architecture.

### Domain Layer

**Entities:**
- `Portfolio` - Aggregate root with Name, Description, Cash, Positions collection
- `Position` - Ticker, Quantity, EntryPrice, EntryDate, Notes (immutable Ticker, PortfolioId, EntryDate)
- `PortfolioCashTransaction` - Type (Deposit/Withdrawal), Amount, TransactionDate, Notes

**Value Objects (Immutable Records):**
- `PortfolioSnapshot` - Point-in-time portfolio view with TotalValue, UnrealizedGainLoss, Positions
- `PositionSnapshot` - Individual position snapshot with MarketValue, AllocationPercentage
- `AllocationWeights` - Target allocation configuration (Dictionary<Ticker, Percentage>)
- `RebalancingPlan` - List<RebalancingSignal>, RequiredCash, IsExecutable
- `PortfolioMetrics` - TotalReturn, Volatility, SharpeRatio, DiversificationRatio
- `PortfolioPerformanceHistory` - Time series of daily portfolio values and returns

**Domain Services (Zero External Dependencies):**
- `PortfolioValuationService.CalculateSnapshot(Portfolio, Dictionary<Ticker, Price>)` - Calculates market values, allocations, unrealized gains/losses
- `PortfolioRebalancingService.CalculateRebalancing(Snapshot, AllocationWeights, Prices, Commission)` - Generates buy/sell signals to reach target allocation
- `PortfolioPerformanceService.CalculateMetrics(Snapshot, HistoricalPoints)` - Calculates volatility, Sharpe ratio, diversification metrics

### Application Layer

**Outbound Ports (Infrastructure Interfaces):**
- `IPortfolioPort` - Portfolio CRUD, cash management, position management (14 methods)
- `IPortfolioExportPort` - CSV import/export

**Inbound Ports (Use Case Interfaces):**
- `ICreatePortfolioUseCase` - Create new portfolio with Name, Description, InitialCash
- `IManagePositionsUseCase` - Add, Update, Delete positions
- `IManageCashUseCase` - Execute deposits/withdrawals, get transaction history
- `IGetPortfolioSnapshotUseCase` - Fetch live prices and calculate current snapshot
- `ICalculateRebalancingUseCase` - Calculate rebalancing plan with target weights
- `IGetPortfolioPerformanceUseCase` - Get performance analytics with metrics

**Key Use Case Pattern (GetPortfolioSnapshotUseCase):**
```csharp
public async Task<PortfolioSnapshot> ExecuteAsync(int portfolioId, IProgress<string>? progress = null)
{
    // 1. Load portfolio from repository
    Portfolio portfolio = await _portfolioPort.GetPortfolioByIdAsync(portfolioId);

    // 2. Extract unique tickers and fetch current prices via IMarketDataPort
    foreach (string ticker in tickers)
    {
        HistoricalData data = await _marketDataPort.FetchHistoricalDataAsync(...);
        currentPrices[ticker] = data.Latest.Close.Value;
    }

    // 3. Call domain service to calculate snapshot
    return _valuationService.CalculateSnapshot(portfolio, currentPrices);
}
```

### Infrastructure Layer

**Database Tables (EF Core):**
- `Portfolios` - PK on Id, Index on Name, Precision on Cash (18,2)
- `Positions` - PK on Id, FK to Portfolio (cascade delete), Unique index on (PortfolioId, Ticker)
- `CashTransactions` - PK on Id, FK to Portfolio (cascade delete), Index on (PortfolioId, Date)

**Repository Implementation:**
```csharp
// PortfolioRepository uses EF Core Include() for loading positions eagerly
public async Task<Portfolio?> GetPortfolioByIdAsync(int portfolioId)
{
    return await _context.Portfolios
        .Include(p => p.Positions)
        .FirstOrDefaultAsync(p => p.Id == portfolioId);
}

// UpdatePosition preserves immutable fields
public async Task UpdatePositionAsync(Position position)
{
    Position existingPosition = await _context.Positions.FindAsync(position.Id);
    existingPosition.Quantity = position.Quantity;
    existingPosition.EntryPrice = position.EntryPrice;
    existingPosition.Notes = position.Notes;
    // Preserves: Ticker, PortfolioId, EntryDate (immutable)
}
```

**CSV Adapter:**
- Format: `Ticker,Quantity,EntryPrice,EntryDate,Notes`
- Custom CSV parser handles quoted fields and escaped quotes
- Located in `Infrastructure/Export/PortfolioCsvAdapter.cs`

### Web UI Layer

**Pages:**
- `/portfolios` - Portfolio grid view with create/delete dialogs (Portfolios.razor)
- `/portfolio/{id}` - Dashboard with metrics cards, position table, refresh prices (PortfolioDashboard.razor)
- `/portfolio/{id}/rebalance` - Rebalancing calculator with dynamic allocations (Rebalancing.razor)
- `/portfolio/{id}/performance` - Performance analytics with date range selector (PerformanceAnalytics.razor)

**Form Models:**
- `CreatePortfolioFormModel` - Name [Required, 3-100 chars], Description [Optional], InitialCash [0-10M]
- `AddPositionFormModel` - Ticker [Required], Quantity [1-1M], EntryPrice [0.01-100K], EntryDate, Notes
- `RebalancingFormModel` - List<TargetAllocationModel>, CashPercentage, CommissionPercentage, MinimumCommission

**State Management:**
```csharp
// PortfolioStateService - Selected portfolio tracking with localStorage
public async Task SetSelectedPortfolioAsync(int portfolioId)
{
    _selectedPortfolioId = portfolioId;
    await _localStorage.SetItemAsync("tradingstrat_selected_portfolio", portfolioId);
    OnPortfolioChanged?.Invoke(this, EventArgs.Empty);
}
```

**Key UI Patterns:**
- All pages use `@rendermode InteractiveServer` for Blazor SignalR
- Progress reporting via `ProgressService.Subscribe()` / `Unsubscribe()`
- Toast notifications via `NotificationService.ShowSuccess()` / `ShowError()`
- Form validation with `DataAnnotationsValidator` and `ValidationMessage`
- Responsive design with Tailwind grid and utility classes
- Color coding: `metric-positive` (green) / `metric-negative` (red)

### Testing Patterns

**Domain Tests (46 tests):**
```csharp
[Fact]
public void CalculateSnapshot_WithSinglePosition_CalculatesCorrectly()
{
    // Arrange
    Portfolio portfolio = new() { Cash = 1000m, Positions = [...] };
    Dictionary<string, decimal> prices = new() { ["AAPL"] = 160m };

    // Act
    PortfolioSnapshot result = _service.CalculateSnapshot(portfolio, prices);

    // Assert
    result.Positions[0].MarketValue.ShouldBe(1600m); // 10 * 160
    result.TotalValue.ShouldBe(2600m); // 1000 cash + 1600 position
}
```

**Application Tests (37 tests) with Test Doubles:**
```csharp
// InMemoryPortfolioRepository implements IPortfolioPort
public class InMemoryPortfolioRepository : IPortfolioPort
{
    private readonly Dictionary<int, Portfolio> _portfolios = new();

    public async Task<Portfolio> CreatePortfolioAsync(string name, string? description, decimal initialCash)
    {
        Portfolio portfolio = new() { Id = ++_nextId, Name = name, Cash = initialCash };
        _portfolios[portfolio.Id] = portfolio;
        return portfolio;
    }
    // ... 13 more methods
}
```

**E2E Tests with Playwright (4 test classes, ~60 tests):**
- `PortfoliosPageTests` - Create portfolio, list portfolios, delete portfolio, navigation
- `PortfolioDashboardPageTests` - Display metrics, position table, refresh prices
- `RebalancingPageTests` - Configure allocations, calculate plan, display signals
- `PerformanceAnalyticsPageTests` - Date range selection, metrics display, historical data

**Test Data Seeding (WebApplicationFixture):**
```csharp
// Automatically seeds 3 portfolios for E2E tests:
// 1. Tech Growth Portfolio (MSFT, AAPL) - Cash: $5,000
// 2. Diversified Mix (GOOGL) - Cash: $10,000
// 3. Empty Portfolio - Cash: $25,000
```

**Page Object Model Pattern:**
```csharp
public class PortfoliosPage : BasePage
{
    protected override string PagePath => "/portfolios";

    private ILocator CreatePortfolioButton => Page.Locator("button:has-text('Create Portfolio')");

    public async Task CreatePortfolioAsync(string name, string? description, decimal initialCash)
    {
        await ClickCreatePortfolioButtonAsync();
        await NameInput.FillAsync(name);
        await InitialCashInput.FillAsync(initialCash.ToString());
        await CreateSubmitButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }
}
```

### Common Portfolio Operations

**Creating a Portfolio:**
```csharp
// Use case call
CreatePortfolioCommand command = new("My Portfolio", "Description", 10000m);
CreatePortfolioResult result = await _createPortfolioUseCase.ExecuteAsync(command);
```

**Getting Current Portfolio Value:**
```csharp
// Fetches live prices and calculates snapshot
PortfolioSnapshot snapshot = await _getSnapshotUseCase.ExecuteAsync(portfolioId);
Console.WriteLine($"Total Value: {snapshot.TotalValue:C2}");
Console.WriteLine($"Gain/Loss: {snapshot.UnrealizedGainLoss:C2}");
```

**Calculating Rebalancing:**
```csharp
// Define target allocations
CalculateRebalancingCommand command = new()
{
    PortfolioId = 1,
    TargetAllocations = new() { ["MSFT"] = 60m, ["AAPL"] = 40m },
    CashPercentage = 0m,
    CommissionPercentage = 0.1m,
    MinimumCommission = 1.0m
};

RebalancingPlan plan = await _calculateRebalancingUseCase.ExecuteAsync(command);
foreach (RebalancingSignal signal in plan.Signals)
{
    Console.WriteLine($"{signal.Ticker}: {signal.Action} {signal.QuantityDelta} shares");
}
```

## Adding New Features

### Adding a New Strategy

1. **Create strategy class in Domain/Strategies:**
```csharp
public class MyNewStrategy : BaseStrategy
{
    private readonly int _period;

    public MyNewStrategy(IIndicatorCalculator indicatorCalculator, int period = 14)
        : base(indicatorCalculator)
    {
        _period = period;
        Name = $"My Strategy ({_period})";
    }

    public override void Initialize(IReadOnlyList<HistoricalPrice> prices)
    {
        base.Initialize(prices);
        // Calculate indicators once during initialization
    }

    public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
    {
        if (currentIndex < _period)
            return TradeSignal.Hold("Insufficient data");

        // Your strategy logic here
        var indicator = _indicatorCalculator.CalculateSMA(ClosePrices, _period);

        if (/* buy condition */)
            return TradeSignal.Buy(quantity, "Buy reason");

        if (/* sell condition */ && currentPosition > 0)
            return TradeSignal.Sell(currentPosition, "Sell reason");

        return TradeSignal.Hold("Neutral");
    }
}
```

2. **Register in StrategyFactory (Application/Factories/StrategyFactory.cs):**
```csharp
return strategyType.ToLowerInvariant() switch
{
    "mynew" => CreateMyNewStrategy(parameters),
    // ... existing strategies
};

private IStrategy CreateMyNewStrategy(Dictionary<string, object>? parameters)
{
    int period = GetParameter<int>(parameters, "Period", 14);
    return new MyNewStrategy(_indicatorCalculator, period);
}
```

3. **Add tests in Domain.Tests/Strategies/MyNewStrategyTests.cs**

### Adding a New Technical Indicator

1. **Add to IIndicatorCalculator interface (Domain/Services/Indicators):**
```csharp
decimal[] CalculateMyIndicator(decimal[] prices, int period);
```

2. **Implement in IndicatorCalculator class:**
```csharp
public decimal[] CalculateMyIndicator(decimal[] prices, int period)
{
    var result = new decimal[prices.Length];
    // Implementation
    return result;
}
```

3. **Use in FeatureEngineering (Application/Services) if needed for ML**

4. **Add tests**

### Adding a New Use Case

1. **Define interface in Application/Ports/Inbound:**
```csharp
public interface IMyNewUseCase
{
    Task<MyResult> ExecuteAsync(MyCommand command, IProgress<string>? progress = null);
}
```

2. **Implement in Application/UseCases:**
```csharp
public class MyNewUseCase : IMyNewUseCase
{
    private readonly IHistoricalDataPort _historicalDataPort;
    // Inject required ports

    public async Task<MyResult> ExecuteAsync(MyCommand command, IProgress<string>? progress = null)
    {
        // Orchestrate domain logic
    }
}
```

3. **Register in Infrastructure DI configuration**

4. **Wire up in Web UI (Blazor pages/components)**

## Data Sources

**Yahoo Finance API:**
- Accessed via `YahooQuotesApi` NuGet package (v7.0.5)
- Adapter: `Infrastructure/MarketData/YahooMarketDataAdapter.cs`
- Timeout: 30 seconds (configurable)
- Max retries: 3 (configurable)

**Data Fetching:**
- Use `IDataFetchingUseCase.ExecuteAsync()` to fetch and save data
- Automatically deduplicates based on (Ticker, DateTime) unique constraint
- Incremental updates: fetches only missing dates

## Machine Learning

**Algorithm:** FastTree Gradient Boosting (ML.NET v5.0)
**Features:** 26 technical indicators from IIndicatorCalculator
**Target:** Next-day return prediction
**Training:** Minimum 100 bars required

**Configuration:**
- NumberOfTrees: 100
- LearningRate: 0.1
- NumberOfLeaves: 31
- MinimumExampleCountPerLeaf: 20

**Note:** Uses FastTree instead of LightGBM for macOS compatibility.

## Common Gotchas

1. **Strategy Constructor Validation:** All strategies validate parameters in constructor and throw `ArgumentException` for invalid inputs (e.g., fast period >= slow period, oversold >= overbought)

2. **Test Data Patterns:** When testing crossover strategies, ensure price data creates actual crossovers. Use `HistoricalPriceBuilder` with patterns like decline-then-recovery for buy signals, rise-then-decline for sell signals.

3. **Date Handling:** The backtest defaults to last 2 years if no date range specified. FakeMarketDataAdapter uses relative dates (DateTime.Today.AddDays(-N)) to avoid hard-coded dates in tests.

4. **Insufficient Data:** Strategies return `TradeSignal.Hold("Insufficient data")` when currentIndex < minimum required period. Always check this in tests.

5. **Duplicate Data Prevention:** The repository automatically filters duplicates during save. Tests verify this behavior with `SaveHistoricalDataAsync_WithDuplicates_ShouldNotInsertDuplicates`.

## Project Evolution

This codebase was refactored from a monolithic architecture with several anti-patterns:
- ❌ Hardcoded database connections (`new TradingContext()`)
- ❌ Reflection hacks to access indicators
- ❌ Duplicated indicator calculations
- ❌ Hardcoded configuration values
- ❌ ML training mixed with strategy logic

All of these issues have been resolved through hexagonal architecture and dependency injection.
