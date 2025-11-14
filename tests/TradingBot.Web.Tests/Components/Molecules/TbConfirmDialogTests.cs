// <copyright file="TbConfirmDialogTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using TradingBot.Web.Components.Molecules;

namespace TradingBot.Web.Tests.Components.Molecules;

/// <summary>
/// Tests for the TbConfirmDialog component.
/// </summary>
public class TbConfirmDialogTests
{
    /// <summary>
    /// Tests that the TbConfirmDialog component renders with default values.
    /// </summary>
    [Fact]
    public void TbConfirmDialog_RendersWithDefaults()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbConfirmDialog>();

        // Assert
        var dialog = cut.Find("[role='dialog']");
        dialog.ShouldNotBeNull();
        dialog.GetAttribute("aria-modal").ShouldBe("true");
    }

    /// <summary>
    /// Tests that the TbConfirmDialog component renders with custom title.
    /// </summary>
    [Fact]
    public void TbConfirmDialog_RendersWithCustomTitle()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbConfirmDialog>(parameters => parameters
            .Add(p => p.Title, "Delete Item"));

        // Assert
        var title = cut.Find("#dialog-title");
        title.TextContent.ShouldBe("Delete Item");
    }

    /// <summary>
    /// Tests that the TbConfirmDialog component renders with message.
    /// </summary>
    [Fact]
    public void TbConfirmDialog_RendersWithMessage()
    {
        // Arrange
        using var ctx = new BunitContext();
        var message = "Are you sure you want to delete this item?";

        // Act
        var cut = ctx.Render<TbConfirmDialog>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var messageElement = cut.Find("p.text-sm");
        messageElement.TextContent.ShouldBe(message);
    }

    /// <summary>
    /// Tests that the TbConfirmDialog component renders with custom button text.
    /// </summary>
    [Fact]
    public void TbConfirmDialog_RendersWithCustomButtonText()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbConfirmDialog>(parameters => parameters
            .Add(p => p.ConfirmButtonText, "Yes, Delete")
            .Add(p => p.CancelButtonText, "No, Keep"));

        // Assert
        var buttons = cut.FindAll("button");
        buttons.Count.ShouldBeGreaterThanOrEqualTo(2);

        var buttonTexts = buttons.Select(b => b.TextContent.Trim()).ToList();
        buttonTexts.ShouldContain("Yes, Delete");
        buttonTexts.ShouldContain("No, Keep");
    }

    /// <summary>
    /// Tests that clicking confirm button invokes callback.
    /// </summary>
    [Fact]
    public void TbConfirmDialog_ClickConfirm_InvokesCallback()
    {
        // Arrange
        using var ctx = new BunitContext();
        var confirmCalled = false;

        var cut = ctx.Render<TbConfirmDialog>(parameters => parameters
            .Add(p => p.ConfirmButtonText, "Confirm")
            .Add(p => p.OnConfirm, () => { confirmCalled = true; return Task.CompletedTask; }));

        // Act
        var buttons = cut.FindAll("button");
        var confirmButton = buttons.First(b => b.TextContent.Trim() == "Confirm");
        confirmButton.Click();

        // Assert
        confirmCalled.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that clicking cancel button invokes callback.
    /// </summary>
    [Fact]
    public void TbConfirmDialog_ClickCancel_InvokesCallback()
    {
        // Arrange
        using var ctx = new BunitContext();
        var cancelCalled = false;

        var cut = ctx.Render<TbConfirmDialog>(parameters => parameters
            .Add(p => p.CancelButtonText, "Cancel")
            .Add(p => p.OnCancel, () => { cancelCalled = true; return Task.CompletedTask; }));

        // Act
        var buttons = cut.FindAll("button");
        var cancelButton = buttons.First(b => b.TextContent.Trim() == "Cancel");
        cancelButton.Click();

        // Assert
        cancelCalled.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that clicking background overlay invokes cancel callback.
    /// </summary>
    [Fact]
    public void TbConfirmDialog_ClickOverlay_InvokesCancelCallback()
    {
        // Arrange
        using var ctx = new BunitContext();
        var cancelCalled = false;

        var cut = ctx.Render<TbConfirmDialog>(parameters => parameters
            .Add(p => p.OnCancel, () => { cancelCalled = true; return Task.CompletedTask; }));

        // Act
        var overlay = cut.Find("div.bg-gray-500.bg-opacity-75");
        overlay.Click();

        // Assert
        cancelCalled.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that dialog renders with danger variant icon.
    /// </summary>
    [Fact]
    public void TbConfirmDialog_DangerVariant_RendersDangerIcon()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbConfirmDialog>(parameters => parameters
            .Add(p => p.Variant, ConfirmDialogVariant.Danger));

        // Assert
        var iconContainer = cut.Find("div.bg-red-100");
        iconContainer.ShouldNotBeNull();
        iconContainer.ClassList.ShouldContain("dark:bg-red-900");
    }

    /// <summary>
    /// Tests that dialog renders with warning variant icon.
    /// </summary>
    [Fact]
    public void TbConfirmDialog_WarningVariant_RendersWarningIcon()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbConfirmDialog>(parameters => parameters
            .Add(p => p.Variant, ConfirmDialogVariant.Warning));

        // Assert
        var iconContainer = cut.Find("div.bg-yellow-100");
        iconContainer.ShouldNotBeNull();
        iconContainer.ClassList.ShouldContain("dark:bg-yellow-900");
    }

    /// <summary>
    /// Tests that dialog renders with info variant icon.
    /// </summary>
    [Fact]
    public void TbConfirmDialog_InfoVariant_RendersInfoIcon()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbConfirmDialog>(parameters => parameters
            .Add(p => p.Variant, ConfirmDialogVariant.Info));

        // Assert
        var iconContainer = cut.Find("div.bg-blue-100");
        iconContainer.ShouldNotBeNull();
        iconContainer.ClassList.ShouldContain("dark:bg-blue-900");
    }

    /// <summary>
    /// Tests that dialog disables buttons when IsConfirming is true.
    /// </summary>
    [Fact]
    public void TbConfirmDialog_IsConfirming_DisablesButtons()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbConfirmDialog>(parameters => parameters
            .Add(p => p.IsConfirming, true));

        // Assert
        var buttons = cut.FindAll("button");
        foreach (var button in buttons)
        {
            button.HasAttribute("disabled").ShouldBeTrue();
        }
    }

    /// <summary>
    /// Tests that dialog does not invoke callbacks when IsConfirming is true.
    /// </summary>
    [Fact]
    public void TbConfirmDialog_IsConfirming_DoesNotInvokeCallbacks()
    {
        // Arrange
        using var ctx = new BunitContext();
        var confirmCalled = false;
        var cancelCalled = false;

        var cut = ctx.Render<TbConfirmDialog>(parameters => parameters
            .Add(p => p.IsConfirming, true)
            .Add(p => p.ConfirmButtonText, "Confirm")
            .Add(p => p.CancelButtonText, "Cancel")
            .Add(p => p.OnConfirm, () => { confirmCalled = true; return Task.CompletedTask; })
            .Add(p => p.OnCancel, () => { cancelCalled = true; return Task.CompletedTask; }));

        // Act
        var buttons = cut.FindAll("button");
        var confirmButton = buttons.First(b => b.TextContent.Trim() == "Confirm");
        var cancelButton = buttons.First(b => b.TextContent.Trim() == "Cancel");

        confirmButton.Click();
        cancelButton.Click();

        // Assert
        confirmCalled.ShouldBeFalse();
        cancelCalled.ShouldBeFalse();
    }
}
