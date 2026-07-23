using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Forms.Radio;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms.Radio;

/// <summary>
/// Tests for the RadioGroup component.
/// </summary>
public class RadioGroupTests : BunitTestContext
{
    [Fact]
    public void RadioGroup_RendersAsDiv()
    {
        // Arrange & Act
        IRenderedComponent<RadioGroup> cut = Render<RadioGroup>();

        // Assert
        IElement div = cut.Find("div[data-slot='control']");
        div.ShouldNotBeNull();
    }

    [Fact]
    public void RadioGroup_AppliesVerticalSpacing()
    {
        // Arrange & Act
        IRenderedComponent<RadioGroup> cut = Render<RadioGroup>();

        // Assert
        cut.Markup.ShouldContain("space-y-3");
    }

    [Fact]
    public void RadioGroup_AppliesDescriptionSpacing()
    {
        // Arrange & Act
        IRenderedComponent<RadioGroup> cut = Render<RadioGroup>();

        // Assert
        cut.Markup.ShouldContain("has-[data-slot=description]:space-y-6");
    }

    [Fact]
    public void RadioGroup_WithChildContent_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<RadioGroup> cut = Render<RadioGroup>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Group Content"))));

        // Assert
        cut.Markup.ShouldContain("Group Content");
    }

    [Fact]
    public void RadioGroup_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<RadioGroup> cut = Render<RadioGroup>(parameters => parameters
            .Add(p => p.Class, "custom-group"));

        // Assert
        IElement div = cut.Find("div");
        div.ClassList.ShouldContain("custom-group");
        div.ClassList.ShouldContain("space-y-3");
    }
}
