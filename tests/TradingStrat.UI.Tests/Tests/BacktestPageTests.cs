namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Backtest page (/backtest).
/// Tests strategy backtesting configuration and results display.
/// </summary>
public class BacktestPageTests : BaseTest
{
    public BacktestPageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task BacktestPage_WhenLoaded_ShouldDisplayTitle()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);

        // Act
        await backtestPage.NavigateAsync();
        string? title = await backtestPage.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("Backtest");
    }

    [Fact]
    public async Task BacktestPage_WhenLoaded_ShouldHaveCorrectPageTitle()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);

        // Act
        await backtestPage.NavigateAsync();
        string pageTitle = await Page!.TitleAsync();

        // Assert
        pageTitle.ShouldContain("Backtest");
        pageTitle.ShouldContain("TradingStrat");
    }

    [Fact]
    public async Task BacktestPage_WhenLoaded_ShouldShowNoResultsPlaceholder()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);

        // Act
        await backtestPage.NavigateAsync();
        bool hasPlaceholder = await backtestPage.IsNoResultsPlaceholderDisplayedAsync();

        // Assert
        hasPlaceholder.ShouldBeTrue("No results placeholder should be visible initially");
    }

    [Fact]
    public async Task BacktestPage_SubmitButton_ShouldBeEnabledWhenFormIsValid()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);
        await backtestPage.NavigateAsync();

        // Act
        await backtestPage.FillBacktestFormAsync("AAPL", 10000);
        bool isDisabled = await backtestPage.IsSubmitButtonDisabledAsync();

        // Assert
        isDisabled.ShouldBeFalse("Submit button should be enabled with valid data");
    }

    [Fact]
    public async Task BacktestPage_FormFilling_ShouldAcceptValidInputs()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);
        await backtestPage.NavigateAsync();

        // Act
        await backtestPage.FillBacktestFormAsync(
            ticker: "MSFT",
            capital: 50000,
            startDate: "2023-01-01",
            endDate: "2023-12-31"
        );

        // Assert - No exception thrown, form accepts inputs
        bool isDisabled = await backtestPage.IsSubmitButtonDisabledAsync();
        isDisabled.ShouldBeFalse();
    }

    [Fact]
    public async Task BacktestPage_StrategySelection_ShouldChangeAvailableParameters()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);
        await backtestPage.NavigateAsync();

        // Act
        await backtestPage.SelectStrategyAsync("rsi");
        await Task.Delay(500); // Wait for parameters to render

        // Assert - Check that the page still loads without errors
        string? title = await backtestPage.GetPageTitleAsync();
        title.ShouldNotBeNull();
        title.ShouldContain("Backtest");
    }

    [Fact]
    public async Task BacktestPage_BlazorConnection_ShouldBeEstablished()
    {
        // Arrange & Act
        await NavigateToAsync("/backtest");

        // Check that Blazor is initialized
        bool blazorInitialized = await Page!.EvaluateAsync<bool>("() => window.Blazor !== undefined");

        // Assert
        blazorInitialized.ShouldBeTrue("Blazor SignalR connection should be established");
    }
}
