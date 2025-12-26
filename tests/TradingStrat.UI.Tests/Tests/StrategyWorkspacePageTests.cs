namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Strategy Workspace page (/workspace).
/// Tests the unified tabbed interface for Define → Test → Optimize → Deploy workflow.
/// </summary>
public class StrategyWorkspacePageTests : BaseTest
{
    public StrategyWorkspacePageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task StrategyWorkspacePage_WhenLoaded_ShouldDisplayPageTitle()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        string? title = await page.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("Strategy Workspace");
    }

    [Fact]
    public async Task StrategyWorkspacePage_WhenLoaded_ShouldDisplayCorrectPageTitle()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        string pageTitle = await Page!.TitleAsync();

        // Assert
        pageTitle.ShouldContain("Strategy Workspace");
        pageTitle.ShouldContain("TradingStrat");
    }

    [Fact]
    public async Task StrategyWorkspacePage_WhenLoaded_ShouldDisplayAllFourTabs()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool allTabsVisible = await page.AreAllTabsVisibleAsync();

        // Assert
        allTabsVisible.ShouldBeTrue("All 4 tabs (Define, Test, Optimize, Deploy) should be visible");
    }

    [Fact]
    public async Task StrategyWorkspacePage_DefineTab_ShouldBeVisible()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsDefineTabVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Define tab should be visible");
    }

    [Fact]
    public async Task StrategyWorkspacePage_TestTab_ShouldBeVisible()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsTestTabVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Test tab should be visible");
    }

    [Fact]
    public async Task StrategyWorkspacePage_OptimizeTab_ShouldBeVisible()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsOptimizeTabVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Optimize tab should be visible");
    }

    [Fact]
    public async Task StrategyWorkspacePage_DeployTab_ShouldBeVisible()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsDeployTabVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Deploy tab should be visible");
    }

    [Fact]
    public async Task StrategyWorkspacePage_DefineTab_ShouldBeClickable()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickDefineTabAsync();
        string? activeTab = await page.GetActiveTabTextAsync();

        // Assert
        activeTab.ShouldNotBeNull();
        activeTab.ShouldContain("Define");
    }

    [Fact]
    public async Task StrategyWorkspacePage_TestTab_ShouldBeClickable()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickTestTabAsync();
        string? activeTab = await page.GetActiveTabTextAsync();

        // Assert
        activeTab.ShouldNotBeNull();
        activeTab.ShouldContain("Test");
    }

    [Fact]
    public async Task StrategyWorkspacePage_OptimizeTab_ShouldBeClickable()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickOptimizeTabAsync();
        string? activeTab = await page.GetActiveTabTextAsync();

        // Assert
        activeTab.ShouldNotBeNull();
        activeTab.ShouldContain("Optimize");
    }

    [Fact]
    public async Task StrategyWorkspacePage_DeployTab_ShouldBeClickable()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickDeployTabAsync();
        string? activeTab = await page.GetActiveTabTextAsync();

        // Assert
        activeTab.ShouldNotBeNull();
        activeTab.ShouldContain("Deploy");
    }

    [Fact]
    public async Task StrategyWorkspacePage_TabSwitching_ShouldPreserveContext()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Switch between tabs
        await page.ClickTestTabAsync();
        await Task.Delay(500);
        await page.ClickDefineTabAsync();
        await Task.Delay(500);

        // Assert - Page should still be displayed
        bool isDisplayed = await page.IsPageDisplayedAsync();
        isDisplayed.ShouldBeTrue("Page should remain displayed after tab switching");
    }

    [Fact]
    public async Task StrategyWorkspacePage_Navigation_ShouldWorkFromLeftSidebar()
    {
        // Arrange
        await NavigateToAsync("/");

        // Act
        await Page!.Locator("nav a[href='/workspace']").ClickAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        Page!.Url.ShouldContain("/workspace");
    }

    [Fact]
    public async Task StrategyWorkspacePage_Breadcrumbs_ShouldBeVisible()
    {
        // Arrange
        var page = new StrategyWorkspacePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        var breadcrumbs = Page!.Locator("nav[aria-label='Breadcrumb']");
        bool hasBreadcrumbs = await breadcrumbs.IsVisibleAsync();

        // Assert
        hasBreadcrumbs.ShouldBeTrue("Breadcrumb navigation should be visible");
    }

    [Fact]
    public async Task StrategyWorkspacePage_WhenLoaded_ShouldNotHaveConsoleErrors()
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

        var page = new StrategyWorkspacePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(1000); // Wait for any delayed console errors

        // Assert
        consoleErrors.ShouldBeEmpty($"There should be no console errors. Errors: {string.Join(", ", consoleErrors)}");
    }

    [Fact]
    public async Task StrategyWorkspacePage_BlazorConnection_ShouldBeEstablished()
    {
        // Arrange & Act
        await NavigateToAsync("/workspace");

        // Check that Blazor is initialized
        bool blazorInitialized = await Page!.EvaluateAsync<bool>("() => window.Blazor !== undefined");

        // Assert
        blazorInitialized.ShouldBeTrue("Blazor SignalR connection should be established");
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
