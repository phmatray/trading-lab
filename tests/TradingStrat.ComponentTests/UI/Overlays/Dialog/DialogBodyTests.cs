using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Overlays.Dialog;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Overlays.Dialog;

/// <summary>
/// Tests for the DialogBody component.
/// </summary>
public class DialogBodyTests : BunitTestContext
{
    [Fact]
    public void DialogBody_RendersAsDiv()
    {
        // Arrange & Act
        IRenderedComponent<DialogBody> cut = Render<DialogBody>();

        // Assert
        IElement div = cut.Find("div");
        div.ShouldNotBeNull();
    }

    [Fact]
    public void DialogBody_AppliesMarginTop()
    {
        // Arrange & Act
        IRenderedComponent<DialogBody> cut = Render<DialogBody>();

        // Assert
        cut.Markup.ShouldContain("mt-6");
    }

    [Fact]
    public void DialogBody_WithChildContent_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<DialogBody> cut = Render<DialogBody>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Body Content"))));

        // Assert
        cut.Markup.ShouldContain("Body Content");
    }

    [Fact]
    public void DialogBody_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<DialogBody> cut = Render<DialogBody>(parameters => parameters
            .Add(p => p.Class, "custom-body"));

        // Assert
        IElement div = cut.Find("div");
        div.ClassList.ShouldContain("custom-body");
        div.ClassList.ShouldContain("mt-6");
    }
}
