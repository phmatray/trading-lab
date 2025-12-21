namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Comparison page (/comparison).
/// Tests A/B strategy comparison functionality.
/// </summary>
public class ComparisonPageTests : BaseTest
{
    public ComparisonPageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task ComparisonPage_WhenLoaded_ShouldDisplayTitle()
    {
        // Arrange
        var comparisonPage = new ComparisonPage(Page!, BaseUrl);

        // Act
        await comparisonPage.NavigateAsync();
        string? title = await comparisonPage.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("Comparison");
    }

    [Fact]
    public async Task ComparisonPage_WhenLoaded_ShouldHaveCorrectPageTitle()
    {
        // Arrange
        var comparisonPage = new ComparisonPage(Page!, BaseUrl);

        // Act
        await comparisonPage.NavigateAsync();
        string pageTitle = await Page!.TitleAsync();

        // Assert
        pageTitle.ShouldContain("Comparison");
        pageTitle.ShouldContain("TradingStrat");
    }

    [Fact]
    public async Task ComparisonPage_WhenLoaded_ShouldShowNoResultsPlaceholder()
    {
        // Arrange
        var comparisonPage = new ComparisonPage(Page!, BaseUrl);

        // Act
        await comparisonPage.NavigateAsync();
        bool hasPlaceholder = await comparisonPage.IsNoResultsPlaceholderDisplayedAsync();

        // Assert
        hasPlaceholder.ShouldBeTrue("No results placeholder should be visible initially");
    }

    [Fact]
    public async Task ComparisonPage_SubmitButton_ShouldBeEnabledWhenFormIsValid()
    {
        // Arrange
        var comparisonPage = new ComparisonPage(Page!, BaseUrl);
        await comparisonPage.NavigateAsync();

        // Act
        await comparisonPage.FillCommonFieldsAsync("AAPL", 10000);
        bool isDisabled = await comparisonPage.IsSubmitButtonDisabledAsync();

        // Assert
        isDisabled.ShouldBeFalse("Submit button should be enabled with valid data");
    }

    [Fact]
    public async Task ComparisonPage_FormFilling_ShouldAcceptValidInputs()
    {
        // Arrange
        var comparisonPage = new ComparisonPage(Page!, BaseUrl);
        await comparisonPage.NavigateAsync();

        // Act
        await comparisonPage.FillCommonFieldsAsync(
            ticker: "MSFT",
            capital: 50000
        );

        // Assert - No exception thrown, form accepts inputs
        bool isDisabled = await comparisonPage.IsSubmitButtonDisabledAsync();
        isDisabled.ShouldBeFalse();
    }

    [Fact]
    public async Task ComparisonPage_StrategySelection_ShouldSupportTwoVariants()
    {
        // Arrange
        var comparisonPage = new ComparisonPage(Page!, BaseUrl);
        await comparisonPage.NavigateAsync();

        // Act
        await comparisonPage.SelectVariantAStrategyAsync("ma");
        await comparisonPage.SelectVariantBStrategyAsync("rsi");
        await Task.Delay(500); // Wait for parameters to render

        // Assert - Check that the page still loads without errors
        string? title = await comparisonPage.GetPageTitleAsync();
        title.ShouldNotBeNull();
        title.ShouldContain("Comparison");
    }

    [Fact]
    public async Task ComparisonPage_TwoStrategyForms_ShouldBeVisible()
    {
        // Arrange
        var comparisonPage = new ComparisonPage(Page!, BaseUrl);

        // Act
        await comparisonPage.NavigateAsync();

        // Assert - Look for both variant sections
        bool variantAText = await Page!.Locator("text=Variant A").IsVisibleAsync();
        bool variantBText = await Page!.Locator("text=Variant B").IsVisibleAsync();

        variantAText.ShouldBeTrue("Variant A section should be visible");
        variantBText.ShouldBeTrue("Variant B section should be visible");
    }

    [Fact]
    public async Task ComparisonPage_BlazorConnection_ShouldBeEstablished()
    {
        // Arrange & Act
        await NavigateToAsync("/comparison");

        // Check that Blazor is initialized
        bool blazorInitialized = await Page!.EvaluateAsync<bool>("() => window.Blazor !== undefined");

        // Assert
        blazorInitialized.ShouldBeTrue("Blazor SignalR connection should be established");
    }

    [Fact]
    public async Task SelectTwoStrategies_ShouldEnableCompare()
    {
        // Arrange
        var comparisonPage = new ComparisonPage(Page!, BaseUrl);
        await comparisonPage.NavigateAsync();

        // Act - Fill common fields and select two different strategies
        await comparisonPage.FillCommonFieldsAsync("AAPL", 10000);
        await comparisonPage.SelectVariantAStrategyAsync("ma");
        await comparisonPage.SelectVariantBStrategyAsync("rsi");
        await Task.Delay(500);

        // Assert - Submit button should be enabled
        bool isDisabled = await comparisonPage.IsSubmitButtonDisabledAsync();
        isDisabled.ShouldBeFalse("Compare button should be enabled when both strategies are selected");
    }

    [Fact]
    public async Task CompareButton_ShouldShowResults()
    {
        // Arrange
        var comparisonPage = new ComparisonPage(Page!, BaseUrl);
        await comparisonPage.NavigateAsync();
        await comparisonPage.FillCommonFieldsAsync("MSFT", 10000);
        await comparisonPage.SelectVariantAStrategyAsync("ma");
        await comparisonPage.SelectVariantBStrategyAsync("rsi");

        // Act
        await comparisonPage.SubmitFormAsync();
        await comparisonPage.WaitForComparisonCompleteAsync();

        // Assert - Check for errors first
        bool hasError = await comparisonPage.HasErrorMessageAsync();
        if (hasError)
        {
            // If there's an error, capture it and fail the test with the error message
            string? errorText = await Page!.Locator("[role='alert']").TextContentAsync();
            hasError.ShouldBeFalse($"Comparison should not have errors. Error message: {errorText}");
        }

        // Assert - Results table should appear
        bool hasResults = await comparisonPage.AreResultsDisplayedAsync();
        hasResults.ShouldBeTrue("Results table should be displayed after comparison completes");
    }

    [Fact]
    public async Task ResultsTable_ShouldShowSideBySide()
    {
        // Arrange
        var comparisonPage = new ComparisonPage(Page!, BaseUrl);
        await comparisonPage.NavigateAsync();
        await comparisonPage.FillCommonFieldsAsync("GOOGL", 10000);
        await comparisonPage.SelectVariantAStrategyAsync("rsi");
        await comparisonPage.SelectVariantBStrategyAsync("macd");

        // Act
        await comparisonPage.SubmitFormAsync();
        await comparisonPage.WaitForComparisonCompleteAsync();

        // Assert - Both variant metrics should be displayed side by side
        bool hasBothMetrics = await comparisonPage.AreBothVariantMetricsDisplayedAsync();
        hasBothMetrics.ShouldBeTrue("Results should display both Variant A and Variant B metrics side by side");
    }

    [Fact]
    public async Task MetricsDiff_ShouldHighlightBetter()
    {
        // Arrange
        var comparisonPage = new ComparisonPage(Page!, BaseUrl);
        await comparisonPage.NavigateAsync();
        await comparisonPage.FillCommonFieldsAsync("AAPL", 10000);
        await comparisonPage.SelectVariantAStrategyAsync("ma");
        await comparisonPage.SelectVariantBStrategyAsync("rsi");

        // Act
        await comparisonPage.SubmitFormAsync();
        await comparisonPage.WaitForComparisonCompleteAsync();

        // Assert - Winner announcement should be displayed
        bool hasWinner = await comparisonPage.IsWinnerAnnouncementDisplayedAsync();
        hasWinner.ShouldBeTrue("Winner should be highlighted/announced in comparison results");
    }

    [Fact]
    public async Task ComparisonTable_ShouldShowMetricRows()
    {
        // Arrange
        var comparisonPage = new ComparisonPage(Page!, BaseUrl);
        await comparisonPage.NavigateAsync();
        await comparisonPage.FillCommonFieldsAsync("TSLA", 10000);
        await comparisonPage.SelectVariantAStrategyAsync("macd");
        await comparisonPage.SelectVariantBStrategyAsync("ma");

        // Act
        await comparisonPage.SubmitFormAsync();
        await comparisonPage.WaitForComparisonCompleteAsync();

        // Assert - Verify metrics are displayed in table rows
        int metricCount = await comparisonPage.GetMetricRowCountAsync();
        metricCount.ShouldBeGreaterThan(0, "Comparison table should display multiple metric rows");
    }
}
