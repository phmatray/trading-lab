# Service Contracts: Blazor Server Application Services

**Feature**: 002-blazor-server-app
**Date**: 2025-11-07
**Purpose**: Define application service interfaces and contracts for the Blazor web layer

## Overview

Blazor Server applications use direct service injection rather than REST APIs. This document defines the service layer contracts that abstract the existing business logic (TradingBot.Core, Infrastructure, Engine, Analytics) for use by Blazor components.

---

## IDashboardService

**Purpose**: Aggregate dashboard data from multiple sources for efficient page load

**Namespace**: `TradingBot.Web.Services`

### GetDashboardDataAsync

**Signature**:
```csharp
Task<DashboardViewModel> GetDashboardDataAsync(CancellationToken cancellationToken = default);
```

**Returns**: Aggregated dashboard view model with all required data

**Throws**:
- `OperationCanceledException`: When operation is cancelled
- `InvalidOperationException`: When account data is unavailable

**Implementation Summary**:
```csharp
public async Task<DashboardViewModel> GetDashboardDataAsync(CancellationToken cancellationToken)
{
    var account = await _portfolioManager.GetAccountAsync(cancellationToken);
    var positions = await _positionRepository.GetOpenPositionsAsync(limit: 10, cancellationToken);
    var trades = await _tradeRepository.GetRecentTradesAsync(limit: 5, cancellationToken);
    var metrics = await _performanceService.GetCurrentMetricsAsync(cancellationToken);
    var riskSettings = await _riskSettingsRepository.GetCurrentAsync(cancellationToken);
    var strategies = await _strategyRepository.GetActiveStrategiesAsync(cancellationToken);

    return new DashboardViewModel
    {
        Account = account,
        OpenPositions = positions,
        RecentTrades = trades,
        PerformanceMetrics = metrics,
        RiskSettings = riskSettings,
        ActiveStrategies = strategies,
        ConnectionStatus = "connected",
        LastUpdated = DateTime.UtcNow
    };
}
```

**Usage**:
```csharp
@inject IDashboardService DashboardService

protected override async Task OnInitializedAsync()
{
    var dashboard = await DashboardService.GetDashboardDataAsync();
    // Render dashboard data
}
```

---

## IPortfolioService

**Purpose**: Portfolio and position management operations

**Namespace**: `TradingBot.Web.Services`

### GetTradeHistoryAsync

**Signature**:
```csharp
Task<PortfolioHistoryResult> GetTradeHistoryAsync(
    PortfolioHistoryFilter filter,
    CancellationToken cancellationToken = default);
```

**Parameters**:
- `filter` (PortfolioHistoryFilter): Filtering and pagination criteria

**Returns**: Paginated trade history result

**Implementation Summary**:
```csharp
public async Task<PortfolioHistoryResult> GetTradeHistoryAsync(
    PortfolioHistoryFilter filter,
    CancellationToken cancellationToken)
{
    var query = _tradeRepository.GetQueryable();

    // Apply filters
    if (filter.StartDate.HasValue)
        query = query.Where(t => t.ExitTime >= filter.StartDate.Value);

    if (filter.EndDate.HasValue)
        query = query.Where(t => t.ExitTime <= filter.EndDate.Value);

    if (!string.IsNullOrEmpty(filter.Symbol))
        query = query.Where(t => t.Symbol == filter.Symbol);

    if (!string.IsNullOrEmpty(filter.StrategyName))
        query = query.Where(t => t.StrategyName == filter.StrategyName);

    // Get total count
    var totalCount = await query.CountAsync(cancellationToken);

    // Apply pagination
    var trades = await query
        .OrderByDescending(t => t.ExitTime)
        .Skip((filter.PageNumber - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToListAsync(cancellationToken);

    return new PortfolioHistoryResult
    {
        Trades = trades,
        TotalCount = totalCount,
        PageNumber = filter.PageNumber,
        PageSize = filter.PageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
        HasPreviousPage = filter.PageNumber > 1,
        HasNextPage = filter.PageNumber * filter.PageSize < totalCount
    };
}
```

**Usage**:
```csharp
@inject IPortfolioService PortfolioService

var filter = new PortfolioHistoryFilter
{
    StartDate = DateTime.UtcNow.AddDays(-30),
    Symbol = "AAPL",
    PageNumber = 1,
    PageSize = 25
};

var result = await PortfolioService.GetTradeHistoryAsync(filter);
```

---

### ClosePositionAsync

**Signature**:
```csharp
Task<Trade> ClosePositionAsync(
    Guid positionId,
    CancellationToken cancellationToken = default);
```

**Parameters**:
- `positionId` (Guid): Position to close

**Returns**: Completed trade details

**Throws**:
- `ArgumentException`: When position ID is invalid
- `InvalidOperationException`: When position cannot be closed (e.g., not found)

**Implementation Summary**:
```csharp
public async Task<Trade> ClosePositionAsync(
    Guid positionId,
    CancellationToken cancellationToken)
{
    var position = await _positionRepository.GetByIdAsync(positionId, cancellationToken)
        ?? throw new InvalidOperationException($"Position {positionId} not found");

    // Close position via portfolio manager
    var trade = await _portfolioManager.ClosePositionAsync(position, cancellationToken);

    _logger.LogInformation(
        "Position {PositionId} ({Symbol}) closed. P&L: {PnL}",
        positionId,
        position.Symbol,
        trade.RealizedPnL);

    return trade;
}
```

**Usage**:
```csharp
@inject IPortfolioService PortfolioService

async Task HandleClosePosition(Guid positionId)
{
    try
    {
        var trade = await PortfolioService.ClosePositionAsync(positionId);
        ShowSuccessMessage($"Position closed. P&L: {trade.RealizedPnL:C2}");
    }
    catch (Exception ex)
    {
        ShowErrorMessage($"Failed to close position: {ex.Message}");
    }
}
```

---

### ExportTradeHistoryAsync

**Signature**:
```csharp
Task<byte[]> ExportTradeHistoryAsync(
    PortfolioHistoryFilter filter,
    ExportFormat format,
    CancellationToken cancellationToken = default);
```

**Parameters**:
- `filter` (PortfolioHistoryFilter): Filtering criteria (no pagination for export)
- `format` (ExportFormat): CSV or Excel

**Returns**: File bytes for download

**Implementation Summary**:
```csharp
public async Task<byte[]> ExportTradeHistoryAsync(
    PortfolioHistoryFilter filter,
    ExportFormat format,
    CancellationToken cancellationToken)
{
    // Get all trades matching filter (no pagination)
    var trades = await GetAllTradesMatchingFilterAsync(filter, cancellationToken);

    return format switch
    {
        ExportFormat.Csv => _csvExporter.ExportTrades(trades),
        ExportFormat.Excel => await _excelExporter.ExportTradesAsync(trades, cancellationToken),
        _ => throw new ArgumentException($"Unsupported export format: {format}")
    };
}
```

**Usage**:
```csharp
async Task HandleExport()
{
    var fileBytes = await PortfolioService.ExportTradeHistoryAsync(
        _currentFilter,
        ExportFormat.Csv);

    await JSRuntime.InvokeVoidAsync("downloadFile", "trades.csv", fileBytes);
}
```

---

## IPerformanceService

**Purpose**: Performance metrics and analytics calculations

**Namespace**: `TradingBot.Web.Services`

### GetCurrentMetricsAsync

**Signature**:
```csharp
Task<PerformanceMetrics> GetCurrentMetricsAsync(CancellationToken cancellationToken = default);
```

**Returns**: Current performance metrics calculated from all trade history

**Implementation Summary**:
```csharp
public async Task<PerformanceMetrics> GetCurrentMetricsAsync(CancellationToken cancellationToken)
{
    var trades = await _tradeRepository.GetAllTradesAsync(cancellationToken);

    if (!trades.Any())
    {
        return PerformanceMetrics.Empty;
    }

    return new PerformanceMetrics
    {
        TotalReturn = CalculateTotalReturn(trades),
        WinRate = CalculateWinRate(trades),
        TotalTrades = trades.Count,
        WinningTrades = trades.Count(t => t.RealizedPnL > 0),
        LosingTrades = trades.Count(t => t.RealizedPnL < 0),
        AverageWin = trades.Where(t => t.RealizedPnL > 0).Average(t => t.RealizedPnL),
        AverageLoss = trades.Where(t => t.RealizedPnL < 0).Average(t => t.RealizedPnL),
        ProfitFactor = CalculateProfitFactor(trades),
        SharpeRatio = CalculateSharpeRatio(trades),
        SortinoRatio = CalculateSortinoRatio(trades),
        CalmarRatio = CalculateCalmarRatio(trades),
        MaxDrawdown = CalculateMaxDrawdown(trades),
        // ... other metrics
    };
}
```

---

### GetEquityCurveAsync

**Signature**:
```csharp
Task<List<EquityCurveDataPoint>> GetEquityCurveAsync(
    DateTime? startDate = null,
    DateTime? endDate = null,
    CancellationToken cancellationToken = default);
```

**Parameters**:
- `startDate` (DateTime?): Optional start date filter
- `endDate` (DateTime?): Optional end date filter

**Returns**: List of equity curve data points for chart visualization

**Implementation Summary**:
```csharp
public async Task<List<EquityCurveDataPoint>> GetEquityCurveAsync(
    DateTime? startDate,
    DateTime? endDate,
    CancellationToken cancellationToken)
{
    var trades = await _tradeRepository.GetTradesInRangeAsync(
        startDate ?? DateTime.MinValue,
        endDate ?? DateTime.MaxValue,
        cancellationToken);

    var initialCapital = 100000m; // From configuration
    var equityCurve = new List<EquityCurveDataPoint>
    {
        new(startDate ?? trades.First().EntryTime, initialCapital)
    };

    var runningEquity = initialCapital;
    foreach (var trade in trades.OrderBy(t => t.ExitTime))
    {
        runningEquity += trade.RealizedPnL;
        equityCurve.Add(new EquityCurveDataPoint(trade.ExitTime, runningEquity));
    }

    return equityCurve;
}
```

**Usage**:
```csharp
@inject IPerformanceService PerformanceService

var equityCurve = await PerformanceService.GetEquityCurveAsync(
    startDate: DateTime.UtcNow.AddMonths(-6),
    endDate: DateTime.UtcNow);

// Pass to chart component
<EquityCurveChart Data="equityCurve" />
```

---

## IStrategyManagementService

**Purpose**: Strategy configuration and management

**Namespace**: `TradingBot.Web.Services`

### GetAllStrategiesAsync

**Signature**:
```csharp
Task<List<Strategy>> GetAllStrategiesAsync(CancellationToken cancellationToken = default);
```

**Returns**: All configured strategies (active and disabled)

---

### EnableStrategyAsync

**Signature**:
```csharp
Task EnableStrategyAsync(Guid strategyId, CancellationToken cancellationToken = default);
```

**Parameters**:
- `strategyId` (Guid): Strategy to enable

**Throws**:
- `InvalidOperationException`: When strategy not found

**Implementation Summary**:
```csharp
public async Task EnableStrategyAsync(Guid strategyId, CancellationToken cancellationToken)
{
    var strategy = await _strategyRepository.GetByIdAsync(strategyId, cancellationToken)
        ?? throw new InvalidOperationException($"Strategy {strategyId} not found");

    strategy.IsEnabled = true;
    await _strategyRepository.UpdateAsync(strategy, cancellationToken);

    _logger.LogInformation("Strategy {StrategyName} enabled", strategy.Name);

    // Notify strategy engine of change
    await _strategyEngine.ReloadStrategiesAsync(cancellationToken);
}
```

---

### DisableStrategyAsync

**Signature**:
```csharp
Task DisableStrategyAsync(Guid strategyId, CancellationToken cancellationToken = default);
```

**Parameters**:
- `strategyId` (Guid): Strategy to disable

---

## IRiskSettingsService

**Purpose**: Risk management configuration

**Namespace**: `TradingBot.Web.Services`

### GetCurrentSettingsAsync

**Signature**:
```csharp
Task<RiskSettings> GetCurrentSettingsAsync(CancellationToken cancellationToken = default);
```

**Returns**: Current risk settings for the account

---

### UpdateSettingsAsync

**Signature**:
```csharp
Task UpdateSettingsAsync(RiskSettings settings, CancellationToken cancellationToken = default);
```

**Parameters**:
- `settings` (RiskSettings): Updated risk settings

**Throws**:
- `ArgumentException`: When validation fails
- `InvalidOperationException`: When update fails

**Validation Rules**:
- Leverage: 1.0 ≤ x ≤ 10.0
- StopLossPercent: 0.1 ≤ x ≤ 50.0
- TakeProfitPercent: 0.1 ≤ x ≤ 100.0
- MaxPositionSizePercent: 1.0 ≤ x ≤ 100.0

**Implementation Summary**:
```csharp
public async Task UpdateSettingsAsync(RiskSettings settings, CancellationToken cancellationToken)
{
    // Validate settings
    ValidateRiskSettings(settings);

    // Update in repository
    await _riskSettingsRepository.UpdateAsync(settings, cancellationToken);

    _logger.LogInformation(
        "Risk settings updated: Leverage={Leverage}, StopLoss={StopLoss}%",
        settings.Leverage,
        settings.StopLossPercent);

    // Notify risk manager of changes
    _riskManager.ReloadSettings(settings);
}

private void ValidateRiskSettings(RiskSettings settings)
{
    if (settings.Leverage < 1.0m || settings.Leverage > 10.0m)
        throw new ArgumentException("Leverage must be between 1.0 and 10.0");

    if (settings.StopLossPercent < 0.1m || settings.StopLossPercent > 50.0m)
        throw new ArgumentException("Stop loss must be between 0.1% and 50%");

    // ... other validations
}
```

**Usage**:
```csharp
@inject IRiskSettingsService RiskSettingsService

async Task HandleSaveSettings()
{
    try
    {
        await RiskSettingsService.UpdateSettingsAsync(_riskSettings);
        ShowSuccessMessage("Settings saved successfully");
    }
    catch (ArgumentException ex)
    {
        ShowErrorMessage(ex.Message);
    }
}
```

---

## IBacktestService

**Purpose**: Backtest result retrieval and analysis

**Namespace**: `TradingBot.Web.Services`

### GetBacktestResultsAsync

**Signature**:
```csharp
Task<List<BacktestResult>> GetBacktestResultsAsync(CancellationToken cancellationToken = default);
```

**Returns**: All completed backtest results

---

### GetBacktestByIdAsync

**Signature**:
```csharp
Task<BacktestResult?> GetBacktestByIdAsync(
    Guid backtestId,
    CancellationToken cancellationToken = default);
```

**Parameters**:
- `backtestId` (Guid): Backtest result to retrieve

**Returns**: Detailed backtest result or null if not found

---

## Service Registration

**Program.cs**:
```csharp
// Register application services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<IStrategyManagementService, StrategyManagementService>();
builder.Services.AddScoped<IRiskSettingsService, RiskSettingsService>();
builder.Services.AddScoped<IBacktestService, BacktestService>();

// Existing infrastructure services
builder.Services.AddTradingBotServices(); // From TradingBot.Infrastructure
```

---

## Error Handling Strategy

### Service-Level Exceptions

**Common Exceptions**:
- `ArgumentException`: Invalid input parameters
- `InvalidOperationException`: Business rule violations
- `OperationCanceledException`: Cancelled operations
- `DbUpdateException`: Database update failures

**Logging Pattern**:
```csharp
public async Task<Trade> ClosePositionAsync(Guid positionId, CancellationToken cancellationToken)
{
    try
    {
        _logger.LogInformation("Closing position {PositionId}", positionId);

        var trade = await _portfolioManager.ClosePositionAsync(positionId, cancellationToken);

        _logger.LogInformation(
            "Position {PositionId} closed successfully. P&L: {PnL}",
            positionId,
            trade.RealizedPnL);

        return trade;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to close position {PositionId}", positionId);
        throw;
    }
}
```

**Component-Level Handling**:
```csharp
@code {
    async Task HandleOperation()
    {
        try
        {
            await Service.PerformOperationAsync();
        }
        catch (ArgumentException ex)
        {
            ShowErrorMessage($"Invalid input: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            ShowErrorMessage($"Operation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
            ShowErrorMessage("An unexpected error occurred. Please try again.");
        }
    }
}
```

---

## Testing Patterns

### Unit Testing Services

```csharp
public class DashboardServiceTests
{
    private readonly IPortfolioManager _portfolioManager;
    private readonly IPositionRepository _positionRepository;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _portfolioManager = A.Fake<IPortfolioManager>();
        _positionRepository = A.Fake<IPositionRepository>();
        // ... other fakes

        _service = new DashboardService(
            _portfolioManager,
            _positionRepository,
            /* ... */);
    }

    [Fact]
    public async Task GetDashboardDataAsync_ReturnsAggregatedData()
    {
        // Arrange
        var expectedAccount = new Account { Equity = 100000m };
        A.CallTo(() => _portfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(expectedAccount);

        // Act
        var result = await _service.GetDashboardDataAsync();

        // Assert
        result.Account.ShouldBe(expectedAccount);
        result.LastUpdated.ShouldBeInRange(
            DateTime.UtcNow.AddSeconds(-5),
            DateTime.UtcNow);
    }
}
```

---

## Summary

**Service Layer Benefits**:
- Abstracts business logic from UI components
- Aggregates data from multiple repositories/managers
- Provides transaction boundaries and error handling
- Simplifies component code (components focus on rendering)
- Testable in isolation from Blazor infrastructure

**Service Responsibilities**:
- Data aggregation and transformation
- Business rule enforcement
- Transaction management
- Logging and error handling
- Caching (where appropriate)

**Service Dependencies**:
- TradingBot.Core: Domain models and interfaces
- TradingBot.Infrastructure: Repositories and data access
- TradingBot.Engine: Portfolio manager, strategy engine
- TradingBot.Analytics: Performance calculations

All services are registered as **Scoped** in the DI container to align with Blazor Server circuit lifetime.
