using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Overlays.Dialog;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Overlays.Dialog;

/// <summary>
/// Tests for the DialogActions component.
/// </summary>
public class DialogActionsTests : BunitTestContext
{
    [Fact]
    public void DialogActions_RendersAsDiv()
    {
        // Arrange & Act
        IRenderedComponent<DialogActions> cut = Render<DialogActions>();

        // Assert
        IElement div = cut.Find("div");
        div.ShouldNotBeNull();
    }

    [Fact]
    public void DialogActions_AppliesMarginTop()
    {
        // Arrange & Act
        IRenderedComponent<DialogActions> cut = Render<DialogActions>();

        // Assert
        cut.Markup.ShouldContain("mt-8");
    }

    [Fact]
    public void DialogActions_AppliesFlexLayout()
    {
        // Arrange & Act
        IRenderedComponent<DialogActions> cut = Render<DialogActions>();

        // Assert
        cut.Markup.ShouldContain("flex");
        cut.Markup.ShouldContain("flex-col-reverse");
        cut.Markup.ShouldContain("items-center");
        cut.Markup.ShouldContain("justify-end");
    }

    [Fact]
    public void DialogActions_AppliesResponsiveFlexDirection()
    {
        // Arrange & Act
        IRenderedComponent<DialogActions> cut = Render<DialogActions>();

        // Assert
        cut.Markup.ShouldContain("sm:flex-row");
    }

    [Fact]
    public void DialogActions_AppliesGapBetweenItems()
    {
        // Arrange & Act
        IRenderedComponent<DialogActions> cut = Render<DialogActions>();

        // Assert
        cut.Markup.ShouldContain("gap-3");
    }

    [Fact]
    public void DialogActions_AppliesFullWidthToChildren()
    {
        // Arrange & Act
        IRenderedComponent<DialogActions> cut = Render<DialogActions>();

        // Assert
        cut.Markup.ShouldContain("*:w-full");
        cut.Markup.ShouldContain("sm:*:w-auto");
    }

    [Fact]
    public void DialogActions_WithChildContent_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<DialogActions> cut = Render<DialogActions>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Actions Content"))));

        // Assert
        cut.Markup.ShouldContain("Actions Content");
    }

    [Fact]
    public void DialogActions_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<DialogActions> cut = Render<DialogActions>(parameters => parameters
            .Add(p => p.Class, "custom-actions"));

        // Assert
        IElement div = cut.Find("div");
        div.ClassList.ShouldContain("custom-actions");
        div.ClassList.ShouldContain("mt-8");
    }
}
