using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the Dialog component.
/// </summary>
public class DialogTests : BunitTestContext
{
    [Fact]
    public void Dialog_WhenClosed_RendersNothing()
    {
        // Arrange & Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, false)
            .Add(p => p.Title, "Test Dialog"));

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact]
    public void Dialog_WhenOpen_RendersDialog()
    {
        // Arrange & Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Test Dialog"));

        // Assert
        var dialog = cut.Find("[data-testid='dialog']");
        dialog.ShouldNotBeNull();
        dialog.GetAttribute("role").ShouldBe("dialog");
        dialog.GetAttribute("aria-modal").ShouldBe("true");
    }

    [Fact]
    public void Dialog_DisplaysTitle()
    {
        // Arrange
        string title = "Confirm Action";

        // Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, title));

        // Assert
        var titleElement = cut.Find("#dialog-title");
        titleElement.TextContent.ShouldBe(title);
        titleElement.ClassList.ShouldContain("text-lg");
        titleElement.ClassList.ShouldContain("font-medium");
    }

    [Fact]
    public void Dialog_WithChildContent_RendersContent()
    {
        // Arrange & Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Dialog")
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddAttribute(1, "class", "test-content");
                builder.AddContent(2, "Dialog content here");
                builder.CloseElement();
            }));

        // Assert
        var content = cut.Find("p.test-content");
        content.ShouldNotBeNull();
        content.TextContent.ShouldBe("Dialog content here");
    }

    [Fact]
    public void Dialog_WithFooter_RendersFooter()
    {
        // Arrange & Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Dialog")
            .Add(p => p.Footer, builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "test-footer-btn");
                builder.AddContent(2, "OK");
                builder.CloseElement();
            }));

        // Assert
        var footer = cut.Find("div.dialog-footer");
        footer.ShouldNotBeNull();

        var button = cut.Find("button.test-footer-btn");
        button.ShouldNotBeNull();
        button.TextContent.ShouldBe("OK");
    }

    [Fact]
    public void Dialog_WithoutFooter_DoesNotRenderFooter()
    {
        // Arrange & Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Dialog"));

        // Assert
        var footers = cut.FindAll("div.dialog-footer");
        footers.ShouldBeEmpty();
    }

    [Fact]
    public void Dialog_CloseButton_InvokesOnClose()
    {
        // Arrange
        bool closeCalled = false;

        // Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Dialog")
            .Add(p => p.OnClose, () => closeCalled = true));

        var closeButton = cut.Find("button[aria-label='Close dialog']");
        closeButton.Click();

        // Assert
        closeCalled.ShouldBeTrue();
    }

    [Fact]
    public void Dialog_BackdropClick_InvokesOnCloseWhenEnabled()
    {
        // Arrange
        bool closeCalled = false;

        // Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Dialog")
            .Add(p => p.CloseOnBackdropClick, true)
            .Add(p => p.OnClose, () => closeCalled = true));

        var backdrop = cut.Find("[data-testid='dialog']");
        backdrop.Click();

        // Assert
        closeCalled.ShouldBeTrue();
    }

    [Fact]
    public void Dialog_BackdropClick_DoesNotCloseWhenDisabled()
    {
        // Arrange
        bool closeCalled = false;

        // Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Dialog")
            .Add(p => p.CloseOnBackdropClick, false)
            .Add(p => p.OnClose, () => closeCalled = true));

        var backdrop = cut.Find("[data-testid='dialog']");
        backdrop.Click();

        // Assert
        closeCalled.ShouldBeFalse();
    }

    [Fact]
    public void Dialog_DialogContent_HasStopPropagation()
    {
        // Arrange & Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Dialog")
            .Add(p => p.CloseOnBackdropClick, true));

        var content = cut.Find("[data-testid='dialog-content']");

        // Assert - Verify that the dialog content has onclick:stoppropagation
        // This prevents clicks on the dialog content from bubbling to the backdrop
        content.HasAttribute("blazor:onclick:stoppropagation").ShouldBeTrue();
    }

    [Fact]
    public void Dialog_WithCustomWidth_AppliesWidthClass()
    {
        // Arrange & Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Dialog")
            .Add(p => p.Width, "w-[600px]"));

        // Assert
        var dialogContent = cut.Find("[data-testid='dialog-content']");
        dialogContent.ClassList.ShouldContain("w-[600px]");
    }

    [Fact]
    public void Dialog_WithHeaderActions_ReplacesDefaultCloseButton()
    {
        // Arrange & Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Dialog")
            .Add(p => p.HeaderActions, builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "custom-header-btn");
                builder.AddContent(2, "Custom Action");
                builder.CloseElement();
            }));

        // Assert
        var customButton = cut.Find("button.custom-header-btn");
        customButton.ShouldNotBeNull();
        customButton.TextContent.ShouldBe("Custom Action");

        // Default close button should not exist
        var defaultCloseButtons = cut.FindAll("button[aria-label='Close dialog']");
        defaultCloseButtons.ShouldBeEmpty();
    }

    [Fact]
    public void Dialog_HasAccessibilityAttributes()
    {
        // Arrange & Act
        var cut = Render<Dialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Accessible Dialog"));

        // Assert
        var dialog = cut.Find("[role='dialog']");
        dialog.ShouldNotBeNull();
        dialog.GetAttribute("aria-modal").ShouldBe("true");
        dialog.GetAttribute("aria-labelledby").ShouldBe("dialog-title");
    }
}
