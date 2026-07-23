using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI;
using TradingStrat.Web.Components.UI.DataDisplay;
using Xunit;

namespace TradingStrat.ComponentTests.UI.DataDisplay;

/// <summary>
/// Tests for the Alert component.
/// </summary>
public class AlertTests : BunitTestContext
{
    [Fact]
    public void Alert_WhenIsOpenTrue_RendersContent()
    {
        // Arrange & Act
        IRenderedComponent<Alert> cut = Render<Alert>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Alert content"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        cut.Markup.ShouldContain("Alert content");
    }

    [Fact]
    public void Alert_WhenIsOpenFalse_RendersNothing()
    {
        // Arrange & Act
        IRenderedComponent<Alert> cut = Render<Alert>(parameters => parameters
            .Add(p => p.IsOpen, false)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Hidden"))));

        // Assert
        // When closed, should not render content
        cut.Markup.ShouldNotContain("Hidden");
    }

    [Fact]
    public void Alert_RendersBackdrop()
    {
        // Arrange & Act
        IRenderedComponent<Alert> cut = Render<Alert>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Test"))));

        // Assert
        IElement backdrop = cut.Find(".dialog-backdrop");
        backdrop.ShouldNotBeNull();
    }

    [Fact]
    public void Alert_RendersDialogPanel()
    {
        // Arrange & Act
        IRenderedComponent<Alert> cut = Render<Alert>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Panel"))));

        // Assert
        IElement panel = cut.Find(".dialog-panel");
        panel.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(AlertSize.XS)]
    [InlineData(AlertSize.Small)]
    [InlineData(AlertSize.Medium)]
    [InlineData(AlertSize.Large)]
    [InlineData(AlertSize.XL)]
    public void Alert_WithDifferentSizes_AppliesCorrectSizeClass(AlertSize size)
    {
        // Arrange & Act
        IRenderedComponent<Alert> cut = Render<Alert>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Size, size)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Sized"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        // Size classes are applied to the panel
        IElement panel = cut.Find(".dialog-panel");
        panel.ShouldNotBeNull();
    }

    [Fact]
    public void Alert_DefaultsToMediumSize()
    {
        // Arrange & Act
        IRenderedComponent<Alert> cut = Render<Alert>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Default"))));

        // Assert
        // Medium size class should be applied
        cut.Markup.ShouldContain("sm:max-w-md");
    }

    [Fact]
    public void Alert_BackdropClick_InvokesOnClose()
    {
        // Arrange
        bool closeCalled = false;
        EventCallback onClose = EventCallback.Factory.Create(this, () => closeCalled = true);

        IRenderedComponent<Alert> cut = Render<Alert>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnClose, onClose)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Close Test"))));

        // Act
        IElement backdrop = cut.Find(".dialog-backdrop");
        backdrop.Click();

        // Assert
        closeCalled.ShouldBeTrue();
    }

    [Fact]
    public void Alert_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Alert> cut = Render<Alert>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Class, "custom-alert")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Custom"))));

        // Assert
        IElement panel = cut.Find(".dialog-panel");
        panel.ClassList.ShouldContain("custom-alert");
    }

    [Fact]
    public void Alert_AppliesDataClosedAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Alert> cut = Render<Alert>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Data"))));

        // Assert
        IElement backdrop = cut.Find(".dialog-backdrop");
        backdrop.HasAttribute("data-closed").ShouldBeTrue();
        IElement panel = cut.Find(".dialog-panel");
        panel.HasAttribute("data-closed").ShouldBeTrue();
    }
}
