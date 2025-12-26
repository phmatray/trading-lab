namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Backtest Archive page (/backtests).
/// Provides methods to interact with saved backtest runs.
/// </summary>
public class BacktestArchivePage : BasePage
{
    public BacktestArchivePage(IPage page, string baseUrl)
        : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/backtests";

    // Locators
    private ILocator PageTitle => Page.Locator("h1:has-text('Backtest Archive')");
    private ILocator BacktestCards => Page.Locator(".card:has(.badge)"); // Cards with strategy badges
    private ILocator FilterSection => Page.Locator("div:has-text('Filter')");
    private ILocator TickerFilter => Page.Locator("input[placeholder*='ticker' i], input[placeholder*='symbol' i]");
    private ILocator SortDropdown => Page.Locator("select:has(option:has-text('Date'))");
    private ILocator EmptyState => Page.Locator("div:has-text('No backtest runs found')");

    // Methods
    public async Task<bool> IsPageDisplayedAsync()
    {
        return await PageTitle.IsVisibleAsync();
    }

    public async Task<string?> GetPageTitleAsync()
    {
        return await PageTitle.TextContentAsync();
    }

    public async Task<int> GetBacktestCardCountAsync()
    {
        return await BacktestCards.CountAsync();
    }

    public async Task<bool> HasBacktestCardsAsync()
    {
        int count = await GetBacktestCardCountAsync();
        return count > 0;
    }

    public async Task<bool> IsFilterSectionVisibleAsync()
    {
        return await FilterSection.IsVisibleAsync();
    }

    public async Task<bool> IsEmptyStateVisibleAsync()
    {
        return await EmptyState.IsVisibleAsync();
    }

    public async Task FilterByTickerAsync(string ticker)
    {
        if (await TickerFilter.IsVisibleAsync())
        {
            await TickerFilter.FillAsync(ticker);
            await Page.WaitForBlazorAsync();
        }
    }

    public async Task<string?> GetFirstBacktestTicker()
    {
        var firstCard = BacktestCards.First;
        var tickerElement = firstCard.Locator("text=/^[A-Z]{1,5}$/");
        return await tickerElement.TextContentAsync();
    }

    public async Task<bool> HasSortOptionsAsync()
    {
        return await SortDropdown.IsVisibleAsync();
    }
}
