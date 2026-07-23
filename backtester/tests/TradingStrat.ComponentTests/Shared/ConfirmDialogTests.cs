using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;
using static TradingStrat.Web.Components.Shared.ConfirmDialog;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the ConfirmDialog component.
/// </summary>
public class ConfirmDialogTests : BunitTestContext
{
    [Fact]
    public void ConfirmDialog_WhenClosed_RendersNothing()
    {
        // Arrange & Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, false));

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact]
    public void ConfirmDialog_WhenOpen_RendersDialog()
    {
        // Arrange & Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Confirm Delete")
            .Add(p => p.Message, "Are you sure?"));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        cut.Markup.ShouldContain("Confirm Delete");
        cut.Markup.ShouldContain("Are you sure?");
    }

    [Fact]
    public void ConfirmDialog_DisplaysMessage()
    {
        // Arrange
        string message = "This action cannot be undone. Are you sure you want to delete this portfolio?";

        // Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.ShouldContain(message);
    }

    [Theory]
    [InlineData(ConfirmType.Danger)]
    [InlineData(ConfirmType.Warning)]
    [InlineData(ConfirmType.Info)]
    public void ConfirmDialog_WithDifferentTypes_DisplaysIcon(ConfirmType type)
    {
        // Arrange & Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Type, type));

        // Assert - Find the icon (h-12 w-12), not the close button (w-6 h-6)
        IReadOnlyList<IElement> svgs = cut.FindAll("svg");
        IElement? iconSvg = svgs.FirstOrDefault(svg => svg.ClassList.Contains("h-12"));
        iconSvg.ShouldNotBeNull();
        iconSvg.ClassList.ShouldContain("w-12");
    }

    [Fact]
    public void ConfirmDialog_DangerType_AppliesRedStyling()
    {
        // Arrange & Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Type, ConfirmType.Danger));

        // Assert - Find the icon (h-12 w-12), not the close button
        IReadOnlyList<IElement> svgs = cut.FindAll("svg");
        IElement? icon = svgs.FirstOrDefault(svg => svg.ClassList.Contains("h-12"));
        icon.ShouldNotBeNull();
        icon.ClassList.ShouldContain("text-red-600");

        IElement confirmButton = cut.FindAll("button").Last();
        string? buttonClass = confirmButton.GetAttribute("class");
        buttonClass.ShouldNotBeNull();
        buttonClass.ShouldContain("bg-red-600");
    }

    [Fact]
    public void ConfirmDialog_WarningType_AppliesYellowStyling()
    {
        // Arrange & Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Type, ConfirmType.Warning));

        // Assert - Find the icon (h-12 w-12), not the close button
        IReadOnlyList<IElement> svgs = cut.FindAll("svg");
        IElement? icon = svgs.FirstOrDefault(svg => svg.ClassList.Contains("h-12"));
        icon.ShouldNotBeNull();
        icon.ClassList.ShouldContain("text-yellow-600");

        IElement confirmButton = cut.FindAll("button").Last();
        string? buttonClass = confirmButton.GetAttribute("class");
        buttonClass.ShouldNotBeNull();
        buttonClass.ShouldContain("bg-yellow-600");
    }

    [Fact]
    public void ConfirmDialog_InfoType_AppliesBlueStyling()
    {
        // Arrange & Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Type, ConfirmType.Info));

        // Assert - Find the icon (h-12 w-12), not the close button
        IReadOnlyList<IElement> svgs = cut.FindAll("svg");
        IElement? icon = svgs.FirstOrDefault(svg => svg.ClassList.Contains("h-12"));
        icon.ShouldNotBeNull();
        icon.ClassList.ShouldContain("text-blue-600");

        IElement confirmButton = cut.FindAll("button").Last();
        string? buttonClass = confirmButton.GetAttribute("class");
        buttonClass.ShouldNotBeNull();
        buttonClass.ShouldContain("bg-blue-600");
    }

    [Fact]
    public void ConfirmDialog_ConfirmButton_InvokesOnConfirm()
    {
        // Arrange
        bool confirmCalled = false;

        // Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnConfirm, () => confirmCalled = true));

        IReadOnlyList<IElement> buttons = cut.FindAll("button");
        IElement confirmButton = buttons.Last();
        confirmButton.Click();

        // Assert
        confirmCalled.ShouldBeTrue();
    }

    [Fact]
    public void ConfirmDialog_CancelButton_InvokesOnCancel()
    {
        // Arrange
        bool cancelCalled = false;

        // Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnCancel, () => cancelCalled = true));

        IReadOnlyList<IElement> buttons = cut.FindAll("button");
        IElement cancelButton = buttons.First();
        cancelButton.Click();

        // Assert
        cancelCalled.ShouldBeTrue();
    }

    [Fact]
    public void ConfirmDialog_WithCustomButtonText_DisplaysCustomText()
    {
        // Arrange & Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.ConfirmText, "Delete")
            .Add(p => p.CancelText, "Keep"));

        // Assert
        cut.Markup.ShouldContain("Delete");
        cut.Markup.ShouldContain("Keep");
    }

    [Fact]
    public void ConfirmDialog_WhenProcessing_DisplaysProcessingText()
    {
        // Arrange & Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsProcessing, true)
            .Add(p => p.ProcessingText, "Deleting..."));

        // Assert
        cut.Markup.ShouldContain("Deleting...");
    }

    [Fact]
    public void ConfirmDialog_WhenProcessing_DisablesButtons()
    {
        // Arrange & Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsProcessing, true));

        // Assert - Check only the confirm/cancel buttons in the footer, not the close button
        IElement footer = cut.Find(".dialog-footer");
        IHtmlCollection<IElement> buttons = footer.QuerySelectorAll("button");
        foreach (IElement button in buttons)
        {
            button.HasAttribute("disabled").ShouldBeTrue();
            button.ClassList.ShouldContain("disabled:opacity-50");
        }
    }

    [Fact]
    public void ConfirmDialog_WhenProcessing_DoesNotInvokeCallbacks()
    {
        // Arrange
        bool confirmCalled = false;
        bool cancelCalled = false;

        // Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.IsProcessing, true)
            .Add(p => p.OnConfirm, () => confirmCalled = true)
            .Add(p => p.OnCancel, () => cancelCalled = true));

        IReadOnlyList<IElement> buttons = cut.FindAll("button");
        buttons.First().Click(); // Cancel button
        buttons.Last().Click();  // Confirm button

        // Assert
        confirmCalled.ShouldBeFalse();
        cancelCalled.ShouldBeFalse();
    }

    [Fact]
    public void ConfirmDialog_WithCustomWidth_AppliesWidth()
    {
        // Arrange & Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Width, "w-[500px]"));

        // Assert
        // Width is passed to the underlying Dialog component
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact]
    public void ConfirmDialog_DefaultValues_AreApplied()
    {
        // Arrange & Act
        IRenderedComponent<ConfirmDialog> cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Markup.ShouldContain("Confirm Action"); // Default title
        cut.Markup.ShouldContain("Are you sure you want to proceed?"); // Default message
        cut.Markup.ShouldContain("Confirm"); // Default confirm text
        cut.Markup.ShouldContain("Cancel");  // Default cancel text
    }
}
