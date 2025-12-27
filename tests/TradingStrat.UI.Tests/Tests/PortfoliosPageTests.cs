namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Portfolios page (/portfolios).
/// Tests portfolio list view, creation, and deletion functionality.
/// </summary>
public class PortfoliosPageTests : BaseTest
{
    public PortfoliosPageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task PortfoliosPage_WhenLoaded_ShouldDisplayTitle()
    {
        // Arrange
        PortfoliosPage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        string? title = await page.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("Portfolios");
    }

    [Fact]
    public async Task PortfoliosPage_WhenLoaded_ShouldDisplaySeededPortfolios()
    {
        // Arrange
        PortfoliosPage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        int portfolioCount = await page.GetPortfolioCountAsync();

        // Assert
        portfolioCount.ShouldBeGreaterThanOrEqualTo(3, "Should have at least 3 seeded portfolios");
    }

    [Fact]
    public async Task PortfoliosPage_WhenLoaded_ShouldShowPortfolioGrid()
    {
        // Arrange
        PortfoliosPage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isGridVisible = await page.IsPortfolioGridVisibleAsync();

        // Assert
        isGridVisible.ShouldBeTrue();
    }

    [Fact]
    public async Task PortfoliosPage_ClickCreateButton_ShouldOpenDialog()
    {
        // Arrange
        PortfoliosPage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickCreatePortfolioButtonAsync();
        bool isDialogVisible = await page.IsCreateDialogVisibleAsync();

        // Assert
        isDialogVisible.ShouldBeTrue();
    }

    [Fact]
    public async Task PortfoliosPage_CreatePortfolio_ShouldSucceed()
    {
        // Arrange
        PortfoliosPage page = new(Page!, BaseUrl);
        await page.NavigateAsync();
        string portfolioName = $"Test Portfolio {DateTime.Now.Ticks}";

        // Act
        await page.CreatePortfolioAsync(portfolioName, "Test Description", 15000m);
        bool hasCard = await page.HasPortfolioCardAsync(portfolioName);

        // Assert
        hasCard.ShouldBeTrue($"Portfolio '{portfolioName}' should be visible after creation");
    }

    [Fact]
    public async Task PortfoliosPage_CancelCreateDialog_ShouldCloseDialog()
    {
        // Arrange
        PortfoliosPage page = new(Page!, BaseUrl);
        await page.NavigateAsync();
        await page.ClickCreatePortfolioButtonAsync();

        // Act
        await page.CancelCreateDialogAsync();
        await Task.Delay(500); // Wait for dialog close
        bool isDialogVisible = await page.IsCreateDialogVisibleAsync();

        // Assert
        isDialogVisible.ShouldBeFalse();
    }

    [Fact]
    public async Task PortfoliosPage_ClickPortfolioCard_ShouldNavigateToDashboard()
    {
        // Arrange
        PortfoliosPage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickPortfolioCardAsync("Tech Growth Portfolio");

        // Assert
        Page!.Url.ShouldContain("/portfolio/");
        Page!.Url.ShouldNotContain("/portfolios");
    }

    [Fact]
    public async Task PortfoliosPage_SeededPortfolio_ShouldDisplayCashAndPositions()
    {
        // Arrange
        PortfoliosPage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        string? cash = await page.GetPortfolioCashAsync("Tech Growth Portfolio");
        string? positions = await page.GetPortfolioPositionsCountAsync("Tech Growth Portfolio");

        // Assert
        cash.ShouldNotBeNull();
        cash.ShouldContain("Cash");
        positions.ShouldNotBeNull();
        positions.ShouldContain("Positions");
    }

    [Fact]
    public async Task PortfoliosPage_DeletePortfolio_ShouldSucceed()
    {
        // Arrange
        PortfoliosPage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Create a portfolio to delete
        string portfolioName = $"Delete Test {DateTime.Now.Ticks}";
        await page.CreatePortfolioAsync(portfolioName, "Will be deleted", 5000m);

        // Verify it exists
        bool existsBeforeDelete = await page.HasPortfolioCardAsync(portfolioName);
        existsBeforeDelete.ShouldBeTrue();

        // Act
        await page.DeletePortfolioAsync(portfolioName);

        // Assert
        bool existsAfterDelete = await page.HasPortfolioCardAsync(portfolioName);
        existsAfterDelete.ShouldBeFalse($"Portfolio '{portfolioName}' should not be visible after deletion");
    }

    [Fact]
    public async Task PortfoliosPage_CancelDelete_ShouldKeepPortfolio()
    {
        // Arrange
        PortfoliosPage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickDeleteButtonAsync("Tech Growth Portfolio");
        await page.CancelDeleteAsync();

        // Assert
        bool stillExists = await page.HasPortfolioCardAsync("Tech Growth Portfolio");
        stillExists.ShouldBeTrue("Portfolio should still exist after canceling delete");
    }

    [Fact]
    public async Task PortfoliosPage_NavigateViaMenu_ShouldWork()
    {
        // Arrange
        await NavigateToAsync("/");

        // Act
        PortfoliosPage page = new(Page!, BaseUrl);
        await page.NavigateViaMenuAsync();

        // Assert
        Page!.Url.ShouldContain("/portfolios");
    }

    [Fact]
    public async Task PortfoliosPage_CreatePortfolioWithoutDescription_ShouldSucceed()
    {
        // Arrange
        PortfoliosPage page = new(Page!, BaseUrl);
        await page.NavigateAsync();
        string portfolioName = $"No Description {DateTime.Now.Ticks}";

        // Act
        await page.CreatePortfolioAsync(portfolioName);
        bool hasCard = await page.HasPortfolioCardAsync(portfolioName);

        // Assert
        hasCard.ShouldBeTrue($"Portfolio '{portfolioName}' should be visible even without description");
    }

    [Fact]
    public async Task PortfoliosPage_EmptyPortfolio_ShouldDisplay()
    {
        // Arrange
        PortfoliosPage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        bool hasEmptyPortfolio = await page.HasPortfolioCardAsync("Empty Portfolio");

        // Assert
        hasEmptyPortfolio.ShouldBeTrue("Empty Portfolio should be visible in the list");
    }

    [Fact]
    public async Task PortfoliosPage_WhenLoaded_ShouldNotHaveConsoleErrors()
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

        PortfoliosPage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await Task.Delay(1000); // Wait for any delayed console errors

        // Assert
        consoleErrors.ShouldBeEmpty($"There should be no console errors on portfolios page. Errors: {string.Join(", ", consoleErrors)}");
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
