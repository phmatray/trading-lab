namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Data Management page (/data).
/// Tests historical data fetching and management.
/// </summary>
public class DataManagementPageTests : BaseTest
{
    public DataManagementPageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task DataManagementPage_WhenLoaded_ShouldDisplayTitle()
    {
        // Arrange
        var dataPage = new DataManagementPage(Page!, BaseUrl);

        // Act
        await dataPage.NavigateAsync();
        string? title = await dataPage.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("Data Management");
    }

    [Fact]
    public async Task DataManagementPage_WhenLoaded_ShouldHaveCorrectPageTitle()
    {
        // Arrange
        var dataPage = new DataManagementPage(Page!, BaseUrl);

        // Act
        await dataPage.NavigateAsync();
        string pageTitle = await Page!.TitleAsync();

        // Assert
        pageTitle.ShouldContain("Data Management");
        pageTitle.ShouldContain("TradingStrat");
    }

    [Fact]
    public async Task DataManagementPage_SubmitButton_ShouldBeEnabledWhenFormIsValid()
    {
        // Arrange
        var dataPage = new DataManagementPage(Page!, BaseUrl);
        await dataPage.NavigateAsync();

        // Act
        await dataPage.FillDataFetchFormAsync("AAPL");
        bool isDisabled = await dataPage.IsSubmitButtonDisabledAsync();

        // Assert
        isDisabled.ShouldBeFalse("Submit button should be enabled with valid ticker");
    }

    [Fact]
    public async Task DataManagementPage_FormFilling_ShouldAcceptValidInputs()
    {
        // Arrange
        var dataPage = new DataManagementPage(Page!, BaseUrl);
        await dataPage.NavigateAsync();

        // Act
        await dataPage.FillDataFetchFormAsync(
            ticker: "MSFT",
            isin: "US5949181045",
            startDate: "2023-01-01",
            endDate: "2023-12-31"
        );

        // Assert - No exception thrown, form accepts inputs
        bool isDisabled = await dataPage.IsSubmitButtonDisabledAsync();
        isDisabled.ShouldBeFalse();
    }

    [Fact]
    public async Task DataManagementPage_FormFilling_ShouldAcceptTickerOnly()
    {
        // Arrange
        var dataPage = new DataManagementPage(Page!, BaseUrl);
        await dataPage.NavigateAsync();

        // Act
        await dataPage.FillDataFetchFormAsync(ticker: "GOOGL");

        // Assert - ISIN and dates are optional
        bool isDisabled = await dataPage.IsSubmitButtonDisabledAsync();
        isDisabled.ShouldBeFalse("Submit should work with just ticker");
    }

    [Fact]
    public async Task DataManagementPage_BlazorConnection_ShouldBeEstablished()
    {
        // Arrange & Act
        await NavigateToAsync("/data");

        // Check that Blazor is initialized
        bool blazorInitialized = await Page!.EvaluateAsync<bool>("() => window.Blazor !== undefined");

        // Assert
        blazorInitialized.ShouldBeTrue("Blazor SignalR connection should be established");
    }
}
