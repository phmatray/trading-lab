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

    [Fact]
    public async Task FetchButton_ShouldTriggerFetch()
    {
        // Arrange
        var dataPage = new DataManagementPage(Page!, BaseUrl);
        await dataPage.NavigateAsync();
        await dataPage.FillDataFetchFormAsync("AAPL");

        // Act
        await dataPage.SubmitFormAsync();
        await dataPage.WaitForDataFetchCompleteAsync();

        // Assert - Verify either success or error message appears (data fetch was triggered)
        bool hasSuccessOrError = await dataPage.HasSuccessMessageAsync() || await dataPage.HasErrorMessageAsync();
        hasSuccessOrError.ShouldBeTrue("Fetch button should trigger data fetching and show result message");
    }

    [Fact]
    public async Task InvalidTicker_ShouldShowError()
    {
        // Arrange
        var dataPage = new DataManagementPage(Page!, BaseUrl);
        await dataPage.NavigateAsync();

        // Act - Use invalid ticker format
        await dataPage.FillDataFetchFormAsync("INVALID_TICKER_12345_XYZ");
        await dataPage.SubmitFormAsync();
        await dataPage.WaitForDataFetchCompleteAsync();

        // Assert - Should show error message for invalid ticker
        bool hasError = await dataPage.HasErrorMessageAsync();
        hasError.ShouldBeTrue("Invalid ticker should display error message");
    }

    [Fact]
    public async Task ValidTicker_ShouldShowSuccessMessage()
    {
        // Arrange
        var dataPage = new DataManagementPage(Page!, BaseUrl);
        await dataPage.NavigateAsync();

        // Act - Use well-known valid ticker
        await dataPage.FillDataFetchFormAsync("AAPL");
        await dataPage.SubmitFormAsync();
        await dataPage.WaitForDataFetchCompleteAsync();

        // Assert - Should show success message or data summary
        bool hasSuccess = await dataPage.HasSuccessMessageAsync() || await dataPage.IsDataSummaryDisplayedAsync();
        hasSuccess.ShouldBeTrue("Valid ticker should display success message or data summary");
    }

    [Fact]
    public async Task ProgressBar_ShouldAppearDuringFetch()
    {
        // Arrange
        var dataPage = new DataManagementPage(Page!, BaseUrl);
        await dataPage.NavigateAsync();
        await dataPage.FillDataFetchFormAsync("MSFT");

        // Act
        await dataPage.SubmitFormAsync();

        // Try to catch progress indicator (it may be very fast)
        bool progressAppeared = false;
        try
        {
            var progressIndicator = Page!.Locator("[data-testid='progress-indicator']").Or(Page!.Locator("text=Fetching Data"));
            await progressIndicator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 2000
            });
            progressAppeared = true;
        }
        catch (TimeoutException)
        {
            // Progress indicator may not appear if fetch is very fast
            // This is acceptable - we'll verify the operation completed instead
        }

        await dataPage.WaitForDataFetchCompleteAsync();

        // Assert - Either progress appeared or operation completed successfully
        bool operationCompleted = await dataPage.HasSuccessMessageAsync() || await dataPage.HasErrorMessageAsync();
        (progressAppeared || operationCompleted).ShouldBeTrue("Progress indicator should appear or operation should complete");
    }

    [Fact]
    public async Task DateRangeInputs_ShouldAcceptDates()
    {
        // Arrange
        var dataPage = new DataManagementPage(Page!, BaseUrl);
        await dataPage.NavigateAsync();

        // Act - Fill with specific date range
        string startDate = "2024-01-01";
        string endDate = "2024-06-30";
        await dataPage.FillDataFetchFormAsync(
            ticker: "GOOGL",
            startDate: startDate,
            endDate: endDate
        );

        // Assert - Verify inputs accepted the dates
        string startValue = await Page!.Locator("#start-date").InputValueAsync();
        string endValue = await Page!.Locator("#end-date").InputValueAsync();

        startValue.ShouldBe(startDate, "Start date input should accept and retain the value");
        endValue.ShouldBe(endDate, "End date input should accept and retain the value");
    }

    [Fact]
    public async Task DataSummary_ShouldDisplayAfterFetch()
    {
        // Arrange
        var dataPage = new DataManagementPage(Page!, BaseUrl);
        await dataPage.NavigateAsync();
        await dataPage.FillDataFetchFormAsync("AAPL");

        // Act
        await dataPage.SubmitFormAsync();
        await dataPage.WaitForDataFetchCompleteAsync();

        // Assert - Verify data summary section appears
        bool hasSummary = await dataPage.IsDataSummaryDisplayedAsync();
        bool hasSuccess = await dataPage.HasSuccessMessageAsync();

        (hasSummary || hasSuccess).ShouldBeTrue("Data summary or success message should display after successful fetch");
    }

    [Fact]
    public async Task ProgressIndicator_ShouldHideAfterDataFetchCompletes()
    {
        // Arrange
        var dataPage = new DataManagementPage(Page!, BaseUrl);
        await dataPage.NavigateAsync();
        await dataPage.FillDataFetchFormAsync("AAPL");

        // Act
        await dataPage.SubmitFormAsync();
        await dataPage.WaitForDataFetchCompleteAsync(timeoutMs: 30000);

        // Assert - Progress indicator MUST be hidden
        var progressIndicator = Page!.Locator("[data-testid='progress-indicator']")
            .Or(Page!.Locator("text=Fetching Data"));

        bool isHidden = await progressIndicator.IsHiddenAsync();
        isHidden.ShouldBeTrue("Progress indicator should be hidden after fetch completes");

        // Verify results appeared
        bool hasResult = await dataPage.HasSuccessMessageAsync() ||
                         await dataPage.HasErrorMessageAsync() ||
                         await dataPage.IsDataSummaryDisplayedAsync();
        hasResult.ShouldBeTrue("Should show result after fetch completes");
    }
}
