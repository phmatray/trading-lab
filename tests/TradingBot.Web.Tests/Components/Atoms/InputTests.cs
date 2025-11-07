// <copyright file="InputTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Tests.Components.Atoms;

using Bunit;
using TradingBot.Web.Components.Atoms;
using Xunit;

/// <summary>
/// Tests for the Input component.
/// </summary>
public class InputTests : Bunit.TestContext
{
    /// <summary>
    /// Tests that the Input component renders with default values.
    /// </summary>
    [Fact]
    public void Input_RendersWithDefaults()
    {
        // Arrange & Act
        var cut = RenderComponent<Input<string>>();

        // Assert
        var input = cut.Find("input");
        input.Should().NotBeNull();
        input.GetAttribute("type").Should().Be("text");
    }

    /// <summary>
    /// Tests that the Input component renders with a string value.
    /// </summary>
    [Fact]
    public void Input_RendersWithStringValue()
    {
        // Arrange & Act
        var cut = RenderComponent<Input<string>>(parameters => parameters
            .Add(p => p.Value, "Test Value"));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("value").Should().Be("Test Value");
    }

    /// <summary>
    /// Tests that the Input component renders with a number value.
    /// </summary>
    [Fact]
    public void Input_RendersWithNumberValue()
    {
        // Arrange & Act
        var cut = RenderComponent<Input<int>>(parameters => parameters
            .Add(p => p.Type, "number")
            .Add(p => p.Value, 42));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("type").Should().Be("number");
        input.GetAttribute("value").Should().Be("42");
    }

    /// <summary>
    /// Tests that the Input component applies error styling when HasError is true.
    /// </summary>
    [Fact]
    public void Input_AppliesErrorStyling_WhenHasErrorIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Input<string>>(parameters => parameters
            .Add(p => p.HasError, true));

        // Assert
        var input = cut.Find("input");
        input.ClassList.Should().Contain("border-red-300");
        input.GetAttribute("aria-invalid").Should().Be("true");
    }

    /// <summary>
    /// Tests that the Input component applies normal styling when HasError is false.
    /// </summary>
    [Fact]
    public void Input_AppliesNormalStyling_WhenHasErrorIsFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<Input<string>>(parameters => parameters
            .Add(p => p.HasError, false));

        // Assert
        var input = cut.Find("input");
        input.ClassList.Should().Contain("border-gray-300");
        input.GetAttribute("aria-invalid").Should().BeNull();
    }

    /// <summary>
    /// Tests that the Input component renders with placeholder text.
    /// </summary>
    [Fact]
    public void Input_RendersWithPlaceholder()
    {
        // Arrange & Act
        var cut = RenderComponent<Input<string>>(parameters => parameters
            .Add(p => p.Placeholder, "Enter text here"));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("placeholder").Should().Be("Enter text here");
    }

    /// <summary>
    /// Tests that the Input component is disabled when IsDisabled is true.
    /// </summary>
    [Fact]
    public void Input_IsDisabled_WhenIsDisabledIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Input<string>>(parameters => parameters
            .Add(p => p.IsDisabled, true));

        // Assert
        var input = cut.Find("input");
        input.HasAttribute("disabled").Should().BeTrue();
    }

    /// <summary>
    /// Tests that the Input component is readonly when IsReadonly is true.
    /// </summary>
    [Fact]
    public void Input_IsReadonly_WhenIsReadonlyIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Input<string>>(parameters => parameters
            .Add(p => p.IsReadonly, true));

        // Assert
        var input = cut.Find("input");
        input.HasAttribute("readonly").Should().BeTrue();
    }

    /// <summary>
    /// Tests that the Input component triggers ValueChanged on input change.
    /// </summary>
    [Fact]
    public void Input_TriggersValueChanged_OnInputChange()
    {
        // Arrange
        var newValue = string.Empty;
        var cut = RenderComponent<Input<string>>(parameters => parameters
            .Add(p => p.Value, "Initial")
            .Add(p => p.ValueChanged, value => newValue = value));

        var input = cut.Find("input");

        // Act
        input.Change("Updated");

        // Assert
        newValue.Should().Be("Updated");
    }

    /// <summary>
    /// Tests that the Input component renders with min and max attributes for numbers.
    /// </summary>
    [Fact]
    public void Input_RendersWithMinMaxAttributes_ForNumberInputs()
    {
        // Arrange & Act
        var cut = RenderComponent<Input<int>>(parameters => parameters
            .Add(p => p.Type, "number")
            .Add(p => p.Min, "1")
            .Add(p => p.Max, "100")
            .Add(p => p.Step, "5"));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("min").Should().Be("1");
        input.GetAttribute("max").Should().Be("100");
        input.GetAttribute("step").Should().Be("5");
    }

    /// <summary>
    /// Tests that the Input component applies custom CSS classes.
    /// </summary>
    [Fact]
    public void Input_AppliesCustomCssClasses()
    {
        // Arrange & Act
        var cut = RenderComponent<Input<string>>(parameters => parameters
            .Add(p => p.Class, "custom-class"));

        // Assert
        var input = cut.Find("input");
        input.ClassList.Should().Contain("custom-class");
    }
}
