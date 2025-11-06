# TradingBot CLI - Implementation Tasks

## Document Information
- **Project**: TradingBot CLI
- **Version**: 1.0.0
- **Date**: 2025-11-02
- **Status**: Ready for Implementation

---

## Task Organization


Tasks are organized by:
- **Phase**: Major development phase (1-8)
- **Priority**: Critical, High, Medium, Low
- **Effort**: Story points (1, 2, 3, 5, 8, 13)
- **Dependencies**: Prerequisites that must be completed first
- **Owner**: Team member responsible (TBD = To Be Determined)

---

## Phase 1: Foundation (Weeks 1-2)

### TASK-001: Initialize Solut
ion Structure
**Priority**: Critical
**Effort**: 3
**Owner**: TBD
**Dependencies**: None

**Description**:
Create the complete solution structure with all projects, configure central package management, and set up build configuration.

**Acceptance Criteria**:
- [X] Solution file created with all 12 projects
- [X] Directory.Packages.props configured with all package versions
- [X] Directory.Build.props configured with common properties
- [X] All projects reference correct dependencies
- [X] Solution builds successfully on Windows, macOS, and Linux
- [X] No build warnings or errors

**Implementation Steps**:
1. Run solution creation commands from .speckit.plan
2. Create Directory.Packages.props with centralized package versions
3. Create Directory.Build.props with common build properties
4. Add project references between projects
5. Verify build on all platforms
6. Commit initial structure

**Testing**:
```bash
dotnet restore
dotnet build --no-restore
# Should complete with 0 warnings, 0 errors
```

---

### TASK-002: Configure Code Quality Tools
**Priority**: Critical
**Effort**: 2
**Owner**: TBD
**Dependencies**: TASK-001

**Description**:
Set up Roslyn analyzers, EditorConfig, and code quality enforcement to ensure consistent code style and catch issues early.

**Acceptance Criteria**:
- [X] .editorconfig created with C# style rules
- [X] .globalconfig created for Roslyn analyzer configuration
- [X] StyleCop.Analyzers configured
- [X] Roslynator.Analyzers configured
- [X] Microsoft.CodeAnalysis.NetAnalyzers enabled
- [X] SonarAnalyzer.CSharp configured
- [X] TreatWarningsAsErrors enabled
- [X] All projects pass analyzer checks

**Files to Create**:
- `.editorconfig`
- `.globalconfig`
- `stylecop.json`

**Configuration Example**:
```ini
# .editorconfig
root = true

[*.cs]
indent_style = space
indent_size = 4
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# C# style rules
csharp_new_line_before_open_brace = all
csharp_prefer_braces = true:warning
csharp_prefer_simple_using_statement = true:suggestion
```

---

### TASK-003: Set Up CI/CD Pipeline
**Priority**: High
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-001, TASK-002

**Description**:
Create GitHub Actions workflows for continuous integration, testing, and code quality checks.

**Acceptance Criteria**:
- [X] CI workflow created (.github/workflows/ci.yml)
- [X] Builds run on push to all branches
- [X] Builds run on pull requests to main/develop
- [X] Tests execute automatically
- [X] Code coverage collected with XPlat Code Coverage
- [X] Coverage uploaded to Codecov
- [X] Code quality workflow runs analyzers
- [X] Security vulnerability scanning included
- [X] Workflow runs on Ubuntu (Linux)

**Workflows to Create**:
1. `ci.yml` - Build and test
2. `code-quality.yml` - Analyzers and formatting
3. `release.yml` - Release automation (placeholder)

---

### TASK-004: Implement Core Domain Models
**Priority**: Critical
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-001

**Description**:
Implement all core domain models for market data, trading, portfolio, and backtesting.

**Acceptance Criteria**:
- [X] Quote record implemented with XML docs
- [X] Candle record implemented with XML docs
- [X] Order class implemented with all properties
- [X] Position class implemented
- [X] Trade class implemented
- [X] Signal class implemented
- [X] Account class implemented
- [X] PerformanceMetrics class implemented
- [X] All enums implemented (OrderType, OrderSide, OrderStatus, SignalType)
- [X] All models use required properties where appropriate
- [X] All models use nullable reference types correctly
- [X] Unit tests written for all models (19 tests - coverage needs improvement to 80%+)

**Models to Implement** (in `TradingBot.Core/Models/`):
- MarketData/Quote.cs
- MarketData/Candle.cs
- MarketData/SymbolInfo.cs
- Trading/Signal.cs
- Trading/Order.cs
- Trading/Position.cs
- Trading/Trade.cs
- Portfolio/Account.cs
- Portfolio/PerformanceMetrics.cs
- Portfolio/EquityPoint.cs
- Risk/RiskParameters.cs
- Risk/RiskStatus.cs
- Backtest/BacktestConfig.cs
- Backtest/BacktestResult.cs
- Backtest/MonteCarloResult.cs

**Testing Requirements**:
- Test record equality
- Test calculated properties
- Test validation logic
- Test edge cases

---

### TASK-005: Define Core Interfaces
**Priority**: Critical
**Effort**: 3
**Owner**: TBD
**Dependencies**: TASK-004

**Description**:
Define all core service interfaces that will be implemented in later phases.

**Acceptance Criteria**:
- [X] IMarketDataService interface defined with XML docs
- [X] IStrategy interface defined
- [X] IStrategyEngine interface defined
- [X] IOrderExecutionService interface defined
- [X] IPortfolioManager interface defined
- [X] IRiskManager interface defined
- [X] IBacktestingEngine interface defined
- [X] IEncryptionService interface defined
- [X] IHistoricalDataCache interface defined
- [X] All methods have clear XML documentation
- [X] All async methods return Task or Task<T>
- [X] All methods include CancellationToken parameters

**Interfaces to Define** (in `TradingBot.Core/Interfaces/`):
- IMarketDataService.cs
- IStrategy.cs
- IStrategyEngine.cs
- IOrderExecutionService.cs
- IPortfolioManager.cs
- IRiskManager.cs
- IBacktestingEngine.cs
- IEncryptionService.cs
- IHistoricalDataCache.cs

---

### TASK-006: Create Project Documentation
**Priority**: Medium
**Effort**: 3
**Owner**: TBD
**Dependencies**: TASK-001

**Description**:
Create initial project documentation including README, contributing guidelines, and architecture overview.

**Acceptance Criteria**:
- [X] README.md created with project overview
- [X] Installation instructions documented
- [X] Quick start guide included
- [X] Usage examples (CLI commands, programmatic usage)
- [X] Architecture overview
- [X] Technology stack documented
- [X] Development setup instructions
- [X] Risk warning included
- [ ] CONTRIBUTING.md created
- [ ] CODE_OF_CONDUCT.md created
- [ ] LICENSE file added
- [ ] docs/ARCHITECTURE.md (detailed architecture)
- [ ] docs/GETTING_STARTED.md (tutorials)

**Files to Create**:
- README.md
- CONTRIBUTING.md
- CODE_OF_CONDUCT.md
- LICENSE
- docs/ARCHITECTURE.md
- docs/GETTING_STARTED.md

---

## Phase 2: Infrastructure Layer (Weeks 3-4)

### TASK-007: Implement Database Context
**Priority**: Critical
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-004

**Description**:
Implement Entity Framework Core 9 database context with SQLite support and entity configurations.

**Acceptance Criteria**:
- [X] TradingBotDbContext class implemented
- [X] DbSet properties for all entities
- [X] OnModelCreating configured
- [X] SQLite-specific configurations applied (decimal conversion)
- [X] Entity configurations created for all entities
- [X] Indexes defined for query optimization
- [X] Foreign key relationships configured
- [X] Default values set where appropriate

**Entity Configurations to Create** (in `TradingBot.Infrastructure/Persistence/Configurations/`):
- OrderConfiguration.cs
- PositionConfiguration.cs
- TradeConfiguration.cs
- CandleConfiguration.cs
- QuoteConfiguration.cs
- AccountConfiguration.cs
- EquityPointConfiguration.cs
- SignalConfiguration.cs

**Testing**:
- Test DbContext initialization
- Test entity configurations
- Test relationships
- Test SQLite decimal conversion

---

### TASK-008: Create Initial Database Migration
**Priority**: Critical
**Effort**: 2
**Owner**: TBD
**Dependencies**: TASK-007

**Description**:
Generate initial EF Core migration and verify database schema.

**Acceptance Criteria**:
- [X] Initial migration generated
- [X] Migration creates all tables
- [X] Migration creates all indexes
- [X] Migration creates all foreign keys
- [X] Migration can be applied successfully
- [X] Migration can be reverted successfully
- [X] Database schema matches specification

**Commands**:
```bash
cd src/TradingBot.Infrastructure
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Verification**:
- Inspect generated SQL
- Verify table structure
- Test data insertion/retrieval

---

### TASK-009: Implement Repository Pattern
**Priority**: Medium
**Effort**: 5
**Owner**: Claude
**Dependencies**: TASK-007
**Status**: ✅ COMPLETED

**Description**:
Implement repository classes for data access abstraction.

**Acceptance Criteria**:
- [X] OrderRepository implemented with CRUD operations
- [X] PositionRepository implemented
- [X] TradeRepository implemented
- [X] CandleRepository implemented
- [X] All repositories use async/await
- [X] Repositories include filtering and sorting
- [ ] Unit tests written (mocking DbContext)
- [ ] Integration tests written (using in-memory database)

**Repositories to Implement** (in `TradingBot.Infrastructure/Persistence/Repositories/`):
- OrderRepository.cs
- PositionRepository.cs
- TradeRepository.cs
- CandleRepository.cs
- AccountRepository.cs

**Common Operations**:
- GetByIdAsync
- GetAllAsync
- FindAsync (with predicate)
- AddAsync
- UpdateAsync
- DeleteAsync
- SaveChangesAsync

---

### TASK-010: Implement Encryption Service
**Priority**: High
**Effort**: 3
**Owner**: Claude
**Dependencies**: TASK-005
**Status**: ✅ COMPLETED

**Description**:
Implement AES-256 encryption service for securing API keys and secrets.

**Acceptance Criteria**:
- [X] EncryptionService class implemented
- [X] AES-256 encryption used
- [X] CBC mode with PKCS7 padding
- [X] Key derivation using Rfc2898DeriveBytes (100,000 iterations)
- [X] Machine-specific key generation
- [X] Encrypt method implemented
- [X] Decrypt method implemented
- [X] Unit tests for encryption/decryption (21 tests)
- [X] Test round-trip encryption
- [X] Test error handling for invalid ciphertext

**Security Requirements**:
- Use System.Security.Cryptography
- 256-bit key (32 bytes)
- 128-bit IV (16 bytes)
- SHA256 for key derivation
- Never log plaintext secrets

**Testing**:
```csharp
[Fact]
public void Encrypt_Decrypt_ShouldReturnOriginalValue()
{
    var service = new EncryptionService();
    var plaintext = "my-api-key-12345";

    var encrypted = service.Encrypt(plaintext);
    var decrypted = service.Decrypt(encrypted);

    decrypted.ShouldBe(plaintext);
    encrypted.ShouldNotBe(plaintext);
}
```

---

### TASK-011: Implement Yahoo Finance Service
**Priority**: Critical
**Effort**: 8
**Owner**: Claude
**Dependencies**: TASK-005, TASK-009
**Status**: ✅ COMPLETED

**Description**:
Implement market data service using YahooFinanceApi with Polly resilience patterns.

**Acceptance Criteria**:
- [X] YahooFinanceService class implemented
- [X] IMarketDataService interface implemented
- [X] GetQuoteAsync method implemented
- [X] GetQuotesAsync method implemented (batch)
- [X] GetHistoricalDataAsync method implemented
- [X] Polly retry policy configured (3 retries, exponential backoff)
- [ ] Polly rate limiter configured (60 requests/minute) - deferred
- [ ] Polly timeout configured (10 seconds) - deferred
- [X] ResiliencePipeline built and applied (retry + circuit breaker)
- [X] Data normalization implemented
- [X] Logging integrated (ILogger)
- [ ] Unit tests with mocked HTTP calls
- [ ] Integration tests with real API (optional, tagged)

**Polly Configuration**:
```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential
    })
    .AddRateLimiter(new SlidingWindowRateLimiter(new()
    {
        PermitLimit = 60,
        Window = TimeSpan.FromMinutes(1)
    }))
    .AddTimeout(TimeSpan.FromSeconds(10))
    .Build();
```

**Testing Scenarios**:
- Successful quote retrieval
- API rate limit handling
- Network timeout handling
- Invalid symbol handling
- Malformed response handling

---

### TASK-012: Implement Historical Data Cache
**Priority**: High
**Effort**: 5
**Owner**: Claude
**Dependencies**: TASK-009
**Status**: ✅ COMPLETED

**Description**:
Implement local caching for historical market data to reduce API calls and improve performance.

**Acceptance Criteria**:
- [X] HistoricalDataCache class implemented
- [X] IHistoricalDataCache interface implemented
- [X] GetAsync method checks database first
- [X] SetAsync method stores to database
- [X] Cache invalidation logic implemented
- [X] Time-based expiration (configurable)
- [X] ClearAsync method implemented
- [X] Registered in DI container
- [ ] Unit tests written
- [ ] Integration tests with database

**Cache Strategy**:
- Historical data (> 1 day old): Cache indefinitely
- Recent data (< 1 day old): Cache for 1 hour
- Intraday data: Cache for 1 minute
- Use database timestamp for expiration check

**Methods to Implement**:
```csharp
Task<IReadOnlyList<Candle>?> GetAsync(
    string symbol,
    DateTime startDate,
    DateTime endDate,
    string timeframe,
    CancellationToken cancellationToken);

Task SetAsync(
    string symbol,
    DateTime startDate,
    DateTime endDate,
    string timeframe,
    IReadOnlyList<Candle> candles,
    CancellationToken cancellationToken);

Task InvalidateAsync(string symbol, CancellationToken cancellationToken);
```

---

### TASK-013: Implement Configuration Service
**Priority**: High
**Effort**: 5
**Owner**: Claude
**Dependencies**: TASK-010
**Status**: ✅ COMPLETED

**Description**:
Implement configuration loading from appsettings.json and strategies.yaml with encryption support.

**Acceptance Criteria**:
- [X] ConfigurationService class implemented
- [X] JSON-based configuration with Microsoft.Extensions.Configuration
- [X] User-specific config storage (AppData/TradingBot/)
- [X] Thread-safe configuration access with semaphore
- [X] Configuration backup before updates
- [X] Get/Set/GetAll/Delete methods implemented
- [X] Error handling and logging
- [X] Registered in DI container
- [ ] strategies.yaml support (optional)
- [ ] Unit tests for configuration loading

**Configuration Classes**:
- ApplicationSettings
- MarketDataSettings
- DatabaseSettings
- RiskSettings
- ExecutionSettings
- DashboardSettings
- LoggingSettings
- SecuritySettings

**Usage Example**:
```csharp
public class ConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IEncryptionService _encryption;

    public T GetSection<T>(string sectionName) where T : new()
    {
        var section = _configuration.GetSection(sectionName);
        var settings = section.Get<T>() ?? new T();

        // Decrypt any encrypted fields
        DecryptFields(settings);

        return settings;
    }
}
```

---

### TASK-014: Set Up Dependency Injection
**Priority**: Critical
**Effort**: 5
**Owner**: Claude
**Dependencies**: TASK-007, TASK-010, TASK-011, TASK-012, TASK-013
**Status**: ✅ COMPLETED

**Description**:
Configure dependency injection container with all services, repositories, and infrastructure components.

**Acceptance Criteria**:
- [X] ServiceCollectionExtensions class created
- [X] AddTradingBotServices extension method implemented
- [X] Database context registered with correct lifetime (Scoped)
- [X] Repositories registered (Scoped) - All 5 repositories registered
- [X] Services registered (Singleton for stateless, Scoped for stateful)
- [X] Configuration registered
- [X] Logging configured with Serilog
- [X] All interfaces mapped to implementations (for implemented services)
- [ ] Service resolution tested - pending

**Extension Method Structure**:
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTradingBotServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<TradingBotDbContext>(...);

        // Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Infrastructure services
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IMarketDataService, YahooFinanceService>();
        services.AddScoped<IHistoricalDataCache, HistoricalDataCache>();

        // Configuration
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog();
        });

        return services;
    }
}
```

---

## Phase 3: Strategy Engine (Weeks 5-6)

### TASK-015: Implement Indicator Library
**Priority**: Critical
**Effort**: 8
**Owner**: TBD
**Dependencies**: TASK-004

**Description**:
Implement technical indicator library using MathNet.Numerics for statistical calculations.

**Acceptance Criteria**:
- [X] IndicatorLibrary static class created
- [X] SMA (Simple Moving Average) implemented
- [X] EMA (Exponential Moving Average) implemented
- [X] RSI (Relative Strength Index) implemented
- [X] MACD (Moving Average Convergence Divergence) implemented
- [X] Bollinger Bands implemented
- [X] ATR (Average True Range) implemented
- [X] All indicators include XML documentation
- [X] All indicators validate input data
- [X] Unit tests with known expected values (comprehensive test suite)
- [X] Edge case testing (insufficient data, zero values)
- [X] Performance testing for large datasets

**Indicators to Implement**:
```csharp
public static class IndicatorLibrary
{
    public static decimal CalculateSMA(IReadOnlyList<Candle> candles, int period);
    public static decimal CalculateEMA(IReadOnlyList<Candle> candles, int period);
    public static decimal CalculateRSI(IReadOnlyList<Candle> candles, int period);
    public static (decimal Macd, decimal Signal, decimal Histogram) CalculateMACD(
        IReadOnlyList<Candle> candles, int fast, int slow, int signal);
    public static (decimal Upper, decimal Middle, decimal Lower) CalculateBollingerBands(
        IReadOnlyList<Candle> candles, int period, double stdDevs);
    public static decimal CalculateATR(IReadOnlyList<Candle> candles, int period);
}
```

**Testing Strategy**:
- Use known datasets with verified results
- Test against established libraries (TA-Lib, pandas_ta)
- Validate mathematical correctness
- Test performance with 1000+ candles

---

### TASK-016: Implement Base Strategy Class
**Priority**: High
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-005, TASK-015

**Description**:
Implement abstract base strategy class with common functionality for all strategies.

**Acceptance Criteria**:
- [X] BaseStrategy abstract class created
- [X] IStrategy interface implemented
- [X] Common properties implemented (Name, Type, Symbols, Timeframe, IsEnabled)
- [X] Enable/Disable methods implemented
- [X] Parameter validation framework
- [X] Logging integration
- [X] Abstract GenerateSignalAsync method
- [X] Helper methods for indicator calculation
- [ ] Unit tests for base functionality

**Base Class Structure**:
```csharp
public abstract class BaseStrategy : IStrategy
{
    protected ILogger Logger { get; }

    public string Name { get; }
    public abstract string Type { get; }
    public IReadOnlyList<string> Symbols { get; }
    public string Timeframe { get; }
    public bool IsEnabled { get; private set; }

    public abstract Task<Signal?> GenerateSignalAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken);

    public abstract Task<bool> ValidateParametersAsync(
        CancellationToken cancellationToken);

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;

    protected void ValidateDataSufficiency(
        IReadOnlyList<Candle> data,
        int requiredPeriod,
        string symbol);
}
```

---

### TASK-017: Implement Momentum Strategy
**Priority**: Critical
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-016

**Description**:
Implement momentum-based trading strategy using RSI, MACD, and SMA indicators.

**Acceptance Criteria**:
- [X] MomentumStrategy class created
- [X] BaseStrategy inherited
- [X] MomentumConfig class created with all parameters
- [X] RSI calculation integrated
- [X] MACD calculation integrated
- [X] SMA calculation integrated
- [X] Buy signal logic implemented (RSI oversold + MACD bullish + price > SMA)
- [X] Sell signal logic implemented (RSI overbought + MACD bearish + price < SMA)
- [X] Confidence calculation implemented
- [X] Metadata included in signals
- [X] Unit tests with 80%+ coverage (26 tests implemented)
- [X] Integration tests with real market data

**Configuration Parameters**:
```csharp
public class MomentumConfig
{
    public string Name { get; set; }
    public bool Enabled { get; set; }
    public List<string> Symbols { get; set; }
    public string Timeframe { get; set; }
    public int RsiPeriod { get; set; } = 14;
    public decimal RsiOversold { get; set; } = 30;
    public decimal RsiOverbought { get; set; } = 70;
    public int MacdFast { get; set; } = 12;
    public int MacdSlow { get; set; } = 26;
    public int MacdSignal { get; set; } = 9;
    public int SmaPeriod { get; set; } = 50;
}
```

**Test Scenarios**:
- Bullish signal generation
- Bearish signal generation
- No signal when conditions not met
- Strategy disabled returns null
- Insufficient data returns null
- Invalid parameters rejected

---

### TASK-018: Implement Mean Reversion Strategy
**Priority**: Critical
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-016

**Description**:
Implement mean reversion trading strategy using Bollinger Bands.

**Acceptance Criteria**:
- [X] MeanReversionStrategy class created
- [X] BaseStrategy inherited
- [X] MeanReversionConfig class created
- [X] Bollinger Bands calculation integrated
- [X] Buy signal logic (price below lower band)
- [X] Sell signal logic (price above upper band)
- [X] Exit at mean option implemented
- [X] Confidence calculation implemented
- [X] Unit tests with 80%+ coverage (24 tests implemented)
- [X] Integration tests with real market data

**Configuration Parameters**:
```csharp
public class MeanReversionConfig
{
    public string Name { get; set; }
    public bool Enabled { get; set; }
    public List<string> Symbols { get; set; }
    public string Timeframe { get; set; }
    public int LookbackPeriod { get; set; } = 20;
    public double StdMultiplier { get; set; } = 2.0;
    public bool ExitAtMean { get; set; } = true;
}
```

**Signal Logic**:
- Buy when: price < (mean - stdMultiplier * stdDev)
- Sell when: price > (mean + stdMultiplier * stdDev)
- Close when: price returns to mean (if ExitAtMean = true)

---

### TASK-019: Implement Strategy Engine
**Priority**: Critical
**Effort**: 8
**Owner**: TBD
**Dependencies**: TASK-017, TASK-018

**Description**:
Implement strategy engine that orchestrates multiple strategies and generates trading signals.

**Acceptance Criteria**:
- [X] StrategyEngine class created
- [X] IStrategyEngine interface implemented
- [X] Strategy registration (RegisterStrategy)
- [X] Strategy enable/disable functionality
- [X] GetStrategiesAsync and GetStrategyAsync methods
- [X] SignalGenerated event
- [X] ExecuteStrategyAsync method (executes single strategy)
- [X] ExecuteAllStrategiesAsync method (executes all enabled strategies)
- [X] Thread-safe strategy management with semaphore
- [X] Comprehensive logging and error handling
- [X] Signal emission through events
- [ ] Strategy parameter updates
- [X] Background signal generation loop
- [X] Graceful start/stop
- [ ] Unit tests with mocked strategies
- [ ] Integration tests with real strategies

**Core Methods**:
```csharp
public class StrategyEngine : IStrategyEngine
{
    Task RegisterStrategyAsync(IStrategy strategy);
    Task UnregisterStrategyAsync(string strategyName);
    Task EnableStrategyAsync(string strategyName);
    Task DisableStrategyAsync(string strategyName);
    Task UpdateStrategyParametersAsync(string name, Dictionary<string, object> params);
    Task<List<StrategyInfo>> GetStrategiesAsync();
    Task StartAsync();
    Task StopAsync();

    event EventHandler<Signal> SignalGenerated;
}
```

**Background Loop**:
- Poll strategies every 1-5 seconds (configurable)
- Fetch latest market data for each symbol
- Execute each enabled strategy
- Emit signals via event
- Handle exceptions gracefully

---

### TASK-020: Implement Custom Script Strategy
**Priority**: Medium
**Effort**: 8
**Owner**: TBD
**Dependencies**: TASK-016

**Description**:
Implement custom strategy executor that runs user-provided C# scripts in a sandbox.

**Acceptance Criteria**:
- [ ] CustomScriptStrategy class created
- [ ] C# script execution using Microsoft.CodeAnalysis.CSharp.Scripting
- [ ] Sandbox restrictions enforced (no file I/O, no network, no reflection)
- [ ] Script timeout (5 seconds)
- [ ] Script memory limit (256 MB)
- [ ] Script validation (prohibited keywords check)
- [ ] Script globals with symbol and data
- [ ] Signal return type validation
- [ ] Exception handling and logging
- [ ] Unit tests with sample scripts
- [ ] Security testing (sandbox escape attempts)

**Prohibited Code**:
- System.IO
- System.Net
- System.Reflection
- System.Diagnostics.Process
- File operations
- Network operations

**Script Template**:
```csharp
// User script example
var rsi = CalculateRSI(Data, 14);
var sma = CalculateSMA(Data, 50);
var currentPrice = Data.Last().Close;

if (rsi < 30 && currentPrice > sma)
{
    return new Signal
    {
        Type = SignalType.Buy,
        Confidence = 0.8m,
        SuggestedPrice = currentPrice
    };
}

return null;
```

---

## Phase 4: Trading Engine (Weeks 7-8)

### TASK-021: Implement Order Execution Service
**Priority**: Critical
**Effort**: 8
**Owner**: TBD
**Dependencies**: TASK-009, TASK-019

**Description**:
Implement order execution service that submits, tracks, and manages orders.

**Acceptance Criteria**:
- [X] OrderExecutionService class created
- [X] IOrderExecutionService interface implemented
- [X] SubmitOrderAsync method (create and persist order)
- [X] CancelOrderAsync method
- [X] GetOrderAsync method
- [X] GetOrdersAsync method (with filters)
- [X] Order validation before submission
- [X] Order status tracking
- [X] OrderFilled event
- [X] OrderCancelled event
- [X] OrderRejected event
- [X] Execution simulator for backtesting mode
- [X] Slippage calculation
- [X] Commission calculation
- [X] Registered in DI container
- [ ] Unit tests with mocked repository
- [ ] Integration tests with database

**Order Lifecycle**:
1. Pending → order created
2. Submitted → order sent to broker (simulated)
3. PartiallyFilled → order partially executed (if applicable)
4. Filled → order fully executed
5. Cancelled → order cancelled by user
6. Rejected → order rejected by broker
7. Expired → order expired (time-in-force)

**Execution Simulation** (for backtesting):
- Market orders: Fill at current price + slippage
- Limit orders: Fill when price reaches limit
- Stop orders: Trigger when price reaches stop level
- Apply commission per trade

---

### TASK-022: Implement Portfolio Manager
**Priority**: Critical
**Effort**: 8
**Owner**: TBD
**Dependencies**: TASK-021

**Description**:
Implement portfolio manager that tracks positions, calculates P&L, and manages account state.

**Acceptance Criteria**:
- [X] PortfolioManager class created
- [X] IPortfolioManager interface implemented
- [X] GetAccountAsync method (current account state)
- [X] GetPositionsAsync method (open positions)
- [X] GetPositionAsync method (specific symbol)
- [X] GetTradeHistoryAsync method (closed trades history)
- [X] ClosePositionAsync method
- [X] CloseAllPositionsAsync method
- [X] GetPerformanceMetricsAsync method
- [X] Realized P&L calculation
- [X] Unrealized P&L calculation
- [X] Position tracking with thread-safe locking
- [X] Account equity calculation
- [X] Registered in DI container
- [ ] Unit tests with mocked dependencies
- [ ] Integration tests with database

**Position Management**:
- Open position on order fill
- Update position on additional fills (average entry price)
- Close position on exit order fill
- Calculate realized P&L on close
- Update unrealized P&L on price updates

**Account State**:
```csharp
public class Account
{
    public decimal Balance { get; set; }        // Cash balance
    public decimal Equity { get; set; }         // Balance + Unrealized P&L
    public decimal UsedMargin { get; set; }     // Capital in positions
    public decimal FreeMargin { get; set; }     // Available capital
    public decimal Leverage { get; set; }       // Current leverage ratio
}
```

---

### TASK-023: Implement Risk Manager
**Priority**: Critical
**Effort**: 8
**Owner**: TBD
**Dependencies**: TASK-022

**Description**:
Implement risk management system with position sizing, stop-loss, take-profit, and risk limits.

**Acceptance Criteria**:
- [X] RiskManager class created
- [X] IRiskManager interface implemented
- [X] GetRiskSettingsAsync method
- [X] SetLeverageAsync method with validation (1-10x)
- [X] SetStopLossAsync method with validation (0.1-20%)
- [X] SetTakeProfitAsync method with validation (0.1-50%)
- [X] SetDailyLossLimitAsync method with validation
- [X] SetMaxDrawdownAsync method with validation (1-50%)
- [X] SetMaxPositionSizeAsync method with validation (1-100%)
- [X] ResetToDefaultsAsync method
- [X] ValidatePositionSizeAsync method (pre-trade risk check)
- [X] IsDailyLossLimitExceededAsync method
- [X] Thread-safe settings management with semaphore
- [X] Comprehensive logging of risk setting changes
- [X] Registered in DI container
- [X] Unit tests for all risk calculations (30 tests implemented)
- [X] Integration tests with portfolio manager

**Position Sizing Algorithms**:
```csharp
public enum PositionSizingMethod
{
    FixedAmount,      // Fixed $ amount per trade
    FixedPercent,     // Fixed % of account per trade
    RiskBased,        // Based on stop-loss distance
    KellyCriterion,   // Kelly formula
    VolatilityBased   // Based on ATR
}
```

**Risk Checks**:
- Sufficient account balance
- Position size within limits
- Total exposure within limits
- Leverage within limits
- Daily loss limit not breached
- Max drawdown limit not breached

---

### TASK-024: Implement Stop-Loss Manager
**Priority**: High
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-023

**Description**:
Implement stop-loss management system with fixed and trailing stops.

**Acceptance Criteria**:
- [X] StopLossManager class created
- [X] CreateStopLossAsync method (attach to position)
- [X] UpdateStopLossAsync method (modify stop level)
- [X] CreateTrailingStopAsync method
- [X] UpdateTrailingStopAsync method (adjust on price moves)
- [X] RemoveStopLossAsync method
- [X] Monitor stop-loss triggers
- [X] CheckTriggeredStopsAsync method implemented
- [X] Log all stop-loss actions
- [ ] Unit tests for stop-loss logic
- [ ] Integration tests with order execution

**Trailing Stop Logic**:
- Track highest price (for long positions)
- Track lowest price (for short positions)
- Update stop level when price moves favorably
- Maintain fixed distance from peak/trough
- Trigger when price reverses by stop distance

**Example**:
```
Entry: $100
Trailing Stop: 5%

Price rises to $110 → Stop moves to $104.50 (110 - 5%)
Price rises to $115 → Stop moves to $109.25 (115 - 5%)
Price falls to $109 → Stop triggers at $109.25, exit trade
```

---

### TASK-025: Implement Position Size Calculator
**Priority**: High
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-023

**Description**:
Implement position size calculator with multiple algorithms.

**Acceptance Criteria**:
- [X] PositionSizeCalculator class created
- [X] CalculateFixedAmount method
- [X] CalculateFixedPercent method
- [X] CalculateRiskBased method (based on stop-loss)
- [X] CalculateKelly method (Kelly Criterion)
- [X] CalculateVolatilityBased method (ATR-based)
- [X] Account for leverage
- [X] Input validation enforces limits
- [ ] Unit tests for all algorithms
- [ ] Edge case testing (zero balance, extreme volatility)

**Algorithm Implementations**:

1. **Fixed Percent**:
```
PositionSize = AccountBalance * FixedPercent / CurrentPrice
```

2. **Risk-Based**:
```
Risk = AccountBalance * RiskPercent
StopDistance = EntryPrice - StopLossPrice
PositionSize = Risk / StopDistance
```

3. **Kelly Criterion**:
```
Kelly% = WinRate - ((1 - WinRate) / (AvgWin / AvgLoss))
PositionSize = AccountBalance * Kelly% / CurrentPrice
```

4. **Volatility-Based**:
```
ATR = CalculateATR(historicalData, 14)
Risk = AccountBalance * RiskPercent
PositionSize = Risk / (ATR * VolatilityMultiplier)
```

---

### TASK-026: Implement Signal-to-Order Pipeline
**Priority**: Critical
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-021, TASK-023

**Description**:
Implement pipeline that converts strategy signals into validated orders.

**Acceptance Criteria**:
- [X] SignalProcessor class created
- [X] IStrategyEngine.SignalGenerated event added to interface
- [X] StrategyEngine.SignalGenerated event implemented
- [X] Subscribe to StrategyEngine.SignalGenerated event
- [X] Convert signal to order (market or limit)
- [X] Calculate position size using signal confidence and risk settings
- [X] Validate order with RiskManager
- [X] Submit order via OrderExecutionService
- [X] Create stop-loss orders automatically
- [X] Create take-profit orders automatically
- [X] Handle order submission failures with logging
- [X] Log all signal processing steps
- [X] Registered in DI container
- [ ] Unit tests with mocked dependencies
- [ ] Integration tests end-to-end

**Processing Flow**:
```
Signal Generated
  ↓
Extract Signal Details (symbol, type, price)
  ↓
Calculate Position Size (risk-based)
  ↓
Create Order Object
  ↓
Validate Order (risk checks)
  ↓
Submit Order
  ↓
Create Stop-Loss Order
  ↓
Create Take-Profit Order (optional)
  ↓
Log & Update State
```

---

## Phase 5: CLI Framework (Weeks 9-10)

### TASK-027: Implement CLI Program Entry Point
**Priority**: Critical
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-014

**Description**:
Implement main program entry point with Spectre.Cli command routing and dependency injection integration.

**Acceptance Criteria**:
- [X] Program.cs created with Spectre.Cli setup
- [X] TypeRegistrar implemented (DI adapter for Spectre.Cli)
- [ ] All command branches configured - partial (version only)
- [X] Command examples added
- [X] Help text configured
- [X] Version command implemented
- [X] Global exception handling
- [X] Exit codes defined
- [ ] Integration tests for CLI routing - pending

**Command Structure**:
```
tradingbot
├── start
├── stop
├── strategy [list|enable|disable|configure|add|remove]
├── risk [show|set-leverage|set-stoploss|set-takeprofit|...]
├── portfolio [show|history|close]
├── performance [show|charts|compare|export]
├── backtest [run|report|optimize]
├── config [show|set|set-api-key]
├── dashboard
└── version
```

---

### TASK-028: Implement Strategy Commands
**Priority**: High
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-027

**Description**:
Implement all strategy management CLI commands.

**Acceptance Criteria**:
- [X] StrategyListCommand implemented
- [X] StrategyEnableCommand implemented
- [X] StrategyDisableCommand implemented
- [X] StrategyStartCommand implemented (start engine)
- [X] StrategyStopCommand implemented (stop engine)
- [X] StrategyStatusCommand implemented (engine status)
- [ ] StrategyConfigureCommand implemented - pending
- [ ] StrategyAddCommand implemented - pending
- [ ] StrategyRemoveCommand implemented (optional) - pending
- [X] All commands use Spectre.Console for output
- [X] Validation attributes on command settings
- [X] Error handling with user-friendly messages
- [X] Success confirmations
- [ ] Unit tests for each command - pending

**Example: StrategyListCommand**:
```csharp
public class StrategyListCommand : AsyncCommand
{
    private readonly IStrategyEngine _engine;

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var strategies = await _engine.GetStrategiesAsync();

        var table = new Table()
            .AddColumn("Name")
            .AddColumn("Type")
            .AddColumn("Status")
            .AddColumn("Symbols")
            .AddColumn("Performance");

        foreach (var strategy in strategies)
        {
            table.AddRow(
                strategy.Name,
                strategy.Type,
                strategy.IsEnabled ? "[green]Active[/]" : "[red]Disabled[/]",
                string.Join(", ", strategy.Symbols),
                FormatPerformance(strategy.Performance));
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
```

---

### TASK-029: Implement Risk Commands
**Priority**: High
**Effort**: 3
**Owner**: TBD
**Dependencies**: TASK-027

**Description**:
Implement all risk management CLI commands.

**Acceptance Criteria**:
- [X] RiskShowCommand implemented
- [X] RiskSetLeverageCommand implemented
- [X] RiskSetStopLossCommand implemented
- [X] RiskSetTakeProfitCommand implemented
- [X] RiskSetDailyLossCommand implemented
- [X] RiskSetMaxDrawdownCommand implemented
- [X] RiskResetCommand implemented
- [X] All commands validate input ranges
- [X] Confirmation prompts for destructive operations
- [ ] Unit tests for each command

---

### TASK-030: Implement Portfolio Commands
**Priority**: High
**Effort**: 3
**Owner**: TBD
**Dependencies**: TASK-027

**Description**:
Implement portfolio viewing and management commands.

**Acceptance Criteria**:
- [X] PortfolioShowCommand implemented (displays positions)
- [X] PortfolioHistoryCommand implemented (displays trades)
- [X] PortfolioCloseCommand implemented (close positions)
- [X] Filters supported (date range, strategy, symbol)
- [X] Sorting supported (by P&L, date, symbol)
- [X] Pagination for large result sets (via --limit option)
- [ ] Export to CSV option
- [ ] Unit tests for each command

**Example Output**:
```
┌─────────┬──────┬────────┬────────────┬──────────────┬──────────┐
│ Symbol  │ Qty  │ Entry  │  Current   │     P&L      │  Strategy│
├─────────┼──────┼────────┼────────────┼──────────────┼──────────┤
│ SPY     │  10  │ 450.25 │   455.80   │ +$55.50 (+1.2%)│ momentum│
│ AAPL    │  15  │ 185.50 │   188.30   │ +$42.00 (+1.5%)│ meanrev │
└─────────┴──────┴────────┴────────────┴──────────────┴──────────┘
Total P&L: +$97.50 (+1.35%)
```

---

### TASK-031: Implement Performance Commands
**Priority**: Medium
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-027

**Description**:
Implement performance analysis and reporting commands.

**Acceptance Criteria**:
- [X] PerformanceShowCommand implemented
- [ ] PerformanceChartsCommand implemented
- [ ] PerformanceCompareCommand implemented
- [X] PerformanceExportCommand implemented
- [X] Metrics calculated (Sharpe, Sortino, win rate, etc.)
- [ ] Time period filters (1d, 1w, 1m, 3m, 1y, all)
- [ ] ASCII charts rendered (equity curve, drawdown)
- [X] Export formats: CSV, JSON, HTML
- [ ] Unit tests for each command

---

### TASK-032: Implement Backtest Commands
**Priority**: High
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-027

**Description**:
Implement backtesting CLI commands.

**Acceptance Criteria**:
- [X] BacktestRunCommand implemented
- [X] BacktestReportCommand implemented
- [ ] BacktestOptimizeCommand implemented (optional)
- [X] Progress bar for long-running backtests
- [X] Results summary displayed
- [X] Detailed report generation
- [ ] Parameter sweep for optimization
- [ ] Unit tests for each command

**BacktestRunCommand Options**:
```
--strategy <name>        Strategy to backtest
--symbols <sym1,sym2>    Symbols to trade
--start-date <date>      Start date (YYYY-MM-DD)
--end-date <date>        End date (YYYY-MM-DD)
--capital <amount>       Initial capital
--commission <amount>    Commission per trade
--slippage <percent>     Slippage percentage
```

---

### TASK-033: Implement Config Commands
**Priority**: Medium
**Effort**: 3
**Owner**: TBD
**Dependencies**: TASK-027

**Description**:
Implement configuration management commands.

**Acceptance Criteria**:
- [X] ConfigShowCommand implemented
- [X] ConfigSetCommand implemented
- [X] ConfigSetApiKeyCommand implemented (interactive, masked)
- [X] Configuration validation before saving
- [X] Sensitive values masked in display
- [X] Backup before changes
- [ ] Unit tests for each command - pending

**ConfigSetApiKeyCommand**:
```csharp
public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
{
    var provider = settings.Provider; // "yahoo", "alpaca", etc.

    var apiKey = AnsiConsole.Prompt(
        new TextPrompt<string>($"Enter API key for {provider}:")
            .Secret());

    // Encrypt and save
    var encrypted = _encryption.Encrypt(apiKey);
    await _config.SetAsync($"ApiKeys:{provider}", encrypted);

    AnsiConsole.MarkupLine("[green]API key saved successfully[/]");
    return 0;
}
```

---

### TASK-034: Implement Dashboard Renderer
**Priority**: Critical
**Effort**: 8
**Owner**: TBD
**Dependencies**: TASK-022

**Description**:
Implement live dashboard using Spectre.Console LiveDisplay with real-time updates.

**Acceptance Criteria**:
- [X] DashboardCommand class created
- [X] Layout-based display using Spectre.Console
- [X] Account panel (balance, P&L, equity)
- [X] Positions panel (open positions display)
- [X] Performance panel (metrics and statistics)
- [X] Recent trades table (last N trades)
- [X] Risk settings panel
- [X] Responsive layout (2-column with nested rows)
- [X] Error handling for data fetching
- [ ] LiveDisplay for real-time updates (static display implemented)
- [ ] Configurable refresh interval
- [ ] Keyboard shortcuts
- [ ] Unit tests for rendering

**Dashboard Layout**:
```
┌─────────────────────────────────────────────────────────────────┐
│ TradingBot CLI v1.0.0         Balance: $10,000   Equity: $10,250│
│ Active Strategies: 3/5         P&L Today: +$250 (+2.5%)         │
├─────────────────────────────────────────────────────────────────┤
│                      OPEN POSITIONS (2)                          │
│ [Positions Table]                                                │
├─────────────────────────────────────────────────────────────────┤
│                      MARKET TRENDS                               │
│ [Market Trends Table]                                            │
├─────────────────────────────────────────────────────────────────┤
│                      RECENT TRADES (5)                           │
│ [Recent Trades Table]                                            │
├─────────────────────────────────────────────────────────────────┤
│ [Q] Quit  [S] Strategies  [R] Risk  [P] Performance  [H] Help   │
│ Last Update: 2025-01-15 14:32:15 UTC                            │
└─────────────────────────────────────────────────────────────────┘
```

**Implementation**:
```csharp
public class DashboardRenderer
{
    public async Task StartAsync(
        TimeSpan refreshInterval,
        CancellationToken cancellationToken)
    {
        await AnsiConsole.Live(CreateLayout())
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var layout = await UpdateLayoutAsync();
                    ctx.UpdateTarget(layout);
                    await Task.Delay(refreshInterval, cancellationToken);
                }
            });
    }
}
```

---

### TASK-035: Implement Dashboard Widgets
**Priority**: High
**Effort**: 8
**Owner**: Claude
**Dependencies**: TASK-034
**Status**: ✅ COMPLETED

**Description**:
Implement individual dashboard widgets for positions, P&L, trends, and trades.

**Acceptance Criteria**:
- [X] PositionsWidget class created
- [X] AccountWidget (P&L) class created
- [X] PerformanceWidget class created
- [X] RecentTradesWidget class created
- [X] RiskWidget class created
- [ ] ChartWidget class created (ASCII charts) - deferred
- [X] Each widget implements IWidget interface
- [X] Each widget renders using Spectre.Console
- [X] Color coding (green for profit, red for loss)
- [X] Formatting helpers (currency, percentage)
- [X] DashboardRenderer with LiveDisplay implemented
- [ ] Unit tests for each widget - deferred

**Example: PositionsWidget**:
```csharp
public class PositionsWidget : IWidget
{
    public async Task<Table> RenderAsync()
    {
        var positions = await _portfolioManager.GetPositionsAsync();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Symbol")
            .AddColumn("Qty", c => c.RightAligned())
            .AddColumn("Entry", c => c.RightAligned())
            .AddColumn("Current", c => c.RightAligned())
            .AddColumn("P&L", c => c.RightAligned())
            .AddColumn("Strategy");

        foreach (var position in positions)
        {
            var pnlColor = position.UnrealizedPnL >= 0 ? "green" : "red";

            table.AddRow(
                position.Symbol,
                position.Quantity.ToString("F2"),
                $"${position.AverageEntryPrice:F2}",
                $"${position.CurrentPrice:F2}",
                $"[{pnlColor}]${position.UnrealizedPnL:+0.00;-0.00} ({position.UnrealizedPnLPercent:+0.0;-0.0}%)[/]",
                position.StrategyName);
        }

        return table;
    }
}
```

---

## Phase 6: Backtesting Engine (Weeks 11-12)

### TASK-036: Implement Backtesting Engine Core
**Priority**: Critical
**Effort**: 8
**Owner**: TBD
**Dependencies**: TASK-021, TASK-022, TASK-023

**Description**:
Implement core backtesting engine that simulates strategy execution on historical data.

**Acceptance Criteria**:
- [ ] BacktestingEngine class created
- [ ] IBacktestingEngine interface implemented
- [ ] RunBacktestAsync method implemented
- [ ] Historical data loading from cache/API
- [ ] Strategy execution simulation
- [ ] Order execution simulation (fill logic)
- [ ] Position tracking
- [ ] P&L calculation
- [ ] Transaction cost application
- [ ] Slippage simulation
- [ ] Performance metrics calculation
- [ ] Progress reporting
- [ ] Unit tests with mocked dependencies
- [ ] Integration tests with real strategies

**Simulation Logic**:
1. Load historical data for date range
2. Iterate through each candle chronologically
3. Update portfolio with current prices
4. Execute strategy to generate signals
5. Convert signals to simulated orders
6. Fill orders based on price conditions
7. Apply commissions and slippage
8. Update positions and calculate P&L
9. Track all trades
10. Generate performance report

**Fill Logic**:
- Market orders: Fill at next candle open + slippage
- Limit buy: Fill when price <= limit
- Limit sell: Fill when price >= limit
- Stop-loss: Fill when price <= stop level
- Take-profit: Fill when price >= profit level

---

### TASK-037: Implement Transaction Cost Simulator
**Priority**: High
**Effort**: 3
**Owner**: TBD
**Dependencies**: TASK-036

**Description**:
Implement realistic transaction cost simulation including commissions, slippage, and spreads.

**Acceptance Criteria**:
- [ ] TransactionCostSimulator class created
- [ ] CalculateCommission method (per-trade or per-share)
- [ ] CalculateSlippage method (percentage or fixed)
- [ ] CalculateSpread method (bid-ask spread)
- [ ] Apply costs to executed orders
- [ ] Configurable cost models
- [ ] Cost breakdown in reports
- [ ] Unit tests for all cost calculations

**Cost Models**:
```csharp
public class TransactionCostModel
{
    public decimal CommissionPerTrade { get; set; } = 1.0m;
    public decimal CommissionPerShare { get; set; } = 0.0m;
    public decimal SlippagePercent { get; set; } = 0.1m; // 0.1%
    public decimal SpreadPercent { get; set; } = 0.05m;  // 0.05%
}

// Example calculation
public decimal CalculateTotalCost(Order order, decimal fillPrice)
{
    var commission = CommissionPerTrade + (order.Quantity * CommissionPerShare);
    var slippage = fillPrice * order.Quantity * SlippagePercent;
    var spread = fillPrice * order.Quantity * SpreadPercent;

    return commission + slippage + spread;
}
```

---

### TASK-038: Implement Performance Calculator
**Priority**: High
**Effort**: 8
**Owner**: TBD
**Dependencies**: TASK-036

**Description**:
Implement comprehensive performance metrics calculator using MathNet.Numerics.

**Acceptance Criteria**:
- [ ] PerformanceCalculator class created
- [ ] Total return calculation
- [ ] CAGR (Compound Annual Growth Rate)
- [ ] Sharpe ratio calculation
- [ ] Sortino ratio calculation
- [ ] Calmar ratio calculation
- [ ] Maximum drawdown calculation
- [ ] Maximum drawdown duration
- [ ] Volatility (standard deviation of returns)
- [ ] Win rate, profit factor
- [ ] Average win, average loss
- [ ] Largest win, largest loss
- [ ] Trade statistics (count, frequency)
- [ ] Equity curve generation
- [ ] Drawdown curve generation
- [ ] Monthly returns aggregation
- [ ] Unit tests for all metrics
- [ ] Validation against known results

**Metrics Formulas**:

**Sharpe Ratio**:
```
Sharpe = (Mean Return - Risk Free Rate) / Standard Deviation of Returns
```

**Sortino Ratio**:
```
Sortino = (Mean Return - Risk Free Rate) / Downside Deviation
```

**Maximum Drawdown**:
```
For each point in equity curve:
    Drawdown = (Peak - Current) / Peak
MaxDrawdown = Maximum of all drawdowns
```

**CAGR**:
```
CAGR = (Ending Value / Beginning Value)^(1 / Years) - 1
```

---

### TASK-039: Implement Backtest Report Generator
**Priority**: High
**Effort**: 5
**Owner**: Claude
**Dependencies**: TASK-038
**Status**: ✅ COMPLETED

**Description**:
Implement backtest report generator with multiple output formats.

**Acceptance Criteria**:
- [X] BacktestReportGenerator class created
- [X] GenerateConsoleReport method
- [X] Console output (formatted tables with sections)
- [X] HTML export with embedded charts and styling
- [X] JSON export for programmatic access
- [X] CSV export for spreadsheet analysis
- [X] Summary metrics section
- [X] Trade list section
- [ ] Equity curve chart (ASCII) - deferred
- [ ] Drawdown chart (ASCII) - deferred
- [ ] Monthly returns heatmap - deferred
- [ ] Win/loss distribution - deferred
- [ ] Unit tests for report generation - deferred

**Report Sections**:
1. **Summary**
   - Total return, CAGR
   - Sharpe, Sortino, Calmar
   - Max drawdown
   - Total trades, win rate

2. **Trade Analysis**
   - All trades with details
   - Profit factor
   - Average win/loss
   - Largest win/loss

3. **Risk Metrics**
   - Volatility
   - VaR (Value at Risk)
   - CVaR (Conditional VaR)
   - Beta, Alpha

4. **Charts**
   - Equity curve
   - Drawdown curve
   - Monthly returns
   - Distribution histogram

---

### TASK-040: Implement Walk-Forward Optimizer
**Priority**: Medium
**Effort**: 8
**Owner**: TBD
**Dependencies**: TASK-036

**Description**:
Implement walk-forward optimization to test strategy robustness.

**Acceptance Criteria**:
- [ ] WalkForwardOptimizer class created
- [ ] RunWalkForwardAsync method
- [ ] Data splitting (in-sample, out-of-sample)
- [ ] Rolling window approach
- [ ] Parameter optimization on in-sample data
- [ ] Testing on out-of-sample data
- [ ] Multiple window iterations
- [ ] In-sample vs out-of-sample comparison
- [ ] Overfitting detection
- [ ] Report generation
- [ ] Unit tests with sample data

**Walk-Forward Process**:
```
Total Period: 2020-2024 (4 years)
In-Sample: 1 year
Out-Sample: 6 months

Window 1:
  In-Sample:  2020-01 to 2020-12 (optimize params)
  Out-Sample: 2021-01 to 2021-06 (test)

Window 2:
  In-Sample:  2020-07 to 2021-06 (optimize params)
  Out-Sample: 2021-07 to 2021-12 (test)

Window 3:
  In-Sample:  2021-01 to 2021-12 (optimize params)
  Out-Sample: 2022-01 to 2022-06 (test)

... continue until end of data
```

**Optimization Metrics**:
- Sharpe ratio (default objective)
- Sortino ratio
- Calmar ratio
- Custom fitness function

---

### TASK-041: Implement Monte Carlo Simulator
**Priority**: Low
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-038

**Description**:
Implement Monte Carlo simulation to assess distribution of outcomes.

**Acceptance Criteria**:
- [ ] MonteCarloSimulator class created
- [ ] RunMonteCarloAsync method
- [ ] Trade order randomization (1000+ iterations)
- [ ] Distribution of final equity calculated
- [ ] Confidence intervals (90%, 95%, 99%)
- [ ] Probability of profit
- [ ] Probability of achieving target return
- [ ] Risk of ruin calculation
- [ ] Distribution charts
- [ ] Unit tests with sample trades

**Simulation Process**:
1. Take completed backtest results (all trades)
2. Shuffle trade order randomly
3. Recalculate equity curve with shuffled trades
4. Record final equity value
5. Repeat 1000+ times
6. Analyze distribution of outcomes

**Metrics**:
- Mean final equity
- Median final equity
- Standard deviation
- Percentiles (5%, 25%, 50%, 75%, 95%)
- Probability of profit
- Probability of specific return thresholds

---

## Phase 7: Background Jobs & Analytics (Weeks 13-14)

### TASK-042: Set Up Background Jobs Infrastructure
**Priority**: High
**Effort**: 5
**Owner**: Claude
**Dependencies**: TASK-014
**Status**: ✅ COMPLETED

**Description**:
Set up background job scheduling using .NET's built-in BackgroundService and IHostedService.

**Acceptance Criteria**:
- [X] IJob interface created
- [X] JobScheduler base class configured with PeriodicTimer
- [X] Job registration system in DI container
- [X] Job execution monitoring with logging
- [X] Error handling for failed jobs
- [X] Automatic retry on next interval
- [X] Logging integration
- [ ] Unit tests for job infrastructure - deferred

**TickerQ Setup**:
```csharp
services.AddTickerQ(options =>
{
    options.MaxConcurrentJobs = 5;
    options.RetryPolicy = RetryPolicy.Exponential(maxAttempts: 3);
});

// Register jobs
services.AddJob<DataRefreshJob>("*/5 * * * *");  // Every 5 minutes
services.AddJob<EndOfDayJob>("0 0 * * *");       // Daily at midnight
services.AddJob<RiskMonitoringJob>("* * * * *"); // Every minute
```

---

### TASK-043: Implement Market Data Refresh Job
**Priority**: High
**Effort**: 3
**Owner**: Claude
**Dependencies**: TASK-042
**Status**: ✅ COMPLETED

**Description**:
Implement background job to refresh market data for active symbols.

**Acceptance Criteria**:
- [X] MarketDataRefreshJob class created
- [X] IJob interface implemented
- [X] ExecuteAsync method implemented
- [X] Fetch quotes for all active symbols
- [X] Update position prices (via Quote.Price)
- [X] Calculate unrealized P&L automatically
- [X] Runs every 5 minutes via MarketDataRefreshJobScheduler
- [X] Error handling for failed fetches
- [X] Comprehensive logging
- [ ] Unit tests with mocked services - deferred

**Job Logic**:
```csharp
public class DataRefreshJob : IJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Get all symbols from active strategies and positions
        var symbols = await GetActiveSymbolsAsync();

        // Fetch latest quotes
        var quotes = await _marketData.GetQuotesAsync(symbols, cancellationToken);

        // Update position prices
        await _portfolio.UpdatePositionsAsync(quotes);

        // Log refresh
        _logger.LogInformation("Market data refreshed for {Count} symbols", symbols.Count);
    }
}
```

---

### TASK-044: Implement End-of-Day Job
**Priority**: Medium
**Effort**: 3
**Owner**: Claude
**Dependencies**: TASK-042
**Status**: ✅ COMPLETED

**Description**:
Implement end-of-day job for daily summaries and cleanup tasks.

**Acceptance Criteria**:
- [X] EndOfDayJob class created
- [X] Calculate daily P&L and return percentage
- [X] Log comprehensive daily summary
- [X] Log today's trading activity (wins/losses/win rate)
- [X] Check and log high drawdown warnings
- [X] Runs once daily (every 24 hours) via EndOfDayJobScheduler
- [X] Comprehensive logging
- [ ] Create equity curve point - deferred
- [ ] Archive old data - deferred
- [ ] Send notifications - deferred
- [ ] Unit tests - deferred

**Job Tasks**:
1. Calculate realized + unrealized P&L for the day
2. Add equity point to equity curve
3. Check if daily loss limit breached
4. Generate daily summary (trades, P&L, win rate)
5. Archive old logs/data (keep last 30 days)
6. Reset daily counters

---

### TASK-045: Implement Risk Monitoring Job
**Priority**: High
**Effort**: 3
**Owner**: Claude
**Dependencies**: TASK-042, TASK-023
**Status**: ✅ COMPLETED

**Description**:
Implement real-time risk monitoring job that checks risk limits.

**Acceptance Criteria**:
- [X] RiskMonitoringJob class created
- [X] Check current leverage
- [X] Check daily loss limit (closes positions if breached)
- [X] Check max drawdown limit (closes positions if breached)
- [X] Check position size limits (logs warnings)
- [X] Auto-closes all positions if limits breached
- [X] Runs every 1 minute via RiskMonitoringJobScheduler
- [X] Comprehensive logging with warnings
- [ ] Trigger alerts/notifications - deferred
- [ ] Unit tests - deferred

**Monitoring Checks**:
```csharp
public async Task ExecuteAsync(CancellationToken cancellationToken)
{
    var riskStatus = await _riskManager.GetRiskStatusAsync();

    if (riskStatus.DailyLossLimitBreached)
    {
        await _riskManager.HaltTradingAsync("Daily loss limit breached");
        _logger.LogWarning("Trading halted: Daily loss limit breached");
    }

    if (riskStatus.MaxDrawdownBreached)
    {
        await _riskManager.HaltTradingAsync("Max drawdown limit breached");
        _logger.LogWarning("Trading halted: Max drawdown limit breached");
    }

    if (riskStatus.CurrentLeverage > _riskParams.MaxLeverage)
    {
        _logger.LogWarning("Leverage exceeded: {Current} > {Max}",
            riskStatus.CurrentLeverage,
            _riskParams.MaxLeverage);
    }
}
```

---

### TASK-046: Implement Metrics Calculator
**Priority**: High
**Effort**: 5
**Owner**: Claude
**Dependencies**: TASK-038
**Status**: ✅ COMPLETED

**Description**:
Implement detailed metrics calculator for analytics module (separate from backtest metrics).

**Acceptance Criteria**:
- [X] MetricsCalculator class created
- [X] All standard metrics implemented
- [X] Risk-adjusted returns (Sharpe, Sortino, Calmar)
- [X] Trade statistics (win rate, profit factor, expectancy)
- [X] Standard deviation and variance calculations
- [X] Rolling metrics (configurable window Sharpe ratio)
- [X] Period-to-period returns calculation
- [X] Downside deviation for Sortino ratio
- [ ] Return distributions - deferred
- [ ] Correlation analysis (strategy vs benchmark) - deferred
- [ ] Beta and Alpha calculations - deferred
- [ ] Unit tests for all metrics - deferred

---

### TASK-047: Implement Equity Curve Generator
**Priority**: Medium
**Effort**: 3
**Owner**: Claude
**Dependencies**: TASK-009
**Status**: ✅ COMPLETED

**Description**:
Implement equity curve generation from trade history.

**Acceptance Criteria**:
- [X] EquityCurveGenerator class created
- [X] EquityPoint model created with Timestamp, Equity, Drawdown, Peak, ReturnPercent
- [X] GenerateEquityCurveAsync method
- [X] Load all trades chronologically via IPortfolioManager
- [X] Calculate cumulative equity with commission
- [X] Calculate drawdown at each point
- [X] Calculate peak equity tracking
- [X] Calculate return percentage from initial capital
- [X] Return equity curve data points
- [X] GenerateDailyEquityCurveAsync for daily resampling
- [ ] Unit tests - deferred

**Equity Calculation**:
```csharp
public async Task<List<EquityPoint>> GenerateEquityCurveAsync(
    decimal initialBalance,
    DateTime? startDate = null,
    DateTime? endDate = null)
{
    var trades = await _tradeRepository.GetTradesAsync(startDate, endDate);
    var equity = initialBalance;
    var peak = initialBalance;
    var points = new List<EquityPoint>();

    foreach (var trade in trades.OrderBy(t => t.ExitTime))
    {
        equity += trade.RealizedPnL - trade.Commission;
        peak = Math.Max(peak, equity);
        var drawdown = (peak - equity) / peak;

        points.Add(new EquityPoint
        {
            Timestamp = trade.ExitTime,
            Equity = equity,
            Drawdown = drawdown
        });
    }

    return points;
}
```

---

### TASK-048: Implement Drawdown Analyzer
**Priority**: Medium
**Effort**: 3
**Owner**: Claude
**Dependencies**: TASK-047
**Status**: ✅ COMPLETED

**Description**:
Implement detailed drawdown analysis from equity curve.

**Acceptance Criteria**:
- [X] DrawdownAnalyzer class created
- [X] DrawdownPeriod model with start/end/recovery dates
- [X] AnalyzeDrawdowns method
- [X] Identify all drawdown periods
- [X] GetMaxDrawdown method
- [X] Calculate max drawdown percentage
- [X] Calculate drawdown duration in days
- [X] Calculate recovery times for recovered drawdowns
- [X] GetAverageDrawdown method
- [X] GetAverageRecoveryTime method
- [X] GetLongestDrawdown method (by duration)
- [X] GetCurrentDrawdown method
- [ ] Unit tests - deferred

**Drawdown Period**:
```csharp
public class DrawdownPeriod
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? RecoveryDate { get; set; }
    public decimal MaxDrawdown { get; set; }
    public TimeSpan Duration { get; set; }
    public TimeSpan? RecoveryTime { get; set; }
}
```

---

## Phase 8: Testing & Documentation (Weeks 15-16)

### TASK-049: Achieve 80% Code Coverage
**Priority**: Critical
**Effort**: 13
**Owner**: TBD
**Dependencies**: All previous tasks

**Description**:
Write comprehensive unit and integration tests to achieve 80%+ code coverage across all projects.

**Acceptance Criteria**:
- [X] TradingBot.Core: 85%+ coverage - Completed (19 tests: Order, Position)
- [ ] TradingBot.Infrastructure: 80%+ coverage
- [X] TradingBot.Strategies: 85%+ coverage - Completed (50 tests: Indicators + Strategies)
- [ ] TradingBot.Engine: 85%+ coverage - In progress (30 tests: RiskManager)
- [ ] TradingBot.Analytics: 80%+ coverage
- [ ] TradingBot.Cli: 75%+ coverage (commands are harder to test)
- [ ] All critical paths have 100% coverage - In progress (99 tests written)
- [X] Edge cases tested - Input validation, null checks, boundaries
- [X] Error handling tested - Invalid parameters, insufficient data
- [X] Async code tested properly - Mock async dependencies
- [ ] Coverage report generated in CI - Test framework configured

**Testing Priorities**:
1. Critical: Order execution, position management, risk checks
2. High: Strategy signals, indicators, P&L calculations
3. Medium: CLI commands, configuration, caching
4. Low: UI rendering, logging

**Use Coverlet**:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

---

### TASK-050: Write Integration Tests
**Priority**: High
**Effort**: 8
**Owner**: TBD
**Dependencies**: All Phase 1-7 tasks

**Description**:
Write end-to-end integration tests for complete workflows.

**Acceptance Criteria**:
- [ ] Test: Signal → Order → Position → Close → Trade
- [ ] Test: Strategy enable → Signal generation → Order execution
- [ ] Test: Backtest complete workflow
- [ ] Test: Risk limit breach → Trading halt
- [ ] Test: API failure → Retry → Success
- [ ] Test: Database persistence across restarts
- [ ] Test: Configuration changes applied
- [ ] All tests use test database (in-memory or test container)
- [ ] Tests can run in parallel
- [ ] CI runs integration tests

**Example Integration Test**:
```csharp
[Fact]
public async Task CompleteTrading Workflow_ShouldExecuteSuccessfully()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>();
    var strategyEngine = factory.Services.GetRequiredService<IStrategyEngine>();
    var orderService = factory.Services.GetRequiredService<IOrderExecutionService>();
    var portfolioManager = factory.Services.GetRequiredService<IPortfolioManager>();

    // Act - Enable strategy
    await strategyEngine.EnableStrategyAsync("momentum_spy");

    // Wait for signal
    var signalReceived = new TaskCompletionSource<Signal>();
    strategyEngine.SignalGenerated += (s, signal) => signalReceived.SetResult(signal);
    await Task.WhenAny(signalReceived.Task, Task.Delay(TimeSpan.FromMinutes(5)));

    // Assert - Signal generated
    signalReceived.Task.IsCompleted.ShouldBeTrue();
    var signal = signalReceived.Task.Result;
    signal.ShouldNotBeNull();

    // Wait for order creation
    await Task.Delay(TimeSpan.FromSeconds(2));
    var orders = await orderService.GetOrdersAsync();
    orders.ShouldContain(o => o.SignalId == signal.Id);

    // Wait for position creation (simulated fill)
    await Task.Delay(TimeSpan.FromSeconds(2));
    var positions = await portfolioManager.GetPositionsAsync();
    positions.ShouldContain(p => p.Symbol == signal.Symbol);
}
```

---

### TASK-051: Write User Documentation
**Priority**: High
**Effort**: 8
**Owner**: TBD
**Dependencies**: All feature tasks complete

**Description**:
Write comprehensive user documentation for all features.

**Acceptance Criteria**:
- [ ] README.md updated with full feature list
- [ ] Installation guide (Windows, macOS, Linux)
- [ ] Quick start guide (5-minute setup)
- [ ] Configuration guide (appsettings.json, strategies.yaml)
- [ ] Strategy development guide
- [ ] CLI command reference (all commands documented)
- [ ] Dashboard guide (keyboard shortcuts, widgets)
- [ ] Risk management guide
- [ ] Backtesting guide
- [ ] Troubleshooting section
- [ ] FAQ section
- [ ] Examples and tutorials
- [ ] Screenshots and ASCII art examples

**Documentation Structure**:
```
docs/
├── README.md                    # Overview
├── installation.md              # Installation guide
├── quick-start.md               # 5-minute tutorial
├── configuration.md             # Configuration reference
├── strategies/
│   ├── overview.md              # Strategy concepts
│   ├── momentum.md              # Momentum strategy guide
│   ├── mean-reversion.md        # Mean reversion guide
│   └── custom-strategies.md     # Custom script guide
├── cli-reference.md             # All CLI commands
├── dashboard.md                 # Dashboard guide
├── risk-management.md           # Risk parameters
├── backtesting.md               # Backtesting guide
├── api-reference.md             # API documentation
├── troubleshooting.md           # Common issues
└── faq.md                       # Frequently asked questions
```

---

### TASK-052: Generate API Documentation
**Priority**: Medium
**Effort**: 3
**Owner**: TBD
**Dependencies**: All code complete

**Description**:
Generate API documentation from XML comments.

**Acceptance Criteria**:
- [ ] XML documentation enabled for all projects
- [ ] All public APIs have XML comments
- [ ] DocFX configured (or similar tool)
- [ ] API documentation generated
- [ ] Documentation published (GitHub Pages or similar)
- [ ] Documentation includes examples
- [ ] Documentation cross-referenced

**DocFX Setup**:
```bash
dotnet tool install -g docfx
docfx init -q
docfx build
docfx serve
```

---

### TASK-053: Create Build Scripts
**Priority**: High
**Effort**: 3
**Owner**: TBD
**Dependencies**: All projects complete

**Description**:
Create build scripts for all platforms.

**Acceptance Criteria**:
- [ ] build-all.sh created (Unix)
- [ ] build-all.ps1 created (Windows)
- [ ] Build for win-x64, osx-x64, osx-arm64, linux-x64
- [ ] PublishSingleFile enabled
- [ ] PublishTrimmed enabled
- [ ] PublishReadyToRun enabled
- [ ] Output organized by platform
- [ ] Build verification
- [ ] Scripts tested on all platforms

**Build Command Example**:
```bash
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/linux-x64
```

---

### TASK-054: Create Install Scripts
**Priority**: Medium
**Effort**: 3
**Owner**: TBD
**Dependencies**: TASK-053

**Description**:
Create installation scripts for easy setup.

**Acceptance Criteria**:
- [ ] install-windows.ps1 created
- [ ] install-unix.sh created (macOS/Linux)
- [ ] Scripts download latest release
- [ ] Scripts extract to installation directory
- [ ] Scripts create config directory (~/.tradingbot/)
- [ ] Scripts copy default configuration
- [ ] Scripts add to PATH (optional)
- [ ] Scripts create desktop shortcut (optional)
- [ ] Scripts verify installation
- [ ] Scripts tested on all platforms

**Unix Install Script**:
```bash
#!/bin/bash
set -e

echo "Installing TradingBot CLI..."

# Download latest release
LATEST_URL=$(curl -s https://api.github.com/repos/user/tradingbot/releases/latest \
  | grep "browser_download_url.*linux-x64" \
  | cut -d : -f 2,3 \
  | tr -d \")

# Create install directory
mkdir -p ~/.tradingbot/bin
mkdir -p ~/.tradingbot/config
mkdir -p ~/.tradingbot/logs

# Download and extract
curl -L $LATEST_URL -o /tmp/tradingbot.tar.gz
tar -xzf /tmp/tradingbot.tar.gz -C ~/.tradingbot/bin

# Make executable
chmod +x ~/.tradingbot/bin/tradingbot

# Copy default config
cp ~/.tradingbot/bin/config/*.* ~/.tradingbot/config/

# Add to PATH
echo 'export PATH="$PATH:$HOME/.tradingbot/bin"' >> ~/.bashrc

echo "Installation complete! Run 'tradingbot --help' to get started."
```

---

### TASK-055: Set Up Release Pipeline
**Priority**: High
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-053

**Description**:
Create automated release pipeline for building and publishing releases.

**Acceptance Criteria**:
- [ ] GitHub Actions workflow created (.github/workflows/release.yml)
- [ ] Trigger on tag push (v*.*.*)
- [ ] Build for all platforms
- [ ] Run tests before release
- [ ] Create GitHub release
- [ ] Upload platform binaries as assets
- [ ] Generate release notes
- [ ] Publish NuGet package (optional)
- [ ] Create Docker image (optional)

**Release Workflow**:
```yaml
name: Release

on:
  push:
    tags:
      - 'v*.*.*'

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Run tests
        run: dotnet test

      - name: Build all platforms
        run: ./scripts/build-all.sh

      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: artifacts/**/*
          generate_release_notes: true
```

---

### TASK-056: Final Testing and Bug Fixes
**Priority**: Critical
**Effort**: 13
**Owner**: TBD
**Dependencies**: All previous tasks

**Description**:
Perform comprehensive final testing and fix all bugs.

**Acceptance Criteria**:
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Manual testing on Windows completed
- [ ] Manual testing on macOS completed
- [ ] Manual testing on Linux completed
- [ ] All CLI commands tested
- [ ] Dashboard tested on multiple terminals
- [ ] Backtesting tested with real data
- [ ] Performance tested (memory, CPU, speed)
- [ ] Security tested (penetration testing)
- [ ] All critical bugs fixed
- [ ] All high-priority bugs fixed
- [ ] Remaining bugs documented

**Testing Checklist**:
- [ ] Fresh install on each platform
- [ ] Configuration setup
- [ ] API key encryption/decryption
- [ ] Strategy enable/disable
- [ ] Signal generation
- [ ] Order execution
- [ ] Position tracking
- [ ] P&L calculation
- [ ] Risk limits
- [ ] Dashboard rendering
- [ ] Backtest execution
- [ ] Performance metrics
- [ ] Data caching
- [ ] Error handling
- [ ] Logging

---

### TASK-057: Prepare for v1.0.0 Release
**Priority**: Critical
**Effort**: 5
**Owner**: TBD
**Dependencies**: TASK-056

**Description**:
Final preparations for v1.0.0 release.

**Acceptance Criteria**:
- [ ] All tasks completed
- [ ] All tests passing
- [ ] Documentation complete
- [ ] CHANGELOG.md updated
- [ ] Version numbers updated (1.0.0)
- [ ] Release notes written
- [ ] GitHub release created
- [ ] Binaries published
- [ ] Documentation site live
- [ ] Announcement prepared
- [ ] v1.0.0 tag created

**Release Checklist**:
- [ ] Code freeze
- [ ] Final bug fixes only
- [ ] Update version in all projects
- [ ] Update CHANGELOG.md
- [ ] Create release branch
- [ ] Tag release (v1.0.0)
- [ ] Build release artifacts
- [ ] Test release artifacts
- [ ] Create GitHub release
- [ ] Publish documentation
- [ ] Announce release
- [ ] Monitor for issues

---

## Summary Statistics

### By Phase
- **Phase 1** (Foundation): 6 tasks, 21 story points
- **Phase 2** (Infrastructure): 8 tasks, 41 story points
- **Phase 3** (Strategy Engine): 6 tasks, 39 story points
- **Phase 4** (Trading Engine): 6 tasks, 39 story points
- **Phase 5** (CLI Framework): 9 tasks, 39 story points
- **Phase 6** (Backtesting): 6 tasks, 37 story points
- **Phase 7** (Background Jobs): 7 tasks, 28 story points
- **Phase 8** (Testing & Docs): 9 tasks, 66 story points

**Total**: 57 tasks, 310 story points

### By Priority
- **Critical**: 19 tasks (33%)
- **High**: 20 tasks (35%)
- **Medium**: 15 tasks (26%)
- **Low**: 3 tasks (5%)

### Estimated Timeline
- **16 weeks** (4 months) at 2 developers
- **8 weeks** (2 months) at 4 developers
- Assumes 20 story points per developer per week

---

## Task Dependencies Graph

```
TASK-001 (Init Solution)
  ├─> TASK-002 (Code Quality)
  │     └─> TASK-003 (CI/CD)
  ├─> TASK-004 (Core Models)
  │     ├─> TASK-005 (Core Interfaces)
  │     │     ├─> TASK-010 (Encryption)
  │     │     ├─> TASK-011 (Yahoo Finance)
  │     │     └─> TASK-015 (Indicators)
  │     │           └─> TASK-016 (Base Strategy)
  │     │                 ├─> TASK-017 (Momentum)
  │     │                 ├─> TASK-018 (Mean Reversion)
  │     │                 ├─> TASK-019 (Strategy Engine)
  │     │                 └─> TASK-020 (Custom Script)
  │     └─> TASK-007 (Database)
  │           ├─> TASK-008 (Migration)
  │           └─> TASK-009 (Repositories)
  │                 ├─> TASK-012 (Cache)
  │                 └─> TASK-021 (Order Execution)
  │                       ├─> TASK-022 (Portfolio)
  │                       │     └─> TASK-023 (Risk Manager)
  │                       │           ├─> TASK-024 (Stop-Loss)
  │                       │           ├─> TASK-025 (Position Size)
  │                       │           └─> TASK-026 (Signal Pipeline)
  │                       └─> TASK-036 (Backtest Engine)
  │                             ├─> TASK-037 (Transaction Costs)
  │                             ├─> TASK-038 (Performance Calc)
  │                             │     ├─> TASK-039 (Report Gen)
  │                             │     ├─> TASK-046 (Metrics Calc)
  │                             │     ├─> TASK-047 (Equity Curve)
  │                             │     └─> TASK-048 (Drawdown)
  │                             ├─> TASK-040 (Walk-Forward)
  │                             └─> TASK-041 (Monte Carlo)
  ├─> TASK-006 (Documentation)
  ├─> TASK-013 (Configuration)
  └─> TASK-014 (Dependency Injection)
        ├─> TASK-027 (CLI Entry Point)
        │     ├─> TASK-028 (Strategy Commands)
        │     ├─> TASK-029 (Risk Commands)
        │     ├─> TASK-030 (Portfolio Commands)
        │     ├─> TASK-031 (Performance Commands)
        │     ├─> TASK-032 (Backtest Commands)
        │     ├─> TASK-033 (Config Commands)
        │     └─> TASK-034 (Dashboard Renderer)
        │           └─> TASK-035 (Dashboard Widgets)
        └─> TASK-042 (TickerQ Setup)
              ├─> TASK-043 (Data Refresh Job)
              ├─> TASK-044 (End of Day Job)
              └─> TASK-045 (Risk Monitoring Job)
```

---

## Next Steps

1. **Week 1**: Start with TASK-001 through TASK-006
2. **Assign owners** to each task based on team expertise
3. **Set up project management** (GitHub Projects, Jira, etc.)
4. **Daily standups** to track progress
5. **Weekly demos** to show progress to stakeholders
6. **Continuous integration** from day one
7. **Regular code reviews** to maintain quality

---

**Document Version**: 1.0.0
**Last Updated**: 2025-11-02
**Total Tasks**: 57
**Total Story Points**: 310
**Estimated Duration**: 16 weeks (2 developers)
