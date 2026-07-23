namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Strategy Comparison page (/strategies/compare).
/// Tests multi-strategy comparison, performance matrix, and equity charts.
/// </summary>
public class StrategyComparisonPageTests : BaseTest
{
    public StrategyComparisonPageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task StrategyComparisonPage_WhenLoaded_ShouldDisplayPageTitle()
    {
        // Arrange
        var page = new StrategyComparisonPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        string? title = await page.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("Compare Strategies");
    }

    [Fact]
    public async Task StrategyComparisonPage_WhenLoaded_ShouldDisplayCorrectPageTitle()
    {
        // Arrange
        var page = new StrategyComparisonPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        string pageTitle = await Page!.TitleAsync();

        // Assert
        pageTitle.ShouldContain("Compare Strategies");
        pageTitle.ShouldContain("TradingStrat");
    }

    [Fact]
    public async Task StrategyComparisonPage_WhenLoaded_ShouldDisplayStrategySelectors()
    {
        // Arrange
        var page = new StrategyComparisonPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        int selectorCount = await page.GetStrategySelectorsCountAsync();

        // Assert
        selectorCount.ShouldBeGreaterThan(0, "Should have at least one strategy selector");
    }

    [Fact]
    public async Task StrategyComparisonPage_WhenLoaded_ShouldDisplayCompareButton()
    {
        // Arrange
        var page = new StrategyComparisonPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsCompareButtonVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Compare button should be visible");
    }

    [Fact]
    public async Task StrategyComparisonPage_WhenLoaded_ShouldDisplayAddStrategyButton()
    {
        // Arrange
        var page = new StrategyComparisonPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsAddStrategyButtonVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Add Strategy button should be visible");
    }

    [Fact]
    public async Task StrategyComparisonPage_Breadcrumbs_ShouldBeVisible()
    {
        // Arrange
        var page = new StrategyComparisonPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        ILocator breadcrumbs = Page!.Locator("nav[aria-label='Breadcrumb']");
        bool hasBreadcrumbs = await breadcrumbs.IsVisibleAsync();

        // Assert
        hasBreadcrumbs.ShouldBeTrue("Breadcrumb navigation should be visible");
    }

    [Fact]
    public async Task StrategyComparisonPage_Navigation_ShouldWorkFromLeftSidebar()
    {
        // Arrange
        await NavigateToAsync("/");

        // Act - Use Catalyst sidebar navigation
        await Page!.Locator("aside[data-testid='left-sidebar'] nav a[href='/strategies/compare']").ClickAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        Page!.Url.ShouldContain("/strategies/compare");
    }

    [Fact]
    public async Task StrategyComparisonPage_WhenLoaded_ShouldNotHaveConsoleErrors()
    {
        // Arrange
        List<string> consoleErrors = new List<string>();
        Page!.Console += (_, msg) =>
        {
            if (msg.Type == "error" && !IsAcceptableError(msg.Text))
            {
                consoleErrors.Add(msg.Text);
            }
        };

        var page = new StrategyComparisonPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(1000); // Wait for any delayed console errors

        // Assert
        consoleErrors.ShouldBeEmpty($"There should be no console errors. Errors: {string.Join(", ", consoleErrors)}");
    }

    [Fact]
    public async Task StrategyComparisonPage_BlazorConnection_ShouldBeEstablished()
    {
        // Arrange & Act
        await NavigateToAsync("/strategies/compare");

        // Check that Blazor is initialized
        bool blazorInitialized = await Page!.EvaluateAsync<bool>("() => window.Blazor !== undefined");

        // Assert
        blazorInitialized.ShouldBeTrue("Blazor SignalR connection should be established");
    }

    [Fact]
    public async Task StrategyComparisonPage_DarkTheme_ShouldBeApplied()
    {
        // Arrange
        var page = new StrategyComparisonPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();

        // Check if dark class is applied to the root element
        ILocator rootElement = Page!.Locator("body > div").First;
        string? className = await rootElement.GetAttributeAsync("class");

        // Assert
        className.ShouldNotBeNullOrEmpty("Root element should have CSS classes");
        className.Contains("dark").ShouldBeTrue("Root element should have 'dark' class for dark theme");
    }

    [Fact]
    public async Task StrategyComparisonPage_PageLoad_ShouldCompleteSuccessfully()
    {
        // Arrange
        var page = new StrategyComparisonPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadAsync();

        // Assert
        bool isDisplayed = await page.IsPageDisplayedAsync();
        isDisplayed.ShouldBeTrue("Page should load successfully");
    }

    [Fact]
    public async Task StrategyComparisonPage_MultipleSelectors_ShouldSupportUpTo5Strategies()
    {
        // Arrange
        var page = new StrategyComparisonPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        int selectorCount = await page.GetStrategySelectorsCountAsync();

        // Assert
        selectorCount.ShouldBeGreaterThanOrEqualTo(2, "Should support at least 2 strategies for comparison");
        selectorCount.ShouldBeLessThanOrEqualTo(5, "Should support up to 5 strategies for comparison");
    }

    private static bool IsAcceptableError(string message)
    {
        // Filter out known acceptable errors
        return message.Contains("favicon.ico") ||
               message.Contains(".map") ||
               message.Contains("sourcemap") ||
               message.Contains("404") ||
               message.Contains("Failed to load resource");
    }
}
