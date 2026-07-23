using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Navigation.Pagination;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Navigation.Pagination;

/// <summary>
/// Tests for the Catalyst PaginationPage component.
/// </summary>
public class PaginationPageTests : BunitTestContext
{
    [Fact]
    public void PaginationPage_WithPageNumber_RendersPageButton()
    {
        // Arrange & Act
        IRenderedComponent<PaginationPage> cut = Render<PaginationPage>(parameters => parameters
            .Add(p => p.Href, "/page/2")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "2"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement link = cut.Find("a");
        link.ShouldNotBeNull();
        link.TextContent.ShouldContain("2");
    }

    [Fact]
    public void PaginationPage_WhenCurrent_AppliesCurrentStyling()
    {
        // Arrange & Act
        IRenderedComponent<PaginationPage> cut = Render<PaginationPage>(parameters => parameters
            .Add(p => p.Href, "/page/2")
            .Add(p => p.Current, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "2"))));

        // Assert
        IElement link = cut.Find("a");
        link.ShouldNotBeNull();
        link.GetAttribute("aria-current").ShouldBe("page");
        link.ClassList.ShouldContain("before:bg-zinc-950/5");
    }

    [Fact]
    public void PaginationPage_WhenNotCurrent_NoAriaCurrentAttribute()
    {
        // Arrange & Act
        IRenderedComponent<PaginationPage> cut = Render<PaginationPage>(parameters => parameters
            .Add(p => p.Href, "/page/3")
            .Add(p => p.Current, false)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "3"))));

        // Assert
        IElement link = cut.Find("a");
        link.HasAttribute("aria-current").ShouldBeFalse();
    }

    [Fact]
    public void PaginationPage_AppliesMinWidthClass()
    {
        // Arrange & Act
        IRenderedComponent<PaginationPage> cut = Render<PaginationPage>(parameters => parameters
            .Add(p => p.Href, "/page/1")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "1"))));

        // Assert
        IElement link = cut.Find("a");
        link.ClassList.ShouldContain("min-w-9");
    }
}
