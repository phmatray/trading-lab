namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Portfolio Dashboard page (/portfolio/{id}).
/// Tests portfolio overview, metrics display, and position table.
/// </summary>
public class PortfolioDashboardPageTests : BaseTest
{
    private const int TechPortfolioId = 1; // Seeded portfolio ID

    public PortfolioDashboardPageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task Dashboard_WhenLoaded_ShouldDisplayTitle()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        string? title = await page.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("Portfolio");
    }

    [Fact]
    public async Task Dashboard_WhenLoaded_ShouldDisplayAllMetricCards()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();
        bool allCardsVisible = await page.AreMetricCardsVisibleAsync();

        // Assert
        allCardsVisible.ShouldBeTrue("All 4 metric cards (Total Value, Cash, Gain/Loss, Return %) should be visible");
    }

    [Fact]
    public async Task Dashboard_WhenLoaded_ShouldDisplayCashValue()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();
        string? cash = await page.GetCashValueAsync();

        // Assert
        cash.ShouldNotBeNull();
        cash.ShouldContain("$"); // Should display currency
    }

    [Fact]
    public async Task Dashboard_WithPositions_ShouldDisplayPositionTable()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();
        bool tableVisible = await page.IsPositionTableVisibleAsync();

        // Assert
        tableVisible.ShouldBeTrue();
    }

    [Fact]
    public async Task Dashboard_WithPositions_ShouldShowCorrectPositionCount()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();
        int positionCount = await page.GetPositionCountAsync();

        // Assert
        positionCount.ShouldBeGreaterThan(0, "Tech Growth Portfolio should have positions");
    }

    [Fact]
    public async Task Dashboard_SeededPositions_ShouldIncludeMSFT()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();
        bool hasMSFT = await page.HasPositionAsync("MSFT");

        // Assert
        hasMSFT.ShouldBeTrue("Tech Growth Portfolio should have MSFT position");
    }

    [Fact]
    public async Task Dashboard_SeededPositions_ShouldIncludeAAPL()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();
        bool hasAAPL = await page.HasPositionAsync("AAPL");

        // Assert
        hasAAPL.ShouldBeTrue("Tech Growth Portfolio should have AAPL position");
    }

    [Fact]
    public async Task Dashboard_GetPositionTickers_ShouldReturnList()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();
        List<string?> tickers = await page.GetPositionTickersAsync();

        // Assert
        tickers.ShouldNotBeEmpty();
        tickers.ShouldContain("MSFT");
        tickers.ShouldContain("AAPL");
    }

    [Fact]
    public async Task Dashboard_RefreshPrices_ShouldWork()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();

        // Act
        await page.ClickRefreshPricesAsync();
        await page.WaitForLoadingToCompleteAsync();

        // Assert - Should still have positions after refresh
        int positionCount = await page.GetPositionCountAsync();
        positionCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Dashboard_ClickBackButton_ShouldNavigateToPortfolios()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();

        // Act
        await page.ClickBackToPortfoliosAsync();

        // Assert
        Page!.Url.ShouldContain("/portfolios");
        Page!.Url.ShouldNotContain("/portfolio/");
    }

    [Fact]
    public async Task Dashboard_NavigateToRebalancing_ShouldWork()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();

        // Act
        await page.NavigateToRebalancingAsync();

        // Assert
        Page!.Url.ShouldContain("/rebalance");
    }

    [Fact]
    public async Task Dashboard_NavigateToPerformance_ShouldWork()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();

        // Act
        await page.NavigateToPerformanceAsync();

        // Assert
        Page!.Url.ShouldContain("/performance");
    }

    [Fact]
    public async Task Dashboard_EmptyPortfolio_ShouldShowNoPositionsMessage()
    {
        // Arrange - Portfolio 3 is the empty portfolio
        PortfolioDashboardPage page = new(Page!, BaseUrl, 3);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();
        bool hasNoPositionsMessage = await page.HasNoPositionsMessageAsync();

        // Assert
        hasNoPositionsMessage.ShouldBeTrue("Empty portfolio should show 'No positions' message");
    }

    [Fact]
    public async Task Dashboard_PositionMarketValue_ShouldBeDisplayed()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();
        string? marketValue = await page.GetPositionMarketValueAsync("MSFT");

        // Assert
        marketValue.ShouldNotBeNull();
        marketValue.ShouldContain("$");
    }

    [Fact]
    public async Task Dashboard_PositionGainLoss_ShouldBeDisplayed()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();
        string? gainLoss = await page.GetPositionGainLossAsync("MSFT");

        // Assert
        gainLoss.ShouldNotBeNull();
    }

    [Fact]
    public async Task Dashboard_TotalValue_ShouldBeDisplayed()
    {
        // Arrange
        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();
        string? totalValue = await page.GetTotalValueAsync();

        // Assert
        totalValue.ShouldNotBeNull();
        totalValue.ShouldContain("$");
    }

    [Fact]
    public async Task Dashboard_WhenLoaded_ShouldNotHaveConsoleErrors()
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

        PortfolioDashboardPage page = new(Page!, BaseUrl, TechPortfolioId);

        // Act
        await page.NavigateAsync();
        await page.WaitForLoadingToCompleteAsync();
        await Task.Delay(1000); // Wait for any delayed console errors

        // Assert
        consoleErrors.ShouldBeEmpty($"There should be no console errors on dashboard page. Errors: {string.Join(", ", consoleErrors)}");
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
