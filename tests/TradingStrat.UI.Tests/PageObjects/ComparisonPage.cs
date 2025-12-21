namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Comparison page (/comparison).
/// Represents the A/B strategy comparison interface.
/// </summary>
public class ComparisonPage : BasePage
{
    public ComparisonPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/comparison";

    // Page Elements
    private ILocator PageTitle => Page.Locator("main h1");
    private ILocator TickerInput => Page.Locator("#ticker");
    private ILocator CapitalInput => Page.Locator("#capital");
    private ILocator SubmitButton => Page.Locator("button[type='submit']");
    private ILocator ProgressIndicator => Page.Locator("[data-testid='progress-indicator']");
    private ILocator ErrorMessage => Page.Locator("[role='alert']").Or(Page.Locator(".alert"));

    // Strategy Form Elements (two variants)
    private ILocator StrategySelects => Page.Locator("select");

    // Results Elements
    private ILocator NoResultsPlaceholder => Page.Locator("text=No comparison results");
    private ILocator WinnerAnnouncement => Page.Locator("text=/Winner:/");
    private ILocator ComparisonTable => Page.Locator("text=Performance Comparison").Locator("..").Locator("table");
    private ILocator MetricRows => Page.Locator("tbody tr");
    private ILocator VariantAMetrics => Page.Locator("text=Variant A:").Or(Page.Locator("text=Variant A Trades"));
    private ILocator VariantBMetrics => Page.Locator("text=Variant B:").Or(Page.Locator("text=Variant B Trades"));

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
    /// Fills the common comparison form fields.
    /// </summary>
    public async Task FillCommonFieldsAsync(string ticker, decimal capital)
    {
        await TickerInput.FillAsync(ticker);
        await CapitalInput.FillAsync(capital.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Selects strategy for Variant A.
    /// </summary>
    public async Task SelectVariantAStrategyAsync(string strategyValue)
    {
        var strategySelect = await StrategySelects.Nth(0).ElementHandleAsync();
        if (strategySelect != null)
        {
            await strategySelect.SelectOptionAsync(strategyValue);
            await Task.Delay(100); // Wait for parameters to load
        }
    }

    /// <summary>
    /// Selects strategy for Variant B.
    /// </summary>
    public async Task SelectVariantBStrategyAsync(string strategyValue)
    {
        var strategySelect = await StrategySelects.Nth(1).ElementHandleAsync();
        if (strategySelect != null)
        {
            await strategySelect.SelectOptionAsync(strategyValue);
            await Task.Delay(100); // Wait for parameters to load
        }
    }

    /// <summary>
    /// Submits the comparison form.
    /// </summary>
    public async Task SubmitFormAsync()
    {
        await SubmitButton.ClickAsync();
    }

    /// <summary>
    /// Waits for the comparison to complete.
    /// </summary>
    public async Task WaitForComparisonCompleteAsync(int timeoutMs = 60000)
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
    /// Checks if results are displayed.
    /// </summary>
    public async Task<bool> AreResultsDisplayedAsync()
    {
        return await ComparisonTable.IsVisibleAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if the "no results" placeholder is displayed.
    /// </summary>
    public async Task<bool> IsNoResultsPlaceholderDisplayedAsync()
    {
        return await NoResultsPlaceholder.IsVisibleAsync();
    }

    /// <summary>
    /// Checks if the winner announcement is displayed.
    /// </summary>
    public async Task<bool> IsWinnerAnnouncementDisplayedAsync()
    {
        return await WinnerAnnouncement.IsVisibleAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the winner announcement text.
    /// </summary>
    public async Task<string?> GetWinnerAnnouncementAsync()
    {
        if (await IsWinnerAnnouncementDisplayedAsync())
        {
            return await WinnerAnnouncement.TextContentAsync();
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
    /// Checks if both variant metrics are displayed.
    /// </summary>
    public async Task<bool> AreBothVariantMetricsDisplayedAsync()
    {
        bool hasVariantA = await VariantAMetrics.IsVisibleAsync().ConfigureAwait(false);
        bool hasVariantB = await VariantBMetrics.IsVisibleAsync().ConfigureAwait(false);
        return hasVariantA && hasVariantB;
    }

    /// <summary>
    /// Gets the number of metric comparison rows in the table.
    /// </summary>
    public async Task<int> GetMetricRowCountAsync()
    {
        if (await AreResultsDisplayedAsync())
        {
            return await MetricRows.CountAsync();
        }
        return 0;
    }
}
