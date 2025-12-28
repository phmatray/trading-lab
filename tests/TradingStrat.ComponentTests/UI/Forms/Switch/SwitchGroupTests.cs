using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Forms.Switch;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms.Switch;

/// <summary>
/// Tests for the SwitchGroup component.
/// </summary>
public class SwitchGroupTests : BunitTestContext
{
    [Fact]
    public void SwitchGroup_RendersAsDiv()
    {
        // Arrange & Act
        IRenderedComponent<SwitchGroup> cut = Render<SwitchGroup>();

        // Assert
        IElement div = cut.Find("div[data-slot='control']");
        div.ShouldNotBeNull();
    }

    [Fact]
    public void SwitchGroup_AppliesVerticalSpacing()
    {
        // Arrange & Act
        IRenderedComponent<SwitchGroup> cut = Render<SwitchGroup>();

        // Assert
        cut.Markup.ShouldContain("space-y-3");
    }

    [Fact]
    public void SwitchGroup_AppliesLabelFontWeight()
    {
        // Arrange & Act
        IRenderedComponent<SwitchGroup> cut = Render<SwitchGroup>();

        // Assert
        cut.Markup.ShouldContain("**:data-[slot=label]:font-normal");
    }

    [Fact]
    public void SwitchGroup_AppliesDescriptionSpacing()
    {
        // Arrange & Act
        IRenderedComponent<SwitchGroup> cut = Render<SwitchGroup>();

        // Assert
        cut.Markup.ShouldContain("has-[data-slot=description]:space-y-6");
    }

    [Fact]
    public void SwitchGroup_WithChildContent_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<SwitchGroup> cut = Render<SwitchGroup>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Group Content"))));

        // Assert
        cut.Markup.ShouldContain("Group Content");
    }

    [Fact]
    public void SwitchGroup_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<SwitchGroup> cut = Render<SwitchGroup>(parameters => parameters
            .Add(p => p.Class, "custom-group"));

        // Assert
        IElement div = cut.Find("div");
        div.ClassList.ShouldContain("custom-group");
        div.ClassList.ShouldContain("space-y-3");
    }
}
