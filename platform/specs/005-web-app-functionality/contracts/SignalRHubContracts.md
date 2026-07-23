# SignalR Hub Contracts

**Feature**: 005-web-app-functionality
**Date**: 2025-01-14
**Purpose**: Define SignalR hub methods and events for real-time updates

## Hub Endpoint

**URL**: `/tradinghub`
**Protocol**: WebSocket (with fallback to Server-Sent Events, then Long Polling)
**Serialization**: MessagePack (binary, more efficient than JSON)

## Hub Interface

### ITradingHubClient

Client-callable methods (server → client push events):

```csharp
namespace TradingBot.Web.Hubs;

using TradingBot.Core.Models.Trading;
using TradingBot.Core.Models.Backtest;
using TradingBot.Core.Models.Configuration;

/// <summary>
/// Defines the contract for SignalR events that the server can push to connected clients.
/// </summary>
public interface ITradingHubClient
{
    /// <summary>
    /// Called when a new position is opened.
    /// </summary>
    /// <param name="position">The newly opened position.</param>
    Task OnPositionOpened(Position position);

    /// <summary>
    /// Called when a position is closed.
    /// </summary>
    /// <param name="positionId">The ID of the closed position.</param>
    /// <param name="trade">The resulting trade record with realized P&amp;L.</param>
    Task OnPositionClosed(Guid positionId, Trade trade);

    /// <summary>
    /// Called when portfolio equity metrics are updated (every 2 seconds).
    /// </summary>
    /// <param name="totalEquity">Total account equity (cash + unrealized P&amp;L).</param>
    /// <param name="unrealizedPnL">Total unrealized profit/loss from open positions.</param>
    /// <param name="realizedPnL">Total realized profit/loss from closed trades.</param>
    Task OnEquityUpdated(decimal totalEquity, decimal unrealizedPnL, decimal realizedPnL);

    /// <summary>
    /// Called during backtest execution to report progress.
    /// </summary>
    /// <param name="backtestId">The ID of the running backtest.</param>
    /// <param name="progressPercent">Progress percentage (0-100).</param>
    /// <param name="statusMessage">Human-readable status message (e.g., "Processing 2020-06-15...").</param>
    Task OnBacktestProgress(string backtestId, int progressPercent, string statusMessage);

    /// <summary>
    /// Called when a backtest completes successfully.
    /// </summary>
    /// <param name="backtestId">The ID of the completed backtest.</param>
    /// <param name="result">The complete backtest result with metrics and trades.</param>
    Task OnBacktestCompleted(string backtestId, BacktestResult result);

    /// <summary>
    /// Called when a backtest fails with an error.
    /// </summary>
    /// <param name="backtestId">The ID of the failed backtest.</param>
    /// <param name="errorMessage">User-friendly error message.</param>
    Task OnBacktestFailed(string backtestId, string errorMessage);

    /// <summary>
    /// Called when risk settings are modified.
    /// </summary>
    /// <param name="newSettings">The updated risk settings.</param>
    Task OnRiskSettingsChanged(RiskSettings newSettings);

    /// <summary>
    /// Called when a strategy's configuration parameters are modified.
    /// </summary>
    /// <param name="strategyName">The name of the configured strategy.</param>
    /// <param name="parameters">Dictionary of parameter names to new values.</param>
    Task OnStrategyConfigurationChanged(string strategyName, Dictionary<string, object> parameters);

    /// <summary>
    /// Called when a strategy is enabled or disabled.
    /// </summary>
    /// <param name="strategyName">The name of the strategy.</param>
    /// <param name="isEnabled">True if strategy was enabled, false if disabled.</param>
    Task OnStrategyStatusChanged(string strategyName, bool isEnabled);
}
```

### TradingHub

Server-side hub implementation:

```csharp
namespace TradingBot.Web.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// SignalR hub for real-time trading updates.
/// </summary>
public class TradingHub : Hub<ITradingHubClient>
{
    // No server-callable methods needed for this feature
    // All communication is server → client push via IHubContext<TradingHub, ITradingHubClient>
}
```

## Event Triggering

### OnPositionOpened

**Triggered by**: `OrderExecutionService.ExecuteAsync()` when order fills and creates new position
**Frequency**: Ad-hoc (when trades execute)
**Payload size**: ~500 bytes (Position object)

**Example**:
```csharp
// In OrderExecutionService
await _hubContext.Clients.All.OnPositionOpened(newPosition);
```

---

### OnPositionClosed

**Triggered by**: `PortfolioService.ClosePositionAsync()` after successfully closing position
**Frequency**: Ad-hoc (when user closes position or stop-loss/take-profit triggers)
**Payload size**: ~800 bytes (Position ID + Trade object)

**Example**:
```csharp
// In PortfolioService
await _hubContext.Clients.All.OnPositionClosed(positionId, trade);
```

---

### OnEquityUpdated

**Triggered by**: `RealtimeUpdateService` background worker (timer-based)
**Frequency**: Every 2 seconds
**Payload size**: ~100 bytes (3 decimal values)

**Example**:
```csharp
// In RealtimeUpdateService
_updateTimer = new Timer(async _ =>
{
    var equity = await _portfolioManager.GetTotalEquityAsync();
    var unrealizedPnL = await _portfolioManager.GetUnrealizedPnLAsync();
    var realizedPnL = await _portfolioManager.GetRealizedPnLAsync();
    await _hubContext.Clients.All.OnEquityUpdated(equity, unrealizedPnL, realizedPnL);
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
```

---

### OnBacktestProgress

**Triggered by**: `BacktestExecutionWorker` during backtest execution
**Frequency**: Ad-hoc (every 10% progress increment)
**Payload size**: ~200 bytes (string + int + string)

**Example**:
```csharp
// In BacktestExecutionWorker
for (int i = 0; i < totalDays; i++)
{
    // Process day...
    if (i % (totalDays / 10) == 0) // Report every 10%
    {
        var progress = (int)((i / (double)totalDays) * 100);
        await _hubContext.Clients.All.OnBacktestProgress(backtestId, progress, $"Processing {currentDate:yyyy-MM-dd}...");
    }
}
```

---

### OnBacktestCompleted

**Triggered by**: `BacktestExecutionWorker` after successful backtest completion
**Frequency**: Ad-hoc (end of backtest)
**Payload size**: ~5-50 KB (BacktestResult with trades and equity curve JSON)

**Example**:
```csharp
// In BacktestExecutionWorker
var result = await CalculateBacktestResultAsync(backtestId);
await _backtestRepository.SaveAsync(result);
await _hubContext.Clients.All.OnBacktestCompleted(backtestId, result);
```

---

### OnBacktestFailed

**Triggered by**: `BacktestExecutionWorker` on exception during backtest
**Frequency**: Ad-hoc (on error)
**Payload size**: ~200 bytes (string + error message)

**Example**:
```csharp
// In BacktestExecutionWorker (catch block)
catch (Exception ex)
{
    _logger.LogError(ex, "Backtest {BacktestId} failed", backtestId);
    await _hubContext.Clients.All.OnBacktestFailed(backtestId, "Insufficient market data for the selected date range");
}
```

---

### OnRiskSettingsChanged

**Triggered by**: `RiskSettingsService.SaveRiskSettingsAsync()` after successful save
**Frequency**: Ad-hoc (when user modifies settings)
**Payload size**: ~300 bytes (RiskSettings object)

**Example**:
```csharp
// In RiskSettingsService
await _riskSettingsRepository.UpdateAsync(settings);
_riskManager.ReloadSettings(settings);
await _hubContext.Clients.All.OnRiskSettingsChanged(settings);
```

---

### OnStrategyConfigurationChanged

**Triggered by**: `StrategyManagementService.ConfigureStrategyAsync()` after successful save
**Frequency**: Ad-hoc (when user modifies strategy parameters)
**Payload size**: ~500 bytes (string + dictionary)

**Example**:
```csharp
// In StrategyManagementService
await _strategyConfigRepository.UpsertAsync(config);
strategy.ApplyParameters(parameters);
await _hubContext.Clients.All.OnStrategyConfigurationChanged(strategyName, parameters);
```

---

### OnStrategyStatusChanged

**Triggered by**: `StrategyManagementService.EnableStrategyAsync()` / `DisableStrategyAsync()`
**Frequency**: Ad-hoc (when user toggles strategy)
**Payload size**: ~100 bytes (string + bool)

**Example**:
```csharp
// In StrategyManagementService
strategy.Enable(); // or strategy.Disable()
await _hubContext.Clients.All.OnStrategyStatusChanged(strategyName, strategy.IsEnabled);
```

---

## Client Subscription Pattern

### Blazor Component Example

```razor
@implements IAsyncDisposable
@inject HubConnection HubConnection

<div>
    <p>Total Equity: @_totalEquity.ToString("C")</p>
    <p>Unrealized P&L: @_unrealizedPnL.ToString("C")</p>
</div>

@code {
    private decimal _totalEquity;
    private decimal _unrealizedPnL;
    private decimal _realizedPnL;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to equity updates
        HubConnection.On<decimal, decimal, decimal>("OnEquityUpdated", (equity, unrealized, realized) =>
        {
            _totalEquity = equity;
            _unrealizedPnL = unrealized;
            _realizedPnL = realized;
            InvokeAsync(StateHasChanged);
        });

        // Subscribe to position events
        HubConnection.On<Guid, Trade>("OnPositionClosed", (positionId, trade) =>
        {
            _toastService.ShowSuccess($"Position closed: {trade.Symbol} with P&L {trade.RealizedPnL:C}");
            InvokeAsync(StateHasChanged);
        });

        // Start connection if not already connected
        if (HubConnection.State == HubConnectionState.Disconnected)
        {
            await HubConnection.StartAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Unsubscribe to prevent memory leaks
        HubConnection.Remove("OnEquityUpdated");
        HubConnection.Remove("OnPositionClosed");
    }
}
```

## Connection Configuration

### Program.cs Registration

```csharp
// Server-side (in TradingBot.Web/Program.cs)
builder.Services.AddSignalR()
    .AddMessagePackProtocol(); // Binary serialization for efficiency

app.MapHub<TradingHub>("/tradinghub");
```

### Client-side Connection (in DI)

```csharp
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    var hubUrl = navigationManager.ToAbsoluteUri("/tradinghub");

    return new HubConnectionBuilder()
        .WithUrl(hubUrl)
        .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
        .AddMessagePackProtocol()
        .Build();
});
```

## Error Handling

### Connection Lost

When connection is lost, the client will automatically attempt to reconnect using the configured retry policy:
- Immediate retry (0s delay)
- Retry after 2 seconds
- Retry after 5 seconds
- Retry after 10 seconds
- Continue retrying every 10 seconds until reconnected

During reconnection, show a toast notification: "Reconnecting to server..."

### Reconnection Success

On successful reconnection, re-synchronize data:
```csharp
HubConnection.Reconnected += async (connectionId) =>
{
    await RefreshDashboardData();
    _toastService.ShowSuccess("Reconnected to server");
};
```

## Performance Characteristics

| Event | Frequency | Payload Size | Recipients | Notes |
|-------|-----------|--------------|------------|-------|
| OnEquityUpdated | Every 2s | ~100 bytes | All | Throttled to avoid excessive updates |
| OnPositionOpened | Ad-hoc | ~500 bytes | All | Low frequency (few per day) |
| OnPositionClosed | Ad-hoc | ~800 bytes | All | Low frequency |
| OnBacktestProgress | Every 10% | ~200 bytes | All | 10 events per backtest |
| OnBacktestCompleted | Ad-hoc | 5-50 KB | All | Large payload due to trades JSON |
| OnBacktestFailed | Ad-hoc | ~200 bytes | All | Rare |
| OnRiskSettingsChanged | Ad-hoc | ~300 bytes | All | Very rare |
| OnStrategyConfigurationChanged | Ad-hoc | ~500 bytes | All | Rare |
| OnStrategyStatusChanged | Ad-hoc | ~100 bytes | All | Occasional |

**Total bandwidth estimate**: ~50 bytes/second during normal operation (equity updates), bursts of 50 KB during backtest completion.

## Testing SignalR Events

### Unit Testing (Mock IHubContext)

```csharp
[Fact]
public async Task ClosePositionAsync_Success_PublishesSignalREvent()
{
    // Arrange
    var hubContext = A.Fake<IHubContext<TradingHub, ITradingHubClient>>();
    var allClients = A.Fake<ITradingHubClient>();
    A.CallTo(() => hubContext.Clients.All).Returns(allClients);

    var service = new PortfolioService(hubContext, _portfolioManager, _logger);

    // Act
    await service.ClosePositionAsync(positionId);

    // Assert
    A.CallTo(() => allClients.OnPositionClosed(positionId, A<Trade>._)).MustHaveHappenedOnceExactly();
}
```

### Integration Testing (TestServer + SignalR Client)

```csharp
[Fact]
public async Task SignalR_OnEquityUpdated_BroadcastsToClients()
{
    // Arrange
    using var webHost = new WebApplicationFactory<Program>();
    var hubUrl = webHost.Server.BaseAddress!.ToString().TrimEnd('/') + "/tradinghub";

    var connection = new HubConnectionBuilder()
        .WithUrl(hubUrl, options => options.HttpMessageHandlerFactory = _ => webHost.Server.CreateHandler())
        .Build();

    decimal? receivedEquity = null;
    connection.On<decimal, decimal, decimal>("OnEquityUpdated", (equity, unrealized, realized) =>
    {
        receivedEquity = equity;
    });

    await connection.StartAsync();

    // Act - trigger update from server
    var service = webHost.Services.GetRequiredService<IPortfolioService>();
    await service.ClosePositionAsync(positionId);

    // Assert
    await Task.Delay(100); // Wait for SignalR propagation
    receivedEquity.ShouldNotBeNull();
}
```
