# Research: Blazor Server Application Implementation Guide

## Decision: Blazor Server + Tailwind CSS Integration

**Rationale**: Tailwind CSS provides a utility-first approach with excellent developer experience in Blazor Server applications. The .NET 9 ecosystem has matured support for Tailwind integration through multiple approaches, with the MSBuild target pattern emerging as the best practice for seamless development and production builds.

**Implementation Approach**:

### Setup Steps

1. **Install Tailwind CSS Dependencies**
   - Use NPM: `npm install -D tailwindcss @tailwindcss/cli`
   - Alternative for non-Node projects: Download standalone Tailwind CLI executable

2. **Configuration Files**
   - Create `tailwind.config.js` in project root:
   ```javascript
   module.exports = {
     content: [
       "./Pages/**/*.razor",
       "./Shared/**/*.razor",
       "./Components/**/*.razor",
       "./**/*.html"
     ],
     theme: {
       extend: {},
     },
     plugins: [],
   }
   ```
   - Create input CSS file at `wwwroot/css/site.css`:
   ```css
   @import "tailwindcss";
   ```

3. **MSBuild Integration (Recommended)**
   - Add to `.csproj` file:
   ```xml
   <ItemGroup>
     <UpToDateCheckBuilt Include="wwwroot/css/site.css" Set="Css" />
     <UpToDateCheckBuilt Include="tailwind.config.js" Set="Css" />
   </ItemGroup>

   <Target Name="Tailwind" BeforeTargets="Build">
     <Exec Command="npm run css:build"/>
   </Target>
   ```
   - Add to `package.json`:
   ```json
   {
     "scripts": {
       "css:watch": "tailwindcss -i ./wwwroot/css/site.css -o ./wwwroot/css/styles.css --watch",
       "css:build": "tailwindcss -i ./wwwroot/css/site.css -o ./wwwroot/css/styles.css --minify"
     }
   }
   ```

4. **Development Workflow**
   - Run `npm run css:watch` in separate terminal during development
   - CSS rebuilds automatically as .razor files are modified
   - For production: MSBuild automatically minifies on `dotnet publish`

5. **Reference Generated CSS in Layout**
   - Update `App.razor` or `Layout.razor`:
   ```html
   <link href="css/styles.css" rel="stylesheet" />
   ```

### Component Organization Best Practices

1. **File Structure**
   ```
   src/TradingBot.Blazor/
   ├── Components/
   │   ├── Common/
   │   │   ├── Header.razor
   │   │   ├── Sidebar.razor
   │   │   └── Footer.razor
   │   ├── Dashboard/
   │   │   ├── PortfolioWidget.razor
   │   │   ├── PerformanceChart.razor
   │   │   └── DashboardPage.razor
   │   ├── Trading/
   │   │   ├── OrderForm.razor
   │   │   ├── OrderHistory.razor
   │   │   └── TradingPage.razor
   │   └── Shared/
   │       ├── Layouts/
   │       └── Dialogs/
   ├── wwwroot/
   │   └── css/
   │       ├── site.css (input)
   │       └── styles.css (output - generated)
   ├── tailwind.config.js
   └── package.json
   ```

2. **CSS Isolation with Tailwind**
   - Avoid component-scoped CSS (`.razor.css` files) unless necessary
   - Use Tailwind utility classes directly in Razor markup
   - For component-specific styles, use CSS custom properties or CSS modules
   - CSS isolation doesn't work well with Tailwind's utility-first approach

3. **Naming Conventions**
   - Class names follow kebab-case (Tailwind standard): `btn-primary-lg`
   - Create semantic component classes in `site.css` for complex patterns:
   ```css
   @layer components {
     @apply text-white bg-blue-500 px-4 py-2 rounded-lg hover:bg-blue-600 transition-colors;
   }
   ```

4. **Configuration for Content Scanning**
   - Ensure `content` array in `tailwind.config.js` matches all component locations
   - Use glob patterns: `./Components/**/*.razor`, `./Pages/**/*.razor`
   - Add any dynamically generated class names to safelist if needed:
   ```javascript
   safelist: [
     'bg-red-500',
     'bg-green-500',
     'bg-blue-500'
   ]
   ```

### Build Process Integration (.NET 9 Specifics)

- **Debug Build**: MSBuild runs Tailwind without minification for source map debugging
- **Release Build**: MSBuild runs Tailwind with `--minify` flag, reducing CSS from ~50KB to ~10-15KB typical
- **Incremental Builds**: Use `UpToDateCheckBuilt` items to ensure CSS rebuilds only when inputs change
- **Standalone CLI Alternative**: If Node.js unavailable, download standalone `tailwindcss-windows-x64.exe` or equivalent for your platform

### Important Encoding Note

- Input CSS file (`site.css`) MUST be UTF-8 without signature encoding
- UTF-16 or UTF-8 with signature causes "Invalid declaration: @import 'tailwindcss'" error
- Set in Visual Studio: File → Advanced Save Options → UTF-8 without Signature

**Alternatives Considered**:

1. **Vite + Tailwind (Not Recommended for Blazor Server)**
   - Vite is primarily designed for SPA frameworks (Vue, React, Svelte)
   - Adds unnecessary complexity for server-rendered applications
   - Requires additional build tooling and configuration

2. **Blazorise Tailwind Component Library**
   - Pre-built Tailwind components as Blazor components
   - Good for rapid prototyping but adds dependency bloat
   - Less flexible for custom trading UI requirements
   - Overkill for internal dashboard application

3. **Bootstrap or Foundation CSS Frameworks**
   - Larger CSS bundles than Tailwind (50KB+ vs 15KB minified)
   - Less utility flexibility for custom trading dashboard layouts
   - Not recommended for performance-conscious applications

---

## Decision: SignalR Best Practices in Blazor Server

**Rationale**: SignalR is the built-in real-time communication mechanism for Blazor Server. For a trading dashboard serving 50+ concurrent users, proper SignalR configuration is essential for reliability, performance, and seamless reconnection handling. The event-driven architecture integrates naturally with the TradingBot's signal processing pipeline.

**Implementation Approach**:

### 1. Server-Side Hub Configuration

**Register SignalR in Program.cs**:
```csharp
builder.Services.AddSignalR(options =>
{
    // Configure for 50+ concurrent users
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);

    // Message size (default 32KB sufficient for trading data)
    options.MaximumMessageSize = 32 * 1024; // 32 KB

    // Enable detailed errors in development only
    options.EnableDetailedErrors = !app.Environment.IsProduction();
});

// Map the hub endpoint
app.MapHub<TradingHub>("/trading-hub", options =>
{
    options.Transports =
        HttpTransportType.WebSockets |
        HttpTransportType.ServerSentEvents;
});
```

**Create Strongly-Typed Hub**:
```csharp
public interface ITradingClient
{
    Task OnPositionUpdated(PositionUpdate update);
    Task OnOrderExecuted(OrderExecutionData order);
    Task OnEquityUpdated(decimal equity);
    Task OnSignalGenerated(SignalData signal);
    Task OnConnectionStatusChanged(string status);
}

public class TradingHub : Hub<ITradingClient>
{
    private readonly IPortfolioManager _portfolioManager;
    private readonly ILogger<TradingHub> _logger;

    public TradingHub(IPortfolioManager portfolioManager, ILogger<TradingHub> logger)
    {
        _portfolioManager = portfolioManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, "TradingUpdates");

        // Send current state to newly connected client
        var portfolio = await _portfolioManager.GetPortfolioStateAsync();
        await Clients.Caller.OnEquityUpdated(portfolio.Equity);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        if (exception != null)
        {
            _logger.LogError(exception, "Disconnection error");
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Request-response method (client calls server)
    public async Task<PositionData[]> GetPositionsAsync()
    {
        return await _portfolioManager.GetOpenPositionsAsync();
    }
}
```

### 2. Broadcasting Updates from Trading Engine

**Integrate with Signal Processing Pipeline**:
```csharp
// In StrategyEngine or OrderExecutionService
public class OrderExecutionService
{
    private readonly IHubContext<TradingHub, ITradingClient> _hubContext;

    public OrderExecutionService(IHubContext<TradingHub, ITradingClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task ExecuteOrderAsync(Order order)
    {
        // Execute order...
        var result = await _executeOrder(order);

        // Broadcast to all connected clients
        await _hubContext.Clients
            .Group("TradingUpdates")
            .OnOrderExecuted(result);
    }
}
```

### 3. Client-Side Initialization and Reconnection

**Configure Blazor for Optimal Reconnection**:
```html
<!-- In App.razor -->
<script src="_framework/blazor.server.js" autostart="false"></script>
<script>
Blazor.start({
    reconnectionOptions: {
        maxRetries: 10,
        retryInterval: 3000,
        dialogId: "components-reconnect-modal"
    }
});
</script>
```

**JavaScript Module for Hub Connection** (`js/trading-hub.js`):
```javascript
export async function initializeTradingHub(dotnetHelper) {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/trading-hub", {
            // Use WebSocket transport for lower latency
            skipNegotiation: true,
            transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: retryContext => {
                // Custom retry strategy for 50+ concurrent users
                if (retryContext.previousRetryCount === 0) {
                    return 0; // Retry immediately
                } else if (retryContext.previousRetryCount === 1) {
                    return 2000; // 2 seconds
                } else if (retryContext.previousRetryCount < 4) {
                    return 5000; // 5 seconds
                } else {
                    return 10000; // 10 seconds, max 4 retries
                }
            }
        })
        .withHubProtocol(new signalR.JsonHubProtocol())
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Connection event handlers
    connection.onreconnecting((error) => {
        console.log(`Reconnecting due to error: ${error}`);
        dotnetHelper.invokeMethodAsync('OnReconnecting');
    });

    connection.onreconnected((connectionId) => {
        console.log(`Reconnected with ID: ${connectionId}`);
        dotnetHelper.invokeMethodAsync('OnReconnected');
    });

    connection.onclose((error) => {
        console.log(`Connection closed: ${error}`);
        dotnetHelper.invokeMethodAsync('OnConnectionClosed');
    });

    // Start connection with retry logic
    await startWithRetry(connection, 3);

    return connection;
}

async function startWithRetry(connection, maxRetries) {
    for (let i = 0; i < maxRetries; i++) {
        try {
            await connection.start();
            console.log("Connected to trading hub");
            return;
        } catch (err) {
            if (i === maxRetries - 1) throw err;
            await new Promise(resolve => setTimeout(resolve, 1000 * (i + 1)));
        }
    }
}
```

**Blazor Component Integration** (`DashboardPage.razor`):
```csharp
@page "/dashboard"
@using Microsoft.AspNetCore.SignalR.Client
@implements IAsyncDisposable
@inject NavigationManager Navigation
@inject IJSRuntime JS

<div id="components-reconnect-modal" class="hidden fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
    <div class="bg-white p-6 rounded-lg shadow-lg">
        <h3 class="text-lg font-bold mb-2">Connection Lost</h3>
        <p class="mb-4">Reconnecting to server...</p>
        <div class="animate-spin inline-block w-4 h-4 border-4 border-blue-500 border-t-transparent rounded-full"></div>
    </div>
</div>

<section class="grid grid-cols-1 md:grid-cols-2 gap-4">
    <PortfolioWidget @ref="portfolioWidget" />
    <PerformanceChart @ref="performanceChart" />
</section>

@code {
    private HubConnection? hubConnection;
    private PortfolioWidget? portfolioWidget;
    private PerformanceChart? performanceChart;
    private IJSObjectReference? module;

    protected override async Task OnInitializedAsync()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/trading-hub"))
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<PositionUpdate>("OnPositionUpdated", OnPositionUpdated);
        hubConnection.On<OrderExecutionData>("OnOrderExecuted", OnOrderExecuted);
        hubConnection.On<decimal>("OnEquityUpdated", OnEquityUpdated);

        await hubConnection.StartAsync();
    }

    private Task OnPositionUpdated(PositionUpdate update)
    {
        portfolioWidget?.UpdatePosition(update);
        return Task.CompletedTask;
    }

    private Task OnOrderExecuted(OrderExecutionData order)
    {
        // Update UI with executed order
        return InvokeAsync(StateHasChanged);
    }

    private Task OnEquityUpdated(decimal equity)
    {
        performanceChart?.UpdateEquity(equity);
        return InvokeAsync(StateHasChanged);
    }

    public async Task OnReconnecting()
    {
        await JS.InvokeVoidAsync("document.getElementById", "components-reconnect-modal").Result?.classList.remove("hidden");
    }

    public async Task OnReconnected()
    {
        // Modal will be hidden by Blazor framework
        await portfolioWidget?.RefreshAsync()!;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
```

### 4. Performance Optimization for 50+ Concurrent Users

**Server-Side Optimization**:
```csharp
// In Program.cs
builder.Services.AddSignalR(options =>
{
    // Increase stream buffer size for large dashboards
    options.StreamBufferCapacity = 100;
})
.AddMessagePackProtocol() // Reduce payload size vs JSON
.AddStackExchangeRedis(o =>
{
    o.Configuration = builder.Configuration.GetConnectionString("Redis");
})
.AddJsonProtocol(); // Fallback for browsers without MessagePack support
```

**Broadcasting Patterns for Scale**:
```csharp
// Broadcast only to relevant users (not all)
await _hubContext.Clients
    .Group("TradingUpdates")
    .OnEquityUpdated(equity);

// For per-user updates
await _hubContext.Clients
    .User(userId)
    .OnPositionUpdated(update);

// Throttle high-frequency updates
private System.Timers.Timer? _updateThrottle;

public void ScheduleEquityUpdate(decimal equity)
{
    _equityBuffer = equity;
    _updateThrottle?.Stop();
    _updateThrottle = new System.Timers.Timer(100) // 100ms throttle
    {
        AutoReset = false,
        Enabled = true
    };
    _updateThrottle.Elapsed += async (s, e) =>
    {
        await _hubContext.Clients.All.OnEquityUpdated(_equityBuffer);
    };
}
```

### 5. Circuit and Connection Management

**Important Blazor Server Specifics**:
- Each connected user gets a **circuit** representing their connection state
- Circuits persist on server for 3 minutes after disconnect
- Client reconnection attempts for configured interval
- On .NET 9, immediate reconnection when navigating back to app
- Session affinity (sticky sessions) required for multi-server deployments

**Configuration for Production**:
```csharp
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DisconnectedCircuitMaxRetained = 100; // For 50+ users
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
        options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(10);
        options.RootComponentParameters = new Dictionary<string, object?>
        {
            { "Configuration", builder.Configuration }
        };
    })
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    });

// Enable WebSockets for production (lowest latency)
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(15),
    ReceiveBufferSize = 4 * 1024
});
```

### 6. Reconnection Strategy Summary

| Scenario | Behavior |
|----------|----------|
| Network Temporary Loss | Automatic reconnect with exponential backoff (0→2→5→10 seconds) |
| Server Restart | Client sees "Reconnecting" UI, automatically reconnects when server up |
| 15+ Second Disconnect | Circuit released, page refresh required on reconnection |
| Multiple Servers (Load Balanced) | Session affinity ensures reconnect to same server |
| .NET 9+ Behavior | Immediate reconnect attempt on navigation back to app |

**Alternatives Considered**:

1. **HTTP Long Polling (Not Recommended)**
   - Higher latency than WebSockets
   - Higher server resource consumption
   - Better fallback but not primary transport
   - Use only if WebSocket not available (rare)

2. **Server-Sent Events (Acceptable but WebSocket Preferred)**
   - One-way server-to-client communication
   - Good for broadcast scenarios
   - Higher latency than WebSockets
   - Not suitable for real-time trading updates requiring acknowledgment

3. **Custom WebSocket Implementation (Not Recommended)**
   - SignalR handles reconnection, groups, user management automatically
   - Reinventing wheel introduces bugs in critical trading code
   - SignalR tested in production at scale

4. **Polling with JavaScript Timer (Not Recommended)**
   - Chatty, increases server load linearly with users
   - High latency (even with 1-second interval)
   - No built-in reconnection handling
   - Wastes bandwidth with constant HTTP requests

---

## Decision: bUnit Testing Framework for Blazor Components

**Rationale**: bUnit is the industry standard for testing Blazor components with mature tooling for mocking dependencies, excellent integration with xUnit/NUnit, and strong community support. For the trading dashboard with real-time SignalR updates, bUnit provides the necessary testing infrastructure while remaining lightweight.

**Implementation Approach**:

### 1. Project Setup

**Create bUnit Test Project**:
```bash
dotnet new xunit -n TradingBot.Blazor.Tests
cd TradingBot.Blazor.Tests
dotnet add package bunit
dotnet add package bunit.web
dotnet add package xunit
dotnet add package FakeItEasy
dotnet add package Shouldly
dotnet add package RichardSzalay.MockHttp
dotnet add reference ../src/TradingBot.Blazor/
```

**Test Project Structure**:
```
TradingBot.Blazor.Tests/
├── Components/
│   ├── Common/
│   │   ├── HeaderTests.cs
│   │   └── SidebarTests.cs
│   ├── Dashboard/
│   │   ├── PortfolioWidgetTests.cs
│   │   └── DashboardPageTests.cs
│   └── Trading/
│       └── OrderFormTests.cs
├── Fixtures/
│   ├── ComponentTestFixture.cs
│   └── MockServiceFixture.cs
└── MockFactories.cs
```

### 2. Testing Components with Dependencies

**Basic Component Test**:
```csharp
using Xunit;
using Shouldly;
using Bunit;

[Collection("Component Tests")]
public class PortfolioWidgetTests : IDisposable
{
    private readonly TestContext _testContext;

    public PortfolioWidgetTests()
    {
        _testContext = new TestContext();
    }

    [Fact]
    public void RendersPortfolioData_WhenInitialized()
    {
        // Arrange
        var portfolioData = new PortfolioData
        {
            Equity = 150000m,
            Cash = 50000m,
            TotalPositionValue = 100000m,
            UnrealizedPnL = 5000m
        };

        var cut = _testContext.RenderComponent<PortfolioWidget>(parameters => parameters
            .Add(p => p.Portfolio, portfolioData)
        );

        // Act
        var equityText = cut.Find(".equity-value").TextContent;

        // Assert
        equityText.ShouldContain("150,000");
    }

    [Fact]
    public void UpdatesDisplay_WhenPortfolioParameterChanges()
    {
        // Arrange
        var initialData = new PortfolioData { Equity = 100000m };
        var cut = _testContext.RenderComponent<PortfolioWidget>(parameters => parameters
            .Add(p => p.Portfolio, initialData)
        );

        // Act
        var updatedData = new PortfolioData { Equity = 150000m };
        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Portfolio, updatedData)
        );

        // Assert
        cut.Find(".equity-value").TextContent.ShouldContain("150,000");
    }

    public void Dispose()
    {
        _testContext?.Dispose();
    }
}
```

### 3. Mocking IJSRuntime for JavaScript Interop

**Mocking JavaScript Calls**:
```csharp
[Fact]
public async Task InitializesChart_WhenComponentRendersAsync()
{
    // Arrange
    _testContext.JSInterop.Mode = JSRuntimeMode.Loose;

    var jsRuntime = _testContext.JSInterop;
    jsRuntime.Setup<object>("initializeChart",
        IsAny<string>(), IsAny<object>())
        .SetResult(null);

    // Act
    var cut = _testContext.RenderComponent<PerformanceChart>();
    await cut.InvokeAsync(async () => await Task.Delay(100)); // Allow JS calls

    // Assert
    jsRuntime.VerifyInvoke("initializeChart").Times(1);
}

[Fact]
public void ThrowsException_WhenJSCallNotSetUp()
{
    // Arrange
    var cut = _testContext.RenderComponent<ChartComponent>();
    _testContext.JSInterop.Mode = JSRuntimeMode.Strict; // Strict = throw on unmocked calls

    // Act & Assert
    Assert.Throws<JSDisconnectedException>(() =>
        cut.InvokeAsync(async () =>
            await cut.Instance.CallUnmockedJSAsync()
        )
    );
}
```

**Module Import Testing**:
```csharp
[Fact]
public async Task LoadsTradingModule_OnComponentInitAsync()
{
    // Arrange
    var module = _testContext.JSInterop.SetupModule("./js/trading.js");
    module.Setup<decimal>("calculatePositionSize", IsAny<decimal>(), IsAny<decimal>())
        .SetResult(100m);

    // Act
    var cut = _testContext.RenderComponent<OrderForm>();
    var result = await cut.InvokeAsync(() =>
        cut.Instance.CalculatePositionSizeAsync(10000m, 0.02m)
    );

    // Assert
    result.ShouldBe(100m);
    module.VerifyInvoke("calculatePositionSize").Times(1);
}
```

### 4. Mocking Services (HttpClient, Custom Services)

**Mock HttpClient with RichardSzalay.MockHttp**:
```csharp
public class OrderServiceTests
{
    private readonly TestContext _testContext = new();

    [Fact]
    public async Task FetchesOrderHistory_FromServerAsync()
    {
        // Arrange
        var mockOrders = new[]
        {
            new Order { Id = 1, Symbol = "AAPL", Quantity = 100 },
            new Order { Id = 2, Symbol = "GOOGL", Quantity = 50 }
        };

        var mock = _testContext.Services.AddMockHttpClient();
        mock.When(HttpMethod.Get, "/api/orders")
            .RespondJson(mockOrders);

        var orderService = new OrderService(_testContext.Services.GetRequiredService<HttpClient>());

        // Act
        var orders = await orderService.GetOrdersAsync();

        // Assert
        orders.ShouldHaveCount(2);
        orders.First().Symbol.ShouldBe("AAPL");
    }
}
```

**Create Helper Extension for Common Mocks**:
```csharp
public static class MockServiceExtensions
{
    public static IServiceCollection AddMockPortfolioManager(
        this IServiceCollection services,
        PortfolioData? data = null)
    {
        data ??= new PortfolioData { Equity = 100000m };

        var mock = A.Fake<IPortfolioManager>();
        A.CallTo(() => mock.GetPortfolioStateAsync())
            .Returns(data);
        A.CallTo(() => mock.GetOpenPositionsAsync())
            .Returns(new[] {
                new Position { Symbol = "AAPL", Quantity = 100, EntryPrice = 150m }
            });

        services.AddSingleton(mock);
        return services;
    }

    public static IServiceCollection AddMockOrderService(
        this IServiceCollection services)
    {
        var mock = A.Fake<IOrderService>();
        services.AddSingleton(mock);
        return services;
    }
}
```

### 5. Testing SignalR Dependent Components

**Mock SignalR Hub Connection**:
```csharp
[Fact]
public async Task ReceivesPositionUpdates_FromHubAsync()
{
    // Arrange
    var mockConnection = A.Fake<HubConnection>();
    var positionUpdates = new List<PositionUpdate>();

    // Capture callbacks registered on the mock
    Action<PositionUpdate>? updateCallback = null;
    A.CallTo(() => mockConnection.On<PositionUpdate>(
        "OnPositionUpdated",
        A<Action<PositionUpdate>>.Ignored))
        .Invokes((string name, Action<PositionUpdate> callback) =>
        {
            updateCallback = callback;
        });

    var cut = _testContext.RenderComponent<DashboardPage>(parameters => parameters
        .Add(p => p.HubConnection, mockConnection)
    );

    // Act
    var update = new PositionUpdate { Symbol = "AAPL", Quantity = 100 };
    updateCallback?.Invoke(update);
    await cut.InvokeAsync(() => Task.Delay(100));

    // Assert
    var widget = cut.FindComponent<PortfolioWidget>();
    widget.Instance.LatestPosition.Symbol.ShouldBe("AAPL");
}

[Fact]
public async Task HandlesConnectionError_GracefullyAsync()
{
    // Arrange
    var mockConnection = A.Fake<HubConnection>();
    A.CallTo(() => mockConnection.StartAsync())
        .Throws<HubException>();

    var cut = _testContext.RenderComponent<DashboardPage>(parameters => parameters
        .Add(p => p.HubConnection, mockConnection)
    );

    // Act & Assert - Component should handle error
    await cut.InvokeAsync(async () =>
    {
        // Component should display error message
        cut.Find(".error-message").TextContent
            .ShouldContain("Connection Failed");
    });
}
```

### 6. Testing Component Lifecycle with Events

**Testing Cascading Parameters and Event Callbacks**:
```csharp
[Fact]
public async Task CallsOnOrderSubmitted_WhenFormSubmittedAsync()
{
    // Arrange
    var orderSubmitted = false;
    var eventCallback = EventCallback.Factory.Create(this, async () =>
    {
        orderSubmitted = true;
        await Task.CompletedTask;
    });

    var cut = _testContext.RenderComponent<OrderForm>(parameters => parameters
        .Add(p => p.OnSubmit, eventCallback)
    );

    // Act
    var submitButton = cut.Find("button[type='submit']");
    await cut.InvokeAsync(() => submitButton.Click());

    // Assert
    orderSubmitted.ShouldBeTrue();
}

[Fact]
public async Task ValidatesInput_BeforeSubmissionAsync()
{
    // Arrange
    var cut = _testContext.RenderComponent<OrderForm>();

    // Act - Try submitting empty form
    var submitButton = cut.Find("button[type='submit']");
    await cut.InvokeAsync(() => submitButton.Click());

    // Assert - Should show validation errors
    cut.FindAll(".validation-error").Count.ShouldBeGreaterThan(0);
}
```

### 7. Component Render Verification

**Verify Rendered Output**:
```csharp
[Fact]
public void RendersCorrectStructure_ForDashboard()
{
    // Arrange & Act
    var cut = _testContext.RenderComponent<DashboardPage>();

    // Assert - Use semantic HTML comparison
    cut.Find(".dashboard-grid").ClassList.ShouldContain("grid");
    cut.FindAll(".widget-card").Count.ShouldBeGreaterThanOrEqualTo(3);

    var header = cut.Find("header");
    header.ShouldNotBeNull();

    var nav = cut.Find("nav");
    nav.TextContent.ShouldContain("Portfolio");
}

[Fact]
public void RendersDynamicContent_BasedOnState()
{
    // Arrange
    var positions = new[]
    {
        new Position { Symbol = "AAPL", Quantity = 100 },
        new Position { Symbol = "GOOGL", Quantity = 50 }
    };

    // Act
    var cut = _testContext.RenderComponent<PositionList>(parameters => parameters
        .Add(p => p.Positions, positions)
    );

    // Assert
    var rows = cut.FindAll("tr[data-symbol]");
    rows.Count.ShouldBe(2);
    rows[0].Attributes["data-symbol"]?.Value.ShouldBe("AAPL");
}
```

### 8. Test Organization Best Practices

**Test Class Structure**:
```csharp
public class DashboardPageTests : IDisposable
{
    private readonly TestContext _testContext;
    private readonly IPortfolioManager _mockPortfolioManager;

    public DashboardPageTests()
    {
        _testContext = new TestContext();
        _mockPortfolioManager = A.Fake<IPortfolioManager>();
        _testContext.Services.AddSingleton(_mockPortfolioManager);
    }

    // Category: Initialization
    [Fact]
    public void InitializesWithPortfolioData() { }

    // Category: User Interaction
    [Fact]
    public async Task UpdatesPositionWhenTradeExecutedAsync() { }

    // Category: Error Handling
    [Fact]
    public void DisplaysErrorMessage_WhenLoadFails() { }

    // Category: SignalR Integration
    [Fact]
    public async Task ReceivesRealTimeUpdatesAsync() { }

    public void Dispose()
    {
        _testContext?.Dispose();
    }
}
```

### 9. Performance Testing Patterns

**Load Testing Component Rendering**:
```csharp
[Fact]
public void RendersManyPositions_Efficiently()
{
    // Arrange
    var positions = Enumerable.Range(1, 500)
        .Select(i => new Position
        {
            Id = i,
            Symbol = $"SYM{i}",
            Quantity = i * 10
        })
        .ToArray();

    var sw = System.Diagnostics.Stopwatch.StartNew();

    // Act
    var cut = _testContext.RenderComponent<PositionList>(parameters => parameters
        .Add(p => p.Positions, positions)
    );
    sw.Stop();

    // Assert - Should render 500 items in < 500ms
    sw.ElapsedMilliseconds.ShouldBeLessThan(500);
}
```

**Alternatives Considered**:

1. **Selenium/Playwright Integration Testing (Not for Unit Tests)**
   - Best for end-to-end UI testing in real browser
   - Much slower than bUnit
   - Better for final acceptance testing, not development cycle
   - Recommended: Use bUnit for unit tests, Playwright for E2E

2. **NUnit instead of xUnit (Equivalent)**
   - Both work equally well with bUnit
   - xUnit is more modern and aligns with .NET best practices
   - NUnit has larger community but xUnit more functional

3. **Moq instead of FakeItEasy (Both Viable)**
   - FakeItEasy has cleaner AAA syntax
   - Moq is more feature-rich for advanced scenarios
   - For this project, FakeItEasy preferred for readability

4. **Manual Testing Only (Not Recommended)**
   - Unreliable for complex dashboard logic
   - No regression detection
   - Poor coverage for edge cases
   - Essential for automated testing in CI/CD

---

## Summary and Integration Recommendations

### Technology Stack Decision
- **CSS Framework**: Tailwind CSS v4 with MSBuild integration
- **Real-time Communication**: SignalR with WebSocket primary transport
- **Component Testing**: bUnit with xUnit test runner

### Key Implementation Priorities

1. **Phase 1 - Foundation**
   - Set up Tailwind CSS with watch mode
   - Configure SignalR hub with strongly-typed clients
   - Create base bUnit test infrastructure

2. **Phase 2 - Core Dashboards**
   - Implement real-time portfolio widget
   - Build order execution monitoring
   - Add comprehensive component tests

3. **Phase 3 - Scaling & Optimization**
   - Implement message throttling for high-frequency updates
   - Add MessagePack protocol for bandwidth reduction
   - Load test with 50+ concurrent users

### Performance Targets
- CSS bundle: < 20KB minified
- Hub connection latency: < 100ms
- Component render time: < 50ms for typical dashboard
- Test suite execution: < 30 seconds

### Development Workflow
```bash
# Terminal 1: Watch CSS
npm run css:watch

# Terminal 2: Run application
dotnet watch run --project src/TradingBot.Cli

# Terminal 3: Run tests
dotnet watch test tests/TradingBot.Blazor.Tests
```

---

## References

- [Microsoft Blazor Documentation (v9.0)](https://learn.microsoft.com/en-us/aspnet/core/blazor)
- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr)
- [Tailwind CSS Official Guide](https://tailwindcss.com)
- [bUnit Documentation](https://bunit.dev)
- [ASP.NET Core SignalR Scaling Guide](https://learn.microsoft.com/en-us/azure/azure-signalr/signalr-concept-performance)

---

**Document Date**: November 7, 2025
**Target Framework**: .NET 9.0
**Last Updated**: During specification phase