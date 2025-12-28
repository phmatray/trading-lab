namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Phase 3 Component Showcase page.
/// Encapsulates all interactions with Phase 3 UI components (Table, Pagination, Combobox, Listbox, Navbar, Sidebar).
/// </summary>
public class Phase3ComponentShowcasePage(IPage page, string baseUrl) : BasePage(page, baseUrl)
{
    protected override string PagePath => "/phase3-component-showcase";

    // Section visibility
    private ILocator TableSection => Page.Locator("[data-testid='table-section']");
    private ILocator PaginationSection => Page.Locator("[data-testid='pagination-section']");
    private ILocator ComboboxSection => Page.Locator("[data-testid='combobox-section']");
    private ILocator ListboxSection => Page.Locator("[data-testid='listbox-section']");
    private ILocator NavbarSection => Page.Locator("[data-testid='navbar-section']");
    private ILocator SidebarSection => Page.Locator("[data-testid='sidebar-section']");

    // Table elements
    private ILocator BasicTable => Page.Locator("[data-testid='basic-table']");
    private ILocator StripedTable => Page.Locator("[data-testid='striped-table']");
    private ILocator ClickableRow1 => Page.Locator("[data-testid='clickable-row-1']");

    // Pagination elements
    private ILocator FullPagination => Page.Locator("[data-testid='full-pagination']");
    private ILocator PrevButton => Page.Locator("[data-testid='prev-button']");
    private ILocator NextButton => Page.Locator("[data-testid='next-button']");
    private ILocator Page1Button => Page.Locator("[data-testid='page-1']");
    private ILocator Page2Button => Page.Locator("[data-testid='page-2']");
    private ILocator DisabledPrevButton => Page.Locator("[data-testid='disabled-prev']");

    // Combobox elements
    private ILocator FruitCombobox => Page.Locator("[data-testid='fruit-combobox']");
    private ILocator FruitComboboxInput => FruitCombobox.Locator("input");
    private ILocator SelectedFruit => Page.Locator("[data-testid='selected-fruit']");

    // Listbox elements
    private ILocator SizeListbox => Page.Locator("[data-testid='size-listbox']");
    private ILocator SizeListboxButton => SizeListbox.Locator("button");
    private ILocator SelectedSize => Page.Locator("[data-testid='selected-size']");

    // Navbar elements
    private ILocator SampleNavbar => Page.Locator("[data-testid='sample-navbar']");
    private ILocator NavHome => Page.Locator("[data-testid='nav-home']");
    private ILocator NavAbout => Page.Locator("[data-testid='nav-about']");
    private ILocator NavContact => Page.Locator("[data-testid='nav-contact']");
    private ILocator CurrentNavPage => Page.Locator("[data-testid='current-nav-page']");

    // Sidebar elements
    private ILocator SampleSidebar => Page.Locator("[data-testid='sample-sidebar']");
    private ILocator SidebarDashboard => Page.Locator("[data-testid='sidebar-dashboard']");
    private ILocator SidebarProjects => Page.Locator("[data-testid='sidebar-projects']");
    private ILocator SidebarTasks => Page.Locator("[data-testid='sidebar-tasks']");
    private ILocator CurrentSidebarPage => Page.Locator("[data-testid='current-sidebar-page']");

    // Section visibility checks
    public Task<bool> IsTableSectionVisibleAsync() => TableSection.IsVisibleAsync();
    public Task<bool> IsPaginationSectionVisibleAsync() => PaginationSection.IsVisibleAsync();
    public Task<bool> IsComboboxSectionVisibleAsync() => ComboboxSection.IsVisibleAsync();
    public Task<bool> IsListboxSectionVisibleAsync() => ListboxSection.IsVisibleAsync();
    public Task<bool> IsNavbarSectionVisibleAsync() => NavbarSection.IsVisibleAsync();
    public Task<bool> IsSidebarSectionVisibleAsync() => SidebarSection.IsVisibleAsync();

    // Table interactions
    public Task<bool> IsBasicTableVisibleAsync() => BasicTable.IsVisibleAsync();
    public Task<int> GetBasicTableRowCountAsync() => BasicTable.Locator("tbody tr").CountAsync();
    public Task<bool> IsStripedTableVisibleAsync() => StripedTable.IsVisibleAsync();
    public async Task<bool> HasStripedStylingAsync()
    {
        ILocator firstRow = StripedTable.Locator("tbody tr").First;
        string? className = await firstRow.GetAttributeAsync("class");
        return className?.Contains("even:bg") == true;
    }
    public Task ClickFirstClickableRowAsync() => ClickableRow1.ClickAsync();

    // Pagination interactions
    public Task<bool> IsPaginationVisibleAsync() => FullPagination.IsVisibleAsync();
    public Task ClickPrevButtonAsync() => PrevButton.ClickAsync();
    public Task ClickNextButtonAsync() => NextButton.ClickAsync();
    public Task ClickPage1Async() => Page1Button.ClickAsync();
    public async Task<bool> IsPage2CurrentAsync()
    {
        string? ariaCurrent = await Page2Button.GetAttributeAsync("aria-current");
        return ariaCurrent == "page";
    }
    public async Task<bool> IsDisabledPrevButtonDisabledAsync()
    {
        return await DisabledPrevButton.Locator("button").IsDisabledAsync();
    }

    // Combobox interactions
    public Task<bool> IsComboboxVisibleAsync() => FruitCombobox.IsVisibleAsync();
    public Task TypeInComboboxAsync(string text) => FruitComboboxInput.FillAsync(text);
    public Task ClickComboboxOptionAsync(string optionText) =>
        Page.Locator($"text={optionText}").First.ClickAsync();
    public async Task<string?> GetSelectedFruitAsync()
    {
        bool isVisible = await SelectedFruit.IsVisibleAsync();
        return isVisible ? await SelectedFruit.TextContentAsync() : null;
    }

    // Listbox interactions
    public Task<bool> IsListboxVisibleAsync() => SizeListbox.IsVisibleAsync();
    public Task OpenListboxAsync() => SizeListboxButton.ClickAsync();
    public Task SelectListboxOptionAsync(string optionText) =>
        Page.Locator($"text={optionText}").First.ClickAsync();
    public async Task<string?> GetSelectedSizeAsync()
    {
        bool isVisible = await SelectedSize.IsVisibleAsync();
        return isVisible ? await SelectedSize.TextContentAsync() : null;
    }

    // Navbar interactions
    public Task<bool> IsNavbarVisibleAsync() => SampleNavbar.IsVisibleAsync();
    public Task ClickNavHomeAsync() => NavHome.ClickAsync();
    public Task ClickNavAboutAsync() => NavAbout.ClickAsync();
    public Task ClickNavContactAsync() => NavContact.ClickAsync();
    public async Task<string?> GetCurrentNavPageAsync()
    {
        bool isVisible = await CurrentNavPage.IsVisibleAsync();
        return isVisible ? await CurrentNavPage.TextContentAsync() : null;
    }

    // Sidebar interactions
    public Task<bool> IsSidebarVisibleAsync() => SampleSidebar.IsVisibleAsync();
    public Task ClickSidebarDashboardAsync() => SidebarDashboard.ClickAsync();
    public Task ClickSidebarProjectsAsync() => SidebarProjects.ClickAsync();
    public Task ClickSidebarTasksAsync() => SidebarTasks.ClickAsync();
    public async Task<string?> GetCurrentSidebarPageAsync()
    {
        bool isVisible = await CurrentSidebarPage.IsVisibleAsync();
        return isVisible ? await CurrentSidebarPage.TextContentAsync() : null;
    }
}
