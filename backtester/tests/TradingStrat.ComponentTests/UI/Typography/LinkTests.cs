using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Typography;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Typography;

/// <summary>
/// Tests for the Link component.
/// </summary>
public class LinkTests : BunitTestContext
{
    [Fact]
    public void Link_WithExternalTrue_RendersAsAnchorElement()
    {
        // Arrange & Act
        IRenderedComponent<Link> cut = Render<Link>(parameters => parameters
            .Add(p => p.Href, "https://example.com")
            .Add(p => p.External, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "External Link"))));

        // Assert
        IElement anchor = cut.Find("a");
        anchor.ShouldNotBeNull();
        anchor.GetAttribute("href").ShouldBe("https://example.com");
        anchor.TextContent.ShouldBe("External Link");
    }

    [Fact]
    public void Link_WithExternalFalse_RendersAsNavLink()
    {
        // Arrange & Act
        IRenderedComponent<Link> cut = Render<Link>(parameters => parameters
            .Add(p => p.Href, "/dashboard")
            .Add(p => p.External, false)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Internal Link"))));

        // Assert
        // NavLink renders as an anchor element
        IElement anchor = cut.Find("a");
        anchor.ShouldNotBeNull();
        anchor.GetAttribute("href").ShouldBe("/dashboard");
    }

    [Fact]
    public void Link_WithTarget_AppliesTargetAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Link> cut = Render<Link>(parameters => parameters
            .Add(p => p.Href, "https://example.com")
            .Add(p => p.External, true)
            .Add(p => p.Target, "_blank")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "New Tab"))));

        // Assert
        IElement anchor = cut.Find("a");
        anchor.GetAttribute("target").ShouldBe("_blank");
    }

    [Fact]
    public void Link_AppliesUnderlineDecoration()
    {
        // Arrange & Act
        IRenderedComponent<Link> cut = Render<Link>(parameters => parameters
            .Add(p => p.Href, "/link")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Link"))));

        // Assert
        cut.Markup.ShouldContain("underline");
        cut.Markup.ShouldContain("decoration-zinc-950/50");
    }

    [Fact]
    public void Link_AppliesHoverState()
    {
        // Arrange & Act
        IRenderedComponent<Link> cut = Render<Link>(parameters => parameters
            .Add(p => p.Href, "/link")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Hover"))));

        // Assert
        cut.Markup.ShouldContain("data-[hover]:decoration-zinc-950");
    }

    [Fact]
    public void Link_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Link> cut = Render<Link>(parameters => parameters
            .Add(p => p.Href, "/custom")
            .Add(p => p.Class, "custom-link")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Custom"))));

        // Assert
        IElement anchor = cut.Find("a");
        anchor.ClassList.ShouldContain("custom-link");
    }

    [Fact]
    public void Link_AppliesDarkModeDecoration()
    {
        // Arrange & Act
        IRenderedComponent<Link> cut = Render<Link>(parameters => parameters
            .Add(p => p.Href, "/dark")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Dark"))));

        // Assert
        cut.Markup.ShouldContain("dark:decoration-white/50");
        cut.Markup.ShouldContain("dark:data-[hover]:decoration-white");
    }

    [Fact]
    public void Link_DefaultsToExternalFalse()
    {
        // Arrange & Act
        IRenderedComponent<Link> cut = Render<Link>(parameters => parameters
            .Add(p => p.Href, "/internal")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Internal"))));

        // Assert
        // Should render as NavLink (internal navigation)
        IElement anchor = cut.Find("a");
        anchor.ShouldNotBeNull();
    }
}
