namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Home page (/).
/// Represents the landing page with feature cards and navigation.
/// </summary>
public class HomePage : BasePage
{
    public HomePage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/";

    // Page Elements
    private ILocator PageTitle => Page.Locator("main h1");  // Target main content h1, not nav h1
    private ILocator HeroSection => Page.Locator(".card.bg-gradient-to-r");
    private ILocator DataManagementCard => Page.Locator("main .grid a[href='/data']");
    private ILocator BacktestCard => Page.Locator("main .grid a[href='/backtest']");
    private ILocator LiveAnalysisCard => Page.Locator("main .grid a[href='/analysis']");
    private ILocator ComparisonCard => Page.Locator("main .grid a[href='/comparison']");
    private ILocator TechnologyStackSection => Page.Locator("text=Technology Stack").Locator("..");
    private ILocator ArchitectureSection => Page.Locator("h2:has-text('Hexagonal Architecture')").Locator("..");

    // Quick Stats
    private ILocator StrategiesStat => Page.Locator("text=Strategies Available").Locator("..");

    /// <summary>
    /// Gets the main page title text.
    /// </summary>
    public async Task<string?> GetPageTitleAsync()
    {
        return await PageTitle.TextContentAsync();
    }

    /// <summary>
    /// Checks if the hero section is visible.
    /// </summary>
    public async Task<bool> IsHeroSectionVisibleAsync()
    {
        return await HeroSection.IsVisibleAsync();
    }

    /// <summary>
    /// Clicks the Data Management feature card and navigates to /data.
    /// </summary>
    public async Task ClickDataManagementCardAsync()
    {
        await DataManagementCard.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Clicks the Backtest feature card and navigates to /backtest.
    /// </summary>
    public async Task ClickBacktestCardAsync()
    {
        await BacktestCard.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Clicks the Live Analysis feature card and navigates to /analysis.
    /// </summary>
    public async Task ClickLiveAnalysisCardAsync()
    {
        await LiveAnalysisCard.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Clicks the Comparison feature card and navigates to /comparison.
    /// </summary>
    public async Task ClickComparisonCardAsync()
    {
        await ComparisonCard.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Verifies that all 4 feature cards are visible.
    /// </summary>
    public async Task<bool> AreAllFeatureCardsVisibleAsync()
    {
        return await DataManagementCard.IsVisibleAsync()
            && await BacktestCard.IsVisibleAsync()
            && await LiveAnalysisCard.IsVisibleAsync()
            && await ComparisonCard.IsVisibleAsync();
    }

    /// <summary>
    /// Gets the number of strategies displayed in the stats.
    /// </summary>
    public async Task<string?> GetStrategiesCountAsync()
    {
        var statElement = StrategiesStat.Locator("p.text-3xl");
        return await statElement.TextContentAsync();
    }

    /// <summary>
    /// Verifies that the Technology Stack section is visible.
    /// </summary>
    public async Task<bool> IsTechnologyStackVisibleAsync()
    {
        return await TechnologyStackSection.IsVisibleAsync();
    }

    /// <summary>
    /// Verifies that the Architecture section is visible.
    /// </summary>
    public async Task<bool> IsArchitectureSectionVisibleAsync()
    {
        return await ArchitectureSection.IsVisibleAsync();
    }

    /// <summary>
    /// Gets all technology stack items as text.
    /// </summary>
    public async Task<IReadOnlyList<string?>> GetTechnologyStackItemsAsync()
    {
        ILocator items = TechnologyStackSection.Locator(".text-sm.font-medium");
        int count = await items.CountAsync();
        List<string?> results = new List<string?>();

        for (int i = 0; i < count; i++)
        {
            string? text = await items.Nth(i).TextContentAsync();
            results.Add(text);
        }

        return results;
    }

    /// <summary>
    /// Checks if a specific technology is listed in the stack.
    /// </summary>
    public async Task<bool> HasTechnologyAsync(string technologyName)
    {
        IReadOnlyList<string?> technologies = await GetTechnologyStackItemsAsync();
        return technologies.Any(t => t?.Contains(technologyName, StringComparison.OrdinalIgnoreCase) == true);
    }
}
