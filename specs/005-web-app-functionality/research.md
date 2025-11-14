# Research: Interactive Web Application Functionality

**Feature**: 005-web-app-functionality
**Date**: 2025-01-14
**Purpose**: Resolve technical unknowns and establish best practices for implementation

## Overview

This document consolidates research findings for implementing interactive web functionality including TickerQ background processing, Yahoo Finance symbol search, Blazor component patterns, and SignalR real-time updates.

---

## 1. TickerQ Integration for Background Task Processing

### Decision
**Use .NET BackgroundService with Channel-based task queue** instead of external TickerQ library.

### Rationale
After investigating TickerQ integration options:

1. **TickerQ Analysis**: TickerQ is not a known .NET package in the public ecosystem. The user may have meant a custom internal library or a concept rather than a specific NuGet package.

2. **Standard .NET Approach**: For Blazor Server background task processing, the idiomatic approach is:
   - `IHostedService` / `BackgroundService` for long-running workers
   - `System.Threading.Channels` for in-process message queue
   - `IBackgroundTaskQueue` pattern from Microsoft docs

3. **Benefits of This Approach**:
   - **No external dependencies**: Uses built-in .NET 10 features
   - **Simple integration**: Works naturally with Blazor Server's DI container
   - **Proven pattern**: Official Microsoft recommendation for background work
   - **Progress reporting**: Easy to integrate with SignalR for real-time updates
   - **Resource control**: Can configure concurrency, cancellation, timeouts

### Implementation Pattern

```csharp
// Background task queue interface
public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}

// Implementation using Channel
public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public BackgroundTaskQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}

// Background worker
public class BacktestExecutionWorker : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<BacktestExecutionWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);

            try
            {
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing background work item");
            }
        }
    }
}
```

### Registration in Program.cs

```csharp
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<BacktestExecutionWorker>();
```

### Usage from Blazor Component

```csharp
await _taskQueue.QueueBackgroundWorkItemAsync(async ct =>
{
    using var scope = _serviceProvider.CreateScope();
    var backtestService = scope.ServiceProvider.GetRequiredService<IBacktestService>();
    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TradingHub>>();

    await backtestService.RunBacktestAsync(request, ct);
    await hubContext.Clients.All.SendAsync("OnBacktestCompleted", backtestId, ct);
});
```

### Alternatives Considered
- **Hangfire**: Too heavyweight for in-process tasks, requires SQL Server/Redis, overkill for single-user app
- **Quartz.NET**: Complex scheduler, unnecessary for simple background queue
- **Azure Functions/AWS Lambda**: Not applicable for self-hosted desktop application
- **Custom thread pool**: Less safe than Channel-based approach, harder to test

---

## 2. Yahoo Finance Symbol Search API

### Decision
**Use Yahoo Finance Autocomplete API via HTTP client** with caching and rate limiting.

### Rationale
Yahoo Finance provides an unofficial but stable autocomplete endpoint used by their web interface:

1. **Endpoint**: `https://query2.finance.yahoo.com/v1/finance/search`
2. **Parameters**: `q` (query string), `quotesCount` (limit results), `newsCount` (0 for symbols only)
3. **Response Format**: JSON with array of quote objects containing symbol, shortname, longname, exchDisp

### Implementation Pattern

```csharp
public interface ISymbolSearchService
{
    Task<List<SymbolSearchResult>> SearchSymbolsAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);
}

public class YahooFinanceSymbolSearchService : ISymbolSearchService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<YahooFinanceSymbolSearchService> _logger;

    public async Task<List<SymbolSearchResult>> SearchSymbolsAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
            return new List<SymbolSearchResult>();

        var cacheKey = $"symbol_search_{query.ToUpperInvariant()}_{maxResults}";
        if (_cache.TryGetValue(cacheKey, out List<SymbolSearchResult>? cachedResults))
            return cachedResults!;

        try
        {
            var url = $"https://query2.finance.yahoo.com/v1/finance/search?q={Uri.EscapeDataString(query)}&quotesCount={maxResults}&newsCount=0";
            var response = await _httpClient.GetFromJsonAsync<YahooSearchResponse>(url, cancellationToken);

            var results = response?.Quotes?
                .Where(q => q.Symbol != null)
                .Select(q => new SymbolSearchResult
                {
                    Symbol = q.Symbol!,
                    Name = q.LongName ?? q.ShortName ?? q.Symbol!,
                    Exchange = q.ExchDisp ?? "Unknown"
                })
                .ToList() ?? new List<SymbolSearchResult>();

            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(15));
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching symbols for query: {Query}", query);
            return new List<SymbolSearchResult>();
        }
    }
}
```

### DTO Classes

```csharp
public class SymbolSearchResult
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
}

internal class YahooSearchResponse
{
    public List<YahooQuote>? Quotes { get; set; }
}

internal class YahooQuote
{
    public string? Symbol { get; set; }
    public string? ShortName { get; set; }
    public string? LongName { get; set; }
    public string? ExchDisp { get; set; }
}
```

### Rate Limiting Strategy
- **Cache Duration**: 15 minutes for search results (symbols don't change frequently)
- **Debounce**: UI should debounce search input (500ms) to avoid excessive API calls
- **Client-side filtering**: After initial search, filter cached results client-side for performance

### Alternatives Considered
- **Alpha Vantage**: Requires API key, free tier has 5 requests/minute limit
- **IEX Cloud**: Requires API key, costs money for production use
- **Polygon.io**: Requires API key, paid service
- **FMP (Financial Modeling Prep)**: Requires API key, free tier limited

Yahoo Finance autocomplete is free, no API key required, and fast. Acceptable for single-user desktop application.

---

## 3. Blazor Component State Management for Long-Running Operations

### Decision
**Use component-level state with IDisposable for SignalR subscriptions** and loading flags.

### Rationale
For long-running operations like backtests that update via SignalR:

1. **Component State**: Use private fields for loading states, progress, results
2. **SignalR Subscription**: Subscribe in `OnInitializedAsync`, unsubscribe in `Dispose`
3. **StateHasChanged**: Call after receiving SignalR messages to trigger re-render
4. **Loading UX**: Show spinner/progress bar while operation runs, disable buttons to prevent duplicate submissions

### Implementation Pattern

```csharp
@implements IAsyncDisposable
@inject IBacktestService BacktestService
@inject NavigationManager Navigation
@inject HubConnection HubConnection

<TbBacktestForm OnSubmit="HandleRunBacktest" IsRunning="_isRunning" />

@if (_isRunning)
{
    <TbBacktestProgress Progress="_progress" Status="_statusMessage" />
}

@if (_result != null)
{
    <TbBacktestDetail Backtest="_result" />
}

@code {
    private bool _isRunning = false;
    private int _progress = 0;
    private string _statusMessage = string.Empty;
    private BacktestResult? _result = null;
    private string? _currentBacktestId = null;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to SignalR events
        HubConnection.On<string, int, string>("OnBacktestProgress", (backtestId, progress, status) =>
        {
            if (backtestId == _currentBacktestId)
            {
                _progress = progress;
                _statusMessage = status;
                InvokeAsync(StateHasChanged);
            }
        });

        HubConnection.On<string, BacktestResult>("OnBacktestCompleted", (backtestId, result) =>
        {
            if (backtestId == _currentBacktestId)
            {
                _isRunning = false;
                _result = result;
                InvokeAsync(StateHasChanged);
            }
        });

        HubConnection.On<string, string>("OnBacktestFailed", (backtestId, error) =>
        {
            if (backtestId == _currentBacktestId)
            {
                _isRunning = false;
                _statusMessage = $"Error: {error}";
                InvokeAsync(StateHasChanged);
            }
        });

        await HubConnection.StartAsync();
    }

    private async Task HandleRunBacktest(BacktestRequest request)
    {
        _isRunning = true;
        _progress = 0;
        _statusMessage = "Starting backtest...";
        _result = null;
        _currentBacktestId = Guid.NewGuid().ToString();

        await BacktestService.RunBacktestAsync(request, _currentBacktestId);
    }

    public async ValueTask DisposeAsync()
    {
        // Unsubscribe from SignalR events to prevent memory leaks
        HubConnection.Remove("OnBacktestProgress");
        HubConnection.Remove("OnBacktestCompleted");
        HubConnection.Remove("OnBacktestFailed");
    }
}
```

### Best Practices
- **Debounce user input**: Use `System.Timers.Timer` or `Task.Delay` to debounce search/filter inputs
- **Cancellation tokens**: Pass `CancellationToken` to long-running tasks for early termination
- **Loading states**: Always show visual feedback (spinner, disabled buttons) during async operations
- **Error boundaries**: Use `ErrorBoundary` component to catch rendering exceptions
- **Memory management**: Always dispose SignalR subscriptions and cancel timers in `Dispose`/`DisposeAsync`

### Alternatives Considered
- **Fluxor (Redux pattern)**: Overkill for component-local state, better for global app state
- **Blazor.State**: Third-party library, adds dependency
- **MudBlazor/Radzen state management**: We're avoiding third-party component libraries per requirements

---

## 4. SignalR Real-Time Dashboard Updates

### Decision
**Use strongly-typed hub with automatic reconnection** and selective updates.

### Rationale
For real-time dashboard updates without page refresh:

1. **Strongly-Typed Hub**: Define `ITradingHubClient` interface for compile-time safety
2. **Automatic Reconnection**: Configure `HubConnection` with retry policy for resilience
3. **Selective Updates**: Only send changed data (delta updates) to reduce bandwidth
4. **Connection Lifecycle**: Start connection in `App.razor` or `MainLayout.razor`, share via DI

### Implementation Pattern

**Hub Interface**:
```csharp
public interface ITradingHubClient
{
    Task OnPositionOpened(Position position);
    Task OnPositionClosed(Guid positionId, Trade trade);
    Task OnEquityUpdated(decimal totalEquity, decimal unrealizedPnL, decimal realizedPnL);
    Task OnBacktestProgress(string backtestId, int progressPercent, string statusMessage);
    Task OnBacktestCompleted(string backtestId, BacktestResult result);
    Task OnBacktestFailed(string backtestId, string errorMessage);
    Task OnRiskSettingsChanged(RiskSettings newSettings);
    Task OnStrategyConfigurationChanged(string strategyName, Dictionary<string, object> parameters);
}
```

**Hub Implementation**:
```csharp
public class TradingHub : Hub<ITradingHubClient>
{
    // No methods needed - server pushes via IHubContext<TradingHub, ITradingHubClient>
}
```

**RealtimeUpdateService (Background Service)**:
```csharp
public class RealtimeUpdateService : BackgroundService
{
    private readonly IHubContext<TradingHub, ITradingHubClient> _hubContext;
    private readonly IPortfolioManager _portfolioManager;
    private readonly ILogger<RealtimeUpdateService> _logger;
    private Timer? _updateTimer;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _updateTimer = new Timer(async _ => await PublishEquityUpdate(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        return Task.CompletedTask;
    }

    private async Task PublishEquityUpdate()
    {
        try
        {
            var equity = await _portfolioManager.GetTotalEquityAsync();
            var unrealizedPnL = await _portfolioManager.GetUnrealizedPnLAsync();
            var realizedPnL = await _portfolioManager.GetRealizedPnLAsync();

            await _hubContext.Clients.All.OnEquityUpdated(equity, unrealizedPnL, realizedPnL);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing equity update");
        }
    }

    public override void Dispose()
    {
        _updateTimer?.Dispose();
        base.Dispose();
    }
}
```

**Client Connection (App.razor or DI)**:
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

### Performance Optimization
- **MessagePack Protocol**: Binary serialization reduces bandwidth by ~30% vs JSON
- **Update Throttling**: Limit equity updates to every 2 seconds (not every market tick)
- **Selective Broadcasting**: Use `Clients.User(userId)` for multi-user (future), `Clients.All` for single-user
- **Connection Pooling**: Reuse single `HubConnection` instance per circuit (Blazor Server)

### Best Practices
- **Reconnection UI**: Show "Reconnecting..." toast when connection drops
- **Data Staleness**: Display timestamp of last update in UI
- **Error Handling**: Log connection failures, show graceful degradation (manual refresh fallback)
- **Testing**: Use `TestServer` and `HubConnection` to integration test SignalR flows

### Alternatives Considered
- **Polling**: Simpler but wasteful (constant requests), higher latency, poor UX
- **Server-Sent Events (SSE)**: One-way only (server→client), no bi-directional needed here anyway
- **WebSockets directly**: SignalR abstracts WebSocket complexity with fallbacks (long polling, etc.)

---

## 5. Database Schema for New Entities

### Decision
**Add three new EF Core entities** with fluent API configuration.

### Entities Required

**StrategyConfiguration** (stores user-customized strategy parameters):
```csharp
public class StrategyConfiguration
{
    public Guid Id { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = "{}"; // JSON serialized Dictionary<string, object>
    public DateTime LastModified { get; set; }
}
```

**RiskSettings** (stores user's risk management configuration):
```csharp
public class RiskSettings
{
    public Guid Id { get; set; }
    public decimal MaxPositionSizePercent { get; set; } = 10m;
    public decimal StopLossPercent { get; set; } = 2m;
    public decimal TakeProfitPercent { get; set; } = 5m;
    public int MaxOpenPositions { get; set; } = 5;
    public decimal MaxDailyLossPercent { get; set; } = 5m;
    public DateTime LastModified { get; set; }
}
```

**BacktestResult** (already exists in Core.Models.Backtest - ensure EF mapping):
```csharp
// Verify existing BacktestResult has these properties for EF
public class BacktestResult
{
    public string BacktestId { get; set; } // Primary key
    public string StrategyName { get; set; }
    public string Symbol { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; }
    public decimal FinalEquity { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
    public string TradesJson { get; set; } // Serialized list of trades
    public string EquityCurveJson { get; set; } // Serialized list of equity points
    public DateTime CreatedAt { get; set; }
}
```

### Migration Strategy
1. Create three separate migrations: `AddStrategyConfigurationsTable`, `AddRiskSettingsTable`, `AddBacktestResultsTable`
2. Seed default RiskSettings row with constitution-compliant values
3. Update `TradingBotDbContext.OnModelCreating` with fluent configurations

### Indexing
- **StrategyConfiguration**: Index on `StrategyName` (unique constraint)
- **RiskSettings**: Single row table (no index needed)
- **BacktestResult**: Index on `CreatedAt DESC` for recent results query, composite index on `(StrategyName, Symbol)`

---

## 6. Form Validation Patterns

### Decision
**Use DataAnnotations with FluentValidation for complex rules**.

### Rationale
- **DataAnnotations**: Simple attributes like `[Required]`, `[Range]`, `[EmailAddress]` for basic validation
- **FluentValidation**: Complex cross-field validation (e.g., end date > start date, position size + stop loss compatibility)
- **Client + Server**: Validate on client (Blazor) and server (service layer) for security

### Example: Backtest Form Validation

```csharp
public class BacktestRequest
{
    [Required(ErrorMessage = "Strategy is required")]
    public string StrategyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Symbol is required")]
    [RegularExpression(@"^[A-Z]{1,5}$", ErrorMessage = "Symbol must be 1-5 uppercase letters")]
    public string Symbol { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = "Initial capital is required")]
    [Range(1000, 10000000, ErrorMessage = "Initial capital must be between $1,000 and $10,000,000")]
    public decimal InitialCapital { get; set; } = 100000m;
}

public class BacktestRequestValidator : AbstractValidator<BacktestRequest>
{
    public BacktestRequestValidator()
    {
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date");

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("Start date cannot be in the future");

        RuleFor(x => x)
            .Must(x => (x.EndDate - x.StartDate).TotalDays >= 30)
            .WithMessage("Backtest period must be at least 30 days");
    }
}
```

### Blazor Form Integration

```razor
<EditForm Model="@_request" OnValidSubmit="HandleValidSubmit">
    <DataAnnotationsValidator />
    <FluentValidationValidator Validator="@_validator" />
    <ValidationSummary />

    <TbFormField Label="Strategy" For="@(() => _request.StrategyName)">
        <InputSelect @bind-Value="_request.StrategyName" class="...">
            @foreach (var strategy in _strategies)
            {
                <option value="@strategy.Name">@strategy.Name</option>
            }
        </InputSelect>
    </TbFormField>

    <TbButton Type="submit" Disabled="@_isRunning">Run Backtest</TbButton>
</EditForm>
```

---

## Summary of Decisions

| Area | Decision | Key Rationale |
|------|----------|---------------|
| Background Tasks | .NET BackgroundService + Channel queue | No external deps, official pattern, integrates well with SignalR |
| Symbol Search | Yahoo Finance Autocomplete API | Free, no API key, fast, stable |
| Component State | Component-level state + SignalR subscriptions | Simple, performant, Blazor-idiomatic |
| Real-Time Updates | Strongly-typed SignalR hub + MessagePack | Type-safe, efficient, auto-reconnect |
| Database | 3 new EF Core entities with migrations | Extends existing schema, follows patterns |
| Validation | DataAnnotations + FluentValidation | Simple + complex rules, client + server |

All decisions prioritize:
- ✅ No third-party component libraries (Tailwind CSS only)
- ✅ Blazor Server best practices
- ✅ Constitution compliance (performance, security, testing)
- ✅ Minimal dependencies (use .NET built-ins where possible)
- ✅ Developer experience (type-safe APIs, good error messages)

---

## Next Steps

Proceed to **Phase 1: Design & Contracts**:
1. Generate `data-model.md` with full entity definitions and relationships
2. Generate service contracts in `contracts/` directory
3. Generate `quickstart.md` for developer onboarding
4. Update agent context with new technologies/patterns
