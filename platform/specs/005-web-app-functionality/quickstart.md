# Developer Quickstart: Interactive Web Application Functionality

**Feature**: 005-web-app-functionality
**Date**: 2025-01-14
**Purpose**: Get developers up to speed on implementing and testing this feature

## Overview

This guide helps developers implement the interactive web application functionality for portfolio management, strategy configuration, backtesting, and risk settings. Follow these steps sequentially to ensure a smooth development experience.

---

## Prerequisites

Ensure you have the following installed:

```bash
# Verify .NET SDK version
dotnet --version
# Expected: 10.0.x or higher

# Verify Node.js (for Tailwind CSS compilation)
node --version
# Expected: 18.x or higher

# Verify npm
npm --version
# Expected: 9.x or higher
```

**Required knowledge**:
- C# 14 / .NET 10 fundamentals
- ASP.NET Core Blazor Server basics
- Entity Framework Core 10
- SignalR real-time communication
- Tailwind CSS utility classes
- xUnit, bUnit, FakeItEasy testing frameworks

---

## Step 1: Branch Setup

```bash
# Ensure you're on the feature branch
git checkout 005-web-app-functionality

# Pull latest changes
git pull origin 005-web-app-functionality

# Verify you're on the correct branch
git branch
# Should show: * 005-web-app-functionality
```

---

## Step 2: Understand the Architecture

### Project Structure

```
src/
├── TradingBot.Core/           # Domain models and interfaces (ADD 3 new entities)
├── TradingBot.Infrastructure/ # EF Core, repositories (ADD migrations)
├── TradingBot.Engine/         # Trading engine (NO CHANGES)
├── TradingBot.Strategies/     # Strategies (NO CHANGES)
├── TradingBot.Analytics/      # Analytics (NO CHANGES)
├── TradingBot.Cli/            # CLI (NO CHANGES)
└── TradingBot.Web/            # Blazor Server ⭐ PRIMARY FOCUS
    ├── Components/            # ADD interactive components
    ├── Services/              # ENHANCE with write operations
    ├── Hubs/                  # ENHANCE TradingHub
    ├── Workers/               # ADD BacktestExecutionWorker
    └── Models/                # ADD DTOs
```

### Key Design Patterns

1. **Clean Architecture**: Core → Infrastructure → Engine → Web (no circular dependencies)
2. **Atomic Design**: Atoms → Molecules → Organisms → Features → Pages (all Tb-prefixed)
3. **Repository Pattern**: Data access abstracted behind interfaces
4. **Service Layer Pattern**: Business logic in services, not controllers/pages
5. **Background Task Queue**: Channel-based queue for async operations (backtests)
6. **SignalR Push**: Real-time updates without polling

---

## Step 3: Database Setup

### Create New Entities in Core

**Location**: `src/TradingBot.Core/Models/Configuration/`

Create three new entity files:
1. `StrategyConfiguration.cs` - Stores custom strategy parameters
2. `RiskSettings.cs` - Stores risk management configuration
3. Verify `BacktestResult.cs` exists in `Models/Backtest/`

**Reference**: See `data-model.md` for complete entity definitions.

### Create EF Core Entity Configurations

**Location**: `src/TradingBot.Infrastructure/Persistence/Configurations/`

Create three configuration files:
1. `StrategyConfigurationEntityConfig.cs`
2. `RiskSettingsEntityConfig.cs`
3. `BacktestResultEntityConfig.cs`

**Example Configuration**:
```csharp
public class StrategyConfigurationEntityConfig : IEntityTypeConfiguration<StrategyConfiguration>
{
    public void Configure(EntityTypeBuilder<StrategyConfiguration> builder)
    {
        builder.ToTable("StrategyConfigurations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StrategyName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.StrategyName).IsUnique();

        builder.Property(x => x.ParametersJson)
            .IsRequired()
            .HasDefaultValue("{}");

        builder.Property(x => x.LastModified).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
```

### Register Configurations in DbContext

**File**: `src/TradingBot.Infrastructure/Persistence/TradingBotDbContext.cs`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configurations ...

    modelBuilder.ApplyConfiguration(new StrategyConfigurationEntityConfig());
    modelBuilder.ApplyConfiguration(new RiskSettingsEntityConfig());
    modelBuilder.ApplyConfiguration(new BacktestResultEntityConfig());
}
```

### Create and Apply Migrations

```bash
# Create migrations (run from repo root)
dotnet ef migrations add AddStrategyConfigurationsTable \
  --project src/TradingBot.Infrastructure \
  --startup-project src/TradingBot.Cli

dotnet ef migrations add AddRiskSettingsTable \
  --project src/TradingBot.Infrastructure \
  --startup-project src/TradingBot.Cli

dotnet ef migrations add AddBacktestResultsTable \
  --project src/TradingBot.Infrastructure \
  --startup-project src/TradingBot.Cli

# Apply migrations
dotnet ef database update \
  --project src/TradingBot.Infrastructure \
  --startup-project src/TradingBot.Cli
```

**Verify**: Check that `tradingbot.db` contains new tables:
```bash
sqlite3 artifacts/bin/TradingBot.Cli/Debug/tradingbot.db ".tables"
# Should show: StrategyConfigurations, RiskSettings, BacktestResults
```

---

## Step 4: Implement Services

### Enhance PortfolioService

**File**: `src/TradingBot.Web/Services/PortfolioService.cs`

Add the `ClosePositionAsync` method:

```csharp
public async Task<bool> ClosePositionAsync(Guid positionId, CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Closing position: {PositionId}", positionId);

        var positions = await _portfolioManager.GetPositionsAsync(cancellationToken);
        var position = positions.FirstOrDefault(p => p.Id == positionId);

        if (position == null)
        {
            _logger.LogWarning("Position not found: {PositionId}", positionId);
            return false;
        }

        var result = await _portfolioManager.ClosePositionAsync(position.Symbol, cancellationToken);

        if (result)
        {
            _logger.LogInformation("Position closed successfully: {PositionId} ({Symbol})", positionId, position.Symbol);

            // Publish SignalR event
            var trade = await _portfolioManager.GetLatestTradeAsync(position.Symbol, cancellationToken);
            await _hubContext.Clients.All.OnPositionClosed(positionId, trade);
        }

        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error closing position: {PositionId}", positionId);
        throw;
    }
}
```

### Enhance StrategyManagementService

**File**: `src/TradingBot.Web/Services/StrategyManagementService.cs`

Add methods:
- `GetStrategyParametersAsync()`
- `ConfigureStrategyAsync()`
- `ResetStrategyToDefaultsAsync()`

**Reference**: See `contracts/IStrategyManagementService.cs` for signatures.

### Enhance BacktestService

**File**: `src/TradingBot.Web/Services/BacktestService.cs`

Replace stub implementation with:
- `RunBacktestAsync()` - Queues backtest to background worker
- `CancelBacktestAsync()` - Cancels running backtest
- `DeleteBacktestAsync()` - Deletes result from DB
- `ExportBacktestTradesToCsvAsync()` - Exports trades

**Reference**: See `contracts/IBacktestService.cs` and `research.md` for implementation patterns.

### Enhance RiskSettingsService

**File**: `src/TradingBot.Web/Services/RiskSettingsService.cs`

Add methods:
- `SaveRiskSettingsAsync()` - Validates and saves settings
- `ResetToDefaultsAsync()` - Resets to defaults

---

## Step 5: Create Background Worker

### BacktestExecutionWorker

**File**: `src/TradingBot.Web/Workers/BacktestExecutionWorker.cs`

```csharp
public class BacktestExecutionWorker : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<BacktestExecutionWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BacktestExecutionWorker(
        IBackgroundTaskQueue taskQueue,
        ILogger<BacktestExecutionWorker> logger,
        IServiceProvider serviceProvider)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Backtest Execution Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing background work item");
            }
        }

        _logger.LogInformation("Backtest Execution Worker stopped");
    }
}
```

### Background Task Queue

**File**: `src/TradingBot.Web/Services/BackgroundTaskQueue.cs`

See `research.md` Section 1 for complete implementation using `System.Threading.Channels`.

---

## Step 6: Enhance SignalR Hub

**File**: `src/TradingBot.Web/Hubs/TradingHub.cs`

Define the client interface:

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
    Task OnStrategyStatusChanged(string strategyName, bool isEnabled);
}

public class TradingHub : Hub<ITradingHubClient>
{
    // No methods needed - all server → client push
}
```

**Reference**: See `contracts/SignalRHubContracts.md` for complete event definitions.

---

## Step 7: Create UI Components

### Atoms (Basic Components)

**Existing**: TbButton, TbInput, TbLabel, TbBadge, TbIcon, TbSpinner, TbToggle

**No new atoms needed** - reuse existing.

### Molecules (Composite Components)

**New Components**:

1. **TbConfirmDialog.razor** - Confirmation dialog for destructive actions
   - **Location**: `src/TradingBot.Web/Components/Molecules/TbConfirmDialog.razor`
   - **Parameters**: Title, Message, ConfirmText, CancelText, OnConfirm, OnCancel
   - **Example**: Confirm before closing position

2. **TbLoadingOverlay.razor** - Full-screen loading indicator
   - **Location**: `src/TradingBot.Web/Components/Molecules/TbLoadingOverlay.razor`
   - **Parameters**: IsVisible, Message
   - **Example**: Show during backtest execution

**Example TbConfirmDialog**:
```razor
@if (IsVisible)
{
    <div class="fixed inset-0 bg-gray-900 bg-opacity-50 flex items-center justify-center z-50" @onclick="HandleCancel">
        <div class="bg-white dark:bg-gray-800 rounded-lg shadow-xl p-6 max-w-md" @onclick:stopPropagation>
            <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">@Title</h3>
            <p class="text-gray-600 dark:text-gray-400 mb-6">@Message</p>

            <div class="flex justify-end gap-3">
                <TbButton Variant="secondary" OnClick="HandleCancel">@CancelText</TbButton>
                <TbButton Variant="danger" OnClick="HandleConfirm">@ConfirmText</TbButton>
            </div>
        </div>
    </div>
}

@code {
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public string Title { get; set; } = "Confirm Action";
    [Parameter] public string Message { get; set; } = "Are you sure?";
    [Parameter] public string ConfirmText { get; set; } = "Confirm";
    [Parameter] public string CancelText { get; set; } = "Cancel";
    [Parameter] public EventCallback OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private async Task HandleConfirm()
    {
        await OnConfirm.InvokeAsync();
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }
}
```

### Organisms (Complex Components)

**New Component**:

**TbSymbolSearchInput.razor** - Autocomplete symbol search
   - **Location**: `src/TradingBot.Web/Components/Organisms/TbSymbolSearchInput.razor`
   - **Features**: Debounced search, dropdown results, keyboard navigation
   - **Integration**: Yahoo Finance API via ISymbolSearchService

### Feature Components

Create new components in `src/TradingBot.Web/Components/Features/`:

**Portfolio/**:
- `TbOpenPositionsTable.razor` - Table with "Close" button per position
- `TbClosePositionDialog.razor` - Wrapper for TbConfirmDialog with position details

**Strategy/**:
- `TbStrategyConfigForm.razor` - Form to edit strategy parameters
- `TbStrategyParameterInput.razor` - Input field for single parameter (handles int/decimal/bool/string types)

**Backtest/**:
- `TbBacktestForm.razor` - Form to configure and submit backtest
- `TbBacktestProgress.razor` - Progress bar with status message
- `TbBacktestRunner.razor` - Container that manages form + progress + results

**Risk/**:
- `TbRiskSettingsForm.razor` - Form to edit risk settings with validation

**Testing Tip**: Each feature component should have a corresponding bUnit test file.

---

## Step 8: Update Pages

### Portfolio.razor

**Enhancements**:
1. Add `TbOpenPositionsTable` component above trade history
2. Wire up close position handler
3. Subscribe to `OnPositionClosed` SignalR event to refresh data

**Example**:
```razor
@page "/portfolio"
@inject IPortfolioService PortfolioService
@inject HubConnection HubConnection
@implements IAsyncDisposable

<!-- Open Positions Section -->
<TbCard Title="Open Positions" CssClass="mb-6">
    @if (_isLoadingPositions)
    {
        <TbSpinner />
    }
    else if (_openPositions != null && _openPositions.Any())
    {
        <TbOpenPositionsTable
            Positions="@_openPositions"
            OnClosePosition="HandleClosePosition" />
    }
    else
    {
        <p class="text-gray-500">No open positions</p>
    }
</TbCard>

<!-- Confirm Close Dialog -->
<TbConfirmDialog
    IsVisible="@_showCloseConfirm"
    Title="Close Position"
    Message="@($"Are you sure you want to close your {_positionToClose?.Symbol} position?")"
    ConfirmText="Close"
    OnConfirm="ConfirmClosePosition"
    OnCancel="@(() => _showCloseConfirm = false)" />

@code {
    private bool _isLoadingPositions = true;
    private IEnumerable<Position>? _openPositions;
    private bool _showCloseConfirm = false;
    private Position? _positionToClose;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to SignalR events
        HubConnection.On<Guid, Trade>("OnPositionClosed", async (positionId, trade) =>
        {
            await LoadOpenPositions();
            InvokeAsync(StateHasChanged);
        });

        await LoadOpenPositions();
    }

    private async Task LoadOpenPositions()
    {
        _isLoadingPositions = true;
        _openPositions = await PortfolioService.GetOpenPositionsAsync();
        _isLoadingPositions = false;
    }

    private void HandleClosePosition(Position position)
    {
        _positionToClose = position;
        _showCloseConfirm = true;
    }

    private async Task ConfirmClosePosition()
    {
        if (_positionToClose == null) return;

        _showCloseConfirm = false;
        var success = await PortfolioService.ClosePositionAsync(_positionToClose.Id);

        if (success)
        {
            _toastService.ShowSuccess($"Position {_positionToClose.Symbol} closed successfully");
        }
        else
        {
            _toastService.ShowError("Failed to close position");
        }

        _positionToClose = null;
    }

    public async ValueTask DisposeAsync()
    {
        HubConnection.Remove("OnPositionClosed");
    }
}
```

### Strategies.razor

**Enhancements**:
1. Add "Configure" button to `TbStrategyCard`
2. Show `TbStrategyConfigForm` in modal when clicked
3. Handle save and update strategy parameters

### Backtest.razor

**Enhancements**:
1. Add `TbBacktestRunner` component above results list
2. Wire up to `IBacktestService.RunBacktestAsync()`
3. Subscribe to `OnBacktestProgress`, `OnBacktestCompleted`, `OnBacktestFailed` events

### RiskSettingsPage.razor

**Enhancements**:
1. Replace read-only display with `TbRiskSettingsForm`
2. Add Save and Reset buttons
3. Subscribe to `OnRiskSettingsChanged` event

---

## Step 9: Register Services in DI

**File**: `src/TradingBot.Web/Program.cs`

```csharp
// Background task queue
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

// Background workers
builder.Services.AddHostedService<BacktestExecutionWorker>();

// SignalR with MessagePack
builder.Services.AddSignalR()
    .AddMessagePackProtocol();

// Hub connection (client-side for Blazor Server)
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

// Map SignalR hub endpoint
app.MapHub<TradingHub>("/tradinghub");
```

---

## Step 10: Testing

### Unit Tests

**Location**: `tests/TradingBot.Web.Tests/Services/`

Create/enhance test files:
- `PortfolioServiceTests.cs`
- `StrategyManagementServiceTests.cs`
- `BacktestServiceTests.cs`
- `RiskSettingsServiceTests.cs`

**Example Test**:
```csharp
[Fact]
public async Task ClosePositionAsync_ValidPosition_ReturnsTrue()
{
    // Arrange
    var portfolioManager = A.Fake<IPortfolioManager>();
    var hubContext = A.Fake<IHubContext<TradingHub, ITradingHubClient>>();
    var logger = A.Fake<ILogger<PortfolioService>>();

    var position = new Position
    {
        Id = Guid.NewGuid(),
        Symbol = "AAPL",
        Quantity = 100m,
        EntryPrice = 150m
    };

    A.CallTo(() => portfolioManager.GetPositionsAsync(A<CancellationToken>._))
        .Returns(new List<Position> { position });
    A.CallTo(() => portfolioManager.ClosePositionAsync(position.Symbol, A<CancellationToken>._))
        .Returns(true);

    var service = new PortfolioService(portfolioManager, logger, hubContext);

    // Act
    var result = await service.ClosePositionAsync(position.Id);

    // Assert
    result.ShouldBeTrue();
    A.CallTo(() => hubContext.Clients.All.OnPositionClosed(position.Id, A<Trade>._))
        .MustHaveHappenedOnceExactly();
}
```

### Component Tests (bUnit)

**Location**: `tests/TradingBot.Web.Tests/Components/`

**Example Test**:
```csharp
public class TbConfirmDialogTests
{
    [Fact]
    public void TbConfirmDialog_ClickConfirm_InvokesCallback()
    {
        // Arrange
        using var ctx = new BunitContext();
        var confirmed = false;

        var cut = ctx.Render<TbConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Delete Item")
            .Add(p => p.Message, "Are you sure?")
            .Add(p => p.OnConfirm, EventCallback.Factory.Create(this, () => confirmed = true)));

        // Act
        var confirmButton = cut.Find("button:contains('Confirm')");
        confirmButton.Click();

        // Assert
        confirmed.ShouldBeTrue();
    }
}
```

### Integration Tests

**Location**: `tests/TradingBot.Web.Tests/Integration/`

Test end-to-end flows:
- `PortfolioManagementIntegrationTests.cs` - Close position flow
- `BacktestExecutionIntegrationTests.cs` - Run backtest flow
- `SignalRIntegrationTests.cs` - Real-time updates

### Run Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~PortfolioServiceTests"

# Run with detailed output
dotnet test --verbosity detailed
```

**Coverage Goal**: 80% overall, 100% for critical paths (ClosePositionAsync, RunBacktestAsync, ValidateRiskSettings).

---

## Step 11: Manual Testing

### Run the Web Application

```bash
# From repo root
dotnet run --project src/TradingBot.Web

# Navigate to: https://localhost:5001
```

### Test Checklist

**Portfolio Management**:
- [ ] View open positions
- [ ] Close a position via UI
- [ ] Confirm dialog appears
- [ ] Position closes and appears in trade history
- [ ] Real-time update reflects closure without refresh

**Strategy Configuration**:
- [ ] View strategies with current parameters
- [ ] Click "Configure" on a strategy
- [ ] Modify parameters (e.g., change MA period from 12 to 10)
- [ ] Save configuration
- [ ] Verify parameters persist after page refresh

**Backtesting**:
- [ ] Fill backtest form (strategy, symbol, date range, capital)
- [ ] Submit backtest
- [ ] See progress indicator updating
- [ ] View results (metrics, equity curve, trade list)
- [ ] Export trades to CSV

**Risk Settings**:
- [ ] View current risk settings
- [ ] Modify settings (e.g., change max position size to 15%)
- [ ] Save settings
- [ ] Click "Reset to Defaults"
- [ ] Verify defaults are restored

**Real-Time Updates**:
- [ ] Open Dashboard
- [ ] Trigger a trade (via strategy or manual position close)
- [ ] Verify equity/P&L updates within 2 seconds
- [ ] No page refresh required

---

## Step 12: Build and Code Quality

### Build with Analyzers

```bash
# Clean build with code analysis
dotnet clean
dotnet build /p:RunAnalyzers=true

# Should have ZERO warnings (TreatWarningsAsErrors=true)
```

### Fix StyleCop Violations

Common violations:
- Missing XML doc comments on public APIs
- Missing file headers
- Incorrect ordering of using statements
- Incorrect spacing/indentation

**Auto-fix where possible**:
```bash
# Use dotnet format
dotnet format
```

---

## Troubleshooting

### Database Migration Issues

**Problem**: Migration fails with "table already exists"
**Solution**:
```bash
# Drop database and recreate
rm artifacts/bin/TradingBot.Cli/Debug/tradingbot.db
dotnet ef database update
```

### SignalR Connection Issues

**Problem**: Hub connection fails with "404 Not Found"
**Solution**: Ensure `app.MapHub<TradingHub>("/tradinghub");` is called in `Program.cs` AFTER `app.UseRouting()`.

**Problem**: Events not received by client
**Solution**: Check subscription syntax. Event names are case-sensitive. Use exact names from `ITradingHubClient`.

### Tailwind CSS Not Compiling

**Problem**: CSS changes not reflected
**Solution**:
```bash
cd src/TradingBot.Web
npm run css:build
```

**Problem**: `npm run css:build` fails
**Solution**:
```bash
cd src/TradingBot.Web
rm -rf node_modules package-lock.json
npm install
npm run css:build
```

### Test Failures

**Problem**: bUnit tests fail with "Render method not found"
**Solution**: Use `ctx.Render<T>()` instead of `ctx.RenderComponent<T>()` (bUnit 2.0 API change).

**Problem**: SignalR integration tests timeout
**Solution**: Increase `Task.Delay()` to 500ms to allow event propagation.

---

## Common Patterns

### Loading States

Always show loading indicators:
```razor
@if (_isLoading)
{
    <TbSpinner />
}
else
{
    <!-- Render content -->
}
```

### Error Handling

Always catch and log exceptions:
```csharp
try
{
    await service.DoSomethingAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error doing something");
    _toastService.ShowError("Failed to complete operation");
}
```

### Form Validation

Always use `EditForm` with validators:
```razor
<EditForm Model="@_request" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <TbFormField Label="Symbol" For="@(() => _request.Symbol)">
        <InputText @bind-Value="_request.Symbol" class="..." />
    </TbFormField>

    <TbButton Type="submit" Disabled="@_isSubmitting">Submit</TbButton>
</EditForm>
```

### Dispose SignalR Subscriptions

Always implement `IAsyncDisposable`:
```csharp
@implements IAsyncDisposable

protected override Task OnInitializedAsync()
{
    HubConnection.On<T>("EventName", HandleEvent);
}

public async ValueTask DisposeAsync()
{
    HubConnection.Remove("EventName");
}
```

---

## Next Steps

After completing this quickstart:

1. **Run `/speckit.tasks`** to generate the detailed task breakdown (`tasks.md`)
2. **Run `/speckit.implement`** to execute the implementation plan
3. **Create Pull Request** when all tasks are complete and tests pass
4. **Request code review** from team members

---

## Resources

- **Spec**: [spec.md](./spec.md)
- **Plan**: [plan.md](./plan.md)
- **Research**: [research.md](./research.md)
- **Data Model**: [data-model.md](./data-model.md)
- **Contracts**: [contracts/](./contracts/)
- **Project Constitution**: [.specify/memory/constitution.md](../../.specify/memory/constitution.md)
- **CLAUDE.md**: [Project-specific guidelines](../../CLAUDE.md)

**External Documentation**:
- [Blazor Server Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [SignalR Docs](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [EF Core Docs](https://learn.microsoft.com/en-us/ef/core/)
- [Tailwind CSS Docs](https://tailwindcss.com/docs)
- [bUnit Docs](https://bunit.dev)

---

**Happy coding!** 🚀
