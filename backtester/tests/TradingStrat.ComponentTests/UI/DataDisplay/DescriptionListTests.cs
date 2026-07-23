using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.DataDisplay;
using Xunit;

namespace TradingStrat.ComponentTests.UI.DataDisplay;

/// <summary>
/// Tests for the DescriptionList component.
/// </summary>
public class DescriptionListTests : BunitTestContext
{
    [Fact]
    public void DescriptionList_RendersAsDlElement()
    {
        // Arrange & Act
        IRenderedComponent<DescriptionList> cut = Render<DescriptionList>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Content"))));

        // Assert
        IElement dl = cut.Find("dl");
        dl.ShouldNotBeNull();
    }

    [Fact]
    public void DescriptionList_AppliesGridLayout()
    {
        // Arrange & Act
        IRenderedComponent<DescriptionList> cut = Render<DescriptionList>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Grid"))));

        // Assert
        cut.Markup.ShouldContain("grid");
        cut.Markup.ShouldContain("grid-cols-1");
        // Catalyst uses custom grid template for better responsive layout
        cut.Markup.ShouldContain("sm:grid-cols-");
    }

    [Fact]
    public void DescriptionList_AppliesGapClasses()
    {
        // Arrange & Act
        IRenderedComponent<DescriptionList> cut = Render<DescriptionList>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Gap"))));

        // Assert
        cut.Markup.ShouldContain("gap-x-6");
        cut.Markup.ShouldContain("gap-y-3");
    }

    [Fact]
    public void DescriptionList_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<DescriptionList> cut = Render<DescriptionList>(parameters => parameters
            .Add(p => p.Class, "custom-list")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Custom"))));

        // Assert
        IElement dl = cut.Find("dl");
        dl.ClassList.ShouldContain("custom-list");
        dl.ClassList.ShouldContain("grid");
    }

    [Fact]
    public void DescriptionList_WithEmptyContent_RendersWithoutError()
    {
        // Arrange & Act
        IRenderedComponent<DescriptionList> cut = Render<DescriptionList>();

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        cut.Find("dl").ShouldNotBeNull();
    }
}
