// <copyright file="InputTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using TradingBot.Web.Components.Atoms;

namespace TradingBot.Web.Tests.Components.Atoms;

/// <summary>
/// Tests for the Input component.
/// </summary>
public class InputTests
{
    /// <summary>
    /// Tests that the Input component renders with default values.
    /// </summary>
    [Fact]
    public void Input_RendersWithDefaults()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbInput<string>>();

        // Assert
        var input = cut.Find("input");
        input.ShouldNotBeNull();
        input.GetAttribute("type").ShouldBe("text");
    }

    /// <summary>
    /// Tests that the Input component renders with a string value.
    /// </summary>
    [Fact]
    public void Input_RendersWithStringValue()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbInput<string>>(parameters => parameters
            .Add(p => p.Value, "Test Value"));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("value").ShouldBe("Test Value");
    }

    /// <summary>
    /// Tests that the Input component renders with a number value.
    /// </summary>
    [Fact]
    public void Input_RendersWithNumberValue()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbInput<int>>(parameters => parameters
            .Add(p => p.Type, "number")
            .Add(p => p.Value, 42));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("number");
        input.GetAttribute("value").ShouldBe("42");
    }

    /// <summary>
    /// Tests that the Input component applies error styling when HasError is true.
    /// </summary>
    [Fact]
    public void Input_AppliesErrorStyling_WhenHasErrorIsTrue()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbInput<string>>(parameters => parameters
            .Add(p => p.HasError, true));

        // Assert
        var input = cut.Find("input");
        input.ClassList.ShouldContain("border-red-300");
        input.GetAttribute("aria-invalid").ShouldBe("true");
    }

    /// <summary>
    /// Tests that the Input component applies normal styling when HasError is false.
    /// </summary>
    [Fact]
    public void Input_AppliesNormalStyling_WhenHasErrorIsFalse()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbInput<string>>(parameters => parameters
            .Add(p => p.HasError, false));

        // Assert
        var input = cut.Find("input");
        input.ClassList.ShouldContain("border-gray-300");
        input.GetAttribute("aria-invalid").ShouldBeNull();
    }

    /// <summary>
    /// Tests that the Input component renders with placeholder text.
    /// </summary>
    [Fact]
    public void Input_RendersWithPlaceholder()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbInput<string>>(parameters => parameters
            .Add(p => p.Placeholder, "Enter text here"));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("placeholder").ShouldBe("Enter text here");
    }

    /// <summary>
    /// Tests that the Input component is disabled when IsDisabled is true.
    /// </summary>
    [Fact]
    public void Input_IsDisabled_WhenIsDisabledIsTrue()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbInput<string>>(parameters => parameters
            .Add(p => p.IsDisabled, true));

        // Assert
        var input = cut.Find("input");
        input.HasAttribute("disabled").ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the Input component is readonly when IsReadonly is true.
    /// </summary>
    [Fact]
    public void Input_IsReadonly_WhenIsReadonlyIsTrue()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbInput<string>>(parameters => parameters
            .Add(p => p.IsReadonly, true));

        // Assert
        var input = cut.Find("input");
        input.HasAttribute("readonly").ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the Input component triggers ValueChanged on input change.
    /// </summary>
    [Fact]
    public void Input_TriggersValueChanged_OnInputChange()
    {
        // Arrange
        using var ctx = new BunitContext();
        var newValue = string.Empty;
        var cut = ctx.Render<TbInput<string>>(parameters => parameters
            .Add(p => p.Value, "Initial")
            .Add(p => p.ValueChanged, value => newValue = value));

        var input = cut.Find("input");

        // Act
        input.Change("Updated");

        // Assert
        newValue.ShouldBe("Updated");
    }

    /// <summary>
    /// Tests that the Input component renders with min and max attributes for numbers.
    /// </summary>
    [Fact]
    public void Input_RendersWithMinMaxAttributes_ForNumberInputs()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbInput<int>>(parameters => parameters
            .Add(p => p.Type, "number")
            .Add(p => p.Min, "1")
            .Add(p => p.Max, "100")
            .Add(p => p.Step, "5"));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("min").ShouldBe("1");
        input.GetAttribute("max").ShouldBe("100");
        input.GetAttribute("step").ShouldBe("5");
    }

    /// <summary>
    /// Tests that the Input component applies custom CSS classes.
    /// </summary>
    [Fact]
    public void Input_AppliesCustomCssClasses()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbInput<string>>(parameters => parameters
            .Add(p => p.Class, "custom-class"));

        // Assert
        var input = cut.Find("input");
        input.ClassList.ShouldContain("custom-class");
    }
}
