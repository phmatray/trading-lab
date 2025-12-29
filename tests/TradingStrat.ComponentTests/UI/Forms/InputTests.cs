using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Forms;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms;

/// <summary>
/// Tests for the Input component.
/// </summary>
public class InputTests : BunitTestContext
{
    [Fact]
    public void Input_RendersAsInputElement()
    {
        // Arrange & Act
        IRenderedComponent<Input> cut = Render<Input>();

        // Assert
        IElement input = cut.Find("input");
        input.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("text")]
    [InlineData("number")]
    [InlineData("email")]
    [InlineData("password")]
    [InlineData("date")]
    public void Input_WithDifferentTypes_AppliesCorrectTypeAttribute(string type)
    {
        // Arrange & Act
        IRenderedComponent<Input> cut = Render<Input>(parameters => parameters
            .Add(p => p.Type, type));

        // Assert
        IElement input = cut.Find("input");
        input.GetAttribute("type").ShouldBe(type);
    }

    [Fact]
    public void Input_DefaultsToTextType()
    {
        // Arrange & Act
        IRenderedComponent<Input> cut = Render<Input>();

        // Assert
        IElement input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("text");
    }

    [Fact]
    public void Input_WithValue_DisplaysValue()
    {
        // Arrange & Act
        IRenderedComponent<Input> cut = Render<Input>(parameters => parameters
            .Add(p => p.Value, "Test Value"));

        // Assert
        IElement input = cut.Find("input");
        input.GetAttribute("value").ShouldBe("Test Value");
    }

    [Fact]
    public void Input_WithPlaceholder_AppliesPlaceholderAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Input> cut = Render<Input>(parameters => parameters
            .Add(p => p.Placeholder, "Enter text..."));

        // Assert
        IElement input = cut.Find("input");
        input.GetAttribute("placeholder").ShouldBe("Enter text...");
    }

    [Fact]
    public void Input_WithDisabled_AppliesDisabledAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Input> cut = Render<Input>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        IElement input = cut.Find("input");
        input.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Input_WithInvalidTrue_AppliesInvalidClass()
    {
        // Arrange & Act
        IRenderedComponent<Input> cut = Render<Input>(parameters => parameters
            .Add(p => p.Invalid, true));

        // Assert
        cut.Markup.ShouldContain("border-red-500");
        cut.Markup.ShouldContain("focus:ring-red-500");
    }

    [Fact]
    public void Input_WithInvalidFalse_AppliesDefaultBorderClass()
    {
        // Arrange & Act
        IRenderedComponent<Input> cut = Render<Input>(parameters => parameters
            .Add(p => p.Invalid, false));

        // Assert
        cut.Markup.ShouldContain("border-zinc-950/10");
    }

    [Fact]
    public void Input_AppliesBaseClasses()
    {
        // Arrange & Act
        IRenderedComponent<Input> cut = Render<Input>();

        // Assert
        cut.Markup.ShouldContain("block");
        cut.Markup.ShouldContain("w-full");
        cut.Markup.ShouldContain("rounded-lg");
        cut.Markup.ShouldContain("border");
    }

    [Fact]
    public void Input_AppliesDarkModeClasses()
    {
        // Arrange & Act
        IRenderedComponent<Input> cut = Render<Input>();

        // Assert
        cut.Markup.ShouldContain("dark:bg-white/5");
        cut.Markup.ShouldContain("dark:text-white");
    }

    [Fact]
    public void Input_AppliesFocusClasses()
    {
        // Arrange & Act
        IRenderedComponent<Input> cut = Render<Input>();

        // Assert
        cut.Markup.ShouldContain("focus:outline");
        cut.Markup.ShouldContain("focus:outline-2");
        cut.Markup.ShouldContain("focus:-outline-offset-1");
        cut.Markup.ShouldContain("focus:outline-blue-500");
    }

    [Fact]
    public void Input_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Input> cut = Render<Input>(parameters => parameters
            .Add(p => p.Class, "custom-input"));

        // Assert
        IElement input = cut.Find("input");
        input.ClassList.ShouldContain("custom-input");
        input.ClassList.ShouldContain("block");
    }

    [Fact]
    public void Input_OnInput_TriggersValueChanged()
    {
        // Arrange
        string? newValue = null;
        EventCallback<string?> callback = EventCallback.Factory.Create(
            this,
            (string? value) => newValue = value);

        IRenderedComponent<Input> cut = Render<Input>(parameters => parameters
            .Add(p => p.ValueChanged, callback));

        // Act
        IElement input = cut.Find("input");
        input.Input("New Value");

        // Assert
        newValue.ShouldBe("New Value");
    }
}
