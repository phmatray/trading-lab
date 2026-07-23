using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Navigation.Navbar;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Navigation.Navbar;

/// <summary>
/// Tests for the Catalyst Navbar component.
/// </summary>
public class NavbarTests : BunitTestContext
{
    [Fact]
    public void Navbar_WithoutParameters_RendersNavElement()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Navigation.Navbar.Navbar> cut = Render<Web.Components.UI.Navigation.Navbar.Navbar>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Navigation"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement nav = cut.Find("nav");
        nav.ShouldNotBeNull();
    }

    [Fact]
    public void Navbar_AppliesFlexClasses()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Navigation.Navbar.Navbar> cut = Render<Web.Components.UI.Navigation.Navbar.Navbar>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Nav"))));

        // Assert
        IElement nav = cut.Find("nav");
        nav.ClassList.ShouldContain("flex");
        nav.ClassList.ShouldContain("flex-1");
        nav.ClassList.ShouldContain("items-center");
        nav.ClassList.ShouldContain("gap-4");
    }

    [Fact]
    public void NavbarDivider_RendersVerticalDivider()
    {
        // Arrange & Act
        IRenderedComponent<NavbarDivider> cut = Render<NavbarDivider>();

        // Assert
        IElement div = cut.Find("div");
        div.ShouldNotBeNull();
        div.GetAttribute("aria-hidden").ShouldBe("true");
        div.ClassList.ShouldContain("h-6");
        div.ClassList.ShouldContain("w-px");
    }

    [Fact]
    public void NavbarSection_RendersFlexContainer()
    {
        // Arrange & Act
        IRenderedComponent<NavbarSection> cut = Render<NavbarSection>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Section"))));

        // Assert
        IElement div = cut.Find("div");
        div.ClassList.ShouldContain("flex");
        div.ClassList.ShouldContain("items-center");
        div.ClassList.ShouldContain("gap-3");
    }

    [Fact]
    public void NavbarSpacer_RendersSpacer()
    {
        // Arrange & Act
        IRenderedComponent<NavbarSpacer> cut = Render<NavbarSpacer>();

        // Assert
        IElement div = cut.Find("div");
        div.GetAttribute("aria-hidden").ShouldBe("true");
        div.ClassList.ShouldContain("flex-1");
    }

    [Fact]
    public void NavbarItem_WithHref_RendersLink()
    {
        // Arrange & Act
        IRenderedComponent<NavbarItem> cut = Render<NavbarItem>(parameters => parameters
            .Add(p => p.Href, "/dashboard")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Dashboard"))));

        // Assert
        IElement link = cut.Find("a");
        link.ShouldNotBeNull();
        link.GetAttribute("href").ShouldBe("/dashboard");
    }

    [Fact]
    public void NavbarItem_WithoutHref_RendersButton()
    {
        // Arrange & Act
        IRenderedComponent<NavbarItem> cut = Render<NavbarItem>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Menu"))));

        // Assert
        IElement button = cut.Find("button");
        button.ShouldNotBeNull();
        button.GetAttribute("type").ShouldBe("button");
    }

    [Fact]
    public void NavbarItem_WhenCurrent_ShowsIndicator()
    {
        // Arrange & Act
        IRenderedComponent<NavbarItem> cut = Render<NavbarItem>(parameters => parameters
            .Add(p => p.Href, "/current")
            .Add(p => p.Current, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Current"))));

        // Assert
        IElement indicator = cut.Find("span.absolute.inset-x-2");
        indicator.ShouldNotBeNull();
        indicator.ClassList.ShouldContain("bg-zinc-950");
    }

    [Fact]
    public void NavbarLabel_RendersTruncatedText()
    {
        // Arrange & Act
        IRenderedComponent<NavbarLabel> cut = Render<NavbarLabel>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Long Label Text"))));

        // Assert
        IElement span = cut.Find("span");
        span.ClassList.ShouldContain("truncate");
        span.TextContent.ShouldBe("Long Label Text");
    }
}
