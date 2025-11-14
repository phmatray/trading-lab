// <copyright file="ButtonTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Shouldly;
using TradingBot.Web.Components.Atoms;
using Xunit;
using static TradingBot.Web.Components.Atoms.Button;

namespace TradingBot.Web.Tests.Components.Atoms;

/// <summary>
/// Tests for the Button component.
/// </summary>
public class ButtonTests
{
    /// <summary>
    /// Tests that the Button component renders with default values.
    /// </summary>
    [Fact]
    public void Button_RendersWithDefaults()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.ChildContent, "Click me"));

        // Assert
        var button = cut.Find("button");
        button.ShouldNotBeNull();
        button.TextContent.ShouldContain("Click me");
        button.GetAttribute("type").ShouldBe("button");
    }

    /// <summary>
    /// Tests that the Button component renders with Primary variant styling.
    /// </summary>
    [Fact]
    public void Button_RendersPrimaryVariant()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Primary)
            .Add(p => p.ChildContent, "Primary"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("bg-blue-600");
        button.ClassList.ShouldContain("text-white");
    }

    /// <summary>
    /// Tests that the Button component renders with Secondary variant styling.
    /// </summary>
    [Fact]
    public void Button_RendersSecondaryVariant()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Secondary)
            .Add(p => p.ChildContent, "Secondary"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("bg-gray-200");
        button.ClassList.ShouldContain("text-gray-900");
    }

    /// <summary>
    /// Tests that the Button component renders with Danger variant styling.
    /// </summary>
    [Fact]
    public void Button_RendersDangerVariant()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Danger)
            .Add(p => p.ChildContent, "Delete"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("bg-red-600");
        button.ClassList.ShouldContain("text-white");
    }

    /// <summary>
    /// Tests that the Button component renders with Ghost variant styling.
    /// </summary>
    [Fact]
    public void Button_RendersGhostVariant()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Ghost)
            .Add(p => p.ChildContent, "Ghost"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("text-gray-700");
    }

    /// <summary>
    /// Tests that the Button component renders with Small size.
    /// </summary>
    [Fact]
    public void Button_RendersSmallSize()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.Size, ButtonSize.Small)
            .Add(p => p.ChildContent, "Small"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("px-3");
        button.ClassList.ShouldContain("py-1.5");
        button.ClassList.ShouldContain("text-sm");
    }

    /// <summary>
    /// Tests that the Button component renders with Medium size.
    /// </summary>
    [Fact]
    public void Button_RendersMediumSize()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.Size, ButtonSize.Medium)
            .Add(p => p.ChildContent, "Medium"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("px-4");
        button.ClassList.ShouldContain("py-2");
        button.ClassList.ShouldContain("text-base");
    }

    /// <summary>
    /// Tests that the Button component renders with Large size.
    /// </summary>
    [Fact]
    public void Button_RendersLargeSize()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.Size, ButtonSize.Large)
            .Add(p => p.ChildContent, "Large"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("px-6");
        button.ClassList.ShouldContain("py-3");
        button.ClassList.ShouldContain("text-lg");
    }

    /// <summary>
    /// Tests that the Button component is disabled when IsDisabled is true.
    /// </summary>
    [Fact]
    public void Button_IsDisabled_WhenIsDisabledIsTrue()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.IsDisabled, true)
            .Add(p => p.ChildContent, "Disabled"));

        // Assert
        var button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the Button component shows loading spinner when IsLoading is true.
    /// </summary>
    [Fact]
    public void Button_ShowsLoadingSpinner_WhenIsLoadingIsTrue()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.ChildContent, "Loading"));

        // Assert
        var svg = cut.Find("svg");
        svg.ShouldNotBeNull();
        svg.ClassList.ShouldContain("animate-spin");
    }

    /// <summary>
    /// Tests that the Button component triggers OnClick event.
    /// </summary>
    [Fact]
    public void Button_TriggersOnClick_WhenClicked()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        var clicked = false;
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.OnClick, (MouseEventArgs e) => clicked = true)
            .Add(p => p.ChildContent, "Click"));

        var button = cut.Find("button");

        // Act
        button.Click();

        // Assert
        clicked.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the Button component does not trigger OnClick when disabled.
    /// </summary>
    [Fact]
    public void Button_DoesNotTriggerOnClick_WhenDisabled()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        var clicked = false;
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.IsDisabled, true)
            .Add(p => p.OnClick, (MouseEventArgs e) => clicked = true)
            .Add(p => p.ChildContent, "Disabled"));

        var button = cut.Find("button");

        // Act
        button.Click();

        // Assert
        clicked.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the Button component does not trigger OnClick when loading.
    /// </summary>
    [Fact]
    public void Button_DoesNotTriggerOnClick_WhenLoading()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        var clicked = false;
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.OnClick, (MouseEventArgs e) => clicked = true)
            .Add(p => p.ChildContent, "Loading"));

        var button = cut.Find("button");

        // Act
        button.Click();

        // Assert
        clicked.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the Button component renders submit type.
    /// </summary>
    [Fact]
    public void Button_RendersSubmitType()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.Type, "submit")
            .Add(p => p.ChildContent, "Submit"));

        // Assert
        var button = cut.Find("button");
        button.GetAttribute("type").ShouldBe("submit");
    }

    /// <summary>
    /// Tests that the Button component applies custom CSS classes.
    /// </summary>
    [Fact]
    public void Button_AppliesCustomCssClasses()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Button>(parameters => parameters
            .Add(p => p.Class, "custom-button")
            .Add(p => p.ChildContent, "Custom"));

        // Assert
        var button = cut.Find("button");
        button.ClassList.ShouldContain("custom-button");
    }
}
