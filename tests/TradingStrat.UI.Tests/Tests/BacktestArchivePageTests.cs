namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Backtest Archive page (/backtests).
/// Tests backtest history display, filtering, and sorting functionality.
/// </summary>
public class BacktestArchivePageTests : BaseTest
{
    public BacktestArchivePageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task BacktestArchivePage_WhenLoaded_ShouldDisplayPageTitle()
    {
        // Arrange
        var page = new BacktestArchivePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        string? title = await page.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("Backtest Archive");
    }

    [Fact]
    public async Task BacktestArchivePage_WhenLoaded_ShouldDisplayCorrectPageTitle()
    {
        // Arrange
        var page = new BacktestArchivePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        string pageTitle = await Page!.TitleAsync();

        // Assert
        pageTitle.ShouldContain("Backtest Archive");
        pageTitle.ShouldContain("TradingStrat");
    }

    [Fact]
    public async Task BacktestArchivePage_WhenLoaded_ShouldDisplayFilterSection()
    {
        // Arrange
        var page = new BacktestArchivePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsFilterSectionVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Filter section should be visible");
    }

    [Fact]
    public async Task BacktestArchivePage_WhenEmpty_ShouldShowEmptyState()
    {
        // Arrange
        var page = new BacktestArchivePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool hasBacktests = await page.HasBacktestCardsAsync();
        bool hasEmptyState = await page.IsEmptyStateVisibleAsync();

        // Assert
        // Either should have backtests OR show empty state
        (hasBacktests || hasEmptyState).ShouldBeTrue("Should either display backtests or empty state");
    }

    [Fact]
    public async Task BacktestArchivePage_WhenLoaded_ShouldHaveSortOptions()
    {
        // Arrange
        var page = new BacktestArchivePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool hasSortOptions = await page.HasSortOptionsAsync();

        // Assert
        hasSortOptions.ShouldBeTrue("Page should have sorting options");
    }

    [Fact]
    public async Task BacktestArchivePage_WhenLoaded_ShouldNotHaveConsoleErrors()
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

        var page = new BacktestArchivePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(1000); // Wait for any delayed console errors

        // Assert
        consoleErrors.ShouldBeEmpty($"There should be no console errors. Errors: {string.Join(", ", consoleErrors)}");
    }

    [Fact]
    public async Task BacktestArchivePage_BlazorConnection_ShouldBeEstablished()
    {
        // Arrange & Act
        await NavigateToAsync("/backtests");

        // Check that Blazor is initialized
        bool blazorInitialized = await Page!.EvaluateAsync<bool>("() => window.Blazor !== undefined");

        // Assert
        blazorInitialized.ShouldBeTrue("Blazor SignalR connection should be established");
    }

    [Fact]
    public async Task BacktestArchivePage_Navigation_ShouldWorkFromLeftSidebar()
    {
        // Arrange
        await NavigateToAsync("/");

        // Act
        await Page!.Locator("nav a[href='/backtests']").ClickAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        Page!.Url.ShouldContain("/backtests");
    }

    [Fact]
    public async Task BacktestArchivePage_Breadcrumbs_ShouldBeVisible()
    {
        // Arrange
        var page = new BacktestArchivePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        var breadcrumbs = Page!.Locator("nav[aria-label='Breadcrumb']");
        bool hasBreadcrumbs = await breadcrumbs.IsVisibleAsync();

        // Assert
        hasBreadcrumbs.ShouldBeTrue("Breadcrumb navigation should be visible");
    }

    [Fact]
    public async Task BacktestArchivePage_PageLoad_ShouldCompleteSuccessfully()
    {
        // Arrange
        var page = new BacktestArchivePage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadAsync();

        // Assert
        bool isDisplayed = await page.IsPageDisplayedAsync();
        isDisplayed.ShouldBeTrue("Page should load successfully");
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
