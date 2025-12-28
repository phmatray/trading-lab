using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Forms.Radio;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms.Radio;

/// <summary>
/// Tests for the RadioField component.
/// </summary>
public class RadioFieldTests : BunitTestContext
{
    [Fact]
    public void RadioField_RendersAsDiv()
    {
        // Arrange & Act
        IRenderedComponent<RadioField> cut = Render<RadioField>();

        // Assert
        IElement div = cut.Find("div[data-slot='field']");
        div.ShouldNotBeNull();
    }

    [Fact]
    public void RadioField_AppliesGridLayout()
    {
        // Arrange & Act
        IRenderedComponent<RadioField> cut = Render<RadioField>();

        // Assert
        cut.Markup.ShouldContain("grid");
        cut.Markup.ShouldContain("grid-cols-[1.1875rem_1fr]");
        cut.Markup.ShouldContain("gap-x-4");
    }

    [Fact]
    public void RadioField_AppliesResponsiveGridLayout()
    {
        // Arrange & Act
        IRenderedComponent<RadioField> cut = Render<RadioField>();

        // Assert
        cut.Markup.ShouldContain("sm:grid-cols-[1.0625rem_1fr]");
    }

    [Fact]
    public void RadioField_AppliesControlColumnLayout()
    {
        // Arrange & Act
        IRenderedComponent<RadioField> cut = Render<RadioField>();

        // Assert
        cut.Markup.ShouldContain("*:[data-slot=control]:col-start-1");
        cut.Markup.ShouldContain("*:[data-slot=control]:row-start-1");
    }

    [Fact]
    public void RadioField_AppliesLabelColumnLayout()
    {
        // Arrange & Act
        IRenderedComponent<RadioField> cut = Render<RadioField>();

        // Assert
        cut.Markup.ShouldContain("*:[data-slot=label]:col-start-2");
        cut.Markup.ShouldContain("*:[data-slot=label]:row-start-1");
    }

    [Fact]
    public void RadioField_AppliesDescriptionLayout()
    {
        // Arrange & Act
        IRenderedComponent<RadioField> cut = Render<RadioField>();

        // Assert
        cut.Markup.ShouldContain("*:[data-slot=description]:col-start-2");
        cut.Markup.ShouldContain("*:[data-slot=description]:row-start-2");
    }

    [Fact]
    public void RadioField_WithChildContent_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<RadioField> cut = Render<RadioField>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Field Content"))));

        // Assert
        cut.Markup.ShouldContain("Field Content");
    }

    [Fact]
    public void RadioField_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<RadioField> cut = Render<RadioField>(parameters => parameters
            .Add(p => p.Class, "custom-field"));

        // Assert
        IElement div = cut.Find("div");
        div.ClassList.ShouldContain("custom-field");
        div.ClassList.ShouldContain("grid");
    }
}
