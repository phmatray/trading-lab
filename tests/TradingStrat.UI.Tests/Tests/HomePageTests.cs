namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Home page (/).
/// Tests page content, navigation, and feature card interactions.
/// </summary>
public class HomePageTests : BaseTest
{
    public HomePageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task HomePage_WhenLoaded_ShouldDisplayTitle()
    {
        // Arrange
        HomePage homePage = new HomePage(Page!, BaseUrl);

        // Act
        await homePage.NavigateAsync();
        string? title = await homePage.GetPageTitleAsync();

        // Assert
        title.ShouldNotBeNull();
        title.ShouldContain("TradingStrat");
    }

    [Fact]
    public async Task HomePage_WhenLoaded_ShouldHaveCorrectPageTitle()
    {
        // Arrange
        var homePage = new HomePage(Page!, BaseUrl);

        // Act
        await homePage.NavigateAsync();
        string pageTitle = await Page!.TitleAsync();

        // Assert
        pageTitle.ShouldContain("Home");
        pageTitle.ShouldContain("TradingStrat");
    }

    [Fact]
    public async Task HomePage_WhenLoaded_ShouldDisplayHeroSection()
    {
        // Arrange
        var homePage = new HomePage(Page!, BaseUrl);

        // Act
        await homePage.NavigateAsync();
        bool isHeroVisible = await homePage.IsHeroSectionVisibleAsync();

        // Assert
        isHeroVisible.ShouldBeTrue();
    }

    [Fact]
    public async Task HomePage_WhenLoaded_ShouldDisplayAllFeatureCards()
    {
        // Arrange
        var homePage = new HomePage(Page!, BaseUrl);

        // Act
        await homePage.NavigateAsync();
        bool allCardsVisible = await homePage.AreAllFeatureCardsVisibleAsync();

        // Assert
        allCardsVisible.ShouldBeTrue("All 4 feature cards should be visible");
    }

    [Fact]
    public async Task HomePage_StrategiesCount_ShouldBe5()
    {
        // Arrange
        var homePage = new HomePage(Page!, BaseUrl);

        // Act
        await homePage.NavigateAsync();
        string? strategiesCount = await homePage.GetStrategiesCountAsync();

        // Assert
        strategiesCount.ShouldBe("5", "There should be 5 strategies available");
    }

    [Theory]
    [InlineData("Data Management", "/data")]
    [InlineData("Backtest", "/backtest")]
    [InlineData("Analysis", "/analysis")]
    [InlineData("Comparison", "/comparison")]
    public async Task HomePage_NavigationToPage_ShouldWork(string pageName, string expectedPath)
    {
        // Arrange
        var homePage = new HomePage(Page!, BaseUrl);
        await homePage.NavigateAsync();

        // Act
        switch (pageName)
        {
            case "Data Management":
                await homePage.ClickDataManagementCardAsync();
                break;
            case "Backtest":
                await homePage.ClickBacktestCardAsync();
                break;
            case "Analysis":
                await homePage.ClickLiveAnalysisCardAsync();
                break;
            case "Comparison":
                await homePage.ClickComparisonCardAsync();
                break;
        }

        // Assert
        Page!.Url.ShouldContain(expectedPath);
    }

    [Fact]
    public async Task HomePage_TechnologyStack_ShouldBeVisible()
    {
        // Arrange
        var homePage = new HomePage(Page!, BaseUrl);

        // Act
        await homePage.NavigateAsync();
        bool isVisible = await homePage.IsTechnologyStackVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Technology Stack section should be visible");
    }

    [Fact]
    public async Task HomePage_ArchitectureSection_ShouldBeVisible()
    {
        // Arrange
        var homePage = new HomePage(Page!, BaseUrl);

        // Act
        await homePage.NavigateAsync();
        bool isVisible = await homePage.IsArchitectureSectionVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Architecture section should be visible");
    }

    [Theory]
    [InlineData(".NET 10.0")]
    [InlineData("Blazor Server")]
    [InlineData("ML.NET")]
    [InlineData("SQLite")]
    public async Task HomePage_TechnologyStack_ShouldIncludeTechnology(string technologyName)
    {
        // Arrange
        var homePage = new HomePage(Page!, BaseUrl);

        // Act
        await homePage.NavigateAsync();
        bool hasTechnology = await homePage.HasTechnologyAsync(technologyName);

        // Assert
        hasTechnology.ShouldBeTrue($"Technology stack should include {technologyName}");
    }

    [Fact]
    public async Task HomePage_WhenLoaded_ShouldNotHaveConsoleErrors()
    {
        // Arrange
        List<string> consoleErrors = new List<string>();
        Page!.Console += (_, msg) =>
        {
            if (msg.Type == "error" && !IsAcceptableError(msg.Text))
            {
                consoleErrors.Add(msg.Text);
            }
        };

        var homePage = new HomePage(Page!, BaseUrl);

        // Act
        await homePage.NavigateAsync();
        await Page!.WaitForBlazorAsync(); // Use Blazor-specific wait
        await Task.Delay(1000); // Wait for any delayed console errors

        // Assert
        consoleErrors.ShouldBeEmpty($"There should be no console errors on the home page. Errors: {string.Join(", ", consoleErrors)}");
    }

    private static bool IsAcceptableError(string message)
    {
        // Filter out known acceptable errors
        return message.Contains("favicon.ico") ||
               message.Contains(".map") ||
               message.Contains("sourcemap") ||
               message.Contains("404") ||
               message.Contains("Failed to load resource");
    }

    [Fact]
    public async Task HomePage_BlazorConnection_ShouldBeEstablished()
    {
        // Arrange & Act
        await NavigateToAsync("/");

        // Check that Blazor is initialized
        bool blazorInitialized = await Page!.EvaluateAsync<bool>("() => window.Blazor !== undefined");

        // Assert
        blazorInitialized.ShouldBeTrue("Blazor SignalR connection should be established");
    }

    [Fact]
    public async Task DarkTheme_ShouldBeApplied()
    {
        // Arrange
        var homePage = new HomePage(Page!, BaseUrl);

        // Act
        await homePage.NavigateAsync();

        // Check if dark class is applied to the root element
        ILocator rootElement = Page!.Locator("body > div").First;
        string? className = await rootElement.GetAttributeAsync("class");

        // Assert
        className.ShouldNotBeNullOrEmpty("Root element should have CSS classes");
        (className?.Contains("dark") ?? false).ShouldBeTrue("Root element should have 'dark' class for dark theme");
    }

    [Fact]
    public async Task DarkTheme_CardsShouldHaveDarkBackground()
    {
        // Arrange
        var homePage = new HomePage(Page!, BaseUrl);

        // Act
        await homePage.NavigateAsync();

        // Get computed background color of a card element
        ILocator card = Page!.Locator(".card").First;
        string bgColor = await card.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundColor");

        // Assert - Dark background should not be white (rgb(255, 255, 255))
        bgColor.ShouldNotBe("rgb(255, 255, 255)", "Cards should have dark background, not white");
        bgColor.ShouldNotBeNullOrEmpty("Cards should have a background color");
    }

    [Fact]
    public async Task DarkTheme_TextShouldBeReadable()
    {
        // Arrange
        var homePage = new HomePage(Page!, BaseUrl);

        // Act
        await homePage.NavigateAsync();

        // Get computed color of primary text (h1)
        ILocator heading = Page!.Locator("main h1").First;
        string textColor = await heading.EvaluateAsync<string>("el => window.getComputedStyle(el).color");

        // Assert - Text should be light colored (not dark/black: rgb(0, 0, 0))
        textColor.ShouldNotBe("rgb(0, 0, 0)", "Text should be light colored for dark theme, not black");
        textColor.ShouldNotBeNullOrEmpty("Heading should have a text color");
    }
}
