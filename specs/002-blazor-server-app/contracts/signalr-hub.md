# SignalR Hub Contract: TradingHub

**Feature**: 002-blazor-server-app
**Date**: 2025-11-07
**Purpose**: Define SignalR hub methods and client callback contracts for real-time trading data updates

## Overview

The `TradingHub` provides real-time bidirectional communication between the Blazor Server application and connected clients. It broadcasts trading data updates (account, positions, trades) and allows clients to subscribe/unsubscribe from specific data streams.

**Hub URL**: `/tradinghub`
**Transport**: WebSockets (preferred), Long Polling (fallback)
**Protocol**: JSON (default), MessagePack (optional for performance)

---

## Server-to-Client Methods (ITradingClient Interface)

These methods are called by the server to push updates to connected clients. Clients must implement handlers for these callbacks.

### ReceiveAccountUpdate

**Purpose**: Broadcast account state changes to subscribed clients

**Signature**:
```csharp
Task ReceiveAccountUpdate(Account account);
```

**Parameters**:
- `account` (Account): Current account state with equity, cash, P&L, etc.

**Frequency**: Every 100ms during market hours (when account state changes)

**Client Handler Example** (JavaScript):
```javascript
connection.on('ReceiveAccountUpdate', (account) => {
    console.log('Account update:', account);
    updateDashboardUI(account);
});
```

**Client Handler Example** (Blazor C#):
```csharp
_hubConnection.On<Account>("ReceiveAccountUpdate", async (account) =>
{
    _currentAccount = account;
    await InvokeAsync(StateHasChanged);
});
```

**Payload Example**:
```json
{
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "equity": 105000.00,
  "cash": 100000.00,
  "positionValue": 5000.00,
  "buyingPower": 100000.00,
  "unrealizedPnL": 500.00,
  "realizedPnL": 4500.00,
  "createdAt": "2025-01-01T00:00:00Z",
  "updatedAt": "2025-11-07T12:34:56Z"
}
```

---

### ReceivePositionUpdate

**Purpose**: Notify clients of position changes (new position opened, existing position updated/closed)

**Signature**:
```csharp
Task ReceivePositionUpdate(Position position);
```

**Parameters**:
- `position` (Position): Updated position data

**Frequency**: When position is opened, updated (price change), or closed

**Client Handler Example**:
```csharp
_hubConnection.On<Position>("ReceivePositionUpdate", async (position) =>
{
    UpdatePositionInList(position);
    await InvokeAsync(StateHasChanged);
});
```

**Payload Example**:
```json
{
  "positionId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "symbol": "AAPL",
  "side": "Buy",
  "quantity": 100,
  "entryPrice": 150.00,
  "currentPrice": 155.00,
  "unrealizedPnL": 500.00,
  "unrealizedPnLPercent": 3.33,
  "strategyName": "MomentumStrategy",
  "openedAt": "2025-11-07T10:00:00Z",
  "updatedAt": "2025-11-07T12:34:56Z"
}
```

---

### ReceiveTradeUpdate

**Purpose**: Notify clients when a new trade is completed

**Signature**:
```csharp
Task ReceiveTradeUpdate(Trade trade);
```

**Parameters**:
- `trade` (Trade): Completed trade details

**Frequency**: When a position is closed and trade is recorded

**Client Handler Example**:
```csharp
_hubConnection.On<Trade>("ReceiveTradeUpdate", async (trade) =>
{
    _recentTrades.Insert(0, trade);
    if (_recentTrades.Count > 5)
        _recentTrades.RemoveAt(5);

    await InvokeAsync(StateHasChanged);
});
```

**Payload Example**:
```json
{
  "tradeId": "a1b2c3d4-e5f6-4789-a012-b3c4d5e6f7a8",
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "symbol": "TSLA",
  "side": "Buy",
  "quantity": 50,
  "entryPrice": 200.00,
  "exitPrice": 210.00,
  "entryTime": "2025-11-06T14:30:00Z",
  "exitTime": "2025-11-07T09:15:00Z",
  "realizedPnL": 485.00,
  "realizedPnLPercent": 4.85,
  "commission": 15.00,
  "strategyName": "MomentumStrategy",
  "duration": "18:45:00"
}
```

---

### ReceiveConnectionStatus

**Purpose**: Inform client of server-side connection state changes

**Signature**:
```csharp
Task ReceiveConnectionStatus(string status);
```

**Parameters**:
- `status` (string): Status message ("connected", "reconnecting", "disconnected", "error")

**Frequency**: On connection state changes

**Client Handler Example**:
```csharp
_hubConnection.On<string>("ReceiveConnectionStatus", async (status) =>
{
    _connectionStatus = status;
    await InvokeAsync(StateHasChanged);
});
```

**Payload Example**:
```json
"connected"
```

---

## Client-to-Server Methods (TradingHub Methods)

These methods are invoked by clients to request actions or subscribe to data streams.

### SubscribeToAccountUpdates

**Purpose**: Subscribe the current connection to receive account updates

**Signature**:
```csharp
Task SubscribeToAccountUpdates();
```

**Parameters**: None

**Returns**: Task (async operation)

**Client Invocation Example**:
```csharp
await _hubConnection.InvokeAsync("SubscribeToAccountUpdates");
```

**Server Implementation**:
```csharp
public async Task SubscribeToAccountUpdates()
{
    await Groups.AddToGroupAsync(Context.ConnectionId, "AccountUpdates");
}
```

**Authorization**: Requires authenticated user

---

### UnsubscribeFromAccountUpdates

**Purpose**: Unsubscribe the current connection from account updates

**Signature**:
```csharp
Task UnsubscribeFromAccountUpdates();
```

**Parameters**: None

**Returns**: Task (async operation)

**Client Invocation Example**:
```csharp
await _hubConnection.InvokeAsync("UnsubscribeFromAccountUpdates");
```

**Server Implementation**:
```csharp
public async Task UnsubscribeFromAccountUpdates()
{
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AccountUpdates");
}
```

---

### GetCurrentDashboardData

**Purpose**: Request full dashboard snapshot (on initial load or reconnect)

**Signature**:
```csharp
Task<DashboardViewModel> GetCurrentDashboardData();
```

**Parameters**: None

**Returns**: `DashboardViewModel` with current account, positions, trades, metrics

**Client Invocation Example**:
```csharp
var dashboardData = await _hubConnection.InvokeAsync<DashboardViewModel>("GetCurrentDashboardData");
```

**Response Example**:
```json
{
  "account": { /* Account object */ },
  "openPositions": [ /* List<Position> */ ],
  "recentTrades": [ /* List<Trade> (last 5) */ ],
  "performanceMetrics": { /* PerformanceMetrics object */ },
  "riskSettings": { /* RiskSettings object */ },
  "activeStrategies": [ /* List<Strategy> */ ],
  "connectionStatus": "connected",
  "lastUpdated": "2025-11-07T12:34:56Z"
}
```

**Authorization**: Requires authenticated user

---

## Connection Lifecycle Events

### OnConnectedAsync

**Purpose**: Server-side event when client connects

**Server Implementation**:
```csharp
public override async Task OnConnectedAsync()
{
    _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);

    // Send initial dashboard data
    var dashboardData = await _dashboardService.GetDashboardDataAsync(CancellationToken.None);
    await Clients.Caller.ReceiveAccountUpdate(dashboardData.Account);

    await base.OnConnectedAsync();
}
```

**Client-Side Handling**:
```csharp
_hubConnection.On<Account>("ReceiveAccountUpdate", HandleAccountUpdate);
await _hubConnection.StartAsync();
// Server automatically calls OnConnectedAsync
```

---

### OnDisconnectedAsync

**Purpose**: Server-side event when client disconnects

**Server Implementation**:
```csharp
public override async Task OnDisconnectedAsync(Exception? exception)
{
    _logger.LogInformation(
        "Client disconnected: {ConnectionId}, Exception: {Exception}",
        Context.ConnectionId,
        exception?.Message);

    await base.OnDisconnectedAsync(exception);
}
```

**Client-Side Handling**:
```csharp
_hubConnection.Closed += async (error) =>
{
    _connectionStatus = "disconnected";
    await InvokeAsync(StateHasChanged);
};
```

---

## Client Reconnection Strategy

### Automatic Reconnection

**Configuration** (Client-side):
```csharp
_hubConnection = new HubConnectionBuilder()
    .WithUrl("/tradinghub")
    .WithAutomaticReconnect(new[] {
        TimeSpan.Zero,           // First retry: immediate
        TimeSpan.FromSeconds(2), // Second retry: 2s
        TimeSpan.FromSeconds(10), // Third retry: 10s
        TimeSpan.FromSeconds(30)  // Fourth+ retry: 30s
    })
    .Build();
```

**Events**:
```csharp
_hubConnection.Reconnecting += (error) =>
{
    _connectionStatus = "reconnecting";
    StateHasChanged();
    return Task.CompletedTask;
};

_hubConnection.Reconnected += async (connectionId) =>
{
    _connectionStatus = "connected";

    // Re-subscribe to updates after reconnect
    await _hubConnection.InvokeAsync("SubscribeToAccountUpdates");

    // Refresh full dashboard data
    var dashboardData = await _hubConnection.InvokeAsync<DashboardViewModel>("GetCurrentDashboardData");
    UpdateDashboard(dashboardData);

    StateHasChanged();
};
```

---

## Performance Considerations

### Update Throttling

**Server-Side**:
- Account updates: Maximum 10 updates/second (100ms interval)
- Position updates: On-demand (when price changes or position modified)
- Trade updates: Immediate (infrequent event)

**Client-Side**:
- Debounce UI updates to prevent excessive re-renders
- Use `StateHasChanged()` sparingly in rapid update scenarios

### Broadcasting Strategy

**Group-Based Broadcasting**:
```csharp
// Broadcast to all subscribed clients
await _hubContext.Clients.Group("AccountUpdates").ReceiveAccountUpdate(account);

// Broadcast to specific user (multi-tab support)
await _hubContext.Clients.User(userId).ReceivePositionUpdate(position);

// Broadcast to specific connection
await _hubContext.Clients.Client(connectionId).ReceiveTradeUpdate(trade);
```

**Selective Updates**:
- Only broadcast to clients that have subscribed (via `SubscribeToAccountUpdates`)
- Use SignalR groups to prevent unnecessary traffic

---

## Error Handling

### Server-Side Errors

**Hub Method Errors**:
```csharp
public async Task<DashboardViewModel> GetCurrentDashboardData()
{
    try
    {
        return await _dashboardService.GetDashboardDataAsync(CancellationToken.None);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching dashboard data for {ConnectionId}", Context.ConnectionId);
        throw new HubException("Failed to load dashboard data. Please try again.");
    }
}
```

**Client Handling**:
```csharp
try
{
    var data = await _hubConnection.InvokeAsync<DashboardViewModel>("GetCurrentDashboardData");
}
catch (HubException ex)
{
    ShowErrorNotification(ex.Message);
}
```

### Connection Errors

**Client Handling**:
```csharp
try
{
    await _hubConnection.StartAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "SignalR connection failed");
    _connectionStatus = "error";
    StateHasChanged();
}
```

---

## Security

### Authentication

**Server Configuration**:
```csharp
app.MapHub<TradingHub>("/tradinghub")
    .RequireAuthorization(); // Require authenticated users
```

**Client Configuration**:
```csharp
_hubConnection = new HubConnectionBuilder()
    .WithUrl("/tradinghub", options =>
    {
        options.AccessTokenProvider = async () =>
        {
            // Return auth token if using token-based auth
            return await GetAccessTokenAsync();
        };
    })
    .Build();
```

### Authorization

**Method-Level Authorization**:
```csharp
[Authorize(Roles = "Trader")]
public async Task SubscribeToAccountUpdates()
{
    // Only users with "Trader" role can subscribe
    await Groups.AddToGroupAsync(Context.ConnectionId, "AccountUpdates");
}
```

---

## Testing

### Unit Testing Hub Methods

```csharp
[Fact]
public async Task SubscribeToAccountUpdates_AddsConnectionToGroup()
{
    // Arrange
    var hubContext = A.Fake<HubCallerContext>();
    var groups = A.Fake<IGroupManager>();
    var hub = new TradingHub(_portfolioManager, _logger)
    {
        Context = hubContext,
        Groups = groups
    };

    A.CallTo(() => hubContext.ConnectionId).Returns("test-connection-id");

    // Act
    await hub.SubscribeToAccountUpdates();

    // Assert
    A.CallTo(() => groups.AddToGroupAsync("test-connection-id", "AccountUpdates", default))
        .MustHaveHappenedOnceExactly();
}
```

### Integration Testing

```csharp
[Fact]
public async Task TradingHub_BroadcastsAccountUpdate_ToSubscribedClients()
{
    // Arrange
    var hubConnection = new HubConnectionBuilder()
        .WithUrl("http://localhost:5000/tradinghub")
        .Build();

    Account? receivedAccount = null;
    hubConnection.On<Account>("ReceiveAccountUpdate", account =>
    {
        receivedAccount = account;
    });

    await hubConnection.StartAsync();
    await hubConnection.InvokeAsync("SubscribeToAccountUpdates");

    // Act
    // Trigger server-side broadcast (via background service or direct call)
    await Task.Delay(500);

    // Assert
    receivedAccount.ShouldNotBeNull();
    receivedAccount.Equity.ShouldBeGreaterThan(0);
}
```

---

## Summary

**Hub Methods Summary**:

| Method | Direction | Purpose | Frequency |
|--------|-----------|---------|-----------|
| `ReceiveAccountUpdate` | Server→Client | Account state changes | Every 100ms |
| `ReceivePositionUpdate` | Server→Client | Position changes | On-demand |
| `ReceiveTradeUpdate` | Server→Client | New trade completed | On-demand |
| `ReceiveConnectionStatus` | Server→Client | Connection state | On state change |
| `SubscribeToAccountUpdates` | Client→Server | Subscribe to updates | Once on connect |
| `UnsubscribeFromAccountUpdates` | Client→Server | Unsubscribe | On disconnect |
| `GetCurrentDashboardData` | Client→Server | Full dashboard snapshot | On connect/reconnect |

**Key Features**:
- Real-time account, position, and trade updates
- Group-based subscription model for efficiency
- Automatic reconnection with exponential backoff
- Full dashboard snapshot on connect/reconnect
- Strongly-typed client interface (`ITradingClient`)
- Authentication and authorization support
- Comprehensive error handling and logging