using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Navigation.Pagination;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Navigation.Pagination;

/// <summary>
/// Tests for the Catalyst PaginationNext component.
/// </summary>
public class PaginationNextTests : BunitTestContext
{
    [Fact]
    public void PaginationNext_WithoutHref_RendersDisabledButton()
    {
        // Arrange & Act
        IRenderedComponent<PaginationNext> cut = Render<PaginationNext>();

        // Assert
        IElement button = cut.Find("button");
        button.ShouldNotBeNull();
        button.HasAttribute("disabled").ShouldBeTrue();
        button.GetAttribute("aria-label").ShouldBe("Next page");
    }

    [Fact]
    public void PaginationNext_WithHref_RendersEnabledLink()
    {
        // Arrange & Act
        IRenderedComponent<PaginationNext> cut = Render<PaginationNext>(parameters => parameters
            .Add(p => p.Href, "/page/3"));

        // Assert
        IElement link = cut.Find("a");
        link.ShouldNotBeNull();
        link.GetAttribute("href").ShouldBe("/page/3");
    }

    [Fact]
    public void PaginationNext_DefaultsToTextNext()
    {
        // Arrange & Act
        IRenderedComponent<PaginationNext> cut = Render<PaginationNext>(parameters => parameters
            .Add(p => p.Href, "/page/3"));

        // Assert
        cut.Markup.ShouldContain("Next");
    }

    [Fact]
    public void PaginationNext_AppliesJustifyEndClass()
    {
        // Arrange & Act
        IRenderedComponent<PaginationNext> cut = Render<PaginationNext>();

        // Assert
        IElement span = cut.Find("span");
        span.ClassList.ShouldContain("justify-end");
    }
}
