// <copyright file="ButtonTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Tests.Components.Atoms;

using Bunit;
using Microsoft.AspNetCore.Components.Web;
using TradingBot.Web.Components.Atoms;
using Xunit;
using static TradingBot.Web.Components.Atoms.Button;

/// <summary>
/// Tests for the Button component.
/// </summary>
public class ButtonTests : Bunit.TestContext
{
    /// <summary>
    /// Tests that the Button component renders with default values.
    /// </summary>
    [Fact]
    public void Button_RendersWithDefaults()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.ChildContent, "Click me"));

        // Assert
        var button = cut.Find("button");
        button.Should().NotBeNull();
        button.TextContent.Should().Contain("Click me");
        button.GetAttribute("type").Should().Be("button");
    }

    /// <summary>
    /// Tests that the Button component renders with Primary variant styling.
    /// </summary>
    [Fact]
    public void Button_RendersPrimaryVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Primary)
            .Add(p => p.ChildContent, "Primary"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.Should().Contain("bg-blue-600");
        button.ClassList.Should().Contain("text-white");
    }

    /// <summary>
    /// Tests that the Button component renders with Secondary variant styling.
    /// </summary>
    [Fact]
    public void Button_RendersSecondaryVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Secondary)
            .Add(p => p.ChildContent, "Secondary"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.Should().Contain("bg-gray-200");
        button.ClassList.Should().Contain("text-gray-900");
    }

    /// <summary>
    /// Tests that the Button component renders with Danger variant styling.
    /// </summary>
    [Fact]
    public void Button_RendersDangerVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Danger)
            .Add(p => p.ChildContent, "Delete"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.Should().Contain("bg-red-600");
        button.ClassList.Should().Contain("text-white");
    }

    /// <summary>
    /// Tests that the Button component renders with Ghost variant styling.
    /// </summary>
    [Fact]
    public void Button_RendersGhostVariant()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Ghost)
            .Add(p => p.ChildContent, "Ghost"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.Should().Contain("text-gray-700");
    }

    /// <summary>
    /// Tests that the Button component renders with Small size.
    /// </summary>
    [Fact]
    public void Button_RendersSmallSize()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Size, ButtonSize.Small)
            .Add(p => p.ChildContent, "Small"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.Should().Contain("px-3");
        button.ClassList.Should().Contain("py-1.5");
        button.ClassList.Should().Contain("text-sm");
    }

    /// <summary>
    /// Tests that the Button component renders with Medium size.
    /// </summary>
    [Fact]
    public void Button_RendersMediumSize()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Size, ButtonSize.Medium)
            .Add(p => p.ChildContent, "Medium"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.Should().Contain("px-4");
        button.ClassList.Should().Contain("py-2");
        button.ClassList.Should().Contain("text-base");
    }

    /// <summary>
    /// Tests that the Button component renders with Large size.
    /// </summary>
    [Fact]
    public void Button_RendersLargeSize()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Size, ButtonSize.Large)
            .Add(p => p.ChildContent, "Large"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.Should().Contain("px-6");
        button.ClassList.Should().Contain("py-3");
        button.ClassList.Should().Contain("text-lg");
    }

    /// <summary>
    /// Tests that the Button component is disabled when IsDisabled is true.
    /// </summary>
    [Fact]
    public void Button_IsDisabled_WhenIsDisabledIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.IsDisabled, true)
            .Add(p => p.ChildContent, "Disabled"));

        // Assert
        var button = cut.Find("button");
        button.HasAttribute("disabled").Should().BeTrue();
    }

    /// <summary>
    /// Tests that the Button component shows loading spinner when IsLoading is true.
    /// </summary>
    [Fact]
    public void Button_ShowsLoadingSpinner_WhenIsLoadingIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.ChildContent, "Loading"));

        // Assert
        var svg = cut.Find("svg");
        svg.Should().NotBeNull();
        svg.ClassList.Should().Contain("animate-spin");
    }

    /// <summary>
    /// Tests that the Button component triggers OnClick event.
    /// </summary>
    [Fact]
    public void Button_TriggersOnClick_WhenClicked()
    {
        // Arrange
        var clicked = false;
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.OnClick, (MouseEventArgs e) => clicked = true)
            .Add(p => p.ChildContent, "Click"));

        var button = cut.Find("button");

        // Act
        button.Click();

        // Assert
        clicked.Should().BeTrue();
    }

    /// <summary>
    /// Tests that the Button component does not trigger OnClick when disabled.
    /// </summary>
    [Fact]
    public void Button_DoesNotTriggerOnClick_WhenDisabled()
    {
        // Arrange
        var clicked = false;
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.IsDisabled, true)
            .Add(p => p.OnClick, (MouseEventArgs e) => clicked = true)
            .Add(p => p.ChildContent, "Disabled"));

        var button = cut.Find("button");

        // Act
        button.Click();

        // Assert
        clicked.Should().BeFalse();
    }

    /// <summary>
    /// Tests that the Button component does not trigger OnClick when loading.
    /// </summary>
    [Fact]
    public void Button_DoesNotTriggerOnClick_WhenLoading()
    {
        // Arrange
        var clicked = false;
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.OnClick, (MouseEventArgs e) => clicked = true)
            .Add(p => p.ChildContent, "Loading"));

        var button = cut.Find("button");

        // Act
        button.Click();

        // Assert
        clicked.Should().BeFalse();
    }

    /// <summary>
    /// Tests that the Button component renders submit type.
    /// </summary>
    [Fact]
    public void Button_RendersSubmitType()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Type, "submit")
            .Add(p => p.ChildContent, "Submit"));

        // Assert
        var button = cut.Find("button");
        button.GetAttribute("type").Should().Be("submit");
    }

    /// <summary>
    /// Tests that the Button component applies custom CSS classes.
    /// </summary>
    [Fact]
    public void Button_AppliesCustomCssClasses()
    {
        // Arrange & Act
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Class, "custom-button")
            .Add(p => p.ChildContent, "Custom"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.Should().Contain("custom-button");
    }
}
