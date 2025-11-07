// <copyright file="ToggleTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Tests.Components.Atoms;

using Bunit;
using TradingBot.Web.Components.Atoms;
using Xunit;

/// <summary>
/// Tests for the Toggle component.
/// </summary>
public class ToggleTests : Bunit.TestContext
{
    /// <summary>
    /// Tests that the Toggle component renders with default values.
    /// </summary>
    [Fact]
    public void Toggle_RendersWithDefaults()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>();

        // Assert
        var button = cut.Find("button");
        button.Should().NotBeNull();
        button.GetAttribute("role").Should().Be("switch");
        button.GetAttribute("aria-checked").Should().Be("false");
    }

    /// <summary>
    /// Tests that the Toggle component renders in checked state.
    /// </summary>
    [Fact]
    public void Toggle_RendersInCheckedState()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.IsChecked, true));

        // Assert
        var button = cut.Find("button");
        button.GetAttribute("aria-checked").Should().Be("true");
        button.ClassList.Should().Contain("bg-blue-600");
    }

    /// <summary>
    /// Tests that the Toggle component renders in unchecked state.
    /// </summary>
    [Fact]
    public void Toggle_RendersInUncheckedState()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.IsChecked, false));

        // Assert
        var button = cut.Find("button");
        button.GetAttribute("aria-checked").Should().Be("false");
        button.ClassList.Should().Contain("bg-gray-200");
    }

    /// <summary>
    /// Tests that the Toggle component is disabled when IsDisabled is true.
    /// </summary>
    [Fact]
    public void Toggle_IsDisabled_WhenIsDisabledIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.IsDisabled, true));

        // Assert
        var button = cut.Find("button");
        button.HasAttribute("disabled").Should().BeTrue();
    }

    /// <summary>
    /// Tests that the Toggle component triggers IsCheckedChanged on click.
    /// </summary>
    [Fact]
    public void Toggle_TriggersIsCheckedChanged_OnClick()
    {
        // Arrange
        var isChecked = false;
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.IsChecked, isChecked)
            .Add(p => p.IsCheckedChanged, value => isChecked = value));

        var button = cut.Find("button");

        // Act
        button.Click();

        // Assert
        isChecked.Should().BeTrue();
    }

    /// <summary>
    /// Tests that the Toggle component does not trigger IsCheckedChanged when disabled.
    /// </summary>
    [Fact]
    public void Toggle_DoesNotTriggerIsCheckedChanged_WhenDisabled()
    {
        // Arrange
        var isChecked = false;
        var wasTriggered = false;
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.IsChecked, isChecked)
            .Add(p => p.IsDisabled, true)
            .Add(p => p.IsCheckedChanged, value => wasTriggered = true));

        var button = cut.Find("button");

        // Act
        button.Click();

        // Assert
        wasTriggered.Should().BeFalse();
    }

    /// <summary>
    /// Tests that the Toggle component applies custom CSS classes.
    /// </summary>
    [Fact]
    public void Toggle_AppliesCustomCssClasses()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.Class, "custom-toggle"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.Should().Contain("custom-toggle");
    }

    /// <summary>
    /// Tests that the Toggle component thumb moves when toggled.
    /// </summary>
    [Fact]
    public void Toggle_ThumbMoves_WhenToggled()
    {
        // Arrange
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.IsChecked, false));

        var thumb = cut.Find("span");

        // Assert initial position
        thumb.ClassList.Should().Contain("translate-x-1");

        // Act - toggle to checked
        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.IsChecked, true));

        // Assert new position
        thumb = cut.Find("span");
        thumb.ClassList.Should().Contain("translate-x-6");
    }

    /// <summary>
    /// Tests that the Toggle component has correct accessibility attributes.
    /// </summary>
    [Fact]
    public void Toggle_HasCorrectAccessibilityAttributes()
    {
        // Arrange & Act
        var cut = RenderComponent<Toggle>(parameters => parameters
            .Add(p => p.Id, "test-toggle")
            .Add(p => p.IsChecked, true));

        // Assert
        var button = cut.Find("button");
        button.GetAttribute("id").Should().Be("test-toggle");
        button.GetAttribute("role").Should().Be("switch");
        button.GetAttribute("aria-checked").Should().Be("true");

        var thumb = cut.Find("span");
        thumb.GetAttribute("aria-hidden").Should().Be("true");
    }
}
