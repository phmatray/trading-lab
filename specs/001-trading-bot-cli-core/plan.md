# TradingBot CLI Implementation Plan

## Document Information
- **Project Name**: TradingBot CLI
- **Target Framework**: .NET 9 (C# 12)
- **Deployment Model**: Single-file, cross-platform binary
- **Date**: 2025-11-02
- **Status**: Ready for Implementation

---

## 1. Executive Summary

### 1.1 Technology Stack Overview

| Category | Technology | Version | Purpose |
|----------|-----------|---------|---------|
| **Runtime** | .NET | 9.0 | Application runtime and SDK |
| **Language** | C# | 12 | Primary programming language |
| **CLI Framework** | Spectre.Cli | 0.49+ | Command routing, validation, DI |
| **Console UI** | Spectre.Console | 0.49+ | Live dashboard, tables, charts |
| **ORM** | Entity Framework Core | 9.0 | Database access and migrations |
| **Database** | SQLite | 3.x | Default embedded database |
| **Market Data** | YahooFinanceApi | 2.x | Yahoo Finance API client |
| **Resilience** | Polly | 8.x | Retry logic, rate limiting |
| **Math/Stats** | MathNet.Numerics | 5.x | Indicators, statistics, analytics |
| **Configuration** | NetEscapades.Configuration.Yaml | 3.x | YAML configuration support |
| **Background Jobs** | TickerQ | 1.x | Cron-like background tasks |
| **Testing** | xUnit v3 | 3.x | Unit testing framework |
| **Assertions** | Shouldly | 4.x | Fluent assertions |
| **Mocking** | FakeItEasy | 8.x | Mocking framework |
| **Coverage** | Coverlet | 6.x | Code coverage collection |
| **Analysis** | Roslyn Analyzers | Latest | Code quality enforcement |

### 1.2 Project Goals

1. **Single-file deployment**: Self-contained executables for Windows, macOS, and Linux
2. **Professional CLI**: Type-safe commands with validation and help text
3. **Real-time dashboard**: Live updating interface with 1-5 second refresh
4. **Resilient data**: Polly-backed retries, rate limiting, and caching
5. **Secure by default**: AES-256 encryption for API keys and secrets
6. **Testable**: 80%+ code coverage with comprehensive test suite
7. **Production ready**: CI/CD with Roslyn analyzers and automated builds

---

## 2. Project Structure

### 2.1 Solution Organization

```
TradingBot/
├── src/
│   ├── TradingBot.Cli/                    # Main CLI application
│   │   ├── Commands/                      # Spectre.Cli commands
│   │   │   ├── StartCommand.cs
│   │   │   ├── StopCommand.cs
│   │   │   ├── Strategy/
│   │   │   │   ├── StrategyListCommand.cs
│   │   │   │   ├── StrategyEnableCommand.cs
│   │   │   │   ├── StrategyDisableCommand.cs
│   │   │   │   ├── StrategyConfigureCommand.cs
│   │   │   │   └── StrategyAddCommand.cs
│   │   │   ├── Risk/
│   │   │   │   ├── RiskShowCommand.cs
│   │   │   │   ├── RiskSetLeverageCommand.cs
│   │   │   │   ├── RiskSetStopLossCommand.cs
│   │   │   │   └── RiskSetTakeProfitCommand.cs
│   │   │   ├── Portfolio/
│   │   │   │   ├── PortfolioShowCommand.cs
│   │   │   │   ├── PortfolioHistoryCommand.cs
│   │   │   │   └── PortfolioCloseCommand.cs
│   │   │   ├── Performance/
│   │   │   │   ├── PerformanceShowCommand.cs
│   │   │   │   ├── PerformanceChartsCommand.cs
│   │   │   │   ├── PerformanceCompareCommand.cs
│   │   │   │   └── PerformanceExportCommand.cs
│   │   │   ├── Backtest/
│   │   │   │   ├── BacktestRunCommand.cs
│   │   │   │   ├── BacktestReportCommand.cs
│   │   │   │   └── BacktestOptimizeCommand.cs
│   │   │   ├── Config/
│   │   │   │   ├── ConfigShowCommand.cs
│   │   │   │   ├── ConfigSetCommand.cs
│   │   │   │   └── ConfigSetApiKeyCommand.cs
│   │   │   └── DashboardCommand.cs
│   │   ├── Dashboard/                     # Live dashboard components
│   │   │   ├── DashboardRenderer.cs
│   │   │   ├── Widgets/
│   │   │   │   ├── PositionsWidget.cs
│   │   │   │   ├── PnLWidget.cs
│   │   │   │   ├── MarketTrendsWidget.cs
│   │   │   │   ├── RecentTradesWidget.cs
│   │   │   │   └── ChartWidget.cs
│   │   │   └── LiveDisplayContext.cs
│   │   ├── Infrastructure/                # DI setup, startup
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   └── TypeRegistrar.cs           # Spectre.Cli DI adapter
│   │   ├── Program.cs                     # Entry point
│   │   └── TradingBot.Cli.csproj
│   │
│   ├── TradingBot.Core/                   # Core domain logic
│   │   ├── Models/                        # Domain models
│   │   │   ├── MarketData/
│   │   │   │   ├── Quote.cs
│   │   │   │   ├── Candle.cs
│   │   │   │   └── SymbolInfo.cs
│   │   │   ├── Trading/
│   │   │   │   ├── Signal.cs
│   │   │   │   ├── Order.cs
│   │   │   │   ├── Position.cs
│   │   │   │   └── Trade.cs
│   │   │   ├── Strategy/
│   │   │   │   ├── StrategyConfig.cs
│   │   │   │   └── StrategyInfo.cs
│   │   │   ├── Portfolio/
│   │   │   │   ├── Account.cs
│   │   │   │   ├── PerformanceMetrics.cs
│   │   │   │   └── EquityPoint.cs
│   │   │   ├── Risk/
│   │   │   │   ├── RiskParameters.cs
│   │   │   │   └── RiskStatus.cs
│   │   │   └── Backtest/
│   │   │       ├── BacktestConfig.cs
│   │   │       ├── BacktestResult.cs
│   │   │       └── MonteCarloResult.cs
│   │   ├── Interfaces/                    # Core abstractions
│   │   │   ├── IMarketDataService.cs
│   │   │   ├── IStrategyEngine.cs
│   │   │   ├── IStrategy.cs
│   │   │   ├── IOrderExecutionService.cs
│   │   │   ├── IPortfolioManager.cs
│   │   │   ├── IRiskManager.cs
│   │   │   ├── IBacktestingEngine.cs
│   │   │   └── IEncryptionService.cs
│   │   ├── Enums/
│   │   │   ├── SignalType.cs
│   │   │   ├── OrderType.cs
│   │   │   ├── OrderSide.cs
│   │   │   └── OrderStatus.cs
│   │   └── TradingBot.Core.csproj
│   │
│   ├── TradingBot.Infrastructure/         # External integrations
│   │   ├── MarketData/
│   │   │   ├── YahooFinanceService.cs     # Market data implementation
│   │   │   ├── DataNormalizer.cs
│   │   │   ├── HistoricalDataCache.cs
│   │   │   └── RealTimeQuoteStream.cs
│   │   ├── Persistence/
│   │   │   ├── TradingBotDbContext.cs     # EF Core DbContext
│   │   │   ├── Configurations/            # Entity configurations
│   │   │   │   ├── OrderConfiguration.cs
│   │   │   │   ├── PositionConfiguration.cs
│   │   │   │   ├── TradeConfiguration.cs
│   │   │   │   └── CandleConfiguration.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── OrderRepository.cs
│   │   │   │   ├── PositionRepository.cs
│   │   │   │   ├── TradeRepository.cs
│   │   │   │   └── CandleRepository.cs
│   │   │   └── Migrations/                # EF Core migrations
│   │   ├── Configuration/
│   │   │   ├── ConfigurationService.cs
│   │   │   └── EncryptionService.cs       # AES-256 encryption
│   │   ├── BackgroundJobs/
│   │   │   ├── DataRefreshJob.cs          # TickerQ jobs
│   │   │   ├── EndOfDayJob.cs
│   │   │   └── RiskMonitoringJob.cs
│   │   └── TradingBot.Infrastructure.csproj
│   │
│   ├── TradingBot.Strategies/             # Trading strategies
│   │   ├── Base/
│   │   │   ├── BaseStrategy.cs            # Abstract base class
│   │   │   └── IndicatorLibrary.cs        # MathNet.Numerics wrappers
│   │   ├── Momentum/
│   │   │   ├── MomentumStrategy.cs
│   │   │   └── MomentumConfig.cs
│   │   ├── MeanReversion/
│   │   │   ├── MeanReversionStrategy.cs
│   │   │   └── MeanReversionConfig.cs
│   │   ├── Custom/
│   │   │   ├── CustomScriptStrategy.cs
│   │   │   └── StrategySandbox.cs
│   │   └── TradingBot.Strategies.csproj
│   │
│   ├── TradingBot.Engine/                 # Trading engine
│   │   ├── StrategyEngine.cs
│   │   ├── OrderExecutionService.cs
│   │   ├── PortfolioManager.cs
│   │   ├── RiskManager.cs
│   │   ├── BacktestingEngine.cs
│   │   ├── PositionSizeCalculator.cs
│   │   ├── StopLossManager.cs
│   │   └── TradingBot.Engine.csproj
│   │
│   └── TradingBot.Analytics/              # Performance analytics
│       ├── PerformanceCalculator.cs
│       ├── MetricsCalculator.cs           # Sharpe, Sortino, etc.
│       ├── EquityCurveGenerator.cs
│       ├── DrawdownAnalyzer.cs
│       ├── MonteCarloSimulator.cs
│       ├── WalkForwardOptimizer.cs
│       └── TradingBot.Analytics.csproj
│
├── tests/
│   ├── TradingBot.Cli.Tests/
│   │   ├── Commands/
│   │   │   ├── StartCommandTests.cs
│   │   │   └── Strategy/
│   │   │       └── StrategyListCommandTests.cs
│   │   └── TradingBot.Cli.Tests.csproj
│   │
│   ├── TradingBot.Core.Tests/
│   │   ├── Models/
│   │   │   └── SignalTests.cs
│   │   └── TradingBot.Core.Tests.csproj
│   │
│   ├── TradingBot.Infrastructure.Tests/
│   │   ├── MarketData/
│   │   │   ├── YahooFinanceServiceTests.cs
│   │   │   └── DataNormalizerTests.cs
│   │   ├── Persistence/
│   │   │   └── TradingBotDbContextTests.cs
│   │   └── TradingBot.Infrastructure.Tests.csproj
│   │
│   ├── TradingBot.Strategies.Tests/
│   │   ├── Momentum/
│   │   │   └── MomentumStrategyTests.cs
│   │   ├── MeanReversion/
│   │   │   └── MeanReversionStrategyTests.cs
│   │   └── TradingBot.Strategies.Tests.csproj
│   │
│   ├── TradingBot.Engine.Tests/
│   │   ├── StrategyEngineTests.cs
│   │   ├── OrderExecutionServiceTests.cs
│   │   ├── PortfolioManagerTests.cs
│   │   ├── RiskManagerTests.cs
│   │   └── TradingBot.Engine.Tests.csproj
│   │
│   └── TradingBot.Analytics.Tests/
│       ├── PerformanceCalculatorTests.cs
│       ├── MetricsCalculatorTests.cs
│       └── TradingBot.Analytics.Tests.csproj
│
├── scripts/
│   ├── install-windows.ps1                # Windows installer
│   ├── install-unix.sh                    # macOS/Linux installer
│   ├── build-all.ps1                      # Build script (Windows)
│   └── build-all.sh                       # Build script (Unix)
│
├── config/
│   ├── appsettings.json                   # Default configuration
│   ├── appsettings.Development.json
│   ├── strategies.yaml                    # Strategy definitions
│   └── README.md                          # Configuration guide
│
├── .github/
│   └── workflows/
│       ├── ci.yml                         # CI pipeline
│       ├── release.yml                    # Release pipeline
│       └── code-quality.yml               # Code analysis
│
├── Directory.Build.props                  # Common MSBuild properties
├── Directory.Packages.props               # Central package management
├── .editorconfig                          # Code style rules
├── .globalconfig                          # Roslyn analyzer config
├── TradingBot.sln
└── README.md
```

### 2.2 Central Package Management (Directory.Packages.props)

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <!-- CLI and Console -->
    <PackageVersion Include="Spectre.Console" Version="0.49.1" />
    <PackageVersion Include="Spectre.Console.Cli" Version="0.49.1" />

    <!-- Data Access -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0" />

    <!-- Configuration -->
    <PackageVersion Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.0" />
    <PackageVersion Include="NetEscapades.Configuration.Yaml" Version="3.1.0" />

    <!-- Dependency Injection -->
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="9.0.0" />

    <!-- Logging -->
    <PackageVersion Include="Serilog" Version="4.0.0" />
    <PackageVersion Include="Serilog.Extensions.Hosting" Version="8.0.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />
    <PackageVersion Include="Serilog.Settings.Configuration" Version="8.0.0" />

    <!-- Market Data -->
    <PackageVersion Include="YahooFinanceApi" Version="2.3.0" />

    <!-- Resilience -->
    <PackageVersion Include="Polly" Version="8.4.0" />
    <PackageVersion Include="Polly.Extensions" Version="8.4.0" />
    <PackageVersion Include="Polly.RateLimiting" Version="8.4.0" />

    <!-- Math and Statistics -->
    <PackageVersion Include="MathNet.Numerics" Version="5.0.0" />

    <!-- Background Jobs -->
    <PackageVersion Include="TickerQ" Version="1.0.0" />

    <!-- Testing -->
    <PackageVersion Include="xunit" Version="3.0.0" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.0.0" />
    <PackageVersion Include="Shouldly" Version="4.2.1" />
    <PackageVersion Include="FakeItEasy" Version="8.3.0" />
    <PackageVersion Include="FakeItEasy.Analyzer.CSharp" Version="8.3.0" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
    <PackageVersion Include="coverlet.msbuild" Version="6.0.2" />

    <!-- Code Analysis -->
    <PackageVersion Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0" />
    <PackageVersion Include="StyleCop.Analyzers" Version="1.2.0-beta.556" />
    <PackageVersion Include="Roslynator.Analyzers" Version="4.12.0" />
    <PackageVersion Include="SonarAnalyzer.CSharp" Version="9.30.0.95878" />
  </ItemGroup>
</Project>
```

### 2.3 Common Build Properties (Directory.Build.props)

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>12</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn> <!-- Missing XML comment -->
  </PropertyGroup>

  <!-- Deterministic builds -->
  <PropertyGroup>
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>

  <!-- Assembly metadata -->
  <PropertyGroup>
    <Product>TradingBot CLI</Product>
    <Company>TradingBot</Company>
    <Copyright>Copyright © 2025 TradingBot</Copyright>
    <Version>1.0.0</Version>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
  </PropertyGroup>

  <!-- Code analyzers -->
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" />
    <PackageReference Include="StyleCop.Analyzers">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Roslynator.Analyzers">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

---

## 3. Phase-by-Phase Implementation

### Phase 1: Project Foundation (Week 1-2)

#### 3.1.1 Initial Setup

**Tasks**:
1. Create solution structure with all projects
2. Configure central package management
3. Set up .editorconfig and .globalconfig
4. Configure Roslyn analyzers
5. Create initial README.md

**Deliverables**:
- Solution builds successfully
- All analyzers active and passing
- CI/CD pipeline configured

**Commands**:
```bash
# Create solution
dotnet new sln -n TradingBot

# Create projects
dotnet new classlib -n TradingBot.Core -o src/TradingBot.Core -f net9.0
dotnet new classlib -n TradingBot.Infrastructure -o src/TradingBot.Infrastructure -f net9.0
dotnet new classlib -n TradingBot.Strategies -o src/TradingBot.Strategies -f net9.0
dotnet new classlib -n TradingBot.Engine -o src/TradingBot.Engine -f net9.0
dotnet new classlib -n TradingBot.Analytics -o src/TradingBot.Analytics -f net9.0
dotnet new console -n TradingBot.Cli -o src/TradingBot.Cli -f net9.0

# Create test projects
dotnet new xunit -n TradingBot.Core.Tests -o tests/TradingBot.Core.Tests -f net9.0
dotnet new xunit -n TradingBot.Infrastructure.Tests -o tests/TradingBot.Infrastructure.Tests -f net9.0
dotnet new xunit -n TradingBot.Strategies.Tests -o tests/TradingBot.Strategies.Tests -f net9.0
dotnet new xunit -n TradingBot.Engine.Tests -o tests/TradingBot.Engine.Tests -f net9.0
dotnet new xunit -n TradingBot.Analytics.Tests -o tests/TradingBot.Analytics.Tests -f net9.0
dotnet new xunit -n TradingBot.Cli.Tests -o tests/TradingBot.Cli.Tests -f net9.0

# Add projects to solution
dotnet sln add src/**/*.csproj
dotnet sln add tests/**/*.csproj

# Add project references
dotnet add src/TradingBot.Cli reference src/TradingBot.Core
dotnet add src/TradingBot.Cli reference src/TradingBot.Infrastructure
dotnet add src/TradingBot.Cli reference src/TradingBot.Strategies
dotnet add src/TradingBot.Cli reference src/TradingBot.Engine
dotnet add src/TradingBot.Cli reference src/TradingBot.Analytics

dotnet add src/TradingBot.Infrastructure reference src/TradingBot.Core
dotnet add src/TradingBot.Strategies reference src/TradingBot.Core
dotnet add src/TradingBot.Engine reference src/TradingBot.Core
dotnet add src/TradingBot.Analytics reference src/TradingBot.Core
```

#### 3.1.2 Core Domain Models

**File**: `src/TradingBot.Core/Models/MarketData/Quote.cs`
```csharp
namespace TradingBot.Core.Models.MarketData;

/// <summary>
/// Represents a real-time market quote for a symbol.
/// </summary>
public sealed record Quote
{
    /// <summary>
    /// Gets the trading symbol (e.g., "AAPL", "SPY").
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Gets the timestamp of the quote in UTC.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the current price.
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Gets the bid price.
    /// </summary>
    public required decimal Bid { get; init; }

    /// <summary>
    /// Gets the ask price.
    /// </summary>
    public required decimal Ask { get; init; }

    /// <summary>
    /// Gets the trading volume.
    /// </summary>
    public required long Volume { get; init; }

    /// <summary>
    /// Gets the absolute price change.
    /// </summary>
    public required decimal Change { get; init; }

    /// <summary>
    /// Gets the percentage price change.
    /// </summary>
    public required decimal ChangePercent { get; init; }

    /// <summary>
    /// Gets the bid-ask spread.
    /// </summary>
    public decimal Spread => Ask - Bid;

    /// <summary>
    /// Gets the midpoint price between bid and ask.
    /// </summary>
    public decimal MidPrice => (Bid + Ask) / 2m;
}
```

**File**: `src/TradingBot.Core/Models/MarketData/Candle.cs`
```csharp
namespace TradingBot.Core.Models.MarketData;

/// <summary>
/// Represents an OHLCV candlestick for a given timeframe.
/// </summary>
public sealed record Candle
{
    /// <summary>
    /// Gets the trading symbol.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Gets the candle timestamp in UTC.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the opening price.
    /// </summary>
    public required decimal Open { get; init; }

    /// <summary>
    /// Gets the highest price.
    /// </summary>
    public required decimal High { get; init; }

    /// <summary>
    /// Gets the lowest price.
    /// </summary>
    public required decimal Low { get; init; }

    /// <summary>
    /// Gets the closing price.
    /// </summary>
    public required decimal Close { get; init; }

    /// <summary>
    /// Gets the trading volume.
    /// </summary>
    public required long Volume { get; init; }

    /// <summary>
    /// Gets the timeframe (e.g., 1m, 5m, 1h, 1d).
    /// </summary>
    public required string Timeframe { get; init; }

    /// <summary>
    /// Gets whether the candle is bullish (close >= open).
    /// </summary>
    public bool IsBullish => Close >= Open;

    /// <summary>
    /// Gets the candle body size (absolute difference between open and close).
    /// </summary>
    public decimal BodySize => Math.Abs(Close - Open);

    /// <summary>
    /// Gets the candle range (difference between high and low).
    /// </summary>
    public decimal Range => High - Low;

    /// <summary>
    /// Gets the typical price (high + low + close) / 3.
    /// </summary>
    public decimal TypicalPrice => (High + Low + Close) / 3m;
}
```

**File**: `src/TradingBot.Core/Enums/OrderType.cs`
```csharp
namespace TradingBot.Core.Enums;

/// <summary>
/// Defines the type of order.
/// </summary>
public enum OrderType
{
    /// <summary>
    /// Market order - executes immediately at current market price.
    /// </summary>
    Market = 0,

    /// <summary>
    /// Limit order - executes only at specified price or better.
    /// </summary>
    Limit = 1,

    /// <summary>
    /// Stop-loss order - triggers when price reaches stop level.
    /// </summary>
    StopLoss = 2,

    /// <summary>
    /// Take-profit order - closes position at profit target.
    /// </summary>
    TakeProfit = 3,

    /// <summary>
    /// Trailing stop - dynamically adjusts stop level.
    /// </summary>
    TrailingStop = 4
}

/// <summary>
/// Defines the side of the order.
/// </summary>
public enum OrderSide
{
    /// <summary>
    /// Buy order - opens long position or closes short position.
    /// </summary>
    Buy = 0,

    /// <summary>
    /// Sell order - closes long position or opens short position.
    /// </summary>
    Sell = 1
}

/// <summary>
/// Defines the status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order created but not yet submitted.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Order submitted to broker/exchange.
    /// </summary>
    Submitted = 1,

    /// <summary>
    /// Order partially filled.
    /// </summary>
    PartiallyFilled = 2,

    /// <summary>
    /// Order fully filled.
    /// </summary>
    Filled = 3,

    /// <summary>
    /// Order cancelled by user.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Order rejected by broker/exchange.
    /// </summary>
    Rejected = 5,

    /// <summary>
    /// Order expired before execution.
    /// </summary>
    Expired = 6
}
```

#### 3.1.3 Core Interfaces

**File**: `src/TradingBot.Core/Interfaces/IMarketDataService.cs`
```csharp
namespace TradingBot.Core.Interfaces;

using TradingBot.Core.Models.MarketData;

/// <summary>
/// Provides market data operations with Yahoo Finance integration.
/// </summary>
public interface IMarketDataService
{
    /// <summary>
    /// Gets a real-time quote for the specified symbol.
    /// </summary>
    /// <param name="symbol">The trading symbol.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current quote.</returns>
    Task<Quote> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets real-time quotes for multiple symbols.
    /// </summary>
    /// <param name="symbols">The trading symbols.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of quotes.</returns>
    Task<IReadOnlyList<Quote>> GetQuotesAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical OHLCV data for the specified symbol and time range.
    /// </summary>
    /// <param name="symbol">The trading symbol.</param>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="timeframe">Candle timeframe (e.g., "1d", "1h").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of candles.</returns>
    Task<IReadOnlyList<Candle>> GetHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string timeframe,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to real-time quote updates for a symbol.
    /// </summary>
    /// <param name="symbol">The trading symbol.</param>
    /// <param name="callback">Callback invoked on each quote update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SubscribeToQuotesAsync(
        string symbol,
        Action<Quote> callback,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from quote updates for a symbol.
    /// </summary>
    /// <param name="symbol">The trading symbol.</param>
    Task UnsubscribeFromQuotesAsync(string symbol);
}
```

**File**: `src/TradingBot.Core/Interfaces/IStrategy.cs`
```csharp
namespace TradingBot.Core.Interfaces;

using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Trading;

/// <summary>
/// Defines a trading strategy that generates signals from market data.
/// </summary>
public interface IStrategy
{
    /// <summary>
    /// Gets the unique strategy name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the strategy type (e.g., "Momentum", "MeanReversion").
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Gets the symbols this strategy trades.
    /// </summary>
    IReadOnlyList<string> Symbols { get; }

    /// <summary>
    /// Gets the timeframe the strategy operates on.
    /// </summary>
    string Timeframe { get; }

    /// <summary>
    /// Gets whether the strategy is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Initializes the strategy with configuration.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a trading signal for the given symbol and data.
    /// </summary>
    /// <param name="symbol">The trading symbol.</param>
    /// <param name="data">Historical candle data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Trading signal or null if no signal.</returns>
    Task<Signal?> GenerateSignalAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates strategy parameters.
    /// </summary>
    /// <returns>True if parameters are valid.</returns>
    Task<bool> ValidateParametersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables the strategy.
    /// </summary>
    void Enable();

    /// <summary>
    /// Disables the strategy.
    /// </summary>
    void Disable();
}
```

---

### Phase 2: Infrastructure Layer (Week 3-4)

#### 3.2.1 Database Setup

**File**: `src/TradingBot.Infrastructure/Persistence/TradingBotDbContext.cs`
```csharp
namespace TradingBot.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Models.Trading;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Portfolio;

/// <summary>
/// Entity Framework Core database context for TradingBot.
/// </summary>
public sealed class TradingBotDbContext : DbContext
{
    public TradingBotDbContext(DbContextOptions<TradingBotDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<Candle> Candles => Set<Candle>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<EquityPoint> EquityPoints => Set<EquityPoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingBotDbContext).Assembly);

        // SQLite-specific configuration
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            // SQLite doesn't support decimal, so use TEXT with custom converter
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                    {
                        property.SetColumnType("TEXT");
                    }
                }
            }
        }
    }
}
```

**File**: `src/TradingBot.Infrastructure/Persistence/Configurations/OrderConfiguration.cs`
```csharp
namespace TradingBot.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.Trading;

/// <summary>
/// Entity configuration for Order.
/// </summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(o => o.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.Side)
            .HasColumnName("side")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(o => o.LimitPrice)
            .HasColumnName("limit_price")
            .HasPrecision(18, 2);

        builder.Property(o => o.StopPrice)
            .HasColumnName("stop_price")
            .HasPrecision(18, 2);

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(o => o.SubmittedAt)
            .HasColumnName("submitted_at");

        builder.Property(o => o.FilledAt)
            .HasColumnName("filled_at");

        builder.Property(o => o.FilledQuantity)
            .HasColumnName("filled_quantity")
            .HasPrecision(18, 8)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(o => o.AverageFillPrice)
            .HasColumnName("average_fill_price")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(o => o.Commission)
            .HasColumnName("commission")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(o => o.StrategyName)
            .HasColumnName("strategy_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.SignalId)
            .HasColumnName("signal_id");

        // Indexes
        builder.HasIndex(o => o.Symbol).HasDatabaseName("idx_orders_symbol");
        builder.HasIndex(o => o.Status).HasDatabaseName("idx_orders_status");
        builder.HasIndex(o => o.StrategyName).HasDatabaseName("idx_orders_strategy");
        builder.HasIndex(o => o.CreatedAt).HasDatabaseName("idx_orders_created_at");
    }
}
```

#### 3.2.2 Market Data Service with Polly

**File**: `src/TradingBot.Infrastructure/MarketData/YahooFinanceService.cs`
```csharp
namespace TradingBot.Infrastructure.MarketData;

using Microsoft.Extensions.Logging;
using Polly;
using Polly.RateLimiting;
using Polly.Retry;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.MarketData;
using YahooFinanceApi;

/// <summary>
/// Market data service implementation using Yahoo Finance API with Polly resilience.
/// </summary>
public sealed class YahooFinanceService : IMarketDataService
{
    private readonly ILogger<YahooFinanceService> _logger;
    private readonly ResiliencePipeline _pipeline;
    private readonly IHistoricalDataCache _cache;

    public YahooFinanceService(
        ILogger<YahooFinanceService> logger,
        IHistoricalDataCache cache)
    {
        _logger = logger;
        _cache = cache;
        _pipeline = BuildResiliencePipeline();
    }

    public async Task<Quote> GetQuoteAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching quote for {Symbol}", symbol);

        try
        {
            var quotes = await _pipeline.ExecuteAsync(
                async ct => await Yahoo.Quotes(symbol).QueryAsync(ct),
                cancellationToken);

            var quote = quotes.First();
            return MapToQuote(symbol, quote);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch quote for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<IReadOnlyList<Quote>> GetQuotesAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default)
    {
        var symbolList = symbols.ToList();
        _logger.LogDebug("Fetching quotes for {Count} symbols", symbolList.Count);

        try
        {
            var securities = await _pipeline.ExecuteAsync(
                async ct => await Yahoo.Symbols(symbolList.ToArray())
                    .QueryAsync(ct),
                cancellationToken);

            return securities
                .Select(s => MapToQuote(s.Symbol, s))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch quotes for multiple symbols");
            throw;
        }
    }

    public async Task<IReadOnlyList<Candle>> GetHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string timeframe,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Fetching historical data for {Symbol} from {Start} to {End} ({Timeframe})",
            symbol,
            startDate,
            endDate,
            timeframe);

        // Check cache first
        var cached = await _cache.GetAsync(symbol, startDate, endDate, timeframe, cancellationToken);
        if (cached is not null && cached.Count > 0)
        {
            _logger.LogDebug("Returning {Count} candles from cache", cached.Count);
            return cached;
        }

        try
        {
            var period = MapToPeriod(timeframe);
            var history = await _pipeline.ExecuteAsync(
                async ct => await Yahoo.GetHistoricalAsync(
                    symbol,
                    startDate,
                    endDate,
                    period,
                    ct),
                cancellationToken);

            var candles = history
                .Select(h => MapToCandle(symbol, h, timeframe))
                .ToList();

            // Cache the results
            await _cache.SetAsync(symbol, startDate, endDate, timeframe, candles, cancellationToken);

            _logger.LogDebug("Fetched and cached {Count} candles", candles.Count);
            return candles;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to fetch historical data for {Symbol}",
                symbol);
            throw;
        }
    }

    public Task SubscribeToQuotesAsync(
        string symbol,
        Action<Quote> callback,
        CancellationToken cancellationToken = default)
    {
        // Real-time streaming would require WebSocket or polling
        // For now, implement polling-based subscription
        throw new NotImplementedException("Real-time quote streaming will be implemented in Phase 3");
    }

    public Task UnsubscribeFromQuotesAsync(string symbol)
    {
        throw new NotImplementedException("Real-time quote streaming will be implemented in Phase 3");
    }

    private ResiliencePipeline BuildResiliencePipeline()
    {
        return new ResiliencePipelineBuilder()
            // Retry with exponential backoff
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry attempt {Attempt} after {Delay}ms due to: {Exception}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            // Rate limiting: 60 requests per minute
            .AddRateLimiter(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 10
            }))
            // Timeout after 10 seconds
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }

    private static Quote MapToQuote(string symbol, Security security)
    {
        return new Quote
        {
            Symbol = symbol,
            Timestamp = DateTime.UtcNow,
            Price = (decimal)security.RegularMarketPrice,
            Bid = (decimal)security.Bid,
            Ask = (decimal)security.Ask,
            Volume = security.RegularMarketVolume,
            Change = (decimal)security.RegularMarketChange,
            ChangePercent = (decimal)security.RegularMarketChangePercent
        };
    }

    private static Candle MapToCandle(string symbol, Candle yahooCandle, string timeframe)
    {
        return new Candle
        {
            Symbol = symbol,
            Timestamp = yahooCandle.DateTime,
            Open = (decimal)yahooCandle.Open,
            High = (decimal)yahooCandle.High,
            Low = (decimal)yahooCandle.Low,
            Close = (decimal)yahooCandle.Close,
            Volume = yahooCandle.Volume,
            Timeframe = timeframe
        };
    }

    private static Period MapToPeriod(string timeframe)
    {
        return timeframe.ToLowerInvariant() switch
        {
            "1m" => Period.Minute,
            "5m" => Period.Minute5,
            "15m" => Period.Minute15,
            "30m" => Period.Minute30,
            "1h" => Period.Hour,
            "1d" => Period.Daily,
            "1w" => Period.Weekly,
            "1mo" => Period.Monthly,
            _ => throw new ArgumentException($"Invalid timeframe: {timeframe}", nameof(timeframe))
        };
    }
}
```

#### 3.2.3 Configuration and Encryption

**File**: `src/TradingBot.Infrastructure/Configuration/EncryptionService.cs`
```csharp
namespace TradingBot.Infrastructure.Configuration;

using System.Security.Cryptography;
using System.Text;
using TradingBot.Core.Interfaces;

/// <summary>
/// AES-256 encryption service for securing API keys and secrets.
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService()
    {
        // Derive key from machine-specific data
        var password = GetMachineSpecificPassword();
        using var deriveBytes = new Rfc2898DeriveBytes(
            password,
            Encoding.UTF8.GetBytes("TradingBotSalt2025"),
            100000,
            HashAlgorithmName.SHA256);

        _key = deriveBytes.GetBytes(32); // 256 bits
        _iv = deriveBytes.GetBytes(16);  // 128 bits
    }

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        return Convert.ToBase64String(ciphertextBytes);
    }

    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var ciphertextBytes = Convert.FromBase64String(ciphertext);
        var plaintextBytes = decryptor.TransformFinalBlock(ciphertextBytes, 0, ciphertextBytes.Length);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private static string GetMachineSpecificPassword()
    {
        // Combine machine-specific data for key derivation
        var machineId = Environment.MachineName;
        var userId = Environment.UserName;
        var osVersion = Environment.OSVersion.VersionString;
        var salt = "TradingBotEncryption2025";

        return $"{machineId}:{userId}:{osVersion}:{salt}";
    }
}
```

---

### Phase 3: Strategy Engine (Week 5-6)

#### 3.3.1 Indicator Library (MathNet.Numerics)

**File**: `src/TradingBot.Strategies/Base/IndicatorLibrary.cs`
```csharp
namespace TradingBot.Strategies.Base;

using MathNet.Numerics.Statistics;
using TradingBot.Core.Models.MarketData;

/// <summary>
/// Technical indicator library using MathNet.Numerics.
/// </summary>
public static class IndicatorLibrary
{
    /// <summary>
    /// Calculates Simple Moving Average (SMA).
    /// </summary>
    public static decimal CalculateSMA(IReadOnlyList<Candle> candles, int period)
    {
        if (candles.Count < period)
        {
            throw new ArgumentException($"Insufficient data: need {period}, got {candles.Count}");
        }

        var closes = candles.TakeLast(period).Select(c => (double)c.Close).ToArray();
        return (decimal)closes.Mean();
    }

    /// <summary>
    /// Calculates Exponential Moving Average (EMA).
    /// </summary>
    public static decimal CalculateEMA(IReadOnlyList<Candle> candles, int period)
    {
        if (candles.Count < period)
        {
            throw new ArgumentException($"Insufficient data: need {period}, got {candles.Count}");
        }

        var multiplier = 2.0 / (period + 1);
        var ema = (double)candles.First().Close;

        foreach (var candle in candles.Skip(1))
        {
            ema = ((double)candle.Close * multiplier) + (ema * (1 - multiplier));
        }

        return (decimal)ema;
    }

    /// <summary>
    /// Calculates Relative Strength Index (RSI).
    /// </summary>
    public static decimal CalculateRSI(IReadOnlyList<Candle> candles, int period)
    {
        if (candles.Count < period + 1)
        {
            throw new ArgumentException($"Insufficient data: need {period + 1}, got {candles.Count}");
        }

        var gains = new List<double>();
        var losses = new List<double>();

        for (int i = 1; i < candles.Count; i++)
        {
            var change = candles[i].Close - candles[i - 1].Close;
            if (change > 0)
            {
                gains.Add((double)change);
                losses.Add(0);
            }
            else
            {
                gains.Add(0);
                losses.Add((double)Math.Abs(change));
            }
        }

        var avgGain = gains.TakeLast(period).Average();
        var avgLoss = losses.TakeLast(period).Average();

        if (avgLoss == 0)
        {
            return 100m;
        }

        var rs = avgGain / avgLoss;
        var rsi = 100 - (100 / (1 + rs));

        return (decimal)rsi;
    }

    /// <summary>
    /// Calculates MACD (Moving Average Convergence Divergence).
    /// </summary>
    public static (decimal Macd, decimal Signal, decimal Histogram) CalculateMACD(
        IReadOnlyList<Candle> candles,
        int fastPeriod = 12,
        int slowPeriod = 26,
        int signalPeriod = 9)
    {
        if (candles.Count < slowPeriod + signalPeriod)
        {
            throw new ArgumentException($"Insufficient data for MACD calculation");
        }

        var fastEma = CalculateEMA(candles, fastPeriod);
        var slowEma = CalculateEMA(candles, slowPeriod);
        var macd = fastEma - slowEma;

        // Calculate signal line (EMA of MACD)
        // For simplicity, using SMA here; proper implementation would track MACD history
        var signal = macd; // Simplified

        var histogram = macd - signal;

        return (macd, signal, histogram);
    }

    /// <summary>
    /// Calculates Bollinger Bands.
    /// </summary>
    public static (decimal Upper, decimal Middle, decimal Lower) CalculateBollingerBands(
        IReadOnlyList<Candle> candles,
        int period,
        double standardDeviations = 2.0)
    {
        if (candles.Count < period)
        {
            throw new ArgumentException($"Insufficient data: need {period}, got {candles.Count}");
        }

        var closes = candles.TakeLast(period).Select(c => (double)c.Close).ToArray();
        var middle = closes.Mean();
        var stdDev = closes.StandardDeviation();

        var upper = middle + (standardDeviations * stdDev);
        var lower = middle - (standardDeviations * stdDev);

        return ((decimal)upper, (decimal)middle, (decimal)lower);
    }

    /// <summary>
    /// Calculates Average True Range (ATR).
    /// </summary>
    public static decimal CalculateATR(IReadOnlyList<Candle> candles, int period)
    {
        if (candles.Count < period + 1)
        {
            throw new ArgumentException($"Insufficient data: need {period + 1}, got {candles.Count}");
        }

        var trueRanges = new List<double>();

        for (int i = 1; i < candles.Count; i++)
        {
            var high = (double)candles[i].High;
            var low = (double)candles[i].Low;
            var prevClose = (double)candles[i - 1].Close;

            var tr = Math.Max(
                high - low,
                Math.Max(
                    Math.Abs(high - prevClose),
                    Math.Abs(low - prevClose)));

            trueRanges.Add(tr);
        }

        var atr = trueRanges.TakeLast(period).Average();
        return (decimal)atr;
    }
}
```

#### 3.3.2 Momentum Strategy

**File**: `src/TradingBot.Strategies/Momentum/MomentumStrategy.cs`
```csharp
namespace TradingBot.Strategies.Momentum;

using Microsoft.Extensions.Logging;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Trading;
using TradingBot.Strategies.Base;

/// <summary>
/// Momentum-based trading strategy using RSI and MACD indicators.
/// </summary>
public sealed class MomentumStrategy : IStrategy
{
    private readonly ILogger<MomentumStrategy> _logger;
    private readonly MomentumConfig _config;

    public MomentumStrategy(
        ILogger<MomentumStrategy> logger,
        MomentumConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public string Name => _config.Name;
    public string Type => "Momentum";
    public IReadOnlyList<string> Symbols => _config.Symbols;
    public string Timeframe => _config.Timeframe;
    public bool IsEnabled { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Initializing momentum strategy '{Name}' with symbols: {Symbols}",
            Name,
            string.Join(", ", Symbols));

        IsEnabled = _config.Enabled;
        return Task.CompletedTask;
    }

    public Task<Signal?> GenerateSignalAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return Task.FromResult<Signal?>(null);
        }

        if (data.Count < _config.RsiPeriod + 1)
        {
            _logger.LogDebug(
                "Insufficient data for {Symbol}: need {Required}, got {Actual}",
                symbol,
                _config.RsiPeriod + 1,
                data.Count);
            return Task.FromResult<Signal?>(null);
        }

        try
        {
            // Calculate indicators
            var rsi = IndicatorLibrary.CalculateRSI(data, _config.RsiPeriod);
            var (macd, signal, histogram) = IndicatorLibrary.CalculateMACD(
                data,
                _config.MacdFast,
                _config.MacdSlow,
                _config.MacdSignal);
            var sma = IndicatorLibrary.CalculateSMA(data, _config.SmaPeriod);
            var currentPrice = data.Last().Close;

            _logger.LogDebug(
                "{Symbol}: RSI={Rsi:F2}, MACD={Macd:F4}, Price={Price}, SMA={Sma}",
                symbol,
                rsi,
                macd,
                currentPrice,
                sma);

            // Bullish signals: RSI oversold + MACD positive + price above SMA
            if (rsi < _config.RsiOversold &&
                macd > signal &&
                currentPrice > sma)
            {
                _logger.LogInformation(
                    "{Symbol}: BUY signal - RSI oversold ({Rsi}), MACD bullish, price above SMA",
                    symbol,
                    rsi);

                return Task.FromResult<Signal?>(new Signal
                {
                    Id = Guid.NewGuid(),
                    StrategyName = Name,
                    Symbol = symbol,
                    Type = SignalType.Buy,
                    Timestamp = DateTime.UtcNow,
                    Confidence = CalculateConfidence(rsi, macd, signal, currentPrice, sma, isBuy: true),
                    SuggestedPrice = currentPrice,
                    Metadata = new Dictionary<string, object>
                    {
                        ["RSI"] = rsi,
                        ["MACD"] = macd,
                        ["Signal"] = signal,
                        ["SMA"] = sma
                    }
                });
            }

            // Bearish signals: RSI overbought + MACD negative + price below SMA
            if (rsi > _config.RsiOverbought &&
                macd < signal &&
                currentPrice < sma)
            {
                _logger.LogInformation(
                    "{Symbol}: SELL signal - RSI overbought ({Rsi}), MACD bearish, price below SMA",
                    symbol,
                    rsi);

                return Task.FromResult<Signal?>(new Signal
                {
                    Id = Guid.NewGuid(),
                    StrategyName = Name,
                    Symbol = symbol,
                    Type = SignalType.Sell,
                    Timestamp = DateTime.UtcNow,
                    Confidence = CalculateConfidence(rsi, macd, signal, currentPrice, sma, isBuy: false),
                    SuggestedPrice = currentPrice,
                    Metadata = new Dictionary<string, object>
                    {
                        ["RSI"] = rsi,
                        ["MACD"] = macd,
                        ["Signal"] = signal,
                        ["SMA"] = sma
                    }
                });
            }

            return Task.FromResult<Signal?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating signal for {Symbol}", symbol);
            return Task.FromResult<Signal?>(null);
        }
    }

    public Task<bool> ValidateParametersAsync(CancellationToken cancellationToken = default)
    {
        var isValid = _config.RsiPeriod > 0 &&
                      _config.RsiOversold > 0 && _config.RsiOversold < 100 &&
                      _config.RsiOverbought > 0 && _config.RsiOverbought < 100 &&
                      _config.RsiOversold < _config.RsiOverbought &&
                      _config.MacdFast > 0 &&
                      _config.MacdSlow > 0 &&
                      _config.MacdSignal > 0 &&
                      _config.MacdFast < _config.MacdSlow &&
                      _config.SmaPeriod > 0;

        return Task.FromResult(isValid);
    }

    public void Enable()
    {
        _logger.LogInformation("Enabling strategy '{Name}'", Name);
        IsEnabled = true;
    }

    public void Disable()
    {
        _logger.LogInformation("Disabling strategy '{Name}'", Name);
        IsEnabled = false;
    }

    private static decimal CalculateConfidence(
        decimal rsi,
        decimal macd,
        decimal signal,
        decimal price,
        decimal sma,
        bool isBuy)
    {
        // Confidence based on how extreme the indicators are
        var confidence = 0.5m;

        if (isBuy)
        {
            // More oversold = higher confidence
            confidence += (30m - rsi) / 60m; // Range: 0 to 0.5
            // Stronger MACD = higher confidence
            confidence += Math.Min((macd - signal) / 10m, 0.25m);
        }
        else
        {
            // More overbought = higher confidence
            confidence += (rsi - 70m) / 60m; // Range: 0 to 0.5
            // Stronger MACD = higher confidence
            confidence += Math.Min((signal - macd) / 10m, 0.25m);
        }

        return Math.Clamp(confidence, 0m, 1m);
    }
}
```

---

### Phase 4: CLI Framework (Week 7-8)

#### 3.4.1 Program Entry Point

**File**: `src/TradingBot.Cli/Program.cs`
```csharp
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Cli.Infrastructure;

var services = new ServiceCollection();
services.AddTradingBotServices();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("tradingbot");
    config.SetApplicationVersion("1.0.0");
    config.ValidateExamples();

    config.AddCommand<StartCommand>("start")
        .WithDescription("Start the trading bot")
        .WithExample(["start"])
        .WithExample(["start", "--config", "myconfig.json"])
        .WithExample(["start", "--no-dashboard"]);

    config.AddCommand<StopCommand>("stop")
        .WithDescription("Stop the trading bot gracefully");

    config.AddBranch("strategy", strategy =>
    {
        strategy.SetDescription("Manage trading strategies");

        strategy.AddCommand<StrategyListCommand>("list")
            .WithDescription("List all strategies");

        strategy.AddCommand<StrategyEnableCommand>("enable")
            .WithDescription("Enable a strategy")
            .WithExample(["strategy", "enable", "momentum_spy"]);

        strategy.AddCommand<StrategyDisableCommand>("disable")
            .WithDescription("Disable a strategy")
            .WithExample(["strategy", "disable", "momentum_spy"]);

        strategy.AddCommand<StrategyConfigureCommand>("configure")
            .WithDescription("Configure strategy parameters")
            .WithExample(["strategy", "configure", "momentum_spy", "--param", "rsi_period=20"]);

        strategy.AddCommand<StrategyAddCommand>("add")
            .WithDescription("Add a new custom strategy")
            .WithExample(["strategy", "add", "--file", "mystrategy.cs", "--name", "MyStrategy"]);
    });

    config.AddBranch("risk", risk =>
    {
        risk.SetDescription("Manage risk parameters");

        risk.AddCommand<RiskShowCommand>("show")
            .WithDescription("Show current risk parameters");

        risk.AddCommand<RiskSetLeverageCommand>("set-leverage")
            .WithDescription("Set maximum leverage")
            .WithExample(["risk", "set-leverage", "2.0"]);

        risk.AddCommand<RiskSetStopLossCommand>("set-stoploss")
            .WithDescription("Set default stop-loss percentage")
            .WithExample(["risk", "set-stoploss", "2.0"])
            .WithExample(["risk", "set-stoploss", "2.0", "--update-existing"]);
    });

    config.AddBranch("portfolio", portfolio =>
    {
        portfolio.SetDescription("View and manage portfolio");

        portfolio.AddCommand<PortfolioShowCommand>("show")
            .WithDescription("Show current positions");

        portfolio.AddCommand<PortfolioHistoryCommand>("history")
            .WithDescription("Show trade history")
            .WithExample(["portfolio", "history"])
            .WithExample(["portfolio", "history", "--start-date", "2024-01-01"])
            .WithExample(["portfolio", "history", "--strategy", "momentum_spy"]);
    });

    config.AddBranch("performance", performance =>
    {
        performance.SetDescription("View performance metrics");

        performance.AddCommand<PerformanceShowCommand>("show")
            .WithDescription("Show performance metrics")
            .WithExample(["performance", "show"])
            .WithExample(["performance", "show", "--period", "1m"]);
    });

    config.AddBranch("backtest", backtest =>
    {
        backtest.SetDescription("Run backtests");

        backtest.AddCommand<BacktestRunCommand>("run")
            .WithDescription("Run a backtest")
            .WithExample(["backtest", "run", "--strategy", "momentum_spy", "--start-date", "2024-01-01", "--end-date", "2024-12-31"]);
    });

    config.AddCommand<DashboardCommand>("dashboard")
        .WithDescription("Show live dashboard");

    config.AddCommand<ConfigShowCommand>("config")
        .WithDescription("Show configuration");
});

try
{
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
    return -1;
}
```

#### 3.4.2 Dashboard Command

**File**: `src/TradingBot.Cli/Commands/DashboardCommand.cs`
```csharp
namespace TradingBot.Cli.Commands;

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Cli.Dashboard;

/// <summary>
/// Command to display the live trading dashboard.
/// </summary>
public sealed class DashboardCommand : AsyncCommand<DashboardCommand.Settings>
{
    private readonly DashboardRenderer _renderer;

    public DashboardCommand(DashboardRenderer renderer)
    {
        _renderer = renderer;
    }

    public sealed class Settings : CommandSettings
    {
        [Description("Refresh interval in seconds")]
        [CommandOption("--refresh <SECONDS>")]
        [DefaultValue(1)]
        public int RefreshInterval { get; init; }

        [Description("Dashboard layout name")]
        [CommandOption("--layout <NAME>")]
        [DefaultValue("default")]
        public string Layout { get; init; } = "default";
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine("Starting TradingBot Dashboard...");
        AnsiConsole.WriteLine("Press Ctrl+C to exit");
        AnsiConsole.WriteLine();

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await _renderer.StartAsync(
                TimeSpan.FromSeconds(settings.RefreshInterval),
                cts.Token);

            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Dashboard stopped by user[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}
```

---

### Phase 5: Testing Infrastructure (Week 9-10)

#### 3.5.1 Test Base Class

**File**: `tests/TradingBot.Core.Tests/TestBase.cs`
```csharp
namespace TradingBot.Core.Tests;

using FakeItEasy;
using Xunit.Abstractions;

/// <summary>
/// Base class for all tests providing common functionality.
/// </summary>
public abstract class TestBase
{
    protected ITestOutputHelper Output { get; }

    protected TestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    /// <summary>
    /// Creates a fake/mock object using FakeItEasy.
    /// </summary>
    protected T CreateFake<T>() where T : class
    {
        return A.Fake<T>();
    }

    /// <summary>
    /// Creates a fake object with strict mode (throws on unconfigured calls).
    /// </summary>
    protected T CreateStrictFake<T>() where T : class
    {
        return A.Fake<T>(options => options.Strict());
    }
}
```

#### 3.5.2 Example Strategy Test

**File**: `tests/TradingBot.Strategies.Tests/Momentum/MomentumStrategyTests.cs`
```csharp
namespace TradingBot.Strategies.Tests.Momentum;

using FakeItEasy;
using Microsoft.Extensions.Logging;
using Shouldly;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.MarketData;
using TradingBot.Strategies.Momentum;
using Xunit;
using Xunit.Abstractions;

public sealed class MomentumStrategyTests : TestBase
{
    private readonly ILogger<MomentumStrategy> _logger;
    private readonly MomentumConfig _config;

    public MomentumStrategyTests(ITestOutputHelper output) : base(output)
    {
        _logger = CreateFake<ILogger<MomentumStrategy>>();
        _config = new MomentumConfig
        {
            Name = "test_momentum",
            Enabled = true,
            Symbols = new List<string> { "SPY" },
            Timeframe = "1h",
            RsiPeriod = 14,
            RsiOversold = 30,
            RsiOverbought = 70,
            MacdFast = 12,
            MacdSlow = 26,
            MacdSignal = 9,
            SmaPeriod = 50
        };
    }

    [Fact]
    public async Task GenerateSignalAsync_WhenRSIOversoldAndMACDBullishAndPriceAboveSMA_ShouldReturnBuySignal()
    {
        // Arrange
        var strategy = new MomentumStrategy(_logger, _config);
        await strategy.InitializeAsync();

        var candles = CreateCandlesWithRSI(25, isMacdBullish: true, isPriceAboveSMA: true);

        // Act
        var signal = await strategy.GenerateSignalAsync("SPY", candles);

        // Assert
        signal.ShouldNotBeNull();
        signal.Type.ShouldBe(SignalType.Buy);
        signal.Symbol.ShouldBe("SPY");
        signal.StrategyName.ShouldBe("test_momentum");
        signal.Confidence.ShouldBeGreaterThan(0.5m);
    }

    [Fact]
    public async Task GenerateSignalAsync_WhenRSIOverboughtAndMACDBearishAndPriceBelowSMA_ShouldReturnSellSignal()
    {
        // Arrange
        var strategy = new MomentumStrategy(_logger, _config);
        await strategy.InitializeAsync();

        var candles = CreateCandlesWithRSI(75, isMacdBullish: false, isPriceAboveSMA: false);

        // Act
        var signal = await strategy.GenerateSignalAsync("SPY", candles);

        // Assert
        signal.ShouldNotBeNull();
        signal.Type.ShouldBe(SignalType.Sell);
        signal.Symbol.ShouldBe("SPY");
        signal.StrategyName.ShouldBe("test_momentum");
        signal.Confidence.ShouldBeGreaterThan(0.5m);
    }

    [Fact]
    public async Task GenerateSignalAsync_WhenStrategyDisabled_ShouldReturnNull()
    {
        // Arrange
        var strategy = new MomentumStrategy(_logger, _config);
        await strategy.InitializeAsync();
        strategy.Disable();

        var candles = CreateCandlesWithRSI(25, isMacdBullish: true, isPriceAboveSMA: true);

        // Act
        var signal = await strategy.GenerateSignalAsync("SPY", candles);

        // Assert
        signal.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateSignalAsync_WhenInsufficientData_ShouldReturnNull()
    {
        // Arrange
        var strategy = new MomentumStrategy(_logger, _config);
        await strategy.InitializeAsync();

        var candles = CreateCandles(5); // Less than RSI period (14)

        // Act
        var signal = await strategy.GenerateSignalAsync("SPY", candles);

        // Assert
        signal.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateParametersAsync_WhenParametersValid_ShouldReturnTrue()
    {
        // Arrange
        var strategy = new MomentumStrategy(_logger, _config);

        // Act
        var isValid = await strategy.ValidateParametersAsync();

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateParametersAsync_WhenRSIOversoldGreaterThanOverbought_ShouldReturnFalse()
    {
        // Arrange
        _config.RsiOversold = 80;
        _config.RsiOverbought = 20;
        var strategy = new MomentumStrategy(_logger, _config);

        // Act
        var isValid = await strategy.ValidateParametersAsync();

        // Assert
        isValid.ShouldBeFalse();
    }

    private static List<Candle> CreateCandles(int count)
    {
        var candles = new List<Candle>();
        var startDate = DateTime.UtcNow.AddHours(-count);

        for (int i = 0; i < count; i++)
        {
            candles.Add(new Candle
            {
                Symbol = "SPY",
                Timestamp = startDate.AddHours(i),
                Open = 450m + i,
                High = 455m + i,
                Low = 445m + i,
                Close = 452m + i,
                Volume = 1000000,
                Timeframe = "1h"
            });
        }

        return candles;
    }

    private List<Candle> CreateCandlesWithRSI(
        decimal targetRSI,
        bool isMacdBullish,
        bool isPriceAboveSMA)
    {
        // Create synthetic candle data that produces desired RSI, MACD, and SMA conditions
        // This is simplified; real implementation would be more sophisticated
        var candles = new List<Candle>();
        var startDate = DateTime.UtcNow.AddDays(-60);

        // Create base price around 450
        decimal basePrice = 450m;
        decimal sma = basePrice;

        // Adjust final price based on SMA condition
        decimal finalPrice = isPriceAboveSMA ? sma + 10m : sma - 10m;

        // Create 60 candles with appropriate price movements for RSI
        for (int i = 0; i < 60; i++)
        {
            decimal close;

            if (i < 45)
            {
                // First 45 candles: neutral
                close = basePrice + (i * 0.1m);
            }
            else
            {
                // Last 15 candles: create RSI condition
                if (targetRSI < 30)
                {
                    // Oversold: declining prices
                    close = basePrice + 5m - ((i - 44) * 1.5m);
                }
                else if (targetRSI > 70)
                {
                    // Overbought: rising prices
                    close = basePrice - 5m + ((i - 44) * 1.5m);
                }
                else
                {
                    // Neutral
                    close = basePrice + (i * 0.1m);
                }
            }

            candles.Add(new Candle
            {
                Symbol = "SPY",
                Timestamp = startDate.AddDays(i),
                Open = close - 1m,
                High = close + 2m,
                Low = close - 2m,
                Close = close,
                Volume = 1000000,
                Timeframe = "1d"
            });
        }

        // Adjust last candle to match conditions
        candles[^1] = candles[^1] with { Close = finalPrice };

        return candles;
    }
}
```

---

## 4. Build and Deployment

### 4.1 Publish Profiles

**File**: `src/TradingBot.Cli/Properties/PublishProfiles/win-x64.pubxml`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <Platform>Any CPU</Platform>
    <PublishDir>bin\Release\net9.0\publish\win-x64\</PublishDir>
    <PublishProtocol>FileSystem</PublishProtocol>
    <TargetFramework>net9.0</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishTrimmed>true</PublishTrimmed>
    <PublishReadyToRun>true</PublishReadyToRun>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
  </PropertyGroup>
</Project>
```

### 4.2 Build Scripts

**File**: `scripts/build-all.sh`
```bash
#!/bin/bash
set -e

echo "Building TradingBot CLI for all platforms..."

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf src/TradingBot.Cli/bin/Release/net9.0/publish

# Build for Windows x64
echo "Building for Windows x64..."
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/win-x64

# Build for macOS x64
echo "Building for macOS x64..."
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj \
  -c Release \
  -r osx-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/osx-x64

# Build for macOS ARM64
echo "Building for macOS ARM64..."
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/osx-arm64

# Build for Linux x64
echo "Building for Linux x64..."
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/linux-x64

echo "Build complete! Artifacts in artifacts/ directory"
```

### 4.3 CI/CD Pipeline

**File**: `.github/workflows/ci.yml`
```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

env:
  DOTNET_VERSION: '9.0.x'
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: 'true'
  DOTNET_CLI_TELEMETRY_OPTOUT: 'true'

jobs:
  build-and-test:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]

    steps:
    - uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore --configuration Release

    - name: Run tests
      run: dotnet test --no-build --configuration Release --verbosity normal --collect:"XPlat Code Coverage" --results-directory ./coverage

    - name: Upload coverage to Codecov
      if: matrix.os == 'ubuntu-latest'
      uses: codecov/codecov-action@v4
      with:
        directory: ./coverage
        fail_ci_if_error: true

  code-quality:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Restore dependencies
      run: dotnet restore

    - name: Run Roslyn analyzers
      run: dotnet build --no-restore --configuration Release /warnaserror

    - name: Install dotnet-format
      run: dotnet tool install -g dotnet-format

    - name: Check code formatting
      run: dotnet format --verify-no-changes --verbosity diagnostic
```

---

## 5. Implementation Checklist

### Week 1: Project Setup
- [ ] Create solution and project structure
- [ ] Configure central package management
- [ ] Set up Roslyn analyzers and .editorconfig
- [ ] Configure CI/CD pipeline
- [ ] Create initial README and documentation

### Week 2: Core Models
- [ ] Implement all domain models (Quote, Candle, Order, Position, Trade)
- [ ] Implement core enums (OrderType, OrderSide, OrderStatus, SignalType)
- [ ] Define all core interfaces (IMarketDataService, IStrategy, etc.)
- [ ] Write unit tests for models
- [ ] Achieve 80% test coverage for Core project

### Week 3: Infrastructure - Database
- [ ] Implement TradingBotDbContext with EF Core 9
- [ ] Create entity configurations
- [ ] Generate initial migration
- [ ] Implement repositories
- [ ] Write integration tests for database operations

### Week 4: Infrastructure - Market Data
- [ ] Implement YahooFinanceService with YahooFinanceApi
- [ ] Configure Polly resilience pipeline (retry, rate limiting, timeout)
- [ ] Implement historical data cache
- [ ] Implement data normalizer
- [ ] Write unit and integration tests

### Week 5: Strategy Engine - Indicators
- [ ] Implement IndicatorLibrary with MathNet.Numerics
- [ ] Implement SMA, EMA, RSI, MACD, Bollinger Bands, ATR
- [ ] Write comprehensive tests for all indicators
- [ ] Validate indicator calculations against known values

### Week 6: Strategy Engine - Strategies
- [ ] Implement base Strategy class
- [ ] Implement MomentumStrategy
- [ ] Implement MeanReversionStrategy
- [ ] Implement StrategyEngine orchestrator
- [ ] Write tests for all strategies (80%+ coverage)

### Week 7: CLI Framework - Commands
- [ ] Set up Spectre.Cli and Program.cs
- [ ] Implement StartCommand and StopCommand
- [ ] Implement all Strategy commands (list, enable, disable, configure, add)
- [ ] Implement all Risk commands
- [ ] Implement all Portfolio commands
- [ ] Implement all Performance commands
- [ ] Implement all Backtest commands
- [ ] Implement Config commands

### Week 8: CLI Framework - Dashboard
- [ ] Implement DashboardRenderer with Spectre.Console
- [ ] Implement PositionsWidget
- [ ] Implement PnLWidget
- [ ] Implement MarketTrendsWidget
- [ ] Implement RecentTradesWidget
- [ ] Implement ChartWidget
- [ ] Test dashboard on all supported terminals

### Week 9: Trading Engine
- [ ] Implement OrderExecutionService
- [ ] Implement PortfolioManager
- [ ] Implement RiskManager
- [ ] Implement PositionSizeCalculator
- [ ] Implement StopLossManager
- [ ] Write tests for all engine components

### Week 10: Backtesting
- [ ] Implement BacktestingEngine
- [ ] Implement transaction cost simulator
- [ ] Implement walk-forward optimizer
- [ ] Implement Monte Carlo simulator
- [ ] Write backtest report generator
- [ ] Write comprehensive tests

### Week 11: Configuration & Security
- [ ] Implement configuration loading (appsettings.json + strategies.yaml)
- [ ] Implement EncryptionService (AES-256)
- [ ] Implement strategy sandbox for custom scripts
- [ ] Implement audit logging
- [ ] Security testing and penetration testing

### Week 12: Background Jobs
- [ ] Set up TickerQ for background tasks
- [ ] Implement DataRefreshJob (cron: every 5 minutes)
- [ ] Implement EndOfDayJob (cron: daily at midnight)
- [ ] Implement RiskMonitoringJob (cron: every 1 minute)
- [ ] Test job scheduling and execution

### Week 13: Analytics
- [ ] Implement PerformanceCalculator
- [ ] Implement MetricsCalculator (Sharpe, Sortino, Calmar)
- [ ] Implement EquityCurveGenerator
- [ ] Implement DrawdownAnalyzer
- [ ] Write tests for all analytics

### Week 14: Integration Testing
- [ ] Write end-to-end tests for complete workflows
- [ ] Test signal generation → order execution → position tracking
- [ ] Test backtest execution
- [ ] Test dashboard rendering
- [ ] Test configuration changes

### Week 15: Build & Deployment
- [ ] Create publish profiles for all platforms
- [ ] Write build scripts (build-all.sh, build-all.ps1)
- [ ] Write install scripts (install-windows.ps1, install-unix.sh)
- [ ] Test single-file deployment on Windows
- [ ] Test single-file deployment on macOS
- [ ] Test single-file deployment on Linux

### Week 16: Documentation & Polish
- [ ] Write comprehensive user documentation
- [ ] Write API documentation
- [ ] Create quickstart guide
- [ ] Create video tutorials
- [ ] Final bug fixes and polish
- [ ] Prepare for v1.0.0 release

---

## 6. Success Criteria

### Technical
- [ ] Solution builds without warnings on all platforms
- [ ] All Roslyn analyzer rules pass
- [ ] 80%+ code coverage across all projects
- [ ] Single-file executables < 50 MB per platform
- [ ] Startup time < 2 seconds
- [ ] Memory usage < 256 MB during normal operation

### Functional
- [ ] All CLI commands work as specified
- [ ] Dashboard updates every 1-5 seconds
- [ ] Strategies generate signals correctly
- [ ] Orders execute successfully
- [ ] Portfolio tracking accurate
- [ ] Backtests complete in < 10 seconds for 1 year daily data
- [ ] Configuration persists correctly
- [ ] API keys encrypted at rest

### Quality
- [ ] Zero critical bugs
- [ ] < 5 high-priority bugs at release
- [ ] All security vulnerabilities resolved
- [ ] Code formatted according to .editorconfig
- [ ] XML documentation for all public APIs
- [ ] User documentation complete

---

## 7. Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Yahoo Finance API changes | High | Implement adapter pattern, support fallback providers |
| Performance issues with live data | Medium | Optimize with caching, use async/await properly |
| Single-file publish size too large | Medium | Use PublishTrimmed, remove unused dependencies |
| Cross-platform compatibility issues | Medium | Test on all platforms in CI/CD |
| Test coverage below 80% | Medium | Write tests incrementally, enforce in CI |
| Security vulnerabilities | Critical | Regular dependency scanning, security audits |
| Complexity of Spectre.Console LiveDisplay | Low | Start simple, iterate on dashboard design |
| MathNet.Numerics indicator accuracy | Medium | Validate against known test data, use established libraries |

---

**Document Version**: 1.0.0
**Last Updated**: 2025-11-02
**Status**: Ready for Implementation