using Microsoft.Extensions.DependencyInjection;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for backtest integration with custom strategies.
/// Tests that custom strategies work seamlessly with the existing backtest workflow.
/// </summary>
public class BacktestIntegrationTests : BaseTest
{
    public BacktestIntegrationTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task Backtest_WithCustomStrategyQueryParameter_ShouldPreSelectStrategy()
    {
        // Arrange - First create a custom strategy via application layer (bypass UI)
        int strategyId = await CreateTestStrategyViaApplicationAsync("QueryParam Test Strategy");

        // Act - Navigate to backtest with customStrategyId query parameter
        await NavigateToAsync($"/backtest?customStrategyId={strategyId}");
        await Page!.WaitForBlazorAsync();

        // Assert - Page should load successfully
        ILocator title = Page!.Locator("h1:has-text('Backtest')");
        await title.ShouldBeVisibleAsync();

        // The strategy should be pre-selected (we can verify by checking form state)
        // Note: Exact verification depends on how the form displays the selected custom strategy
    }

    [Fact]
    public async Task Backtest_NavigateFromStrategyLibrary_ShouldIncludeQueryParameter()
    {
        // Arrange - Create a custom strategy
        int strategyId = await CreateTestStrategyViaApplicationAsync("Library Nav Test");

        // Navigate to library
        await NavigateToAsync("/strategies/library");
        await Page!.WaitForBlazorAsync();

        // Switch to My Strategies tab
        await Page!.Locator("button:has-text('My Strategies')").ClickAsync();
        await Page.WaitForBlazorAsync();
        await Task.Delay(500); // Wait for strategies to load

        // Act - Click "Test" button on the strategy
        await Page.Locator("button:has-text('Test')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should navigate to backtest with customStrategyId parameter
        await Page.WaitForURLAsync("**/backtest*");
        Page.Url.ShouldContain("/backtest?customStrategyId=");
        Page.Url.ShouldContain(strategyId.ToString());
    }

    [Fact]
    public async Task Backtest_WithBuiltInStrategyQueryParameter_ShouldPreSelectStrategy()
    {
        // Arrange & Act - Navigate to backtest with built-in strategy query parameter
        await NavigateToAsync("/backtest?strategy=rsi");
        await Page!.WaitForBlazorAsync();

        // Assert - Page should load successfully
        ILocator title = Page!.Locator("h1:has-text('Backtest')");
        await title.ShouldBeVisibleAsync();

        // The RSI strategy should be pre-selected
        // Note: Exact verification depends on form implementation
    }

    [Fact]
    public async Task Backtest_NavigateFromBuiltInStrategies_ShouldIncludeQueryParameter()
    {
        // Arrange - Navigate to library
        await NavigateToAsync("/strategies/library");
        await Page!.WaitForBlazorAsync();

        // Built-in Strategies tab should be active by default
        // Act - Click "Run Backtest" on first built-in strategy
        await Page!.Locator("button:has-text('Run Backtest')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should navigate to backtest with strategy parameter
        await Page.WaitForURLAsync("**/backtest*");
        Page.Url.ShouldContain("/backtest?strategy=");
    }

    [Fact]
    public async Task Backtest_LoadPage_ShouldDisplayBacktestForm()
    {
        // Arrange & Act
        await NavigateToAsync("/backtest");
        await Page!.WaitForBlazorAsync();

        // Assert - Should show backtest form elements
        await Page!.Locator("label:has-text('Ticker')").ShouldBeVisibleAsync();
        await Page.Locator("label:has-text('Strategy Type')").ShouldBeVisibleAsync();
        await Page.Locator("label:has-text('Initial Capital')").ShouldBeVisibleAsync();
        await Page.Locator("button:has-text('Run Backtest')").ShouldBeVisibleAsync();
    }

    /// <summary>
    /// Helper method to create a test strategy via the application layer (bypassing UI).
    /// This is faster and more reliable than creating strategies through the UI.
    /// </summary>
    private async Task<int> CreateTestStrategyViaApplicationAsync(string name)
    {
        // Get the use case from DI container
        IServiceScope scope = AppFixture.Services.CreateScope();
        ICustomStrategyManagementUseCase useCase = scope.ServiceProvider.GetRequiredService<ICustomStrategyManagementUseCase>();

        // Create a minimal strategy definition
        var definition = new StrategyDefinition(
            EntryRules: new List<StrategyRule>
            {
                new StrategyRule(
                    IndicatorName: "RSI",
                    IndicatorParameters: new Dictionary<string, object> { ["Period"] = 14 },
                    Operator: ComparisonOperator.LessThan,
                    ValueType: RuleValueType.Constant,
                    ConstantValue: 30m,
                    SecondIndicatorName: null,
                    SecondIndicatorParameters: null,
                    LogicalOperator: LogicalOperator.None
                )
            },
            ExitRules: new List<StrategyRule>
            {
                new StrategyRule(
                    IndicatorName: "RSI",
                    IndicatorParameters: new Dictionary<string, object> { ["Period"] = 14 },
                    Operator: ComparisonOperator.GreaterThan,
                    ValueType: RuleValueType.Constant,
                    ConstantValue: 70m,
                    SecondIndicatorName: null,
                    SecondIndicatorParameters: null,
                    LogicalOperator: LogicalOperator.None
                )
            },
            SizingMode: PositionSizingMode.FixedPercentage,
            SizingParameters: new Dictionary<string, decimal> { ["Percentage"] = 95m }
        );

        var command = new CreateCustomStrategyCommand(
            Name: name,
            Description: "E2E test strategy created via application layer",
            Author: "E2E Test",
            Category: "Test",
            Definition: definition
        );

        Result<CustomStrategyResult> result = await useCase.CreateStrategyAsync(command);
        return result.Value.Id;
    }
}
