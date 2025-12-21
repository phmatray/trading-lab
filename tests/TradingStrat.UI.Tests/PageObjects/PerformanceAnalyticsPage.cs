namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Performance Analytics page (/portfolio/{id}/performance).
/// Represents the portfolio performance analysis interface with metrics and historical data.
/// </summary>
public class PerformanceAnalyticsPage : BasePage
{
    private readonly int _portfolioId;

    public PerformanceAnalyticsPage(IPage page, string baseUrl, int portfolioId) : base(page, baseUrl)
    {
        _portfolioId = portfolioId;
    }

    protected override string PagePath => $"/portfolio/{_portfolioId}/performance";

    // Page Elements
    private ILocator PageTitle => Page.Locator("main h1");
    private ILocator BackButton => Page.Locator("a:has-text('← Back to Dashboard')");

    // Date Range Selector
    private ILocator StartDateInput => Page.Locator("input[type='date']").First;
    private ILocator EndDateInput => Page.Locator("input[type='date']").Nth(1);
    private ILocator AnalyzeButton => Page.Locator("button:has-text('Analyze Performance')");

    // Quick Date Range Buttons
    private ILocator OneMonthButton => Page.Locator("button:has-text('1M')");
    private ILocator ThreeMonthButton => Page.Locator("button:has-text('3M')");
    private ILocator SixMonthButton => Page.Locator("button:has-text('6M')");
    private ILocator OneYearButton => Page.Locator("button:has-text('1Y')");
    private ILocator AllTimeButton => Page.Locator("button:has-text('All')");

    // Performance Metrics Cards
    private ILocator MetricsGrid => Page.Locator(".grid.grid-cols-1.md\\:grid-cols-2");
    private ILocator MetricCards => MetricsGrid.Locator(".card");

    // Historical Data Table
    private ILocator HistoricalTable => Page.Locator("table");
    private ILocator HistoricalRows => HistoricalTable.Locator("tbody tr");

    // Loading Indicator
    private ILocator LoadingSpinner => Page.Locator(".animate-spin");

    /// <summary>
    /// Gets the page title text.
    /// </summary>
    public async Task<string?> GetPageTitleAsync()
    {
        return await PageTitle.TextContentAsync();
    }

    /// <summary>
    /// Sets the start date for analysis.
    /// </summary>
    public async Task SetStartDateAsync(DateTime date)
    {
        await StartDateInput.FillAsync(date.ToString("yyyy-MM-dd"));
    }

    /// <summary>
    /// Sets the end date for analysis.
    /// </summary>
    public async Task SetEndDateAsync(DateTime date)
    {
        await EndDateInput.FillAsync(date.ToString("yyyy-MM-dd"));
    }

    /// <summary>
    /// Clicks the Analyze Performance button.
    /// </summary>
    public async Task ClickAnalyzeAsync()
    {
        await AnalyzeButton.ClickAsync();
        await Task.Delay(500); // Wait for analysis
    }

    /// <summary>
    /// Clicks a quick date range button (1M, 3M, 6M, 1Y, All).
    /// </summary>
    public async Task ClickQuickDateRangeAsync(string range)
    {
        ILocator button = range switch
        {
            "1M" => OneMonthButton,
            "3M" => ThreeMonthButton,
            "6M" => SixMonthButton,
            "1Y" => OneYearButton,
            "All" => AllTimeButton,
            _ => throw new ArgumentException($"Invalid range: {range}")
        };

        await button.ClickAsync();
        await Task.Delay(500); // Wait for analysis
    }

    /// <summary>
    /// Waits for the loading spinner to disappear.
    /// </summary>
    public async Task WaitForAnalysisToCompleteAsync(int timeoutMs = 10000)
    {
        try
        {
            await LoadingSpinner.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = timeoutMs
            });
        }
        catch
        {
            // Ignore timeout if spinner is not found or already hidden
        }
    }

    /// <summary>
    /// Gets the count of metric cards displayed.
    /// </summary>
    public async Task<int> GetMetricCardCountAsync()
    {
        return await MetricCards.CountAsync();
    }

    /// <summary>
    /// Gets a specific metric card by title.
    /// </summary>
    public ILocator GetMetricCardByTitle(string title)
    {
        return Page.Locator($".card:has-text('{title}')");
    }

    /// <summary>
    /// Checks if a specific metric card exists.
    /// </summary>
    public async Task<bool> HasMetricCardAsync(string title)
    {
        ILocator card = GetMetricCardByTitle(title);
        return await card.IsVisibleAsync();
    }

    /// <summary>
    /// Gets the value from a specific metric card.
    /// </summary>
    public async Task<string?> GetMetricValueAsync(string metricTitle)
    {
        ILocator card = GetMetricCardByTitle(metricTitle);
        ILocator valueElement = card.Locator("p.text-2xl, p.text-3xl").First;
        return await valueElement.TextContentAsync();
    }

    /// <summary>
    /// Checks if the historical data table is visible.
    /// </summary>
    public async Task<bool> IsHistoricalTableVisibleAsync()
    {
        return await HistoricalTable.IsVisibleAsync();
    }

    /// <summary>
    /// Gets the count of rows in the historical data table.
    /// </summary>
    public async Task<int> GetHistoricalRowCountAsync()
    {
        return await HistoricalRows.CountAsync();
    }

    /// <summary>
    /// Gets all dates from the historical data table.
    /// </summary>
    public async Task<List<string?>> GetHistoricalDatesAsync()
    {
        int count = await GetHistoricalRowCountAsync();
        List<string?> dates = new();

        for (int i = 0; i < count; i++)
        {
            ILocator row = HistoricalRows.Nth(i);
            ILocator dateCell = row.Locator("td").First;
            string? date = await dateCell.TextContentAsync();
            dates.Add(date?.Trim());
        }

        return dates;
    }

    /// <summary>
    /// Clicks the Back to Dashboard button.
    /// </summary>
    public async Task ClickBackToDashboardAsync()
    {
        await BackButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Performs a complete performance analysis with custom date range.
    /// </summary>
    public async Task AnalyzePerformanceAsync(DateTime startDate, DateTime endDate)
    {
        await SetStartDateAsync(startDate);
        await SetEndDateAsync(endDate);
        await ClickAnalyzeAsync();
        await WaitForAnalysisToCompleteAsync();
    }

    /// <summary>
    /// Checks if the "No data" message is displayed.
    /// </summary>
    public async Task<bool> HasNoDataMessageAsync()
    {
        ILocator message = Page.Locator("text=No performance data available");
        return await message.IsVisibleAsync();
    }

    /// <summary>
    /// Gets the Total Return metric value.
    /// </summary>
    public async Task<string?> GetTotalReturnAsync()
    {
        return await GetMetricValueAsync("Total Return");
    }

    /// <summary>
    /// Gets the Volatility metric value.
    /// </summary>
    public async Task<string?> GetVolatilityAsync()
    {
        return await GetMetricValueAsync("Volatility");
    }

    /// <summary>
    /// Gets the Sharpe Ratio metric value.
    /// </summary>
    public async Task<string?> GetSharpeRatioAsync()
    {
        return await GetMetricValueAsync("Sharpe Ratio");
    }

    /// <summary>
    /// Gets the Diversification Ratio metric value.
    /// </summary>
    public async Task<string?> GetDiversificationRatioAsync()
    {
        return await GetMetricValueAsync("Diversification");
    }
}
