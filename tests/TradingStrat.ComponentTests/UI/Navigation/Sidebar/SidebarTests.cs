using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Navigation.Sidebar;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Navigation.Sidebar;

/// <summary>
/// Tests for the Catalyst Sidebar component.
/// </summary>
public class SidebarTests : BunitTestContext
{
    [Fact]
    public void Sidebar_WithoutParameters_RendersNavElement()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Navigation.Sidebar.Sidebar> cut = Render<Web.Components.UI.Navigation.Sidebar.Sidebar>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Sidebar"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement nav = cut.Find("nav");
        nav.ShouldNotBeNull();
    }

    [Fact]
    public void Sidebar_AppliesFlexColumnClasses()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Navigation.Sidebar.Sidebar> cut = Render<Web.Components.UI.Navigation.Sidebar.Sidebar>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Sidebar"))));

        // Assert
        IElement nav = cut.Find("nav");
        nav.ClassList.ShouldContain("flex");
        nav.ClassList.ShouldContain("h-full");
        nav.ClassList.ShouldContain("flex-col");
    }

    [Fact]
    public void SidebarHeader_RendersWithBorder()
    {
        // Arrange & Act
        IRenderedComponent<SidebarHeader> cut = Render<SidebarHeader>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Header"))));

        // Assert
        IElement div = cut.Find("div");
        div.ClassList.ShouldContain("border-b");
        div.ClassList.ShouldContain("border-zinc-950/5");
    }

    [Fact]
    public void SidebarBody_RendersScrollableArea()
    {
        // Arrange & Act
        IRenderedComponent<SidebarBody> cut = Render<SidebarBody>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Body"))));

        // Assert
        IElement div = cut.Find("div");
        div.ClassList.ShouldContain("overflow-y-auto");
        div.ClassList.ShouldContain("flex-1");
    }

    [Fact]
    public void SidebarFooter_RendersWithTopBorder()
    {
        // Arrange & Act
        IRenderedComponent<SidebarFooter> cut = Render<SidebarFooter>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Footer"))));

        // Assert
        IElement div = cut.Find("div");
        div.ClassList.ShouldContain("border-t");
        div.ClassList.ShouldContain("border-zinc-950/5");
    }

    [Fact]
    public void SidebarSection_RendersWithDataSlot()
    {
        // Arrange & Act
        IRenderedComponent<SidebarSection> cut = Render<SidebarSection>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Section"))));

        // Assert
        IElement div = cut.Find("div");
        div.GetAttribute("data-slot").ShouldBe("section");
        div.ClassList.ShouldContain("flex-col");
    }

    [Fact]
    public void SidebarDivider_RendersHorizontalRule()
    {
        // Arrange & Act
        IRenderedComponent<SidebarDivider> cut = Render<SidebarDivider>();

        // Assert
        IElement hr = cut.Find("hr");
        hr.ShouldNotBeNull();
        hr.ClassList.ShouldContain("border-t");
    }

    [Fact]
    public void SidebarSpacer_RendersSpacer()
    {
        // Arrange & Act
        IRenderedComponent<SidebarSpacer> cut = Render<SidebarSpacer>();

        // Assert
        IElement div = cut.Find("div");
        div.GetAttribute("aria-hidden").ShouldBe("true");
        div.ClassList.ShouldContain("flex-1");
    }

    [Fact]
    public void SidebarHeading_RendersH3Element()
    {
        // Arrange & Act
        IRenderedComponent<SidebarHeading> cut = Render<SidebarHeading>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Navigation"))));

        // Assert
        IElement h3 = cut.Find("h3");
        h3.ShouldNotBeNull();
        h3.TextContent.ShouldBe("Navigation");
        h3.ClassList.ShouldContain("text-xs/6");
    }

    [Fact]
    public void SidebarItem_WithHref_RendersLink()
    {
        // Arrange & Act
        IRenderedComponent<SidebarItem> cut = Render<SidebarItem>(parameters => parameters
            .Add(p => p.Href, "/settings")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Settings"))));

        // Assert
        IElement link = cut.Find("a");
        link.ShouldNotBeNull();
        link.GetAttribute("href").ShouldBe("/settings");
    }

    [Fact]
    public void SidebarItem_WithoutHref_RendersButton()
    {
        // Arrange & Act
        IRenderedComponent<SidebarItem> cut = Render<SidebarItem>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Menu"))));

        // Assert
        IElement button = cut.Find("button");
        button.ShouldNotBeNull();
        button.GetAttribute("type").ShouldBe("button");
    }

    [Fact]
    public void SidebarItem_WhenCurrent_ShowsVerticalIndicator()
    {
        // Arrange & Act
        IRenderedComponent<SidebarItem> cut = Render<SidebarItem>(parameters => parameters
            .Add(p => p.Href, "/current")
            .Add(p => p.Current, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Current"))));

        // Assert
        IElement indicator = cut.Find("span.absolute.inset-y-2");
        indicator.ShouldNotBeNull();
        indicator.ClassList.ShouldContain("bg-zinc-950");
        indicator.ClassList.ShouldContain("w-0.5");
    }

    [Fact]
    public void SidebarLabel_RendersTruncatedText()
    {
        // Arrange & Act
        IRenderedComponent<SidebarLabel> cut = Render<SidebarLabel>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Long Menu Item"))));

        // Assert
        IElement span = cut.Find("span");
        span.ClassList.ShouldContain("truncate");
        span.TextContent.ShouldBe("Long Menu Item");
    }
}
