namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Phase 4 Layout Showcase page.
/// </summary>
public class Phase4LayoutShowcasePage : BasePage
{
    protected override string PagePath => "/phase4-layout-showcase";

    public Phase4LayoutShowcasePage(IPage page, string baseUrl)
        : base(page, baseUrl)
    {
    }

    // Layout Switcher
    private ILocator LayoutSwitcher => Page.Locator("[data-testid='layout-switcher']");
    private ILocator SwitchToSidebarButton => Page.Locator("[data-testid='switch-to-sidebar']");
    private ILocator SwitchToStackedButton => Page.Locator("[data-testid='switch-to-stacked']");
    private ILocator SwitchToAuthButton => Page.Locator("[data-testid='switch-to-auth']");

    // Layout Demos
    private ILocator SidebarLayoutDemo => Page.Locator("[data-testid='sidebar-layout-demo']");
    private ILocator StackedLayoutDemo => Page.Locator("[data-testid='stacked-layout-demo']");
    private ILocator AuthLayoutDemo => Page.Locator("[data-testid='auth-layout-demo']");

    // Sidebar Layout Elements
    private ILocator SidebarLayoutNavbar => Page.Locator("[data-testid='sidebar-layout-navbar']");
    private ILocator SidebarLayoutContent => Page.Locator("[data-testid='sidebar-layout-content']");

    // Stacked Layout Elements
    private ILocator StackedLayoutNavbar => Page.Locator("[data-testid='stacked-layout-navbar']");
    private ILocator StackedLayoutContent => Page.Locator("[data-testid='stacked-layout-content']");

    // Auth Layout Elements
    private ILocator AuthLayoutContent => Page.Locator("[data-testid='auth-layout-content']");

    // Current Layout Indicator
    private ILocator CurrentLayoutName => Page.Locator("[data-testid='current-layout-name']");

    // Actions
    public async Task SwitchToSidebarLayoutAsync()
    {
        await SwitchToSidebarButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    public async Task SwitchToStackedLayoutAsync()
    {
        await SwitchToStackedButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    public async Task SwitchToAuthLayoutAsync()
    {
        await SwitchToAuthButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    // Queries
    public async Task<bool> IsLayoutSwitcherVisibleAsync()
    {
        return await LayoutSwitcher.IsVisibleAsync();
    }

    public async Task<bool> IsSidebarLayoutVisibleAsync()
    {
        return await SidebarLayoutDemo.IsVisibleAsync();
    }

    public async Task<bool> IsStackedLayoutVisibleAsync()
    {
        return await StackedLayoutDemo.IsVisibleAsync();
    }

    public async Task<bool> IsAuthLayoutVisibleAsync()
    {
        return await AuthLayoutDemo.IsVisibleAsync();
    }

    public async Task<string?> GetCurrentLayoutNameAsync()
    {
        return await CurrentLayoutName.TextContentAsync();
    }

    public async Task<bool> IsSidebarLayoutNavbarVisibleAsync()
    {
        return await SidebarLayoutNavbar.IsVisibleAsync();
    }

    public async Task<bool> IsSidebarLayoutContentVisibleAsync()
    {
        return await SidebarLayoutContent.IsVisibleAsync();
    }

    public async Task<bool> IsStackedLayoutNavbarVisibleAsync()
    {
        return await StackedLayoutNavbar.IsVisibleAsync();
    }

    public async Task<bool> IsStackedLayoutContentVisibleAsync()
    {
        return await StackedLayoutContent.IsVisibleAsync();
    }

    public async Task<bool> IsAuthLayoutContentVisibleAsync()
    {
        return await AuthLayoutContent.IsVisibleAsync();
    }

    public async Task<string?> GetSidebarLayoutContentTextAsync()
    {
        return await SidebarLayoutContent.TextContentAsync();
    }

    public async Task<string?> GetStackedLayoutContentTextAsync()
    {
        return await StackedLayoutContent.TextContentAsync();
    }

    public async Task<string?> GetAuthLayoutContentTextAsync()
    {
        return await AuthLayoutContent.TextContentAsync();
    }
}
