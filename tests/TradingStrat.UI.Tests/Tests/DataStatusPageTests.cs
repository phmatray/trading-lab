namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Data Status page (/data/status).
/// Tests data coverage display, gap detection, and update functionality.
/// </summary>
public class DataStatusPageTests : BaseTest
{
    public DataStatusPageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task DataStatusPage_WhenLoaded_ShouldDisplayPageTitle()
    {
        // Arrange
        var page = new DataStatusPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        string? title = await page.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("Data Status");
    }

    [Fact]
    public async Task DataStatusPage_WhenLoaded_ShouldDisplayCoverageSummary()
    {
        // Arrange
        var page = new DataStatusPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsCoverageSummaryVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Coverage summary card should be visible");
    }

    [Fact]
    public async Task DataStatusPage_WhenLoaded_ShouldDisplayDataTable()
    {
        // Arrange
        var page = new DataStatusPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsDataTableVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Data coverage table should be visible");
    }

    [Fact]
    public async Task DataStatusPage_DataTable_ShouldHaveHeaders()
    {
        // Arrange
        var page = new DataStatusPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool hasHeaders = await page.HasTableHeadersAsync();

        // Assert
        hasHeaders.ShouldBeTrue("Table should have column headers (Ticker, Records, Start Date, End Date, Coverage)");
    }

    [Fact]
    public async Task DataStatusPage_WhenLoaded_ShouldDisplayRefreshButton()
    {
        // Arrange
        var page = new DataStatusPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsRefreshButtonVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Refresh Status button should be visible");
    }

    [Fact]
    public async Task DataStatusPage_RefreshButton_ShouldBeClickable()
    {
        // Arrange
        var page = new DataStatusPage(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickRefreshButtonAsync();

        // Assert - Page should still be displayed after refresh
        bool isDisplayed = await page.IsPageDisplayedAsync();
        isDisplayed.ShouldBeTrue("Page should remain displayed after refresh");
    }

    [Fact]
    public async Task DataStatusPage_WhenLoaded_ShouldNotHaveConsoleErrors()
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

        var page = new DataStatusPage(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(1000); // Wait for any delayed console errors

        // Assert
        consoleErrors.ShouldBeEmpty($"There should be no console errors. Errors: {string.Join(", ", consoleErrors)}");
    }

    [Fact]
    public async Task DataStatusPage_BlazorConnection_ShouldBeEstablished()
    {
        // Arrange & Act
        await NavigateToAsync("/data/status");

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
