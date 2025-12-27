namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Strategy Comparison page (/strategies/compare).
/// Provides methods to interact with multi-strategy comparison features.
/// </summary>
public class StrategyComparisonPage : BasePage
{
    public StrategyComparisonPage(IPage page, string baseUrl)
        : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/strategies/compare";

    // Locators
    private ILocator PageTitle => Page.Locator("h1:has-text('Compare Strategies')");
    private ILocator StrategySelectors => Page.Locator("select:has(option:has-text('Strategy'))");
    private ILocator AddStrategyButton => Page.Locator("button:has-text('Add Strategy')");
    private ILocator CompareButton => Page.Locator("button:has-text('Compare')");
    private ILocator ComparisonMatrix => Page.Locator("table:has(th:has-text('Strategy'))");
    private ILocator EquityChart => Page.Locator(".equity-chart, [data-testid='equity-chart']");
    private ILocator ExportButton => Page.Locator("button:has-text('Export')");
    private ILocator SelectedStrategyCards => Page.Locator(".strategy-card, .card:has(.badge)");

    // Methods
    public async Task<bool> IsPageDisplayedAsync()
    {
        return await PageTitle.IsVisibleAsync();
    }

    public async Task<string?> GetPageTitleAsync()
    {
        return await PageTitle.TextContentAsync();
    }

    public async Task<bool> IsCompareButtonVisibleAsync()
    {
        return await CompareButton.IsVisibleAsync();
    }

    public async Task<int> GetStrategySelectorsCountAsync()
    {
        return await StrategySelectors.CountAsync();
    }

    public async Task ClickCompareButtonAsync()
    {
        await CompareButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    public async Task<bool> IsComparisonMatrixVisibleAsync()
    {
        return await ComparisonMatrix.IsVisibleAsync();
    }

    public async Task<bool> IsEquityChartVisibleAsync()
    {
        return await EquityChart.IsVisibleAsync();
    }

    public async Task<bool> IsExportButtonVisibleAsync()
    {
        return await ExportButton.IsVisibleAsync();
    }

    public async Task<bool> IsAddStrategyButtonVisibleAsync()
    {
        return await AddStrategyButton.IsVisibleAsync();
    }

    public async Task SelectStrategyAsync(int selectorIndex, string strategyName)
    {
        ILocator selector = StrategySelectors.Nth(selectorIndex);
        await selector.SelectOptionAsync(new[] { strategyName });
        await Page.WaitForBlazorAsync();
    }

    public async Task<int> GetSelectedStrategiesCountAsync()
    {
        return await SelectedStrategyCards.CountAsync();
    }
}
