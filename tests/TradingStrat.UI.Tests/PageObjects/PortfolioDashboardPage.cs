namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Portfolio Dashboard page (/portfolio/{id}).
/// Represents the portfolio overview with metrics, positions, and actions.
/// </summary>
public class PortfolioDashboardPage : BasePage
{
    private readonly int _portfolioId;

    public PortfolioDashboardPage(IPage page, string baseUrl, int portfolioId) : base(page, baseUrl)
    {
        _portfolioId = portfolioId;
    }

    protected override string PagePath => $"/portfolio/{_portfolioId}";

    // Page Elements
    private ILocator PageTitle => Page.Locator("main h1");
    private ILocator BackButton => Page.Locator("a:has-text('← Back to Portfolios')");
    private ILocator RefreshButton => Page.Locator("button:has-text('Refresh Prices')");

    // Metric Cards
    private ILocator TotalValueCard => Page.Locator(".card:has-text('Total Value')");
    private ILocator CashCard => Page.Locator(".card:has-text('Cash')");
    private ILocator GainLossCard => Page.Locator(".card:has-text('Gain/Loss')");
    private ILocator ReturnCard => Page.Locator(".card:has-text('Return %')");

    // Position Table
    private ILocator PositionTable => Page.Locator("table");
    private ILocator PositionRows => PositionTable.Locator("tbody tr");

    // Action Buttons
    private ILocator ManagePositionsButton => Page.Locator("a[href*='/positions']");
    private ILocator RebalanceButton => Page.Locator("a[href*='/rebalance']");
    private ILocator PerformanceButton => Page.Locator("a[href*='/performance']");

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
    /// Checks if all metric cards are visible.
    /// </summary>
    public async Task<bool> AreMetricCardsVisibleAsync()
    {
        return await TotalValueCard.IsVisibleAsync()
            && await CashCard.IsVisibleAsync()
            && await GainLossCard.IsVisibleAsync()
            && await ReturnCard.IsVisibleAsync();
    }

    /// <summary>
    /// Gets the total value from the metric card.
    /// </summary>
    public async Task<string?> GetTotalValueAsync()
    {
        ILocator valueElement = TotalValueCard.Locator("p.text-3xl");
        return await valueElement.TextContentAsync();
    }

    /// <summary>
    /// Gets the cash value from the metric card.
    /// </summary>
    public async Task<string?> GetCashValueAsync()
    {
        ILocator valueElement = CashCard.Locator("p.text-3xl");
        return await valueElement.TextContentAsync();
    }

    /// <summary>
    /// Gets the gain/loss value from the metric card.
    /// </summary>
    public async Task<string?> GetGainLossValueAsync()
    {
        ILocator valueElement = GainLossCard.Locator("p.text-3xl");
        return await valueElement.TextContentAsync();
    }

    /// <summary>
    /// Gets the return percentage from the metric card.
    /// </summary>
    public async Task<string?> GetReturnPercentageAsync()
    {
        ILocator valueElement = ReturnCard.Locator("p.text-3xl");
        return await valueElement.TextContentAsync();
    }

    /// <summary>
    /// Checks if the position table is visible.
    /// </summary>
    public async Task<bool> IsPositionTableVisibleAsync()
    {
        return await PositionTable.IsVisibleAsync();
    }

    /// <summary>
    /// Gets the count of position rows in the table.
    /// </summary>
    public async Task<int> GetPositionCountAsync()
    {
        return await PositionRows.CountAsync();
    }

    /// <summary>
    /// Gets all position tickers displayed in the table.
    /// </summary>
    public async Task<List<string?>> GetPositionTickersAsync()
    {
        int count = await GetPositionCountAsync();
        List<string?> tickers = new();

        for (int i = 0; i < count; i++)
        {
            ILocator row = PositionRows.Nth(i);
            ILocator tickerCell = row.Locator("td").First;
            string? ticker = await tickerCell.TextContentAsync();
            tickers.Add(ticker?.Trim());
        }

        return tickers;
    }

    /// <summary>
    /// Clicks the Refresh Prices button.
    /// </summary>
    public async Task ClickRefreshPricesAsync()
    {
        await RefreshButton.ClickAsync();
        await Task.Delay(500); // Wait for price fetching
    }

    /// <summary>
    /// Waits for the loading spinner to disappear.
    /// </summary>
    public async Task WaitForLoadingToCompleteAsync(int timeoutMs = 10000)
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
    /// Clicks the Back to Portfolios button.
    /// </summary>
    public async Task ClickBackToPortfoliosAsync()
    {
        await BackButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Navigates to the Manage Positions page.
    /// </summary>
    public async Task NavigateToManagePositionsAsync()
    {
        await ManagePositionsButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Navigates to the Rebalancing page.
    /// </summary>
    public async Task NavigateToRebalancingAsync()
    {
        await RebalanceButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Navigates to the Performance Analytics page.
    /// </summary>
    public async Task NavigateToPerformanceAsync()
    {
        await PerformanceButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Checks if the "No positions" message is displayed.
    /// </summary>
    public async Task<bool> HasNoPositionsMessageAsync()
    {
        ILocator message = Page.Locator("text=No positions");
        return await message.IsVisibleAsync();
    }

    /// <summary>
    /// Gets a position row by ticker symbol.
    /// </summary>
    public ILocator GetPositionRowByTicker(string ticker)
    {
        return PositionTable.Locator($"tr:has-text('{ticker}')");
    }

    /// <summary>
    /// Checks if a specific position exists in the table.
    /// </summary>
    public async Task<bool> HasPositionAsync(string ticker)
    {
        ILocator row = GetPositionRowByTicker(ticker);
        return await row.IsVisibleAsync();
    }

    /// <summary>
    /// Gets the market value for a specific position.
    /// </summary>
    public async Task<string?> GetPositionMarketValueAsync(string ticker)
    {
        ILocator row = GetPositionRowByTicker(ticker);
        ILocator cells = row.Locator("td");
        // Market value is typically in the 5th column (index 4)
        ILocator marketValueCell = cells.Nth(4);
        return await marketValueCell.TextContentAsync();
    }

    /// <summary>
    /// Gets the gain/loss value for a specific position.
    /// </summary>
    public async Task<string?> GetPositionGainLossAsync(string ticker)
    {
        ILocator row = GetPositionRowByTicker(ticker);
        ILocator cells = row.Locator("td");
        // Gain/Loss is typically in the 6th column (index 5)
        ILocator gainLossCell = cells.Nth(5);
        return await gainLossCell.TextContentAsync();
    }
}
