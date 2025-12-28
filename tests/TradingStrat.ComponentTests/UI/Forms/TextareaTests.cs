using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Forms;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms;

/// <summary>
/// Tests for the Textarea component.
/// </summary>
public class TextareaTests : BunitTestContext
{
    [Fact]
    public void Textarea_RendersAsTextareaElement()
    {
        // Arrange & Act
        IRenderedComponent<Textarea> cut = Render<Textarea>();

        // Assert
        IElement textarea = cut.Find("textarea");
        textarea.ShouldNotBeNull();
    }

    [Fact]
    public void Textarea_WithValue_DisplaysValue()
    {
        // Arrange & Act
        IRenderedComponent<Textarea> cut = Render<Textarea>(parameters => parameters
            .Add(p => p.Value, "Test content"));

        // Assert
        IElement textarea = cut.Find("textarea");
        textarea.GetAttribute("value").ShouldBe("Test content");
    }

    [Fact]
    public void Textarea_WithPlaceholder_AppliesPlaceholderAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Textarea> cut = Render<Textarea>(parameters => parameters
            .Add(p => p.Placeholder, "Enter description..."));

        // Assert
        IElement textarea = cut.Find("textarea");
        textarea.GetAttribute("placeholder").ShouldBe("Enter description...");
    }

    [Fact]
    public void Textarea_WithDisabled_AppliesDisabledAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Textarea> cut = Render<Textarea>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        IElement textarea = cut.Find("textarea");
        textarea.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Textarea_WithResizableFalse_AppliesResizeNoneClass()
    {
        // Arrange & Act
        IRenderedComponent<Textarea> cut = Render<Textarea>(parameters => parameters
            .Add(p => p.Resizable, false));

        // Assert
        cut.Markup.ShouldContain("resize-none");
    }

    [Fact]
    public void Textarea_WithResizableTrue_DoesNotApplyResizeNoneClass()
    {
        // Arrange & Act
        IRenderedComponent<Textarea> cut = Render<Textarea>(parameters => parameters
            .Add(p => p.Resizable, true));

        // Assert
        cut.Markup.ShouldNotContain("resize-none");
    }

    [Fact]
    public void Textarea_DefaultsToNonResizable()
    {
        // Arrange & Act
        IRenderedComponent<Textarea> cut = Render<Textarea>();

        // Assert
        cut.Markup.ShouldContain("resize-none");
    }

    [Fact]
    public void Textarea_WithInvalidTrue_AppliesInvalidClass()
    {
        // Arrange & Act
        IRenderedComponent<Textarea> cut = Render<Textarea>(parameters => parameters
            .Add(p => p.Invalid, true));

        // Assert
        cut.Markup.ShouldContain("border-red-500");
        cut.Markup.ShouldContain("focus:ring-red-500");
    }

    [Fact]
    public void Textarea_AppliesBaseClasses()
    {
        // Arrange & Act
        IRenderedComponent<Textarea> cut = Render<Textarea>();

        // Assert
        cut.Markup.ShouldContain("block");
        cut.Markup.ShouldContain("w-full");
        cut.Markup.ShouldContain("rounded-lg");
        cut.Markup.ShouldContain("border");
    }

    [Fact]
    public void Textarea_AppliesDarkModeClasses()
    {
        // Arrange & Act
        IRenderedComponent<Textarea> cut = Render<Textarea>();

        // Assert
        cut.Markup.ShouldContain("dark:bg-white/5");
        cut.Markup.ShouldContain("dark:text-white");
        cut.Markup.ShouldContain("dark:placeholder:text-zinc-400");
    }

    [Fact]
    public void Textarea_AppliesFocusClasses()
    {
        // Arrange & Act
        IRenderedComponent<Textarea> cut = Render<Textarea>();

        // Assert
        cut.Markup.ShouldContain("focus:outline");
        cut.Markup.ShouldContain("focus:outline-2");
        cut.Markup.ShouldContain("focus:-outline-offset-1");
        cut.Markup.ShouldContain("focus:outline-blue-500");
    }

    [Fact]
    public void Textarea_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Textarea> cut = Render<Textarea>(parameters => parameters
            .Add(p => p.Class, "custom-textarea"));

        // Assert
        IElement textarea = cut.Find("textarea");
        textarea.ClassList.ShouldContain("custom-textarea");
        textarea.ClassList.ShouldContain("block");
    }

    [Fact]
    public void Textarea_OnInput_TriggersValueChanged()
    {
        // Arrange
        string? newValue = null;
        EventCallback<string?> callback = EventCallback.Factory.Create<string?>(
            this,
            (string? value) => newValue = value);

        IRenderedComponent<Textarea> cut = Render<Textarea>(parameters => parameters
            .Add(p => p.ValueChanged, callback));

        // Act
        IElement textarea = cut.Find("textarea");
        textarea.Input("Updated content");

        // Assert
        newValue.ShouldBe("Updated content");
    }
}
