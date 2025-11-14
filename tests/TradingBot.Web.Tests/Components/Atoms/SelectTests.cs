// <copyright file="SelectTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingBot.Web.Components.Atoms;
using Xunit;

namespace TradingBot.Web.Tests.Components.Atoms;

/// <summary>
/// Tests for the Select component.
/// </summary>
public class SelectTests
{
    /// <summary>
    /// Tests that the Select component renders with default values.
    /// </summary>
    [Fact]
    public void Select_RendersWithDefaults()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Select<string>>();

        // Assert
        var select = cut.Find("select");
        select.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the Select component renders with options.
    /// </summary>
    [Fact]
    public void Select_RendersWithOptions()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        RenderFragment childContent = builder =>
        {
            builder.OpenElement(0, "option");
            builder.AddAttribute(1, "value", "option1");
            builder.AddContent(2, "Option 1");
            builder.CloseElement();
            builder.OpenElement(3, "option");
            builder.AddAttribute(4, "value", "option2");
            builder.AddContent(5, "Option 2");
            builder.CloseElement();
        };

        // Act
        var cut = ctx.RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.ChildContent, childContent));

        // Assert
        var options = cut.FindAll("option");
        options.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Tests that the Select component renders with a placeholder.
    /// </summary>
    [Fact]
    public void Select_RendersWithPlaceholder()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.Placeholder, "Select an option"));

        // Assert
        var placeholder = cut.Find("option[disabled]");
        placeholder.TextContent.ShouldBe("Select an option");
    }

    /// <summary>
    /// Tests that the Select component applies error styling when HasError is true.
    /// </summary>
    [Fact]
    public void Select_AppliesErrorStyling_WhenHasErrorIsTrue()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.HasError, true));

        // Assert
        var select = cut.Find("select");
        select.ClassList.ShouldContain("border-red-300");
        select.GetAttribute("aria-invalid").ShouldBe("true");
    }

    /// <summary>
    /// Tests that the Select component applies normal styling when HasError is false.
    /// </summary>
    [Fact]
    public void Select_AppliesNormalStyling_WhenHasErrorIsFalse()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.HasError, false));

        // Assert
        var select = cut.Find("select");
        select.ClassList.ShouldContain("border-gray-300");
        select.GetAttribute("aria-invalid").ShouldBeNull();
    }

    /// <summary>
    /// Tests that the Select component is disabled when IsDisabled is true.
    /// </summary>
    [Fact]
    public void Select_IsDisabled_WhenIsDisabledIsTrue()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.IsDisabled, true));

        // Assert
        var select = cut.Find("select");
        select.HasAttribute("disabled").ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the Select component triggers ValueChanged on selection change.
    /// </summary>
    [Fact]
    public void Select_TriggersValueChanged_OnSelectionChange()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        var newValue = string.Empty;
        RenderFragment childContent = builder =>
        {
            builder.OpenElement(0, "option");
            builder.AddAttribute(1, "value", "option1");
            builder.AddContent(2, "Option 1");
            builder.CloseElement();
            builder.OpenElement(3, "option");
            builder.AddAttribute(4, "value", "option2");
            builder.AddContent(5, "Option 2");
            builder.CloseElement();
        };

        var cut = ctx.RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.Value, "option1")
            .Add(p => p.ValueChanged, value => newValue = value)
            .Add(p => p.ChildContent, childContent));

        var select = cut.Find("select");

        // Act
        select.Change("option2");

        // Assert
        newValue.ShouldBe("option2");
    }

    /// <summary>
    /// Tests that the Select component applies custom CSS classes.
    /// </summary>
    [Fact]
    public void Select_AppliesCustomCssClasses()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.Class, "custom-select"));

        // Assert
        var select = cut.Find("select");
        select.ClassList.ShouldContain("custom-select");
    }

    /// <summary>
    /// Tests that the Select component does not show placeholder when ShowPlaceholder is false.
    /// </summary>
    [Fact]
    public void Select_DoesNotShowPlaceholder_WhenShowPlaceholderIsFalse()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();

        // Act
        var cut = ctx.RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.Placeholder, "Select option")
            .Add(p => p.ShowPlaceholder, false));

        // Assert
        var placeholders = cut.FindAll("option[disabled]");
        placeholders.ShouldBeEmpty();
    }
}
