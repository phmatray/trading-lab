namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Strategy Workspace page (/workspace).
/// Provides methods to interact with the unified tabbed interface for Define → Test → Optimize → Deploy.
/// </summary>
public class StrategyWorkspacePage : BasePage
{
    public StrategyWorkspacePage(IPage page, string baseUrl)
        : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/workspace";

    // Locators
    private ILocator PageTitle => Page.Locator("h1:has-text('Strategy Workspace')");
    private ILocator DefineTab => Page.Locator("[role='tab']:has-text('Define'), button:has-text('Define')");
    private ILocator TestTab => Page.Locator("[role='tab']:has-text('Test'), button:has-text('Test')");
    private ILocator OptimizeTab => Page.Locator("[role='tab']:has-text('Optimize'), button:has-text('Optimize')");
    private ILocator DeployTab => Page.Locator("[role='tab']:has-text('Deploy'), button:has-text('Deploy')");
    private ILocator TabPanels => Page.Locator("[role='tabpanel'], .tab-content");
    private ILocator ActiveTab => Page.Locator("[role='tab'][aria-selected='true'], button.active");

    // Methods
    public async Task<bool> IsPageDisplayedAsync()
    {
        return await PageTitle.IsVisibleAsync();
    }

    public async Task<string?> GetPageTitleAsync()
    {
        return await PageTitle.TextContentAsync();
    }

    public async Task<bool> IsDefineTabVisibleAsync()
    {
        return await DefineTab.IsVisibleAsync();
    }

    public async Task<bool> IsTestTabVisibleAsync()
    {
        return await TestTab.IsVisibleAsync();
    }

    public async Task<bool> IsOptimizeTabVisibleAsync()
    {
        return await OptimizeTab.IsVisibleAsync();
    }

    public async Task<bool> IsDeployTabVisibleAsync()
    {
        return await DeployTab.IsVisibleAsync();
    }

    public async Task<bool> AreAllTabsVisibleAsync()
    {
        return await IsDefineTabVisibleAsync() &&
               await IsTestTabVisibleAsync() &&
               await IsOptimizeTabVisibleAsync() &&
               await IsDeployTabVisibleAsync();
    }

    public async Task ClickDefineTabAsync()
    {
        await DefineTab.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    public async Task ClickTestTabAsync()
    {
        await TestTab.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    public async Task ClickOptimizeTabAsync()
    {
        await OptimizeTab.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    public async Task ClickDeployTabAsync()
    {
        await DeployTab.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    public async Task<string?> GetActiveTabTextAsync()
    {
        return await ActiveTab.TextContentAsync();
    }

    public async Task<bool> IsTabPanelVisibleAsync()
    {
        return await TabPanels.First.IsVisibleAsync();
    }
}
