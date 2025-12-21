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
# Run all tests (39 total: 17 Domain + 14 Application + 8 Infrastructure)
dotnet test

# Run specific test project
dotnet test tests/TradingStrat.Domain.Tests
dotnet test tests/TradingStrat.Application.Tests
dotnet test tests/TradingStrat.Infrastructure.Tests

# Run a single test class
dotnet test --filter "FullyQualifiedName~RSIStrategyTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName~RSIStrategyTests.GenerateSignal_WhenRSIOversold_ReturnsBuySignal"
```

### UI Testing with Playwright
```bash
# Install Playwright browsers (first time only)
pwsh tests/TradingStrat.UI.Tests/bin/Debug/net10.0/playwright.ps1 install

# Run UI tests
dotnet test tests/TradingStrat.UI.Tests

# Run specific test class
dotnet test --filter "FullyQualifiedName~HomePageTests"

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
│  ├─ Entities: HistoricalPrice, Trade, Security         │
│  ├─ Value Objects: TradeSignal, MLFeatures             │
│  ├─ Strategies: RSI, MACD, MA Crossover, ML FastTree   │
│  └─ Services: IIndicatorCalculator, PerformanceCalc    │
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
- **Future:** BacktestPageTests, DataManagementPageTests, LiveAnalysisPageTests, ComparisonPageTests

**Key Practices:**
- Use `main .grid a[href='/data']` instead of just `a[href='/data']` to avoid matching navigation menu links (Playwright strict mode)
- Always call `await Page.WaitForBlazorAsync()` after navigation to ensure SignalR connection is established
- Filter acceptable console errors (favicon 404, sourcemaps) in error tests
- Each test gets its own browser context for complete isolation
- Screenshots automatically saved on test failure for debugging

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

### Entity Framework Core
**Database:** SQLite (connection string in appsettings.json)
**Context:** `TradingContext` in Infrastructure/Persistence/EfCore
**Migrations:** Managed via EF Core migrations

**Key Tables:**
- `HistoricalPrices`: Ticker, DateTime (unique constraint), OHLC data, Volume
- `Securities`: Ticker (unique), ISIN (unique), metadata

**Important:** Never instantiate `TradingContext` directly. Always use `IHistoricalDataPort` injected via DI.

### Walk-Forward Backtesting
The ML strategy uses walk-forward optimization:
1. Train model on historical data (minimum 100 bars)
2. Test on forward period
3. Roll window forward and retrain

This prevents look-ahead bias common in traditional backtesting.

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
