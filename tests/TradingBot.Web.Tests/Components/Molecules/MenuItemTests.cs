// <copyright file="MenuItemTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TradingBot.Web.Components.Atoms;
using TradingBot.Web.Components.Molecules;
using Xunit;

namespace TradingBot.Web.Tests.Components.Molecules;

/// <summary>
/// Tests for the MenuItem component.
/// </summary>
public class MenuItemTests
{
    [Fact]
    public void MenuItem_RendersWithLabel()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.RenderComponent<MenuItem>(parameters => parameters
            .Add(p => p.Label, "Dashboard")
            .Add(p => p.Href, "/")
            .Add(p => p.IconName, IconName.Home));

        // Assert
        var span = cut.Find("span");
        span.TextContent.ShouldBe("Dashboard");
    }

    [Fact]
    public void MenuItem_RendersIcon()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.RenderComponent<MenuItem>(parameters => parameters
            .Add(p => p.Label, "Dashboard")
            .Add(p => p.Href, "/")
            .Add(p => p.IconName, IconName.Home));

        // Assert
        var svg = cut.Find("svg");
        svg.ShouldNotBeNull();
    }

    [Fact]
    public void MenuItem_WhenCollapsed_HidesLabel()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.RenderComponent<MenuItem>(parameters => parameters
            .Add(p => p.Label, "Dashboard")
            .Add(p => p.Href, "/")
            .Add(p => p.IconName, IconName.Home)
            .Add(p => p.IsCollapsed, true));

        // Assert
        cut.FindAll("span").Count.ShouldBe(0);
    }

    [Fact]
    public void MenuItem_WhenCollapsed_IconHasAriaLabel()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.RenderComponent<MenuItem>(parameters => parameters
            .Add(p => p.Label, "Dashboard")
            .Add(p => p.Href, "/")
            .Add(p => p.IconName, IconName.Home)
            .Add(p => p.IsCollapsed, true));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("aria-label").ShouldBe("Dashboard");
    }

    [Fact]
    public void MenuItem_WhenExpanded_IconIsAriaHidden()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.RenderComponent<MenuItem>(parameters => parameters
            .Add(p => p.Label, "Dashboard")
            .Add(p => p.Href, "/")
            .Add(p => p.IconName, IconName.Home)
            .Add(p => p.IsCollapsed, false));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Fact]
    public void MenuItem_AppliesHoverStyles()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.RenderComponent<MenuItem>(parameters => parameters
            .Add(p => p.Label, "Dashboard")
            .Add(p => p.Href, "/")
            .Add(p => p.IconName, IconName.Home));

        // Assert
        var link = cut.Find("a");
        link.ClassList.ShouldContain("hover:bg-gray-100");
        link.ClassList.ShouldContain("dark:hover:bg-gray-800");
    }

    [Fact]
    public void MenuItem_AppliesFocusStyles()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.RenderComponent<MenuItem>(parameters => parameters
            .Add(p => p.Label, "Dashboard")
            .Add(p => p.Href, "/")
            .Add(p => p.IconName, IconName.Home));

        // Assert
        var link = cut.Find("a");
        link.ClassList.ShouldContain("focus:outline-none");
        link.ClassList.ShouldContain("focus:ring-2");
        link.ClassList.ShouldContain("focus:ring-blue-500");
    }

    [Fact]
    public void MenuItem_WhenCollapsed_AppliesJustifyCenter()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.RenderComponent<MenuItem>(parameters => parameters
            .Add(p => p.Label, "Dashboard")
            .Add(p => p.Href, "/")
            .Add(p => p.IconName, IconName.Home)
            .Add(p => p.IsCollapsed, true));

        // Assert
        var link = cut.Find("a");
        link.ClassList.ShouldContain("justify-center");
    }

    [Fact]
    public void MenuItem_SetsCorrectHref()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act
        var cut = ctx.RenderComponent<MenuItem>(parameters => parameters
            .Add(p => p.Label, "Portfolio")
            .Add(p => p.Href, "/portfolio")
            .Add(p => p.IconName, IconName.Briefcase));

        // Assert
        var link = cut.Find("a");
        link.GetAttribute("href").ShouldBe("/portfolio");
    }

    [Fact]
    public void MenuItem_IconSizeChanges_WhenCollapsed()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddSingleton<NavigationManager>(new MockNavigationManager());

        // Act - Expanded
        var cutExpanded = ctx.RenderComponent<MenuItem>(parameters => parameters
            .Add(p => p.Label, "Dashboard")
            .Add(p => p.Href, "/")
            .Add(p => p.IconName, IconName.Home)
            .Add(p => p.IsCollapsed, false));

        // Assert - Expanded
        var svgExpanded = cutExpanded.Find("svg");
        svgExpanded.ClassList.ShouldContain("w-5");
        svgExpanded.ClassList.ShouldContain("h-5");

        // Arrange & Act - Collapsed
        using var ctx2 = new Bunit.TestContext();
        ctx2.Services.AddSingleton<NavigationManager>(new MockNavigationManager());
        var cutCollapsed = ctx2.RenderComponent<MenuItem>(parameters => parameters
            .Add(p => p.Label, "Dashboard")
            .Add(p => p.Href, "/")
            .Add(p => p.IconName, IconName.Home)
            .Add(p => p.IsCollapsed, true));

        // Assert - Collapsed
        var svgCollapsed = cutCollapsed.Find("svg");
        svgCollapsed.ClassList.ShouldContain("w-6");
        svgCollapsed.ClassList.ShouldContain("h-6");
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
