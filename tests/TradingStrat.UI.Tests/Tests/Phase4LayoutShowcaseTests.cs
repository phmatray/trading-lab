namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for Phase 4 Layout components (SidebarLayout, StackedLayout, AuthLayout).
/// Tests layout switching, component visibility, and responsive behavior.
/// </summary>
public class Phase4LayoutShowcaseTests : BaseTest
{
    public Phase4LayoutShowcaseTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    #region Page Load Tests

    [Fact]
    public async Task Page_WhenLoaded_ShouldDisplayTitle()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();

        // Assert
        string? title = await Page!.TitleAsync();
        title.ShouldContain("Phase 4 Layout Showcase");
    }

    [Fact]
    public async Task Page_WhenLoaded_ShouldShowLayoutSwitcher()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsLayoutSwitcherVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Layout switcher should be visible");
    }

    [Fact]
    public async Task Page_WhenLoaded_ShouldDefaultToSidebarLayout()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isSidebarVisible = await page.IsSidebarLayoutVisibleAsync();
        string? currentLayout = await page.GetCurrentLayoutNameAsync();

        // Assert
        isSidebarVisible.ShouldBeTrue("Sidebar layout should be visible by default");
        currentLayout.ShouldBe("sidebar");
    }

    #endregion

    #region Sidebar Layout Tests

    [Fact]
    public async Task SidebarLayout_WhenDisplayed_ShouldShowContent()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool contentVisible = await page.IsSidebarLayoutContentVisibleAsync();

        // Assert
        contentVisible.ShouldBeTrue("Sidebar layout content should be visible");
    }

    [Fact]
    public async Task SidebarLayout_Content_ShouldContainExpectedText()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        string? contentText = await page.GetSidebarLayoutContentTextAsync();

        // Assert
        contentText.ShouldNotBeNull();
        contentText.ShouldContain("Sidebar Layout Content");
        contentText.ShouldContain("fixed sidebar on desktop");
    }

    #endregion

    #region Stacked Layout Tests

    [Fact]
    public async Task StackedLayout_WhenSwitched_ShouldBeVisible()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await page.SwitchToStackedLayoutAsync();
        bool isVisible = await page.IsStackedLayoutVisibleAsync();
        string? currentLayout = await page.GetCurrentLayoutNameAsync();

        // Assert
        isVisible.ShouldBeTrue("Stacked layout should be visible after switching");
        currentLayout.ShouldBe("stacked");
    }

    [Fact]
    public async Task StackedLayout_WhenDisplayed_ShouldShowContent()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await page.SwitchToStackedLayoutAsync();
        bool contentVisible = await page.IsStackedLayoutContentVisibleAsync();

        // Assert
        contentVisible.ShouldBeTrue("Stacked layout content should be visible");
    }

    [Fact]
    public async Task StackedLayout_Content_ShouldContainExpectedText()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await page.SwitchToStackedLayoutAsync();
        string? contentText = await page.GetStackedLayoutContentTextAsync();

        // Assert
        contentText.ShouldNotBeNull();
        contentText.ShouldContain("Stacked Layout Content");
        contentText.ShouldContain("navbar on top");
    }

    #endregion

    #region Auth Layout Tests

    [Fact]
    public async Task AuthLayout_WhenSwitched_ShouldBeVisible()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await page.SwitchToAuthLayoutAsync();
        bool isVisible = await page.IsAuthLayoutVisibleAsync();
        string? currentLayout = await page.GetCurrentLayoutNameAsync();

        // Assert
        isVisible.ShouldBeTrue("Auth layout should be visible after switching");
        currentLayout.ShouldBe("auth");
    }

    [Fact]
    public async Task AuthLayout_WhenDisplayed_ShouldShowContent()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await page.SwitchToAuthLayoutAsync();
        bool contentVisible = await page.IsAuthLayoutContentVisibleAsync();

        // Assert
        contentVisible.ShouldBeTrue("Auth layout content should be visible");
    }

    [Fact]
    public async Task AuthLayout_Content_ShouldContainSignInForm()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await page.SwitchToAuthLayoutAsync();
        string? contentText = await page.GetAuthLayoutContentTextAsync();

        // Assert
        contentText.ShouldNotBeNull();
        contentText.ShouldContain("Sign In");
        contentText.ShouldContain("Email");
        contentText.ShouldContain("Password");
    }

    #endregion

    #region Layout Switching Tests

    [Fact]
    public async Task LayoutSwitching_ShouldHideOtherLayouts()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool sidebarVisible = await page.IsSidebarLayoutVisibleAsync();

        await page.SwitchToStackedLayoutAsync();
        bool stackedVisible = await page.IsStackedLayoutVisibleAsync();
        bool sidebarStillVisible = await page.IsSidebarLayoutVisibleAsync();

        // Assert
        sidebarVisible.ShouldBeTrue("Sidebar should be visible initially");
        stackedVisible.ShouldBeTrue("Stacked should be visible after switch");
        sidebarStillVisible.ShouldBeFalse("Sidebar should be hidden after switching");
    }

    [Fact]
    public async Task LayoutSwitching_FromStackedToAuth_ShouldWork()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await page.SwitchToStackedLayoutAsync();
        string? stackedLayout = await page.GetCurrentLayoutNameAsync();

        await page.SwitchToAuthLayoutAsync();
        string? authLayout = await page.GetCurrentLayoutNameAsync();

        // Assert
        stackedLayout.ShouldBe("stacked");
        authLayout.ShouldBe("auth");
    }

    [Fact]
    public async Task LayoutSwitching_BackToSidebar_ShouldWork()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await page.SwitchToAuthLayoutAsync();
        await page.SwitchToSidebarLayoutAsync();

        string? currentLayout = await page.GetCurrentLayoutNameAsync();
        bool isSidebarVisible = await page.IsSidebarLayoutVisibleAsync();

        // Assert
        currentLayout.ShouldBe("sidebar");
        isSidebarVisible.ShouldBeTrue("Should be able to switch back to sidebar layout");
    }

    #endregion

    #region Console Error Tests

    [Fact]
    public async Task Phase4Layouts_ShouldNotHaveConsoleErrors()
    {
        // Arrange
        Phase4LayoutShowcasePage page = new(Page!, BaseUrl);
        List<string> consoleErrors = new();

        Page!.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        // Act
        await page.NavigateAsync();
        await page.SwitchToStackedLayoutAsync();
        await page.SwitchToAuthLayoutAsync();
        await page.SwitchToSidebarLayoutAsync();

        // Wait a bit for any async errors
        await Page!.WaitForTimeoutAsync(1000);

        // Assert - filter out acceptable errors
        List<string> unacceptableErrors = consoleErrors
            .Where(e => !e.Contains("favicon") && !e.Contains("sourcemap"))
            .ToList();

        unacceptableErrors.ShouldBeEmpty($"Should have no console errors, but found: {string.Join(", ", unacceptableErrors)}");
    }

    #endregion
}
