using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Layout;
using Xunit;

namespace TradingStrat.ComponentTests.Layout;

/// <summary>
/// Tests for the LeftSidebar component (app-specific sidebar using Catalyst components).
/// </summary>
public class LeftSidebarTests : BunitTestContext
{
    [Fact]
    public void LeftSidebar_RendersAsideElement()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>();

        // Assert
        IElement aside = cut.Find("aside");
        aside.ShouldNotBeNull();
        aside.GetAttribute("role").ShouldBe("navigation");
        aside.GetAttribute("aria-label").ShouldBe("Main navigation");
        aside.GetAttribute("data-testid").ShouldBe("left-sidebar");
    }

    [Fact]
    public void LeftSidebar_WhenExpanded_HasCorrectWidth()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>(parameters => parameters
            .Add(p => p.IsCollapsed, false));

        // Assert
        IElement aside = cut.Find("aside");
        aside.ClassList.ShouldContain("w-64");
        aside.ClassList.ShouldNotContain("w-16");
    }

    [Fact]
    public void LeftSidebar_WhenCollapsed_HasCorrectWidth()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>(parameters => parameters
            .Add(p => p.IsCollapsed, true));

        // Assert
        IElement aside = cut.Find("aside");
        aside.ClassList.ShouldContain("w-16");
        aside.ClassList.ShouldNotContain("w-64");
    }

    [Fact]
    public void LeftSidebar_RendersAllNavigationGroups()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>();

        // Assert - Check for all expected navigation items
        cut.Markup.ShouldContain("Dashboard");
        cut.Markup.ShouldContain("Strategy Workspace");
        cut.Markup.ShouldContain("Strategy Library");
        cut.Markup.ShouldContain("Strategy Builder");
        cut.Markup.ShouldContain("Backtest");
        cut.Markup.ShouldContain("Fetch Data");
        cut.Markup.ShouldContain("Portfolios");
        cut.Markup.ShouldContain("Settings");
    }

    [Fact]
    public void LeftSidebar_WhenExpanded_ShowsGroupHeadings()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>(parameters => parameters
            .Add(p => p.IsCollapsed, false));

        // Assert - Check for group headings
        cut.Markup.ShouldContain("Workspace");
        cut.Markup.ShouldContain("Strategy Research");
        cut.Markup.ShouldContain("Data Management");
        cut.Markup.ShouldContain("Portfolio");
        cut.Markup.ShouldContain("System");
    }

    [Fact]
    public void LeftSidebar_WhenCollapsed_HidesGroupHeadings()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>(parameters => parameters
            .Add(p => p.IsCollapsed, true));

        // Assert - Group headings should be hidden via @if (!IsCollapsed)
        // The heading elements won't be rendered at all
        IEnumerable<IElement> headings = cut.FindAll("h3");
        headings.ShouldBeEmpty();
    }

    [Fact]
    public void LeftSidebar_RendersToggleButton()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>();

        // Assert
        IElement toggleButton = cut.Find("button[aria-label*='sidebar']");
        toggleButton.ShouldNotBeNull();
    }

    [Fact]
    public void LeftSidebar_ToggleButton_WhenExpanded_ShowsCollapseLabel()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>(parameters => parameters
            .Add(p => p.IsCollapsed, false));

        // Assert
        IElement toggleButton = cut.Find("button[aria-label='Collapse sidebar']");
        toggleButton.ShouldNotBeNull();
        toggleButton.TextContent.ShouldContain("Collapse");
    }

    [Fact]
    public void LeftSidebar_ToggleButton_WhenCollapsed_ShowsExpandLabel()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>(parameters => parameters
            .Add(p => p.IsCollapsed, true));

        // Assert
        IElement toggleButton = cut.Find("button[aria-label='Expand sidebar']");
        toggleButton.ShouldNotBeNull();
        // When collapsed, text is hidden
        toggleButton.TextContent.ShouldNotContain("Collapse");
    }

    [Fact]
    public void LeftSidebar_ToggleButton_Click_InvokesCallback()
    {
        // Arrange
        bool callbackInvoked = false;
        bool newState = false;

        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>(parameters => parameters
            .Add(p => p.IsCollapsed, false)
            .Add(p => p.IsCollapsedChanged, EventCallback.Factory.Create<bool>(
                this, value => { callbackInvoked = true; newState = value; })));

        // Act
        IElement toggleButton = cut.Find("button[aria-label='Collapse sidebar']");
        toggleButton.Click();

        // Assert
        callbackInvoked.ShouldBeTrue();
        newState.ShouldBeTrue(); // Should toggle to collapsed
    }

    [Fact]
    public void LeftSidebar_RendersNavigationLinksWithHrefs()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>();

        // Assert - Check for key navigation links
        IElement dashboardLink = cut.Find("a[href='/']");
        dashboardLink.ShouldNotBeNull();

        IElement dataLink = cut.Find("a[href='/data']");
        dataLink.ShouldNotBeNull();

        IElement portfoliosLink = cut.Find("a[href='/portfolios']");
        portfoliosLink.ShouldNotBeNull();

        IElement settingsLink = cut.Find("a[href='/settings']");
        settingsLink.ShouldNotBeNull();
    }

    [Fact]
    public void LeftSidebar_NavigationLinks_HaveIcons()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>();

        // Assert - All navigation items should have SVG icons
        IEnumerable<IElement> icons = cut.FindAll("svg[data-slot='icon']");
        icons.Count().ShouldBeGreaterThan(10); // At least all the nav items
    }

    [Fact]
    public void LeftSidebar_UsesCatalystSidebarComponents()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>();

        // Assert - Check for Catalyst component structure
        IElement sidebar = cut.Find("nav"); // Catalyst Sidebar renders as nav
        sidebar.ShouldNotBeNull();
        sidebar.ClassList.ShouldContain("flex");
        sidebar.ClassList.ShouldContain("flex-col");

        // Check for SidebarSection data-slot
        IEnumerable<IElement> sections = cut.FindAll("[data-slot='section']");
        sections.ShouldNotBeEmpty();
    }

    [Fact]
    public void LeftSidebar_AppliesFixedPositioning()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>();

        // Assert
        IElement aside = cut.Find("aside");
        aside.ClassList.ShouldContain("fixed");
        aside.ClassList.ShouldContain("top-16"); // Below TopBar
        aside.ClassList.ShouldContain("left-0");
        aside.ClassList.ShouldContain("bottom-0");
        aside.ClassList.ShouldContain("z-30");
    }

    [Fact]
    public void LeftSidebar_RendersSidebarDividers()
    {
        // Arrange & Act
        IRenderedComponent<LeftSidebar> cut = Render<LeftSidebar>();

        // Assert - There should be dividers between groups
        IEnumerable<IElement> dividers = cut.FindAll("hr");
        dividers.ShouldNotBeEmpty();
    }
}
