using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Overlays.Dialog;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Overlays.Dialog;

/// <summary>
/// Tests for the DialogTitle component.
/// </summary>
public class DialogTitleTests : BunitTestContext
{
    [Fact]
    public void DialogTitle_RendersAsH2()
    {
        // Arrange & Act
        IRenderedComponent<DialogTitle> cut = Render<DialogTitle>();

        // Assert
        IElement h2 = cut.Find("h2#dialog-title");
        h2.ShouldNotBeNull();
    }

    [Fact]
    public void DialogTitle_AppliesFontSemibold()
    {
        // Arrange & Act
        IRenderedComponent<DialogTitle> cut = Render<DialogTitle>();

        // Assert
        cut.Markup.ShouldContain("font-semibold");
    }

    [Fact]
    public void DialogTitle_AppliesTextBalance()
    {
        // Arrange & Act
        IRenderedComponent<DialogTitle> cut = Render<DialogTitle>();

        // Assert
        cut.Markup.ShouldContain("text-balance");
    }

    [Fact]
    public void DialogTitle_AppliesResponsiveTextSize()
    {
        // Arrange & Act
        IRenderedComponent<DialogTitle> cut = Render<DialogTitle>();

        // Assert
        cut.Markup.ShouldContain("text-lg/6");
        cut.Markup.ShouldContain("sm:text-base/6");
    }

    [Fact]
    public void DialogTitle_AppliesDarkModeTextColor()
    {
        // Arrange & Act
        IRenderedComponent<DialogTitle> cut = Render<DialogTitle>();

        // Assert
        cut.Markup.ShouldContain("text-zinc-950");
        cut.Markup.ShouldContain("dark:text-white");
    }

    [Fact]
    public void DialogTitle_WithChildContent_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<DialogTitle> cut = Render<DialogTitle>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Delete Account"))));

        // Assert
        cut.Markup.ShouldContain("Delete Account");
    }

    [Fact]
    public void DialogTitle_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<DialogTitle> cut = Render<DialogTitle>(parameters => parameters
            .Add(p => p.Class, "custom-title"));

        // Assert
        IElement h2 = cut.Find("h2");
        h2.ClassList.ShouldContain("custom-title");
        h2.ClassList.ShouldContain("font-semibold");
    }
}
