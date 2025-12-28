namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Data Management page (/data).
/// Represents the historical data fetching interface.
/// </summary>
public class DataManagementPage : BasePage
{
    public DataManagementPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/data";

    // Page Elements
    private ILocator PageTitle => Page.Locator("main h1");
    private ILocator TickerInput => Page.Locator("#ticker");
    private ILocator ISINInput => Page.Locator("#isin");
    private ILocator StartDateInput => Page.Locator("#start-date");
    private ILocator EndDateInput => Page.Locator("#end-date");
    private ILocator SubmitButton => Page.Locator("button[type='submit']");
    private ILocator ProgressIndicator => Page.Locator("[data-testid='progress-indicator']").Or(Page.Locator("text=Fetching Data"));
    private ILocator ErrorMessage => Page.Locator("[role='alert'] >> text=/Error/");
    private ILocator SuccessMessage => Page.Locator("[role='alert'] >> text=/Success/");

    // Results Elements
    private ILocator DataSummary => Page.Locator("text=Data Summary");
    private ILocator TickerDisplay => Page.Locator("dt:has-text('Ticker') + dd");

    /// <summary>
    /// Gets the main page title text.
    /// </summary>
    public async Task<string?> GetPageTitleAsync()
    {
        return await PageTitle.TextContentAsync();
    }

    /// <summary>
    /// Checks if the submit button is disabled.
    /// </summary>
    public async Task<bool> IsSubmitButtonDisabledAsync()
    {
        return await SubmitButton.IsDisabledAsync();
    }

    /// <summary>
    /// Fills the data fetch form with the given values.
    /// </summary>
    public async Task FillDataFetchFormAsync(string ticker, string? isin = null, string? startDate = null, string? endDate = null)
    {
        await TickerInput.FillAsync(ticker);

        if (isin is not null)
        {
            await ISINInput.FillAsync(isin);
        }

        if (startDate is not null)
        {
            await StartDateInput.FillAsync(startDate);
        }

        if (endDate is not null)
        {
            await EndDateInput.FillAsync(endDate);
        }
    }

    /// <summary>
    /// Submits the data fetch form.
    /// </summary>
    public async Task SubmitFormAsync()
    {
        await SubmitButton.ClickAsync();
    }

    /// <summary>
    /// Waits for the data fetch to complete.
    /// </summary>
    public async Task WaitForDataFetchCompleteAsync(int timeoutMs = 30000)
    {
        try
        {
            // Wait for progress indicator to appear
            await ProgressIndicator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 2000
            });
        }
        catch (TimeoutException)
        {
            // Progress might not appear for fast operations
        }

        try
        {
            // Wait for progress indicator to disappear
            await ProgressIndicator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = timeoutMs
            });
        }
        catch (TimeoutException)
        {
            // Already hidden or never appeared
        }

        await Task.Delay(500); // Additional delay for results rendering
    }

    /// <summary>
    /// Checks if the data summary is displayed.
    /// </summary>
    public async Task<bool> IsDataSummaryDisplayedAsync()
    {
        return await DataSummary.IsVisibleAsync();
    }

    /// <summary>
    /// Checks if a success message is displayed.
    /// </summary>
    public async Task<bool> HasSuccessMessageAsync()
    {
        return await SuccessMessage.IsVisibleAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the success message text if displayed.
    /// </summary>
    public async Task<string?> GetSuccessMessageAsync()
    {
        if (await HasSuccessMessageAsync())
        {
            return await SuccessMessage.TextContentAsync();
        }
        return null;
    }

    /// <summary>
    /// Checks if an error message is displayed.
    /// </summary>
    public async Task<bool> HasErrorMessageAsync()
    {
        return await ErrorMessage.IsVisibleAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the error message text if displayed.
    /// </summary>
    public async Task<string?> GetErrorMessageAsync()
    {
        if (await HasErrorMessageAsync())
        {
            return await ErrorMessage.TextContentAsync();
        }
        return null;
    }

    /// <summary>
    /// Gets the displayed ticker from the results.
    /// </summary>
    public async Task<string?> GetDisplayedTickerAsync()
    {
        return await TickerDisplay.TextContentAsync();
    }
}
