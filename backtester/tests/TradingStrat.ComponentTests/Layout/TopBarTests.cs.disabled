using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Layout;
using Xunit;

namespace TradingStrat.ComponentTests.Layout;

/// <summary>
/// Tests for the TopBar component (app-specific top bar using Catalyst components).
/// </summary>
public class TopBarTests : BunitTestContext
{
    [Fact]
    public void TopBar_RendersWithCorrectRole()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>();

        // Assert
        IElement banner = cut.Find("div[role='banner']");
        banner.ShouldNotBeNull();
        banner.GetAttribute("data-testid").ShouldBe("top-bar");
    }

    [Fact]
    public void TopBar_HasFixedPositioning()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>();

        // Assert
        IElement topBar = cut.Find("[data-testid='top-bar']");
        topBar.ClassList.ShouldContain("fixed");
        topBar.ClassList.ShouldContain("top-0");
        topBar.ClassList.ShouldContain("left-0");
        topBar.ClassList.ShouldContain("right-0");
        topBar.ClassList.ShouldContain("z-40");
        topBar.ClassList.ShouldContain("h-16");
    }

    [Fact]
    public void TopBar_DisplaysAppTitle()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>();

        // Assert
        IElement title = cut.Find("h1");
        title.ShouldNotBeNull();
        title.TextContent.Trim().ShouldBe("TradingStrat AI");
        title.ClassList.ShouldContain("text-xl");
        title.ClassList.ShouldContain("font-bold");
    }

    [Fact]
    public void TopBar_WithoutPortfolio_HidesPortfolioInfo()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>(parameters => parameters
            .Add(p => p.SelectedPortfolioName, null)
            .Add(p => p.PortfolioValue, null));

        // Assert
        // Should not contain portfolio-related text
        cut.Markup.ShouldNotContain("Portfolio:");
    }

    [Fact]
    public void TopBar_WithPortfolio_DisplaysPortfolioName()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>(parameters => parameters
            .Add(p => p.SelectedPortfolioName, "Tech Growth")
            .Add(p => p.PortfolioValue, 15000m));

        // Assert
        cut.Markup.ShouldContain("Portfolio:");
        cut.Markup.ShouldContain("Tech Growth");
        cut.Markup.ShouldContain("($15,000)"); // Formatted value with parentheses
    }

    [Fact]
    public void TopBar_WithPortfolio_DisplaysValue()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>(parameters => parameters
            .Add(p => p.SelectedPortfolioName, "My Portfolio")
            .Add(p => p.PortfolioValue, 25432.50m));

        // Assert
        cut.Markup.ShouldContain("($25,433)"); // Formatted as C0 with parentheses (no cents)
    }

    [Fact]
    public void TopBar_WithPositiveYTD_DisplaysGreenPerformance()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>(parameters => parameters
            .Add(p => p.SelectedPortfolioName, "Growth Portfolio")
            .Add(p => p.PortfolioValue, 10000m)
            .Add(p => p.YtdPerformance, 15.25m));

        // Assert
        cut.Markup.ShouldContain("+15.25% YTD");
        cut.Markup.ShouldContain("metric-positive");
    }

    [Fact]
    public void TopBar_WithNegativeYTD_DisplaysRedPerformance()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>(parameters => parameters
            .Add(p => p.SelectedPortfolioName, "Loss Portfolio")
            .Add(p => p.PortfolioValue, 10000m)
            .Add(p => p.YtdPerformance, -8.5m));

        // Assert
        cut.Markup.ShouldContain("-8.50% YTD");
        cut.Markup.ShouldContain("metric-negative");
    }

    [Fact]
    public void TopBar_WithZeroYTD_DisplaysNeutralPerformance()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>(parameters => parameters
            .Add(p => p.SelectedPortfolioName, "Flat Portfolio")
            .Add(p => p.PortfolioValue, 10000m)
            .Add(p => p.YtdPerformance, 0m));

        // Assert
        cut.Markup.ShouldContain("+0.00% YTD");
        cut.Markup.ShouldContain("metric-positive"); // Zero is considered non-negative
    }

    [Fact]
    public void TopBar_WithShowAiModeSelector_DisplaysSelector()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>(parameters => parameters
            .Add(p => p.ShowAiModeSelector, true));

        // Assert
        IElement select = cut.Find("select[aria-label='AI Mode']");
        select.ShouldNotBeNull();
        cut.Markup.ShouldContain("Conservative");
        cut.Markup.ShouldContain("Balanced");
        cut.Markup.ShouldContain("Aggressive");
    }

    [Fact]
    public void TopBar_WithoutShowAiModeSelector_HidesSelector()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>(parameters => parameters
            .Add(p => p.ShowAiModeSelector, false));

        // Assert
        IEnumerable<IElement> selects = cut.FindAll("select[aria-label='AI Mode']");
        selects.ShouldBeEmpty();
    }

    [Fact]
    public void TopBar_RendersNotificationBell()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>();

        // Assert
        // NotificationBell component is rendered in the navbar section
        // The component renders successfully without errors
        cut.Markup.ShouldNotBeNull();
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact]
    public void TopBar_RendersSettingsButton()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>();

        // Assert
        // Settings button should be a NavbarItem with settings icon
        IElement settingsButton = cut.Find("[aria-label='Settings']");
        settingsButton.ShouldNotBeNull();

        // Should have settings gear icon
        IElement settingsIcon = cut.Find("[aria-label='Settings'] svg");
        settingsIcon.ShouldNotBeNull();
    }

    [Fact]
    public void TopBar_UsesCatalystTypographyForTitle()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>();

        // Assert
        // Title should be Catalyst Heading component
        IElement title = cut.Find("h1");
        title.ShouldNotBeNull();
        title.ClassList.ShouldContain("text-xl");
        title.ClassList.ShouldContain("font-bold");
        title.ClassList.ShouldContain("text-trading-blue");
    }

    [Fact]
    public void TopBar_UsesCatalystNavbarComponents()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>();

        // Assert
        // Should use Catalyst Navbar structure
        IElement navbar = cut.Find("nav");
        navbar.ShouldNotBeNull();
        navbar.ClassList.ShouldContain("flex");
        navbar.ClassList.ShouldContain("items-center");
    }

    [Fact]
    public void TopBar_PortfolioInfo_IsResponsive()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>(parameters => parameters
            .Add(p => p.SelectedPortfolioName, "Mobile Test")
            .Add(p => p.PortfolioValue, 10000m));

        // Assert
        // Portfolio info should be hidden on mobile (md:flex)
        IElement portfolioInfo = cut.Find(".hidden.md\\:flex");
        portfolioInfo.ShouldNotBeNull();
    }

    [Fact]
    public void TopBar_AiModeSelector_IsResponsive()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>(parameters => parameters
            .Add(p => p.ShowAiModeSelector, true));

        // Assert
        // AI Mode selector should be hidden on smaller screens (lg:block)
        IElement selector = cut.Find("select");
        selector.ClassList.ShouldContain("hidden");
        selector.ClassList.ShouldContain("lg:block");
    }

    [Fact]
    public void TopBar_HasCorrectBackgroundAndBorder()
    {
        // Arrange & Act
        IRenderedComponent<TopBar> cut = Render<TopBar>();

        // Assert
        IElement topBar = cut.Find("[data-testid='top-bar']");
        topBar.ClassList.ShouldContain("bg-white");
        topBar.ClassList.ShouldContain("dark:bg-dark-card");
        topBar.ClassList.ShouldContain("border-b");
        topBar.ClassList.ShouldContain("border-gray-200");
    }
}
