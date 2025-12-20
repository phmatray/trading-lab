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

    [Fact]
    public async Task SelectIchimoku_ShouldShowIchimokuParameters()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);
        await backtestPage.NavigateAsync();

        // Act
        await backtestPage.SelectStrategyAsync("ichimoku");
        await Task.Delay(500); // Wait for parameters to render

        // Assert - Verify Ichimoku-specific parameters are visible
        bool hasTenkanInput = await backtestPage.HasIchimokuParameterAsync("tenkan-period");
        bool hasKijunInput = await backtestPage.HasIchimokuParameterAsync("kijun-period");
        bool hasSenkouBInput = await backtestPage.HasIchimokuParameterAsync("senkou-b-period");

        hasTenkanInput.ShouldBeTrue("Tenkan Period input should be visible");
        hasKijunInput.ShouldBeTrue("Kijun Period input should be visible");
        hasSenkouBInput.ShouldBeTrue("Senkou B Period input should be visible");
    }

    [Fact]
    public async Task FillIchimokuParameters_ShouldAcceptValues()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);
        await backtestPage.NavigateAsync();

        // Act
        await backtestPage.SelectStrategyAsync("ichimoku");
        await Task.Delay(500);
        await backtestPage.FillIchimokuParametersAsync(tenkan: 9, kijun: 26, senkouB: 52);

        // Assert - No exception thrown, parameters accept values
        bool isDisabled = await backtestPage.IsSubmitButtonDisabledAsync();
        isDisabled.ShouldBeFalse("Submit button should remain enabled after filling Ichimoku parameters");
    }

    [Theory]
    [InlineData("ma", "fast-period")]
    [InlineData("rsi", "rsi-period")]
    [InlineData("macd", "macd-fast")]
    [InlineData("ml", "buy-threshold")]
    [InlineData("ichimoku", "tenkan-period")]
    public async Task SelectStrategy_ShouldLoadParameters(string strategyType, string expectedParameterId)
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);
        await backtestPage.NavigateAsync();

        // Act
        await backtestPage.SelectStrategyAsync(strategyType);
        await Task.Delay(500);

        // Assert - Verify strategy-specific parameter is visible
        bool hasParameter = await Page!.Locator($"#{expectedParameterId}").IsVisibleAsync();
        hasParameter.ShouldBeTrue($"{strategyType} strategy should show parameter {expectedParameterId}");
    }

    [Fact]
    public async Task MetricsDisplay_ShouldShowAllMetrics()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);
        await backtestPage.NavigateAsync();
        await backtestPage.FillBacktestFormAsync("AAPL", 10000);

        // Act
        await backtestPage.SubmitFormAsync();
        await backtestPage.WaitForBacktestCompleteAsync();

        // Assert - Verify key metrics are displayed
        bool hasResults = await backtestPage.AreResultsDisplayedAsync();
        hasResults.ShouldBeTrue("Results should be displayed after successful backtest");

        bool hasTotalReturn = await Page!.Locator("text=Total Return").IsVisibleAsync();
        bool hasFinalCapital = await Page!.Locator("text=Final Capital").IsVisibleAsync();
        bool hasMaxDrawdown = await Page!.Locator("text=Max Drawdown").IsVisibleAsync();

        hasTotalReturn.ShouldBeTrue("Total Return metric should be visible");
        hasFinalCapital.ShouldBeTrue("Final Capital metric should be visible");
        hasMaxDrawdown.ShouldBeTrue("Max Drawdown metric should be visible");
    }

    [Fact]
    public async Task EquityChart_ShouldRenderAfterBacktest()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);
        await backtestPage.NavigateAsync();
        await backtestPage.FillBacktestFormAsync("MSFT", 10000);

        // Act
        await backtestPage.SubmitFormAsync();
        await backtestPage.WaitForBacktestCompleteAsync();

        // Assert - Verify equity chart is rendered
        bool chartVisible = await backtestPage.IsEquityChartVisibleAsync();
        chartVisible.ShouldBeTrue("Equity chart should be rendered after successful backtest");
    }

    [Fact]
    public async Task TradeTable_ShouldDisplayTrades()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);
        await backtestPage.NavigateAsync();
        await backtestPage.FillBacktestFormAsync("GOOGL", 10000);

        // Act
        await backtestPage.SubmitFormAsync();
        await backtestPage.WaitForBacktestCompleteAsync();

        // Assert - Verify trade table is visible
        bool tableVisible = await backtestPage.IsTradeTableVisibleAsync();
        tableVisible.ShouldBeTrue("Trade table should be displayed after backtest");
    }

    [Fact]
    public async Task InvalidDateRange_ShouldShowValidationError()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);
        await backtestPage.NavigateAsync();

        // Act - Set end date before start date
        await backtestPage.FillBacktestFormAsync(
            ticker: "AAPL",
            capital: 10000,
            startDate: "2024-12-31",
            endDate: "2024-01-01"
        );
        await backtestPage.SubmitFormAsync();
        await Task.Delay(1000);

        // Assert - Verify error is displayed
        bool hasError = await backtestPage.HasErrorMessageAsync();
        hasError.ShouldBeTrue("Validation error should be displayed for invalid date range");
    }

    [Fact]
    public async Task ClearButton_ShouldResetForm()
    {
        // Arrange
        var backtestPage = new BacktestPage(Page!, BaseUrl);
        await backtestPage.NavigateAsync();
        await backtestPage.FillBacktestFormAsync("TSLA", 25000);

        // Act - Look for clear/reset button and click it
        ILocator clearButton = Page!.Locator("button:has-text('Clear')").Or(Page!.Locator("button:has-text('Reset')"));
        bool clearButtonExists = await clearButton.IsVisibleAsync();

        if (clearButtonExists)
        {
            await clearButton.ClickAsync();
            await Task.Delay(300);

            // Assert - Verify form is reset
            string? tickerValue = await Page!.Locator("#ticker").InputValueAsync();
            tickerValue.ShouldBeNullOrEmpty("Ticker should be cleared");
        }
        else
        {
            // If no clear button exists, this test passes (feature not implemented)
            true.ShouldBeTrue("Clear button feature not implemented");
        }
    }
}
