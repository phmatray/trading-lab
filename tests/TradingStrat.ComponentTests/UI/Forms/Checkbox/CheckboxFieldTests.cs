using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Forms.Checkbox;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms.Checkbox;

/// <summary>
/// Tests for the CheckboxField component.
/// </summary>
public class CheckboxFieldTests : BunitTestContext
{
    [Fact]
    public void CheckboxField_RendersAsDiv()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxField> cut = Render<CheckboxField>();

        // Assert
        IElement div = cut.Find("div[data-slot='field']");
        div.ShouldNotBeNull();
    }

    [Fact]
    public void CheckboxField_AppliesGridLayout()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxField> cut = Render<CheckboxField>();

        // Assert
        cut.Markup.ShouldContain("grid");
        cut.Markup.ShouldContain("grid-cols-[1.125rem_1fr]");
        cut.Markup.ShouldContain("gap-x-4");
    }

    [Fact]
    public void CheckboxField_AppliesResponsiveGridLayout()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxField> cut = Render<CheckboxField>();

        // Assert
        cut.Markup.ShouldContain("sm:grid-cols-[1rem_1fr]");
    }

    [Fact]
    public void CheckboxField_AppliesControlColumnLayout()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxField> cut = Render<CheckboxField>();

        // Assert
        cut.Markup.ShouldContain("*:[data-slot=control]:col-start-1");
        cut.Markup.ShouldContain("*:[data-slot=control]:row-start-1");
    }

    [Fact]
    public void CheckboxField_AppliesLabelColumnLayout()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxField> cut = Render<CheckboxField>();

        // Assert
        cut.Markup.ShouldContain("*:[data-slot=label]:col-start-2");
        cut.Markup.ShouldContain("*:[data-slot=label]:row-start-1");
    }

    [Fact]
    public void CheckboxField_AppliesDescriptionLayout()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxField> cut = Render<CheckboxField>();

        // Assert
        cut.Markup.ShouldContain("*:[data-slot=description]:col-start-2");
        cut.Markup.ShouldContain("*:[data-slot=description]:row-start-2");
    }

    [Fact]
    public void CheckboxField_WithChildContent_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxField> cut = Render<CheckboxField>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Field Content"))));

        // Assert
        cut.Markup.ShouldContain("Field Content");
    }

    [Fact]
    public void CheckboxField_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxField> cut = Render<CheckboxField>(parameters => parameters
            .Add(p => p.Class, "custom-field"));

        // Assert
        IElement div = cut.Find("div");
        div.ClassList.ShouldContain("custom-field");
        div.ClassList.ShouldContain("grid");
    }
}
