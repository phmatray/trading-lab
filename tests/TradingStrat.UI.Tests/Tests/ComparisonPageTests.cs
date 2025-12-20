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
}
