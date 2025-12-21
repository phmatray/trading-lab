namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Performance Analytics page (/portfolio/{id}/performance).
/// Tests performance metrics display, date range selection, and historical data.
/// </summary>
public class PerformanceAnalyticsPageTests : BaseTest
{
    private const int TechPortfolioId = 1; // Seeded portfolio ID with MSFT and AAPL

    public PerformanceAnalyticsPageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task PerformancePage_WhenLoaded_ShouldDisplayTitle()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        string? title = await page.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("Performance");
    }

    [Fact]
    public async Task PerformancePage_ClickQuickDateRange_ShouldWork()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync("1M");
        await page.WaitForAnalysisToCompleteAsync();

        // Assert - Verify page is still displayed (analysis completed successfully)
        Page!.Url.ShouldContain("/performance");
    }

    [Theory]
    [InlineData("1M")]
    [InlineData("3M")]
    [InlineData("6M")]
    [InlineData("1Y")]
    [InlineData("All")]
    public async Task PerformancePage_AllQuickRangeButtons_ShouldWork(string range)
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync(range);
        await page.WaitForAnalysisToCompleteAsync();

        // Assert - Verify page is still displayed
        Page!.Url.ShouldContain("/performance");
    }

    [Fact]
    public async Task PerformancePage_SetCustomDateRange_ShouldWork()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        DateTime endDate = DateTime.Today;
        DateTime startDate = endDate.AddMonths(-2);

        // Act
        await page.AnalyzePerformanceAsync(startDate, endDate);

        // Assert - Verify page is still displayed (analysis completed)
        Page!.Url.ShouldContain("/performance");
    }

    [Fact]
    public async Task PerformancePage_GetMetricCardCount_ShouldBeGreaterThanZero()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync("3M");
        await page.WaitForAnalysisToCompleteAsync();
        int cardCount = await page.GetMetricCardCountAsync();

        // Assert
        cardCount.ShouldBeGreaterThan(0, "Should display performance metric cards");
    }

    [Fact]
    public async Task PerformancePage_HasTotalReturnMetric_ShouldBeTrue()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync("3M");
        await page.WaitForAnalysisToCompleteAsync();
        bool hasTotalReturn = await page.HasMetricCardAsync("Total Return");

        // Assert
        hasTotalReturn.ShouldBeTrue("Should display Total Return metric");
    }

    [Fact]
    public async Task PerformancePage_HasVolatilityMetric_ShouldBeTrue()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync("3M");
        await page.WaitForAnalysisToCompleteAsync();
        bool hasVolatility = await page.HasMetricCardAsync("Volatility");

        // Assert
        hasVolatility.ShouldBeTrue("Should display Volatility metric");
    }

    [Fact]
    public async Task PerformancePage_HasSharpeRatioMetric_ShouldBeTrue()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync("3M");
        await page.WaitForAnalysisToCompleteAsync();
        bool hasSharpe = await page.HasMetricCardAsync("Sharpe Ratio");

        // Assert
        hasSharpe.ShouldBeTrue("Should display Sharpe Ratio metric");
    }

    [Fact]
    public async Task PerformancePage_GetTotalReturn_ShouldReturnValue()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync("3M");
        await page.WaitForAnalysisToCompleteAsync();
        string? totalReturn = await page.GetTotalReturnAsync();

        // Assert
        totalReturn.ShouldNotBeNull();
    }

    [Fact]
    public async Task PerformancePage_GetVolatility_ShouldReturnValue()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync("3M");
        await page.WaitForAnalysisToCompleteAsync();
        string? volatility = await page.GetVolatilityAsync();

        // Assert
        volatility.ShouldNotBeNull();
    }

    [Fact]
    public async Task PerformancePage_GetSharpeRatio_ShouldReturnValue()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync("3M");
        await page.WaitForAnalysisToCompleteAsync();
        string? sharpe = await page.GetSharpeRatioAsync();

        // Assert
        sharpe.ShouldNotBeNull();
    }

    [Fact]
    public async Task PerformancePage_HistoricalTable_ShouldBeVisible()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync("3M");
        await page.WaitForAnalysisToCompleteAsync();
        bool tableVisible = await page.IsHistoricalTableVisibleAsync();

        // Assert
        tableVisible.ShouldBeTrue("Historical data table should be visible");
    }

    [Fact]
    public async Task PerformancePage_GetHistoricalRowCount_ShouldBeGreaterThanZero()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync("3M");
        await page.WaitForAnalysisToCompleteAsync();
        int rowCount = await page.GetHistoricalRowCountAsync();

        // Assert
        rowCount.ShouldBeGreaterThan(0, "Should have historical data rows");
    }

    [Fact]
    public async Task PerformancePage_GetHistoricalDates_ShouldReturnDates()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync("1M");
        await page.WaitForAnalysisToCompleteAsync();
        List<string?> dates = await page.GetHistoricalDatesAsync();

        // Assert
        dates.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task PerformancePage_ClickBackButton_ShouldNavigateToDashboard()
    {
        // Arrange
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickBackToDashboardAsync();

        // Assert
        Page!.Url.ShouldContain($"/portfolio/{TechPortfolioId}");
        Page!.Url.ShouldNotContain("/performance");
    }

    [Fact]
    public async Task PerformancePage_EmptyPortfolio_ShouldHandleGracefully()
    {
        // Arrange - Portfolio 3 is the empty portfolio
        PerformanceAnalyticsPage page = new(Page!, BaseUrl, 3);
        await page.NavigateAsync();

        // Act
        await page.ClickQuickDateRangeAsync("3M");
        await page.WaitForAnalysisToCompleteAsync();

        // Assert - Should either show no data message or handle gracefully
        bool hasNoDataMessage = await page.HasNoDataMessageAsync();
        bool hasTable = await page.IsHistoricalTableVisibleAsync();

        // Either no data message or empty table is acceptable
        (hasNoDataMessage || hasTable).ShouldBeTrue("Should handle empty portfolio gracefully");
    }

    [Fact]
    public async Task PerformancePage_WhenLoaded_ShouldNotHaveConsoleErrors()
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

        PerformanceAnalyticsPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await Task.Delay(1000); // Wait for any delayed console errors

        // Assert
        consoleErrors.ShouldBeEmpty($"There should be no console errors on performance page. Errors: {string.Join(", ", consoleErrors)}");
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
