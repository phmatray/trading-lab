namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Data Status page (/data/status).
/// Provides methods to interact with data coverage metrics and gap detection.
/// </summary>
public class DataStatusPage : BasePage
{
    public DataStatusPage(IPage page, string baseUrl)
        : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/data/status";

    // Locators
    private ILocator PageTitle => Page.Locator("h1:has-text('Data Status')");
    private ILocator CoverageSummaryCard => Page.Locator(".card:has-text('Coverage Summary')");
    private ILocator DataCoverageTable => Page.Locator("table.data-table");
    private ILocator TableRows => DataCoverageTable.Locator("tbody tr");
    private ILocator RefreshButton => Page.Locator("button:has-text('Refresh Status')");

    // Methods
    public async Task<bool> IsPageDisplayedAsync()
    {
        return await PageTitle.IsVisibleAsync();
    }

    public async Task<string?> GetPageTitleAsync()
    {
        return await PageTitle.TextContentAsync();
    }

    public async Task<bool> IsCoverageSummaryVisibleAsync()
    {
        return await CoverageSummaryCard.IsVisibleAsync();
    }

    public async Task<int> GetTickerCountAsync()
    {
        return await TableRows.CountAsync();
    }

    public async Task<bool> IsDataTableVisibleAsync()
    {
        return await DataCoverageTable.IsVisibleAsync();
    }

    public async Task ClickRefreshButtonAsync()
    {
        await RefreshButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    public async Task<bool> IsRefreshButtonVisibleAsync()
    {
        return await RefreshButton.IsVisibleAsync();
    }

    public async Task<bool> HasTableHeadersAsync()
    {
        var headers = await DataCoverageTable.Locator("thead th").AllAsync();
        return headers.Count >= 5; // Ticker, Records, Start Date, End Date, Coverage
    }

    public async Task<string?> GetFirstTickerAsync()
    {
        var firstRow = TableRows.First;
        return await firstRow.Locator("td").First.TextContentAsync();
    }
}
