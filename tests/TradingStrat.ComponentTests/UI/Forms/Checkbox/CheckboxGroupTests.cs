using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Forms.Checkbox;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms.Checkbox;

/// <summary>
/// Tests for the CheckboxGroup component.
/// </summary>
public class CheckboxGroupTests : BunitTestContext
{
    [Fact]
    public void CheckboxGroup_RendersAsDiv()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxGroup> cut = Render<CheckboxGroup>();

        // Assert
        IElement div = cut.Find("div[data-slot='control']");
        div.ShouldNotBeNull();
    }

    [Fact]
    public void CheckboxGroup_AppliesVerticalSpacing()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxGroup> cut = Render<CheckboxGroup>();

        // Assert
        cut.Markup.ShouldContain("space-y-3");
    }

    [Fact]
    public void CheckboxGroup_AppliesDescriptionSpacing()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxGroup> cut = Render<CheckboxGroup>();

        // Assert
        cut.Markup.ShouldContain("has-[data-slot=description]:space-y-6");
    }

    [Fact]
    public void CheckboxGroup_WithChildContent_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxGroup> cut = Render<CheckboxGroup>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Group Content"))));

        // Assert
        cut.Markup.ShouldContain("Group Content");
    }

    [Fact]
    public void CheckboxGroup_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<CheckboxGroup> cut = Render<CheckboxGroup>(parameters => parameters
            .Add(p => p.Class, "custom-group"));

        // Assert
        IElement div = cut.Find("div");
        div.ClassList.ShouldContain("custom-group");
        div.ClassList.ShouldContain("space-y-3");
    }
}
