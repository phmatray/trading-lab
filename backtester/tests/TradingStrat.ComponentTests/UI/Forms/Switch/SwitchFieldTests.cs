using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Forms.Switch;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms.Switch;

/// <summary>
/// Tests for the SwitchField component.
/// </summary>
public class SwitchFieldTests : BunitTestContext
{
    [Fact]
    public void SwitchField_RendersAsDiv()
    {
        // Arrange & Act
        IRenderedComponent<SwitchField> cut = Render<SwitchField>();

        // Assert
        IElement div = cut.Find("div[data-slot='field']");
        div.ShouldNotBeNull();
    }

    [Fact]
    public void SwitchField_AppliesGridLayout()
    {
        // Arrange & Act
        IRenderedComponent<SwitchField> cut = Render<SwitchField>();

        // Assert
        cut.Markup.ShouldContain("grid");
        cut.Markup.ShouldContain("grid-cols-[1fr_auto]");
        cut.Markup.ShouldContain("gap-x-8");
    }

    [Fact]
    public void SwitchField_AppliesControlOnRightSide()
    {
        // Arrange & Act
        IRenderedComponent<SwitchField> cut = Render<SwitchField>();

        // Assert
        cut.Markup.ShouldContain("*:[data-slot=control]:col-start-2");
        cut.Markup.ShouldContain("*:[data-slot=control]:self-start");
    }

    [Fact]
    public void SwitchField_AppliesLabelOnLeftSide()
    {
        // Arrange & Act
        IRenderedComponent<SwitchField> cut = Render<SwitchField>();

        // Assert
        cut.Markup.ShouldContain("*:[data-slot=label]:col-start-1");
        cut.Markup.ShouldContain("*:[data-slot=label]:row-start-1");
    }

    [Fact]
    public void SwitchField_AppliesDescriptionLayout()
    {
        // Arrange & Act
        IRenderedComponent<SwitchField> cut = Render<SwitchField>();

        // Assert
        cut.Markup.ShouldContain("*:[data-slot=description]:col-start-1");
        cut.Markup.ShouldContain("*:[data-slot=description]:row-start-2");
    }

    [Fact]
    public void SwitchField_WithChildContent_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<SwitchField> cut = Render<SwitchField>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Field Content"))));

        // Assert
        cut.Markup.ShouldContain("Field Content");
    }

    [Fact]
    public void SwitchField_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<SwitchField> cut = Render<SwitchField>(parameters => parameters
            .Add(p => p.Class, "custom-field"));

        // Assert
        IElement div = cut.Find("div");
        div.ClassList.ShouldContain("custom-field");
        div.ClassList.ShouldContain("grid");
    }
}
