namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Strategy Library page (/strategies/library).
/// Tests built-in strategies display, custom strategy management, and navigation.
/// </summary>
public class StrategyLibraryPageTests : BaseTest
{
    public StrategyLibraryPageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task StrategyLibrary_WhenLoaded_ShouldDisplayTitle()
    {
        // Arrange & Act
        await NavigateToAsync("/strategies/library");

        // Assert
        ILocator title = Page!.Locator("h1:has-text('Strategy Library')");
        await title.ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyLibrary_WhenLoaded_ShouldShowBothTabs()
    {
        // Arrange & Act
        await NavigateToAsync("/strategies/library");

        // Assert
        await Page!.Locator("button:has-text('Built-in Strategies')").ShouldBeVisibleAsync();
        await Page!.Locator("button:has-text('My Strategies')").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyLibrary_BuiltInTab_ShouldDisplayFiveStrategies()
    {
        // Arrange & Act
        await NavigateToAsync("/strategies/library");

        // Assert - Should show 5 built-in strategies
        await Page!.Locator("text=Moving Average Crossover").ShouldBeVisibleAsync();
        await Page!.Locator("text=RSI Strategy").ShouldBeVisibleAsync();
        await Page!.Locator("text=MACD Strategy").ShouldBeVisibleAsync();
        await Page!.Locator("text=Machine Learning").ShouldBeVisibleAsync();
        await Page!.Locator("text=Ichimoku Cloud").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyLibrary_BuiltInTab_ShouldShowRunBacktestButton()
    {
        // Arrange & Act
        await NavigateToAsync("/strategies/library");

        // Assert - Each built-in strategy should have "Run Backtest" button
        IReadOnlyList<ILocator> backtestButtons = await Page!.Locator("button:has-text('Run Backtest')").AllAsync();
        backtestButtons.Count.ShouldBeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task StrategyLibrary_BuiltInStrategy_ShouldNavigateToBacktest()
    {
        // Arrange
        await NavigateToAsync("/strategies/library");

        // Act - Click "Run Backtest" for first strategy
        await Page!.Locator("button:has-text('Run Backtest')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should navigate to backtest page with strategy query parameter
        await Page.WaitForURLAsync("**/backtest*");
        Page.Url.ShouldContain("/backtest?strategy=");
    }

    [Fact]
    public async Task StrategyLibrary_MyStrategiesTab_ShouldBeAccessible()
    {
        // Arrange
        await NavigateToAsync("/strategies/library");

        // Act
        await Page!.Locator("button:has-text('My Strategies')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Tab should be active (check for active styling)
        ILocator myStrategiesButton = Page.Locator("button:has-text('My Strategies')");
        string? className = await myStrategiesButton.GetAttributeAsync("class");
        className.ShouldNotBeNull();
        className.ShouldContain("border-blue-500"); // Active tab styling
    }

    [Fact]
    public async Task StrategyLibrary_MyStrategiesTab_WhenEmpty_ShouldShowEmptyState()
    {
        // Arrange & Act
        await NavigateToAsync("/strategies/library");
        await Page!.Locator("button:has-text('My Strategies')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should show empty state message
        await Page.Locator("text=No Custom Strategies Yet").ShouldBeVisibleAsync();
        await Page.Locator("text=Create your first custom strategy to get started").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyLibrary_CreateStrategyButton_ShouldNavigateToBuilder()
    {
        // Arrange
        await NavigateToAsync("/strategies/library");

        // Act
        await Page!.Locator("button:has-text('Create Strategy')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert
        await Page.WaitForURLAsync("**/strategies/builder");
        Page.Url.ShouldContain("/strategies/builder");
    }

    [Fact]
    public async Task StrategyLibrary_WithCustomStrategy_ShouldDisplayInMyStrategies()
    {
        // Arrange - Create a custom strategy first
        await NavigateToAsync("/strategies/builder");
        string strategyName = $"Library Test Strategy {DateTime.Now.Ticks}";

        await Page!.Locator("#Name").FillAsync(strategyName);
        await Page.Locator("#Author").FillAsync("E2E Test");
        await Page.Locator("#Category").FillAsync("Test");
        await Page.Locator("textarea").FillAsync("Test strategy for library");

        // Add entry rule
        await Page.Locator("button:has-text('Add First Rule')").First.ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.Locator("[data-testid='rule-0-indicator']").SelectOptionAsync("RSI");
        await Page.WaitForBlazorAsync();
        await Page.Locator("[data-testid='rule-0-operator']").SelectOptionAsync("LessThan");
        await Page.Locator("[data-testid='rule-0-value']").FillAsync("30");

        // Add exit rule
        ILocator exitSection = Page.Locator(".card:has(h2:has-text('Exit Rules'))");
        await exitSection.Locator("button:has-text('Add First Rule')").ClickAsync();
        await Page.WaitForBlazorAsync();
        await exitSection.Locator("[data-testid='rule-0-indicator']").SelectOptionAsync("RSI");
        await Page.WaitForBlazorAsync();
        await exitSection.Locator("[data-testid='rule-0-operator']").SelectOptionAsync("GreaterThan");
        await exitSection.Locator("[data-testid='rule-0-value']").FillAsync("70");

        // Submit
        await Page.Locator("button[type='submit']:has-text('Create Strategy')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Wait for navigation to library page
        await Page.WaitForURLAsync("**/strategies/library");
        await Page.WaitForBlazorAsync();

        // Act - Switch to My Strategies tab
        await Page.Locator("button:has-text('My Strategies')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should display the created strategy
        await Page.Locator($"text={strategyName}").ShouldBeVisibleAsync();
        await Page.Locator("text=Test strategy for library").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyLibrary_CustomStrategyCard_ShouldShowAllActions()
    {
        // Arrange - Create a custom strategy
        await CreateTestStrategyAsync("Action Test Strategy");
        await NavigateToAsync("/strategies/library");
        await Page!.Locator("button:has-text('My Strategies')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should show all action buttons (scope to first strategy card)
        ILocator firstCard = Page.Locator(".bg-white.dark\\:bg-zinc-900").First;
        await firstCard.Locator("button:has-text('Edit')").ShouldBeVisibleAsync();
        await firstCard.Locator("button:has-text('Clone')").ShouldBeVisibleAsync();
        await firstCard.Locator("button:has-text('Optimize')").ShouldBeVisibleAsync();
        await firstCard.Locator("button:has-text('Test')").ShouldBeVisibleAsync();
        await firstCard.Locator("button:has-text('Delete')").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyLibrary_EditButton_ShouldNavigateToBuilderWithId()
    {
        // Arrange - Create a custom strategy
        await CreateTestStrategyAsync("Edit Test Strategy");
        await NavigateToAsync("/strategies/library");
        await Page!.Locator("button:has-text('My Strategies')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Act
        await Page.Locator("button:has-text('Edit')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should navigate to builder with ID
        await Page.WaitForURLAsync("**/strategies/builder/*");
        Page.Url.ShouldMatch(@"/strategies/builder/\d+");
    }

    [Fact]
    public async Task StrategyLibrary_TestButton_ShouldNavigateToBacktestWithCustomStrategyId()
    {
        // Arrange - Create a custom strategy
        await CreateTestStrategyAsync("Backtest Test Strategy");
        await NavigateToAsync("/strategies/library");
        await Page!.Locator("button:has-text('My Strategies')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Act
        await Page.Locator("button:has-text('Test')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should navigate to backtest with customStrategyId parameter
        await Page.WaitForURLAsync("**/backtest*");
        Page.Url.ShouldContain("/backtest?customStrategyId=");
    }

    [Fact]
    public async Task StrategyLibrary_CloneButton_ShouldCreateCopy()
    {
        // Arrange - Create a custom strategy
        string originalName = $"Clone Source {DateTime.Now.Ticks}";
        await CreateTestStrategyAsync(originalName);
        await NavigateToAsync("/strategies/library");
        await Page!.Locator("button:has-text('My Strategies')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Count strategies before cloning
        int countBefore = await Page.Locator("text=Entry Rules:").CountAsync();

        // Act
        await Page.Locator("button:has-text('Clone')").First.ClickAsync();
        await Page.WaitForBlazorAsync();
        await Task.Delay(1000); // Wait for clone operation

        // Assert - Should show cloned strategy with "(Copy)" suffix
        await Page.Locator($"text={originalName} (Copy)").ShouldBeVisibleAsync();

        // Should have one more strategy
        int countAfter = await Page.Locator("text=Entry Rules:").CountAsync();
        countAfter.ShouldBe(countBefore + 1);
    }

    [Fact]
    public async Task StrategyLibrary_DeleteButton_ShouldShowConfirmation()
    {
        // Arrange - Create a custom strategy
        await CreateTestStrategyAsync("Delete Test Strategy");
        await NavigateToAsync("/strategies/library");
        await Page!.Locator("button:has-text('My Strategies')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Act - Click delete button
        await Page.Locator("button:has-text('Delete')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Dialog should appear with confirmation message
        ILocator dialog = Page.Locator("[role='dialog']");
        await dialog.WaitForAsync();
        await dialog.Locator("text=Delete Strategy").ShouldBeVisibleAsync();
        await dialog.Locator("strong:has-text('Delete Test Strategy')").ShouldBeVisibleAsync();
        await dialog.Locator("text=This action cannot be undone").ShouldBeVisibleAsync();

        // Cancel deletion
        await dialog.Locator("button:has-text('Cancel')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Strategy should still be visible (deletion cancelled)
        await Page.Locator("h3:has-text('Delete Test Strategy')").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyLibrary_DeleteConfirmed_ShouldRemoveStrategy()
    {
        // Arrange - Create a custom strategy
        string strategyName = $"Delete Confirmed {DateTime.Now.Ticks}";
        await CreateTestStrategyAsync(strategyName);
        await NavigateToAsync("/strategies/library");
        await Page!.Locator("button:has-text('My Strategies')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Act - Click delete button
        await Page.Locator("button:has-text('Delete')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Wait for dialog to appear
        ILocator dialog = Page.Locator("[role='dialog']");
        await dialog.WaitForAsync();

        // Confirm deletion by clicking Delete button in dialog
        await dialog.Locator("button:has-text('Delete')").ClickAsync();
        await Page.WaitForBlazorAsync();
        await Task.Delay(2000); // Wait for deletion to complete and list to refresh

        // Assert - Strategy should be removed (check specifically for H3 heading)
        ILocator strategyHeading = Page.Locator($"h3:has-text('{strategyName}')");
        int count = await strategyHeading.CountAsync();
        count.ShouldBe(0);
    }

    [Fact]
    public async Task StrategyLibrary_StrategyCard_ShouldDisplayMetadata()
    {
        // Arrange - Create a custom strategy with specific metadata
        string name = $"Metadata Test {DateTime.Now.Ticks}";
        string category = "Momentum";
        string description = "Test description for metadata";

        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("#Name").FillAsync(name);
        await Page.Locator("#Author").FillAsync("Test Author");
        await Page.Locator("#Category").FillAsync(category);
        await Page.Locator("textarea").FillAsync(description);

        // Add minimal rules
        await Page.Locator("button:has-text('Add First Rule')").First.ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.Locator("[data-testid='rule-0-indicator']").SelectOptionAsync("RSI");
        await Page.WaitForBlazorAsync();

        ILocator exitSection = Page.Locator(".card:has(h2:has-text('Exit Rules'))");
        await exitSection.Locator("button:has-text('Add First Rule')").ClickAsync();
        await Page.WaitForBlazorAsync();

        await Page.Locator("button[type='submit']:has-text('Create Strategy')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Wait for navigation to library page
        await Page.WaitForURLAsync("**/strategies/library", new() { Timeout = 60000 });
        await Page.WaitForBlazorAsync();
        await Task.Delay(1000); // Extra delay to ensure page is fully loaded

        // Act
        await Page.Locator("button:has-text('My Strategies')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should display all metadata
        await Page.Locator($"text={name}").ShouldBeVisibleAsync();
        await Page.Locator($"text={description}").ShouldBeVisibleAsync();
        await Page.Locator($"text={category}").ShouldBeVisibleAsync();
        await Page.Locator("text=Test Author").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyLibrary_TabSwitch_ShouldUpdateActiveState()
    {
        // Arrange
        await NavigateToAsync("/strategies/library");

        // Built-in tab should be active by default
        ILocator builtInTab = Page!.Locator("button:has-text('Built-in Strategies')");
        string? builtInClass = await builtInTab.GetAttributeAsync("class");
        builtInClass.ShouldNotBeNull();
        builtInClass.ShouldContain("border-blue-500");

        // Act - Switch to My Strategies
        await Page.Locator("button:has-text('My Strategies')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - My Strategies should be active
        ILocator myStrategiesTab = Page.Locator("button:has-text('My Strategies')");
        string? myStrategiesClass = await myStrategiesTab.GetAttributeAsync("class");
        myStrategiesClass.ShouldNotBeNull();
        myStrategiesClass.ShouldContain("border-blue-500");

        // Built-in tab should be inactive
        builtInClass = await builtInTab.GetAttributeAsync("class");
        builtInClass.ShouldNotBeNull();
        builtInClass.ShouldNotContain("border-blue-500");
    }

    /// <summary>
    /// Helper method to create a test strategy with minimal required fields.
    /// </summary>
    private async Task CreateTestStrategyAsync(string name)
    {
        await NavigateToAsync("/strategies/builder");
        await Page!.WaitForBlazorAsync();

        await Page!.Locator("#Name").FillAsync(name);
        await Page.Locator("#Author").FillAsync("E2E Test");
        await Page.Locator("#Category").FillAsync("Test");
        await Page.Locator("textarea").FillAsync("Test strategy");

        // Add entry rule
        await Page.Locator("button:has-text('Add First Rule')").First.ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.Locator("[data-testid='rule-0-indicator']").SelectOptionAsync("RSI");
        await Page.WaitForBlazorAsync();
        await Page.Locator("[data-testid='rule-0-operator']").SelectOptionAsync("LessThan");
        await Page.Locator("[data-testid='rule-0-value']").FillAsync("30");

        // Add exit rule
        ILocator exitSection = Page.Locator(".card:has(h2:has-text('Exit Rules'))");
        await exitSection.Locator("button:has-text('Add First Rule')").ClickAsync();
        await Page.WaitForBlazorAsync();
        await exitSection.Locator("[data-testid='rule-0-indicator']").SelectOptionAsync("RSI");
        await Page.WaitForBlazorAsync();
        await exitSection.Locator("[data-testid='rule-0-operator']").SelectOptionAsync("GreaterThan");
        await exitSection.Locator("[data-testid='rule-0-value']").FillAsync("70");

        // Submit
        await Page.Locator("button[type='submit']:has-text('Create Strategy')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Wait for navigation to library page
        await Page.WaitForURLAsync("**/strategies/library");
        await Page.WaitForBlazorAsync();
    }
}
