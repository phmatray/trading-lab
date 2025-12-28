using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Navigation.Pagination;

/// <summary>
/// Tests for the Catalyst Pagination component.
/// </summary>
public class PaginationTests : BunitTestContext
{
    [Fact]
    public void Pagination_WithoutParameters_RendersNavElement()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Navigation.Pagination.Pagination> cut = Render<Web.Components.UI.Navigation.Pagination.Pagination>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Pages"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement nav = cut.Find("nav");
        nav.ShouldNotBeNull();
        nav.GetAttribute("aria-label").ShouldBe("Page navigation");
    }

    [Fact]
    public void Pagination_WithCustomAriaLabel_AppliesLabel()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Navigation.Pagination.Pagination> cut = Render<Web.Components.UI.Navigation.Pagination.Pagination>(parameters => parameters
            .Add(p => p.AriaLabel, "Custom pagination")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Pages"))));

        // Assert
        IElement nav = cut.Find("nav");
        nav.GetAttribute("aria-label").ShouldBe("Custom pagination");
    }

    [Fact]
    public void Pagination_AppliesFlexGapClasses()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.Navigation.Pagination.Pagination> cut = Render<Web.Components.UI.Navigation.Pagination.Pagination>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Pages"))));

        // Assert
        IElement nav = cut.Find("nav");
        nav.ClassList.ShouldContain("flex");
        nav.ClassList.ShouldContain("gap-x-2");
    }
}
