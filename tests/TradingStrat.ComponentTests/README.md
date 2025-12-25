# TradingStrat.ComponentTests

Comprehensive BUnit test suite for all Blazor components in TradingStrat.Web.

## Overview

This project contains unit tests for 53+ Blazor components using **BUnit 2.4.2**, targeting >90% test coverage. Tests follow the TDD (Test-Driven Development) approach: test existing components first, then safely refactor large components with test coverage as a safety net.

## Tech Stack

- **BUnit 2.4.2** - Blazor component testing library
- **xUnit 2.9.3** - Test framework (consistent with other test projects)
- **FakeItEasy 8.3.0** - Mocking library for application layer dependencies
- **Shouldly 4.3.0** - Fluent assertion library
- **.NET 10.0** - Target framework

## Project Structure

```
TradingStrat.ComponentTests/
├── Infrastructure/
│   └── BunitTestContext.cs          # Base class for all component tests
├── TestDoubles/
│   ├── FakeLocalStorageService.cs   # In-memory localStorage
│   ├── FakeNotificationService.cs   # In-memory notifications
│   ├── FakeProgressService.cs       # In-memory progress tracking
│   ├── FakePortfolioStateService.cs # In-memory portfolio state
│   ├── FakeChatStateService.cs      # In-memory chat state
│   └── FakeUserPreferencesService.cs # In-memory user prefs
├── Shared/                          # Tests for 35 shared components
│   ├── MetricCardTests.cs
│   ├── LoadingSpinnerTests.cs
│   ├── NotificationToastTests.cs
│   └── ... (32 more files)
├── Pages/                           # Tests for 13 page components
│   ├── HomeTests.cs
│   ├── BacktestTests.cs
│   ├── PortfolioDashboardTests.cs
│   └── ... (10 more files)
└── Layout/                          # Tests for 7 layout components
    ├── NavMenuTests.cs
    ├── MainLayoutTests.cs
    └── ... (5 more files)
```

## Quick Start

### Running Tests

```bash
# Run all component tests
dotnet test tests/TradingStrat.ComponentTests

# Run specific test class
dotnet test --filter "FullyQualifiedName~MetricCardTests"

# Run tests for a specific category
dotnet test --filter "FullyQualifiedName~Shared"
dotnet test --filter "FullyQualifiedName~Pages"
dotnet test --filter "FullyQualifiedName~Layout"

# Run with verbose output
dotnet test tests/TradingStrat.ComponentTests -v detailed

# Run with code coverage
dotnet test tests/TradingStrat.ComponentTests /p:CollectCoverage=true
```

### Writing a Test

```csharp
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;

namespace TradingStrat.ComponentTests.Shared;

public class MetricCardTests : BunitTestContext
{
    [Fact]
    public void MetricCard_WithValidParameters_RendersCorrectly()
    {
        // Arrange
        string title = "Total Return";
        string value = "+15.50%";
        string trend = "up";

        // Act
        var cut = RenderComponent<MetricCard>(parameters => parameters
            .Add(p => p.Title, title)
            .Add(p => p.Value, value)
            .Add(p => p.Trend, trend));

        // Assert
        cut.Find("h3").TextContent.ShouldBe(title);
        cut.Find(".metric-value").TextContent.ShouldBe(value);
        cut.Markup.ShouldContain("metric-positive");
    }
}
```

## Test Doubles

### Available Fake Services

All fake services are automatically registered in `BunitTestContext` and available via protected properties:

| Service | Purpose | Access |
|---------|---------|--------|
| **FakeLocalStorage** | In-memory localStorage | `FakeLocalStorage` property |
| **FakeNotificationService** | In-memory notifications | `FakeNotificationService` property |
| **FakeProgressService** | In-memory progress tracking | `FakeProgressService` property |
| **FakePortfolioState** | In-memory portfolio state | `FakePortfolioState` property |
| **FakeChatState** | In-memory chat history | `FakeChatState` property |
| **FakeUserPreferences** | In-memory user preferences | `FakeUserPreferences` property |

### Using Fake Services

```csharp
public class NotificationToastTests : BunitTestContext
{
    [Fact]
    public async Task NotificationToast_DisplaysNotification()
    {
        // Arrange - Add notification via fake service
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Success,
            "Success",
            "Operation completed successfully");

        // Act
        var cut = RenderComponent<NotificationToastContainer>();

        // Assert
        cut.Markup.ShouldContain("Success");
        cut.Markup.ShouldContain("Operation completed successfully");
    }
}
```

### Mocking Application Dependencies

For components that depend on application layer interfaces (use cases, ports), use FakeItEasy:

```csharp
using FakeItEasy;
using TradingStrat.Application.Ports.Inbound;

public class PortfolioDashboardTests : BunitTestContext
{
    [Fact]
    public async Task PortfolioDashboard_LoadsPortfolioOnInitialize()
    {
        // Arrange
        var getSnapshotUseCase = A.Fake<IGetPortfolioSnapshotUseCase>();
        var expectedSnapshot = new PortfolioSnapshot
        {
            Id = 1,
            TotalValue = 15000m
        };

        A.CallTo(() => getSnapshotUseCase.ExecuteAsync(1, null))
            .Returns(Task.FromResult(expectedSnapshot));

        Services.AddScoped(_ => getSnapshotUseCase);

        // Act
        var cut = RenderComponent<PortfolioDashboard>(parameters => parameters
            .Add(p => p.PortfolioId, 1));

        // Assert
        A.CallTo(() => getSnapshotUseCase.ExecuteAsync(1, null))
            .MustHaveHappenedOnceExactly();
    }
}
```

## Testing Patterns

### 1. Basic Component Rendering

```csharp
[Fact]
public void Component_WithoutParameters_RendersSuccessfully()
{
    // Act
    var cut = RenderComponent<LoadingSpinner>();

    // Assert
    cut.Markup.ShouldNotBeEmpty();
}
```

### 2. Component with Parameters

```csharp
[Theory]
[InlineData("sm", "h-4 w-4")]
[InlineData("md", "h-8 w-8")]
[InlineData("lg", "h-12 w-12")]
public void LoadingSpinner_WithSize_AppliesCorrectClasses(string size, string expectedClass)
{
    // Act
    var cut = RenderComponent<LoadingSpinner>(parameters => parameters
        .Add(p => p.Size, size));

    // Assert
    cut.Markup.ShouldContain(expectedClass);
}
```

### 3. Event Callbacks

```csharp
[Fact]
public async Task Dialog_OnCloseButtonClick_InvokesCallback()
{
    // Arrange
    bool closeCalled = false;
    var cut = RenderComponent<Dialog>(parameters => parameters
        .Add(p => p.IsOpen, true)
        .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true)));

    // Act
    var closeButton = cut.Find("[data-testid='close-button']");
    await closeButton.ClickAsync(new MouseEventArgs());

    // Assert
    closeCalled.ShouldBeTrue();
}
```

### 4. Async Operations

```csharp
[Fact]
public async Task Component_LoadsDataAsync()
{
    // Arrange
    var useCase = A.Fake<IGetDataUseCase>();
    A.CallTo(() => useCase.ExecuteAsync(A<string>._))
        .Returns(Task.FromResult(new Data()));

    Services.AddScoped(_ => useCase);

    // Act
    var cut = RenderComponent<MyComponent>();
    await cut.InvokeAsync(async () => await Task.Delay(100)); // Wait for async init

    // Assert
    cut.Markup.ShouldContain("data loaded");
}
```

### 5. Form Validation

```csharp
[Fact]
public async Task Form_WithInvalidData_ShowsValidationErrors()
{
    // Arrange
    var cut = RenderComponent<CreatePortfolioForm>();

    // Act
    var nameInput = cut.Find("input[name='name']");
    await nameInput.FillAsync("A"); // Too short

    var submitButton = cut.Find("button[type='submit']");
    await submitButton.ClickAsync(new MouseEventArgs());

    // Assert
    cut.Markup.ShouldContain("validation-error");
}
```

### 6. State Changes

```csharp
[Fact]
public async Task Component_RespondsToStateChanges()
{
    // Arrange
    Services.AddScoped(_ => FakePortfolioState);
    var cut = RenderComponent<PortfolioSelector>();

    // Act
    await FakePortfolioState.SetSelectedPortfolioAsync(123);
    await cut.InvokeAsync(async () => await Task.Delay(50)); // Let event propagate

    // Assert
    cut.Markup.ShouldContain("123");
}
```

## Best Practices

### DO

✅ **Inherit from BunitTestContext** for automatic fake service setup
✅ **Use Arrange-Act-Assert pattern** for test clarity
✅ **Name tests descriptively**: `ComponentName_Scenario_ExpectedBehavior`
✅ **Test one thing per test** - focused, isolated tests
✅ **Use Theory for parameterized tests** - reduces duplication
✅ **Verify both markup and behavior** - comprehensive coverage
✅ **Test edge cases** - null, empty, invalid inputs
✅ **Test accessibility** - ARIA labels, keyboard navigation
✅ **Clean up after tests** - Dispose() is called automatically

### DON'T

❌ **Don't use real services** - always use fakes/mocks
❌ **Don't test framework behavior** - test your component logic
❌ **Don't make tests dependent on each other** - each test must be isolated
❌ **Don't hardcode values** - use constants or test data builders
❌ **Don't skip error scenarios** - test both happy and unhappy paths
❌ **Don't forget async/await** - properly handle async operations
❌ **Don't test implementation details** - test user-visible behavior

## Component Test Coverage

### Simple Components (6 components, ~40 tests)
- MetricCard, LoadingSpinner, ProgressIndicator
- EquityChart, TradingViewWidget, AlertMessage

### Medium Components (14 components, ~147 tests)
- DataTable, TradeTable, NotificationToast, DataSummaryCard
- PageHeader, Dialog, MetricCardGrid, MetricsGrid
- NotificationBell, LoadingState, FormWrapper, FormInputGroup
- EmptyState, NotificationToastContainer

### Complex Components (9 components, ~155 tests)
- RuleListComponent, NotificationCenter, StrategyForm
- AiDataAnalysisPanel, StrategyAnalysisPanel, ConfirmDialog
- DateRangeInput, AiAssistantWidget, TickerInput

### Pages (13 components, ~285 tests)
- Home, Settings, DataManagement, Backtest, LiveAnalysis
- Comparison, Portfolios, PortfolioDashboard, Rebalancing
- PerformanceAnalytics, StrategyLibrary, StrategyBuilder
- StrategyOptimization

### Layouts (7 components, ~94 tests)
- NavMenu, ReconnectModal, TopBar, BottomPanel
- LeftSidebar, MainLayout, AiPanel

### **Total: 55+ components, 832+ comprehensive tests**

## Continuous Integration

Tests run automatically in CI/CD pipeline:

```yaml
- name: Run Component Tests
  run: dotnet test tests/TradingStrat.ComponentTests --logger trx --collect:"XPlat Code Coverage"

- name: Verify Coverage
  run: dotnet test tests/TradingStrat.ComponentTests /p:CollectCoverage=true /p:Threshold=90
```

## Troubleshooting

### Test Fails with "No service for type..."

**Solution**: Register the missing service in your test class:

```csharp
var fakeService = A.Fake<IMissingService>();
Services.AddScoped(_ => fakeService);
```

### Component Requires JSRuntime

**Solution**: BUnit provides a fake JSRuntime automatically. For localStorage, use `FakeLocalStorage`.

### Async operation not completing

**Solution**: Use `await cut.InvokeAsync(async () => await Task.Delay(N))` to wait for async operations.

### Event handler not firing

**Solution**: Ensure you're using `await element.ClickAsync()` instead of synchronous click.

## Resources

- [BUnit Documentation](https://bunit.dev/)
- [BUnit GitHub](https://github.com/bUnit-dev/bUnit)
- [xUnit Documentation](https://xunit.net/)
- [Shouldly Documentation](https://github.com/shouldly/shouldly)
- [FakeItEasy Documentation](https://fakeiteasy.github.io/)

## License

Same as parent project (TradingStrat).
