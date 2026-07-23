using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Overlays.Dialog;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Overlays.Dialog;

/// <summary>
/// Tests for the DialogDescription component.
/// </summary>
public class DialogDescriptionTests : BunitTestContext
{
    [Fact]
    public void DialogDescription_RendersAsText()
    {
        // Arrange & Act
        IRenderedComponent<DialogDescription> cut = Render<DialogDescription>();

        // Assert
        cut.Find("p").ShouldNotBeNull(); // Text component renders as <p>
    }

    [Fact]
    public void DialogDescription_AppliesMarginTop()
    {
        // Arrange & Act
        IRenderedComponent<DialogDescription> cut = Render<DialogDescription>();

        // Assert
        cut.Markup.ShouldContain("mt-2");
    }

    [Fact]
    public void DialogDescription_AppliesTextPretty()
    {
        // Arrange & Act
        IRenderedComponent<DialogDescription> cut = Render<DialogDescription>();

        // Assert
        cut.Markup.ShouldContain("text-pretty");
    }

    [Fact]
    public void DialogDescription_WithChildContent_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<DialogDescription> cut = Render<DialogDescription>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "This action cannot be undone"))));

        // Assert
        cut.Markup.ShouldContain("This action cannot be undone");
    }

    [Fact]
    public void DialogDescription_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<DialogDescription> cut = Render<DialogDescription>(parameters => parameters
            .Add(p => p.Class, "custom-description"));

        // Assert
        cut.Markup.ShouldContain("custom-description");
        cut.Markup.ShouldContain("mt-2");
    }
}
