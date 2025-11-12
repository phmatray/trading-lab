// <copyright file="ToggleTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using TradingBot.Web.Components.Atoms;

namespace TradingBot.Web.Tests.Components.Atoms;

/// <summary>
/// Tests for the Toggle component.
/// </summary>
public class ToggleTests
{
    /// <summary>
    /// Tests that the Toggle component renders with default values.
    /// </summary>
    [Fact]
    public void Toggle_RendersWithDefaults()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbToggle>();

        // Assert
        var button = cut.Find("button");
        button.ShouldNotBeNull();
        button.GetAttribute("role").ShouldBe("switch");
        button.GetAttribute("aria-checked").ShouldBe("false");
    }

    /// <summary>
    /// Tests that the Toggle component renders in checked state.
    /// </summary>
    [Fact]
    public void Toggle_RendersInCheckedState()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbToggle>(parameters => parameters
            .Add(p => p.IsChecked, true));

        // Assert
        var button = cut.Find("button");
        button.GetAttribute("aria-checked").ShouldBe("true");
        button.ClassList.ShouldContain("bg-blue-600");
    }

    /// <summary>
    /// Tests that the Toggle component renders in unchecked state.
    /// </summary>
    [Fact]
    public void Toggle_RendersInUncheckedState()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbToggle>(parameters => parameters
            .Add(p => p.IsChecked, false));

        // Assert
        var button = cut.Find("button");
        button.GetAttribute("aria-checked").ShouldBe("false");
        button.ClassList.ShouldContain("bg-gray-200");
    }

    /// <summary>
    /// Tests that the Toggle component is disabled when IsDisabled is true.
    /// </summary>
    [Fact]
    public void Toggle_IsDisabled_WhenIsDisabledIsTrue()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbToggle>(parameters => parameters
            .Add(p => p.IsDisabled, true));

        // Assert
        var button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the Toggle component triggers IsCheckedChanged on click.
    /// </summary>
    [Fact]
    public void Toggle_TriggersIsCheckedChanged_OnClick()
    {
        // Arrange
        using var ctx = new BunitContext();
        var isChecked = false;
        var cut = ctx.Render<TbToggle>(parameters => parameters
            .Add(p => p.IsChecked, isChecked)
            .Add(p => p.IsCheckedChanged, value => isChecked = value));

        var button = cut.Find("button");

        // Act
        button.Click();

        // Assert
        isChecked.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the Toggle component does not trigger IsCheckedChanged when disabled.
    /// </summary>
    [Fact]
    public void Toggle_DoesNotTriggerIsCheckedChanged_WhenDisabled()
    {
        // Arrange
        using var ctx = new BunitContext();
        var isChecked = false;
        var wasTriggered = false;
        var cut = ctx.Render<TbToggle>(parameters => parameters
            .Add(p => p.IsChecked, isChecked)
            .Add(p => p.IsDisabled, true)
            .Add(p => p.IsCheckedChanged, _ => wasTriggered = true));

        var button = cut.Find("button");

        // Act
        button.Click();

        // Assert
        wasTriggered.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the Toggle component applies custom CSS classes.
    /// </summary>
    [Fact]
    public void Toggle_AppliesCustomCssClasses()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbToggle>(parameters => parameters
            .Add(p => p.Class, "custom-toggle"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("custom-toggle");
    }

    /// <summary>
    /// Tests that the Toggle component thumb moves when toggled.
    /// </summary>
    [Fact]
    public void Toggle_ThumbMoves_WhenToggled()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act - Render unchecked
        var cutUnchecked = ctx.Render<TbToggle>(parameters => parameters
            .Add(p => p.IsChecked, false));

        var thumbUnchecked = cutUnchecked.Find("span");

        // Assert unchecked position
        thumbUnchecked.ClassList.ShouldContain("translate-x-1");

        // Act - Render checked
        var cutChecked = ctx.Render<TbToggle>(parameters => parameters
            .Add(p => p.IsChecked, true));

        var thumbChecked = cutChecked.Find("span");

        // Assert checked position
        thumbChecked.ClassList.ShouldContain("translate-x-6");
    }

    /// <summary>
    /// Tests that the Toggle component has correct accessibility attributes.
    /// </summary>
    [Fact]
    public void Toggle_HasCorrectAccessibilityAttributes()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbToggle>(parameters => parameters
            .Add(p => p.Id, "test-toggle")
            .Add(p => p.IsChecked, true));

        // Assert
        var button = cut.Find("button");
        button.GetAttribute("id").ShouldBe("test-toggle");
        button.GetAttribute("role").ShouldBe("switch");
        button.GetAttribute("aria-checked").ShouldBe("true");

        var thumb = cut.Find("span");
        thumb.GetAttribute("aria-hidden").ShouldBe("true");
    }
}
