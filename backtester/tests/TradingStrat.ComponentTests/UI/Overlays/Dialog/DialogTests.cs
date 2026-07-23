using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Overlays.Dialog;

/// <summary>
/// Tests for the Dialog component.
/// </summary>
public class DialogTests : BunitTestContext
{
    [Fact]
    public void Dialog_WithIsOpenFalse_DoesNotRender()
    {
        // Arrange
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);

        // Act
        IRenderedComponent<Web.Components.UI.Overlays.Dialog.Dialog> cut = Render<Web.Components.UI.Overlays.Dialog.Dialog>(parameters => parameters
            .Add(p => p.IsOpen, false));

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact]
    public void Dialog_WithIsOpenTrue_RendersDialogContainer()
    {
        // Arrange
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);

        // Act
        IRenderedComponent<Web.Components.UI.Overlays.Dialog.Dialog> cut = Render<Web.Components.UI.Overlays.Dialog.Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Find("div.dialog-container").ShouldNotBeNull();
    }

    [Fact]
    public void Dialog_WithIsOpenTrue_RendersBackdrop()
    {
        // Arrange
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);

        // Act
        IRenderedComponent<Web.Components.UI.Overlays.Dialog.Dialog> cut = Render<Web.Components.UI.Overlays.Dialog.Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        IElement backdrop = cut.Find("div[data-state='open']");
        backdrop.ShouldNotBeNull();
        backdrop.ClassList.ShouldContain("bg-zinc-950/25");
    }

    [Fact]
    public void Dialog_WithIsOpenTrue_RendersDialogPanel()
    {
        // Arrange
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);

        // Act
        IRenderedComponent<Web.Components.UI.Overlays.Dialog.Dialog> cut = Render<Web.Components.UI.Overlays.Dialog.Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        IElement panel = cut.Find("div[role='dialog']");
        panel.ShouldNotBeNull();
        panel.GetAttribute("aria-modal").ShouldBe("true");
    }

    [Theory]
    [InlineData(DialogSize.XS, "sm:max-w-xs")]
    [InlineData(DialogSize.Small, "sm:max-w-sm")]
    [InlineData(DialogSize.Medium, "sm:max-w-md")]
    [InlineData(DialogSize.Large, "sm:max-w-lg")]
    [InlineData(DialogSize.XL, "sm:max-w-xl")]
    public void Dialog_WithDifferentSizes_AppliesCorrectSizeClass(DialogSize size, string expectedClass)
    {
        // Arrange
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);

        // Act
        IRenderedComponent<Web.Components.UI.Overlays.Dialog.Dialog> cut = Render<Web.Components.UI.Overlays.Dialog.Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Size, size));

        // Assert
        cut.Markup.ShouldContain(expectedClass);
    }

    [Fact]
    public void Dialog_DefaultsToLargeSize()
    {
        // Arrange
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);

        // Act
        IRenderedComponent<Web.Components.UI.Overlays.Dialog.Dialog> cut = Render<Web.Components.UI.Overlays.Dialog.Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Markup.ShouldContain("sm:max-w-lg");
    }

    [Fact]
    public void Dialog_AppliesRoundedStyles()
    {
        // Arrange
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);

        // Act
        IRenderedComponent<Web.Components.UI.Overlays.Dialog.Dialog> cut = Render<Web.Components.UI.Overlays.Dialog.Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Markup.ShouldContain("rounded-t-3xl");
        cut.Markup.ShouldContain("sm:rounded-2xl");
    }

    [Fact]
    public void Dialog_AppliesBackgroundStyles()
    {
        // Arrange
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);

        // Act
        IRenderedComponent<Web.Components.UI.Overlays.Dialog.Dialog> cut = Render<Web.Components.UI.Overlays.Dialog.Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        IElement panel = cut.Find("div[role='dialog']");
        panel.ClassList.ShouldContain("bg-white");
        panel.ClassList.ShouldContain("dark:bg-zinc-900");
    }

    [Fact]
    public void Dialog_AppliesShadowAndRing()
    {
        // Arrange
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);

        // Act
        IRenderedComponent<Web.Components.UI.Overlays.Dialog.Dialog> cut = Render<Web.Components.UI.Overlays.Dialog.Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        IElement panel = cut.Find("div[role='dialog']");
        panel.ClassList.ShouldContain("shadow-lg");
        panel.ClassList.ShouldContain("ring-1");
    }

    [Fact]
    public void Dialog_WithChildContent_RendersContent()
    {
        // Arrange
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);

        // Act
        IRenderedComponent<Web.Components.UI.Overlays.Dialog.Dialog> cut = Render<Web.Components.UI.Overlays.Dialog.Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Dialog Content"))));

        // Assert
        cut.Markup.ShouldContain("Dialog Content");
    }

    [Fact]
    public void Dialog_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);

        // Act
        IRenderedComponent<Web.Components.UI.Overlays.Dialog.Dialog> cut = Render<Web.Components.UI.Overlays.Dialog.Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Class, "custom-dialog"));

        // Assert
        IElement panel = cut.Find("div[role='dialog']");
        panel.ClassList.ShouldContain("custom-dialog");
    }
}
