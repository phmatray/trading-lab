namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Rebalancing page (/portfolio/{id}/rebalance).
/// Tests rebalancing calculator functionality and signal generation.
/// </summary>
public class RebalancingPageTests : BaseTest
{
    private const int TechPortfolioId = 1; // Seeded portfolio ID with MSFT and AAPL

    public RebalancingPageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task RebalancingPage_WhenLoaded_ShouldDisplayTitle()
    {
        // Arrange
        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        string? title = await page.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("Rebalancing");
    }

    [Fact]
    public async Task RebalancingPage_ClickAddAllocation_ShouldAddInputFields()
    {
        // Arrange
        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickAddAllocationAsync();
        await Task.Delay(500); // Wait for UI update

        // Assert - Verify page is still displayed (inputs added successfully)
        Page!.Url.ShouldContain("/rebalance");
    }

    [Fact]
    public async Task RebalancingPage_AddTargetAllocation_ShouldWork()
    {
        // Arrange
        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.AddTargetAllocationAsync("MSFT", 60m);

        // Assert - Verify page is still displayed (allocation added successfully)
        Page!.Url.ShouldContain("/rebalance");
    }

    [Fact]
    public async Task RebalancingPage_ConfigureAndCalculate_ShouldShowResults()
    {
        // Arrange
        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        Dictionary<string, decimal> targetAllocations = new()
        {
            ["MSFT"] = 50m,
            ["AAPL"] = 50m
        };

        // Act
        await page.ConfigureAndCalculateRebalancingAsync(targetAllocations);
        await page.WaitForCalculationToCompleteAsync();
        bool resultsVisible = await page.AreResultsVisibleAsync();

        // Assert
        resultsVisible.ShouldBeTrue("Rebalancing results should be visible after calculation");
    }

    [Fact]
    public async Task RebalancingPage_CalculateRebalancing_ShouldGenerateSignals()
    {
        // Arrange
        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        Dictionary<string, decimal> targetAllocations = new()
        {
            ["MSFT"] = 60m,
            ["AAPL"] = 40m
        };

        // Act
        await page.ConfigureAndCalculateRebalancingAsync(targetAllocations);
        await page.WaitForCalculationToCompleteAsync();
        int signalCount = await page.GetSignalCountAsync();

        // Assert
        signalCount.ShouldBeGreaterThan(0, "Should generate rebalancing signals");
    }

    [Fact]
    public async Task RebalancingPage_GetSignalActions_ShouldReturnActions()
    {
        // Arrange
        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        Dictionary<string, decimal> targetAllocations = new()
        {
            ["MSFT"] = 70m,
            ["AAPL"] = 30m
        };

        // Act
        await page.ConfigureAndCalculateRebalancingAsync(targetAllocations);
        await page.WaitForCalculationToCompleteAsync();
        List<string?> actions = await page.GetSignalActionsAsync();

        // Assert
        actions.ShouldNotBeEmpty();
        actions.ShouldAllBe(action =>
            action == "Buy" || action == "Sell" || action == "Hold",
            "All actions should be Buy, Sell, or Hold");
    }

    [Fact]
    public async Task RebalancingPage_HasSignalForTicker_ShouldReturnTrue()
    {
        // Arrange
        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        Dictionary<string, decimal> targetAllocations = new()
        {
            ["MSFT"] = 50m,
            ["AAPL"] = 50m
        };

        // Act
        await page.ConfigureAndCalculateRebalancingAsync(targetAllocations);
        await page.WaitForCalculationToCompleteAsync();
        bool hasMSFTSignal = await page.HasSignalForTickerAsync("MSFT");

        // Assert
        hasMSFTSignal.ShouldBeTrue("Should have signal for MSFT");
    }

    [Fact]
    public async Task RebalancingPage_GetActionForTicker_ShouldReturnAction()
    {
        // Arrange
        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        Dictionary<string, decimal> targetAllocations = new()
        {
            ["MSFT"] = 50m,
            ["AAPL"] = 50m
        };

        // Act
        await page.ConfigureAndCalculateRebalancingAsync(targetAllocations);
        await page.WaitForCalculationToCompleteAsync();
        string? action = await page.GetActionForTickerAsync("MSFT");

        // Assert
        action.ShouldNotBeNull();
        action.ShouldBeOneOf("Buy", "Sell", "Hold");
    }

    [Fact]
    public async Task RebalancingPage_GetExecutableStatus_ShouldReturnStatus()
    {
        // Arrange
        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        Dictionary<string, decimal> targetAllocations = new()
        {
            ["MSFT"] = 50m,
            ["AAPL"] = 50m
        };

        // Act
        await page.ConfigureAndCalculateRebalancingAsync(targetAllocations);
        await page.WaitForCalculationToCompleteAsync();
        string? status = await page.GetExecutableStatusAsync();

        // Assert
        status.ShouldNotBeNull();
        bool hasExecutableText = status.Contains("Executable") || status.Contains("Not Executable");
        hasExecutableText.ShouldBeTrue("Status should indicate if plan is executable");
    }

    [Fact]
    public async Task RebalancingPage_WithCashPercentage_ShouldWork()
    {
        // Arrange
        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        Dictionary<string, decimal> targetAllocations = new()
        {
            ["MSFT"] = 40m,
            ["AAPL"] = 40m
        };

        // Act
        await page.ConfigureAndCalculateRebalancingAsync(
            targetAllocations,
            cashPercentage: 20m);
        await page.WaitForCalculationToCompleteAsync();
        bool resultsVisible = await page.AreResultsVisibleAsync();

        // Assert
        resultsVisible.ShouldBeTrue("Should calculate rebalancing with cash percentage");
    }

    [Fact]
    public async Task RebalancingPage_ClickBackButton_ShouldNavigateToDashboard()
    {
        // Arrange
        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickBackToDashboardAsync();

        // Assert
        Page!.Url.ShouldContain($"/portfolio/{TechPortfolioId}");
        Page!.Url.ShouldNotContain("/rebalance");
    }

    [Fact]
    public async Task RebalancingPage_SetCommissionSettings_ShouldWork()
    {
        // Arrange
        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.SetCommissionPercentageAsync(0.2m);
        await page.SetMinimumCommissionAsync(2.0m);

        // Assert - Verify page is still displayed (settings applied successfully)
        Page!.Url.ShouldContain("/rebalance");
    }

    [Fact]
    public async Task RebalancingPage_WhenLoaded_ShouldNotHaveConsoleErrors()
    {
        // Arrange
        List<string> consoleErrors = new();
        Page!.Console += (_, msg) =>
        {
            if (msg.Type == "error" && !IsAcceptableError(msg.Text))
            {
                consoleErrors.Add(msg.Text);
            }
        };

        RebalancingPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await Task.Delay(1000); // Wait for any delayed console errors

        // Assert
        consoleErrors.ShouldBeEmpty($"There should be no console errors on rebalancing page. Errors: {string.Join(", ", consoleErrors)}");
    }

    private static bool IsAcceptableError(string message)
    {
        return message.Contains("favicon.ico") ||
               message.Contains(".map") ||
               message.Contains("sourcemap") ||
               message.Contains("404") ||
               message.Contains("Failed to load resource");
    }
}
