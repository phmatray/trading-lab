namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Backtest page (/backtest).
/// Represents the strategy backtesting interface with form and results.
/// </summary>
public class BacktestPage : BasePage
{
    public BacktestPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/backtest";

    // Page Elements
    private ILocator PageTitle => Page.Locator("main h1");
    private ILocator TickerInput => Page.Locator("#ticker");
    private ILocator CapitalInput => Page.Locator("#capital");
    private ILocator StartDateInput => Page.Locator("#start-date");
    private ILocator EndDateInput => Page.Locator("#end-date");
    private ILocator SubmitButton => Page.Locator("button[type='submit']");
    private ILocator ProgressIndicator => Page.Locator("[data-testid='progress-indicator']").Or(Page.Locator("text=Running Backtest"));
    private ILocator ErrorMessage => Page.Locator("[role='alert']").Or(Page.Locator(".alert"));

    // Strategy Form Elements
    private ILocator StrategySelect => Page.Locator("select").First;

    // Results Elements
    private ILocator NoResultsPlaceholder => Page.Locator("text=No backtest results");
    private ILocator MetricsGrid => Page.Locator("text=Total Return").Or(Page.Locator("text=Final Capital"));
    private ILocator EquityChart => Page.Locator(".apexcharts-canvas");
    private ILocator TradeTable => Page.Locator("text=Trade History").Or(Page.Locator("text=No trades"));

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
    /// Fills the backtest form with the given values.
    /// </summary>
    public async Task FillBacktestFormAsync(string ticker, decimal capital, string? startDate = null, string? endDate = null)
    {
        await TickerInput.FillAsync(ticker);
        await CapitalInput.FillAsync(capital.ToString(System.Globalization.CultureInfo.InvariantCulture));

        if (startDate != null)
        {
            await StartDateInput.FillAsync(startDate);
        }

        if (endDate != null)
        {
            await EndDateInput.FillAsync(endDate);
        }
    }

    /// <summary>
    /// Selects a strategy from the dropdown.
    /// </summary>
    public async Task SelectStrategyAsync(string strategyValue)
    {
        await StrategySelect.SelectOptionAsync(strategyValue);
        await Task.Delay(100); // Wait for parameters to load
    }

    /// <summary>
    /// Submits the backtest form.
    /// </summary>
    public async Task SubmitFormAsync()
    {
        await SubmitButton.ClickAsync();
    }

    /// <summary>
    /// Waits for the backtest to complete.
    /// </summary>
    public async Task WaitForBacktestCompleteAsync(int timeoutMs = 30000)
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
        // Check if metrics are visible (results present)
        bool hasMetrics = await MetricsGrid.IsVisibleAsync().ConfigureAwait(false);
        return hasMetrics;
    }

    /// <summary>
    /// Checks if the "no results" placeholder is displayed.
    /// </summary>
    public async Task<bool> IsNoResultsPlaceholderDisplayedAsync()
    {
        return await NoResultsPlaceholder.IsVisibleAsync();
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
    /// Checks if the equity chart is visible.
    /// </summary>
    public async Task<bool> IsEquityChartVisibleAsync()
    {
        try
        {
            await EquityChart.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000
            });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the trade table is visible.
    /// </summary>
    public async Task<bool> IsTradeTableVisibleAsync()
    {
        return await TradeTable.IsVisibleAsync();
    }

    /// <summary>
    /// Checks if a specific Ichimoku parameter input is visible.
    /// </summary>
    public async Task<bool> HasIchimokuParameterAsync(string parameterId)
    {
        var input = Page.Locator($"#{parameterId}");
        return await input.IsVisibleAsync();
    }

    /// <summary>
    /// Fills Ichimoku strategy parameters.
    /// </summary>
    public async Task FillIchimokuParametersAsync(int tenkan = 9, int kijun = 26, int senkouB = 52)
    {
        await Page.Locator("#tenkan-period").FillAsync(tenkan.ToString());
        await Page.Locator("#kijun-period").FillAsync(kijun.ToString());
        await Page.Locator("#senkou-b-period").FillAsync(senkouB.ToString());
    }
}
