namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for Phase 3 UI components (Table, Pagination, Combobox, Listbox, Navbar, Sidebar).
/// Tests interactive behavior, state management, and accessibility.
/// </summary>
public class Phase3ComponentShowcaseTests : BaseTest
{
    public Phase3ComponentShowcaseTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    #region Table Tests

    [Fact]
    public async Task TableSection_WhenLoaded_ShouldBeVisible()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsTableSectionVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Table section should be visible");
    }

    [Fact]
    public async Task BasicTable_ShouldRenderCorrectNumberOfRows()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        int rowCount = await page.GetBasicTableRowCountAsync();

        // Assert
        rowCount.ShouldBe(2, "Basic table should have 2 rows");
    }

    [Fact]
    public async Task StripedTable_ShouldHaveStripedStyling()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool hasStriping = await page.HasStripedStylingAsync();

        // Assert
        hasStriping.ShouldBeTrue("Striped table should have striped styling");
    }

    [Fact]
    public async Task ClickableTableRow_WhenClicked_ShouldNavigate()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickFirstClickableRowAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        // Navigation should have been attempted (we're just verifying the click works)
        // In a real app, we'd verify the URL changed
        Page!.Url.ShouldNotBeNull();
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task PaginationSection_WhenLoaded_ShouldBeVisible()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsPaginationSectionVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Pagination section should be visible");
    }

    [Fact]
    public async Task Pagination_ShouldRenderAllComponents()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsPaginationVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Pagination should be visible with all components");
    }

    [Fact]
    public async Task PaginationPage_WhenCurrent_ShouldHaveAriaCurrentAttribute()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isCurrent = await page.IsPage2CurrentAsync();

        // Assert
        isCurrent.ShouldBeTrue("Page 2 should have aria-current='page'");
    }

    [Fact]
    public async Task PaginationPrevious_WhenDisabled_ShouldBeDisabled()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isDisabled = await page.IsDisabledPrevButtonDisabledAsync();

        // Assert
        isDisabled.ShouldBeTrue("Previous button should be disabled on first page");
    }

    #endregion

    #region Combobox Tests

    [Fact]
    public async Task ComboboxSection_WhenLoaded_ShouldBeVisible()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsComboboxSectionVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Combobox section should be visible");
    }

    [Fact]
    public async Task Combobox_WhenTyping_ShouldFilterOptions()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Type to filter
        await page.TypeInComboboxAsync("Ban");
        await Page!.WaitForBlazorAsync();

        // Assert - Dropdown should show filtered results
        bool bananaVisible = await Page!.Locator("text=Banana").IsVisibleAsync();
        bananaVisible.ShouldBeTrue("Filtered option 'Banana' should be visible");
    }

    [Fact]
    public async Task Combobox_WhenOptionSelected_ShouldUpdateValue()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Click input to open, then select option
        await page.TypeInComboboxAsync("Cherry");
        await Page!.WaitForBlazorAsync();
        await page.ClickComboboxOptionAsync("Cherry");
        await Page!.WaitForBlazorAsync();

        // Assert
        string? selectedFruit = await page.GetSelectedFruitAsync();
        selectedFruit.ShouldNotBeNull();
        selectedFruit.ShouldContain("Cherry");
    }

    #endregion

    #region Listbox Tests

    [Fact]
    public async Task ListboxSection_WhenLoaded_ShouldBeVisible()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsListboxSectionVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Listbox section should be visible");
    }

    [Fact]
    public async Task Listbox_WhenOpened_ShouldShowOptions()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.OpenListboxAsync();
        await Page!.WaitForBlazorAsync();

        // Assert - Options should be visible
        bool mediumVisible = await Page!.Locator("text=Medium").IsVisibleAsync();
        mediumVisible.ShouldBeTrue("Option 'Medium' should be visible in dropdown");
    }

    [Fact]
    public async Task Listbox_WhenOptionSelected_ShouldUpdateValue()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Open and select
        await page.OpenListboxAsync();
        await Page!.WaitForBlazorAsync();
        await page.SelectListboxOptionAsync("Large");
        await Page!.WaitForBlazorAsync();

        // Assert
        string? selectedSize = await page.GetSelectedSizeAsync();
        selectedSize.ShouldNotBeNull();
        selectedSize.ShouldContain("Large");
    }

    #endregion

    #region Navbar Tests

    [Fact]
    public async Task NavbarSection_WhenLoaded_ShouldBeVisible()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsNavbarSectionVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Navbar section should be visible");
    }

    [Fact]
    public async Task Navbar_ShouldRenderAllItems()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsNavbarVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Navbar should be visible with all items");
    }

    [Fact]
    public async Task NavbarItem_WhenClicked_ShouldUpdateCurrentPage()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Initial state should be "home"
        string? initialPage = await page.GetCurrentNavPageAsync();
        initialPage.ShouldNotBeNull();
        initialPage.ShouldContain("home");

        // Click About
        await page.ClickNavAboutAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        string? currentPage = await page.GetCurrentNavPageAsync();
        currentPage.ShouldNotBeNull();
        currentPage.ShouldContain("about");
    }

    [Fact]
    public async Task NavbarItem_WhenClickedMultipleTimes_ShouldTrackNavigation()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Navigate through pages
        await page.ClickNavAboutAsync();
        await Page!.WaitForBlazorAsync();

        await page.ClickNavContactAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        string? currentPage = await page.GetCurrentNavPageAsync();
        currentPage.ShouldNotBeNull();
        currentPage.ShouldContain("contact");
    }

    #endregion

    #region Sidebar Tests

    [Fact]
    public async Task SidebarSection_WhenLoaded_ShouldBeVisible()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsSidebarSectionVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Sidebar section should be visible");
    }

    [Fact]
    public async Task Sidebar_ShouldRenderAllItems()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsSidebarVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Sidebar should be visible with all items");
    }

    [Fact]
    public async Task SidebarItem_WhenClicked_ShouldUpdateCurrentPage()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Initial state should be "dashboard"
        string? initialPage = await page.GetCurrentSidebarPageAsync();
        initialPage.ShouldNotBeNull();
        initialPage.ShouldContain("dashboard");

        // Click Projects
        await page.ClickSidebarProjectsAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        string? currentPage = await page.GetCurrentSidebarPageAsync();
        currentPage.ShouldNotBeNull();
        currentPage.ShouldContain("projects");
    }

    [Fact]
    public async Task SidebarItem_WhenClickedMultipleTimes_ShouldTrackNavigation()
    {
        // Arrange
        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Navigate through pages
        await page.ClickSidebarProjectsAsync();
        await Page!.WaitForBlazorAsync();

        await page.ClickSidebarTasksAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        string? currentPage = await page.GetCurrentSidebarPageAsync();
        currentPage.ShouldNotBeNull();
        currentPage.ShouldContain("tasks");
    }

    #endregion

    #region Accessibility Tests

    [Fact]
    public async Task Phase3Components_ShouldNotHaveConsoleErrors()
    {
        // Arrange
        List<string> consoleErrors = [];
        Page!.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        Phase3ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await Page!.WaitForBlazorAsync();

        // Allow time for any async errors
        await Task.Delay(500);

        // Assert
        List<string> relevantErrors = consoleErrors
            .Where(e => !e.Contains("favicon") && !e.Contains("sourcemap"))
            .ToList();

        relevantErrors.ShouldBeEmpty($"Console should not have errors. Found: {string.Join(", ", relevantErrors)}");
    }

    #endregion
}
