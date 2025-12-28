using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Navigation.Pagination;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Navigation.Pagination;

/// <summary>
/// Tests for the Catalyst PaginationPrevious component.
/// </summary>
public class PaginationPreviousTests : BunitTestContext
{
    [Fact]
    public void PaginationPrevious_WithoutHref_RendersDisabledButton()
    {
        // Arrange & Act
        IRenderedComponent<PaginationPrevious> cut = Render<PaginationPrevious>();

        // Assert
        IElement button = cut.Find("button");
        button.ShouldNotBeNull();
        button.HasAttribute("disabled").ShouldBeTrue();
        button.GetAttribute("aria-label").ShouldBe("Previous page");
    }

    [Fact]
    public void PaginationPrevious_WithHref_RendersEnabledLink()
    {
        // Arrange & Act
        IRenderedComponent<PaginationPrevious> cut = Render<PaginationPrevious>(parameters => parameters
            .Add(p => p.Href, "/page/1"));

        // Assert
        IElement link = cut.Find("a");
        link.ShouldNotBeNull();
        link.GetAttribute("href").ShouldBe("/page/1");
    }

    [Fact]
    public void PaginationPrevious_DefaultsToTextPrevious()
    {
        // Arrange & Act
        IRenderedComponent<PaginationPrevious> cut = Render<PaginationPrevious>(parameters => parameters
            .Add(p => p.Href, "/page/1"));

        // Assert
        cut.Markup.ShouldContain("Previous");
    }

    [Fact]
    public void PaginationPrevious_WithCustomContent_RendersCustomText()
    {
        // Arrange & Act
        IRenderedComponent<PaginationPrevious> cut = Render<PaginationPrevious>(parameters => parameters
            .Add(p => p.Href, "/page/1")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Prev"))));

        // Assert
        IElement link = cut.Find("a");
        link.TextContent.ShouldContain("Prev");
        link.TextContent.ShouldNotContain("Previous");
    }

    [Fact]
    public void PaginationPrevious_RendersArrowIcon()
    {
        // Arrange & Act
        IRenderedComponent<PaginationPrevious> cut = Render<PaginationPrevious>();

        // Assert
        IElement svg = cut.Find("svg");
        svg.ShouldNotBeNull();
        svg.GetAttribute("viewBox").ShouldBe("0 0 16 16");
    }

    [Fact]
    public void PaginationPrevious_AppliesGrowClasses()
    {
        // Arrange & Act
        IRenderedComponent<PaginationPrevious> cut = Render<PaginationPrevious>();

        // Assert
        IElement span = cut.Find("span");
        span.ClassList.ShouldContain("grow");
        span.ClassList.ShouldContain("basis-0");
    }
}
