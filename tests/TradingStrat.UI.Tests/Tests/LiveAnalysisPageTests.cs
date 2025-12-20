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
}
