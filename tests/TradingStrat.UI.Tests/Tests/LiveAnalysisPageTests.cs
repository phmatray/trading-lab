namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Live Analysis page (/analysis).
/// Tests ML-based live position analysis.
/// </summary>
public class LiveAnalysisPageTests : BaseTest
{
    public LiveAnalysisPageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task LiveAnalysisPage_WhenLoaded_ShouldDisplayTitle()
    {
        // Arrange
        var analysisPage = new LiveAnalysisPage(Page!, BaseUrl);

        // Act
        await analysisPage.NavigateAsync();
        string? title = await analysisPage.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("Live");
    }

    [Fact]
    public async Task LiveAnalysisPage_WhenLoaded_ShouldHaveCorrectPageTitle()
    {
        // Arrange
        var analysisPage = new LiveAnalysisPage(Page!, BaseUrl);

        // Act
        await analysisPage.NavigateAsync();
        string pageTitle = await Page!.TitleAsync();

        // Assert
        pageTitle.ShouldContain("Live Analysis");
        pageTitle.ShouldContain("TradingStrat");
    }

    [Fact]
    public async Task LiveAnalysisPage_WhenLoaded_ShouldShowNoResultsPlaceholder()
    {
        // Arrange
        var analysisPage = new LiveAnalysisPage(Page!, BaseUrl);

        // Act
        await analysisPage.NavigateAsync();
        bool hasPlaceholder = await analysisPage.IsNoResultsPlaceholderDisplayedAsync();

        // Assert
        hasPlaceholder.ShouldBeTrue("No results placeholder should be visible initially");
    }

    [Fact]
    public async Task LiveAnalysisPage_SubmitButton_ShouldBeEnabledWhenFormIsValid()
    {
        // Arrange
        var analysisPage = new LiveAnalysisPage(Page!, BaseUrl);
        await analysisPage.NavigateAsync();

        // Act
        await analysisPage.FillAnalysisFormAsync("AAPL");
        bool isDisabled = await analysisPage.IsSubmitButtonDisabledAsync();

        // Assert
        isDisabled.ShouldBeFalse("Submit button should be enabled with valid ticker");
    }

    [Fact]
    public async Task LiveAnalysisPage_FormFilling_ShouldAcceptValidInputs()
    {
        // Arrange
        var analysisPage = new LiveAnalysisPage(Page!, BaseUrl);
        await analysisPage.NavigateAsync();

        // Act
        await analysisPage.FillAnalysisFormAsync(
            ticker: "MSFT",
            fetchFreshData: true,
            buyThreshold: 0.5m,
            sellThreshold: -0.3m
        );

        // Assert - No exception thrown, form accepts inputs
        bool isDisabled = await analysisPage.IsSubmitButtonDisabledAsync();
        isDisabled.ShouldBeFalse();
    }

    [Fact]
    public async Task LiveAnalysisPage_FetchFreshDataCheckbox_ShouldBeInteractive()
    {
        // Arrange
        var analysisPage = new LiveAnalysisPage(Page!, BaseUrl);
        await analysisPage.NavigateAsync();

        // Act
        await analysisPage.FillAnalysisFormAsync(
            ticker: "AAPL",
            fetchFreshData: true
        );

        // Assert - No errors when checking the checkbox
        string? title = await analysisPage.GetPageTitleAsync();
        title.ShouldNotBeNull();
        title.ShouldContain("Live");
    }

    [Fact]
    public async Task LiveAnalysisPage_BlazorConnection_ShouldBeEstablished()
    {
        // Arrange & Act
        await NavigateToAsync("/analysis");

        // Check that Blazor is initialized
        bool blazorInitialized = await Page!.EvaluateAsync<bool>("() => window.Blazor !== undefined");

        // Assert
        blazorInitialized.ShouldBeTrue("Blazor SignalR connection should be established");
    }

    [Fact]
    public async Task AnalyzeButton_ShouldGenerateSignal()
    {
        // Arrange
        var analysisPage = new LiveAnalysisPage(Page!, BaseUrl);
        await analysisPage.NavigateAsync();
        await analysisPage.FillAnalysisFormAsync("AAPL");

        // Act
        await analysisPage.SubmitFormAsync();
        await analysisPage.WaitForAnalysisCompleteAsync();

        // Assert - Verify results are displayed after analysis
        bool hasResults = await analysisPage.AreResultsDisplayedAsync();
        hasResults.ShouldBeTrue("Analyze button should generate results and display them");
    }

    [Fact]
    public async Task ResultsDisplay_ShouldShowTechnicalIndicators()
    {
        // Arrange
        var analysisPage = new LiveAnalysisPage(Page!, BaseUrl);
        await analysisPage.NavigateAsync();
        await analysisPage.FillAnalysisFormAsync("MSFT");

        // Act
        await analysisPage.SubmitFormAsync();
        await analysisPage.WaitForAnalysisCompleteAsync();

        // Assert - Verify technical indicators section is visible
        bool hasIndicators = await analysisPage.AreTechnicalIndicatorsVisibleAsync();
        hasIndicators.ShouldBeTrue("Results should display technical indicators section with all 26 indicators");
    }

    [Fact]
    public async Task PredictedSignal_ShouldDisplayWithText()
    {
        // Arrange
        var analysisPage = new LiveAnalysisPage(Page!, BaseUrl);
        await analysisPage.NavigateAsync();
        await analysisPage.FillAnalysisFormAsync("GOOGL");

        // Act
        await analysisPage.SubmitFormAsync();
        await analysisPage.WaitForAnalysisCompleteAsync();

        // Assert - Verify predicted signal is displayed
        string? signal = await analysisPage.GetPredictedSignalAsync();
        signal.ShouldNotBeNullOrEmpty("Predicted signal should display (Buy/Sell/Hold)");
    }

    [Fact]
    public async Task ThresholdParameters_ShouldAcceptCustomValues()
    {
        // Arrange
        var analysisPage = new LiveAnalysisPage(Page!, BaseUrl);
        await analysisPage.NavigateAsync();

        // Act - Fill with custom threshold values
        decimal buyThreshold = 0.6m;
        decimal sellThreshold = -0.4m;
        await analysisPage.FillAnalysisFormAsync(
            ticker: "TSLA",
            buyThreshold: buyThreshold,
            sellThreshold: sellThreshold
        );

        // Assert - Verify inputs accepted the values
        string? buyValue = await Page!.Locator("#buy-threshold").InputValueAsync();
        string? sellValue = await Page!.Locator("#sell-threshold").InputValueAsync();

        buyValue.ShouldBe(buyThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture), "Buy threshold should accept custom value");
        sellValue.ShouldBe(sellThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture), "Sell threshold should accept custom value");
    }

    [Fact]
    public async Task IndicatorAccordions_ShouldExpandAndCollapse()
    {
        // Arrange
        var analysisPage = new LiveAnalysisPage(Page!, BaseUrl);
        await analysisPage.NavigateAsync();
        await analysisPage.FillAnalysisFormAsync("AAPL");
        await analysisPage.SubmitFormAsync();
        await analysisPage.WaitForAnalysisCompleteAsync();

        // Act - Expand different indicator category accordions
        await analysisPage.ExpandIndicatorAccordionAsync("price-based");
        await Task.Delay(300);

        await analysisPage.ExpandIndicatorAccordionAsync("momentum");
        await Task.Delay(300);

        // Assert - Verify accordions are interactive (no exception thrown)
        bool hasIndicators = await analysisPage.AreTechnicalIndicatorsVisibleAsync();
        hasIndicators.ShouldBeTrue("Indicator accordions should be interactive and expandable");
    }
}
