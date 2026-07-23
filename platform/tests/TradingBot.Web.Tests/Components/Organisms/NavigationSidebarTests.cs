// <copyright file="NavigationSidebarTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TradingBot.Web.Components.Organisms;
using TradingBot.Web.Services;

namespace TradingBot.Web.Tests.Components.Organisms;

/// <summary>
/// Tests for the NavigationSidebar component.
/// </summary>
public class NavigationSidebarTests
{
    [Fact]
    public void NavigationSidebar_RendersCorrectly()
    {
        // Arrange
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new UIStateService());
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.Render<TbNavigationSidebar>();

        // Assert
        var aside = cut.Find("aside");
        aside.ShouldNotBeNull();
        aside.GetAttribute("role").ShouldBe("navigation");
        aside.GetAttribute("aria-label").ShouldBe("Main navigation");
    }

    [Fact]
    public void NavigationSidebar_ShowsLogoWhenExpanded()
    {
        // Arrange
        using var ctx = new BunitContext();
        var uiStateService = new UIStateService();
        uiStateService.SidebarCollapsed = false;
        ctx.Services.AddSingleton(uiStateService);
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.Render<TbNavigationSidebar>();

        // Assert
        var heading = cut.Find("h1");
        heading.TextContent.ShouldBe("TradingBot");
    }

    [Fact]
    public void NavigationSidebar_HidesLogoWhenCollapsed()
    {
        // Arrange
        using var ctx = new BunitContext();
        var uiStateService = new UIStateService();
        uiStateService.SidebarCollapsed = true;
        ctx.Services.AddSingleton(uiStateService);
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.Render<TbNavigationSidebar>();

        // Assert
        cut.FindAll("h1").Count.ShouldBe(0);
    }

    [Fact]
    public void NavigationSidebar_ToggleButton_CollapsesAndExpands()
    {
        // Arrange
        using var ctx = new BunitContext();
        var uiStateService = new UIStateService();
        ctx.Services.AddSingleton(uiStateService);
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());
        var cut = ctx.Render<TbNavigationSidebar>();
        var button = cut.Find("button");

        // Initial state should be expanded
        uiStateService.SidebarCollapsed.ShouldBeFalse();

        // Act - Collapse
        button.Click();

        // Assert - Collapsed
        uiStateService.SidebarCollapsed.ShouldBeTrue();

        // Act - Expand
        button.Click();

        // Assert - Expanded
        uiStateService.SidebarCollapsed.ShouldBeFalse();
    }

    [Fact]
    public void NavigationSidebar_AppliesExpandedWidthClass()
    {
        // Arrange
        using var ctx = new BunitContext();
        var uiStateService = new UIStateService();
        uiStateService.SidebarCollapsed = false;
        ctx.Services.AddSingleton(uiStateService);
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.Render<TbNavigationSidebar>();

        // Assert
        var aside = cut.Find("aside");
        aside.ClassList.ShouldContain("w-64");
    }

    [Fact]
    public void NavigationSidebar_AppliesCollapsedWidthClass()
    {
        // Arrange
        using var ctx = new BunitContext();
        var uiStateService = new UIStateService();
        uiStateService.SidebarCollapsed = true;
        ctx.Services.AddSingleton(uiStateService);
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.Render<TbNavigationSidebar>();

        // Assert
        var aside = cut.Find("aside");
        aside.ClassList.ShouldContain("w-16");
    }

    [Fact]
    public void NavigationSidebar_AppliesTransitionClasses()
    {
        // Arrange
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new UIStateService());
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.Render<TbNavigationSidebar>();

        // Assert
        var aside = cut.Find("aside");
        aside.ClassList.ShouldContain("transition-all");
        aside.ClassList.ShouldContain("duration-300");
        aside.ClassList.ShouldContain("ease-in-out");
    }

    [Fact]
    public void NavigationSidebar_RendersAllMenuItems()
    {
        // Arrange
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new UIStateService());
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.Render<TbNavigationSidebar>();

        // Assert - Should have 7 menu items (Dashboard, Portfolio, Performance, Strategies, Backtesting, Settings, Help)
        var navLinks = cut.FindAll("a");
        navLinks.Count.ShouldBe(7);
    }

    [Fact]
    public void NavigationSidebar_MenuItemsHaveCorrectHrefs()
    {
        // Arrange
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new UIStateService());
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.Render<TbNavigationSidebar>();

        // Assert
        var links = cut.FindAll("a");
        links[0].GetAttribute("href").ShouldBe("/");
        links[1].GetAttribute("href").ShouldBe("/portfolio");
        links[2].GetAttribute("href").ShouldBe("/performance");
        links[3].GetAttribute("href").ShouldBe("/strategies");
        links[4].GetAttribute("href").ShouldBe("/backtest");
        links[5].GetAttribute("href").ShouldBe("/settings");
        links[6].GetAttribute("href").ShouldBe("/help");
    }

    [Fact]
    public void NavigationSidebar_ToggleButtonHasCorrectAriaLabel_WhenExpanded()
    {
        // Arrange
        using var ctx = new BunitContext();
        var uiStateService = new UIStateService();
        uiStateService.SidebarCollapsed = false;
        ctx.Services.AddSingleton(uiStateService);
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.Render<TbNavigationSidebar>();

        // Assert
        var button = cut.Find("button");
        button.GetAttribute("aria-label").ShouldBe("Collapse sidebar");
    }

    [Fact]
    public void NavigationSidebar_ToggleButtonHasCorrectAriaLabel_WhenCollapsed()
    {
        // Arrange
        using var ctx = new BunitContext();
        var uiStateService = new UIStateService();
        uiStateService.SidebarCollapsed = true;
        ctx.Services.AddSingleton(uiStateService);
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.Render<TbNavigationSidebar>();

        // Assert
        var button = cut.Find("button");
        button.GetAttribute("aria-label").ShouldBe("Expand sidebar");
    }

    [Fact]
    public void NavigationSidebar_ToggleIconChanges_BasedOnState()
    {
        // Arrange
        using var ctx = new BunitContext();
        var uiStateService = new UIStateService();
        uiStateService.SidebarCollapsed = false;
        ctx.Services.AddSingleton(uiStateService);
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act - Expanded
        var cutExpanded = ctx.Render<TbNavigationSidebar>();

        // Assert - Should show ChevronLeft when expanded
        var svgExpanded = cutExpanded.Find("button svg");
        var pathExpanded = svgExpanded.QuerySelector("path");
        pathExpanded!.GetAttribute("d")?.ShouldContain("M15.75 19.5L8.25 12");

        // Arrange - Collapsed
        using var ctx2 = new BunitContext();
        var uiStateService2 = new UIStateService();
        uiStateService2.SidebarCollapsed = true;
        ctx2.Services.AddSingleton(uiStateService2);
        ctx2.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cutCollapsed = ctx2.Render<TbNavigationSidebar>();

        // Assert - Should show ChevronRight when collapsed
        var svgCollapsed = cutCollapsed.Find("button svg");
        var pathCollapsed = svgCollapsed.QuerySelector("path");
        pathCollapsed!.GetAttribute("d")?.ShouldContain("M8.25 4.5l7.5 7.5");
    }

    [Fact]
    public void NavigationSidebar_UpdatesOnStateChange()
    {
        // Arrange
        using var ctx = new BunitContext();
        var uiStateService = new UIStateService();
        ctx.Services.AddSingleton(uiStateService);
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());
        var cut = ctx.Render<TbNavigationSidebar>();
        var aside = cut.Find("aside");

        // Initial state
        aside.ClassList.ShouldContain("w-64");

        // Act - Change state using InvokeAsync to ensure it's on the correct dispatcher thread
        cut.InvokeAsync(() => uiStateService.SidebarCollapsed = true);

        // Assert - Component should re-render
        aside = cut.Find("aside");
        aside.ClassList.ShouldContain("w-16");
    }

    [Fact]
    public void NavigationSidebar_HasFixedPositioning()
    {
        // Arrange
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new UIStateService());
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.Render<TbNavigationSidebar>();

        // Assert
        var aside = cut.Find("aside");
        aside.ClassList.ShouldContain("fixed");
        aside.ClassList.ShouldContain("inset-y-0");
        aside.ClassList.ShouldContain("left-0");
    }

    [Fact]
    public void NavigationSidebar_HasCorrectZIndex()
    {
        // Arrange
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new UIStateService());
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.Render<TbNavigationSidebar>();

        // Assert
        var aside = cut.Find("aside");
        aside.ClassList.ShouldContain("z-50");
    }

    /// <summary>
    /// Mock NavigationManager for testing.
    /// </summary>
    private sealed class MockNavigationManager : NavigationManager
    {
        public MockNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            // Mock implementation - do nothing
        }
    }
}
