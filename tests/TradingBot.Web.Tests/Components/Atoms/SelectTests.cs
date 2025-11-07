// <copyright file="SelectTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Tests.Components.Atoms;

using Bunit;
using Microsoft.AspNetCore.Components;
using TradingBot.Web.Components.Atoms;
using Xunit;

/// <summary>
/// Tests for the Select component.
/// </summary>
public class SelectTests : Bunit.TestContext
{
    /// <summary>
    /// Tests that the Select component renders with default values.
    /// </summary>
    [Fact]
    public void Select_RendersWithDefaults()
    {
        // Arrange & Act
        var cut = RenderComponent<Select<string>>();

        // Assert
        var select = cut.Find("select");
        select.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the Select component renders with options.
    /// </summary>
    [Fact]
    public void Select_RendersWithOptions()
    {
        // Arrange & Act
        var cut = RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddAttribute(1, "value", "option1");
                builder.AddContent(2, "Option 1");
                builder.CloseElement();
                builder.OpenElement(3, "option");
                builder.AddAttribute(4, "value", "option2");
                builder.AddContent(5, "Option 2");
                builder.CloseElement();
            })));

        // Assert
        var options = cut.FindAll("option");
        options.Count.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that the Select component renders with a placeholder.
    /// </summary>
    [Fact]
    public void Select_RendersWithPlaceholder()
    {
        // Arrange & Act
        var cut = RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.Placeholder, "Select an option"));

        // Assert
        var placeholder = cut.Find("option[disabled]");
        placeholder.TextContent.Should().Be("Select an option");
    }

    /// <summary>
    /// Tests that the Select component applies error styling when HasError is true.
    /// </summary>
    [Fact]
    public void Select_AppliesErrorStyling_WhenHasErrorIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.HasError, true));

        // Assert
        var select = cut.Find("select");
        select.ClassList.Should().Contain("border-red-300");
        select.GetAttribute("aria-invalid").Should().Be("true");
    }

    /// <summary>
    /// Tests that the Select component applies normal styling when HasError is false.
    /// </summary>
    [Fact]
    public void Select_AppliesNormalStyling_WhenHasErrorIsFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.HasError, false));

        // Assert
        var select = cut.Find("select");
        select.ClassList.Should().Contain("border-gray-300");
        select.GetAttribute("aria-invalid").Should().BeNull();
    }

    /// <summary>
    /// Tests that the Select component is disabled when IsDisabled is true.
    /// </summary>
    [Fact]
    public void Select_IsDisabled_WhenIsDisabledIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.IsDisabled, true));

        // Assert
        var select = cut.Find("select");
        select.HasAttribute("disabled").Should().BeTrue();
    }

    /// <summary>
    /// Tests that the Select component triggers ValueChanged on selection change.
    /// </summary>
    [Fact]
    public void Select_TriggersValueChanged_OnSelectionChange()
    {
        // Arrange
        var newValue = string.Empty;
        var cut = RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.Value, "option1")
            .Add(p => p.ValueChanged, value => newValue = value)
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddAttribute(1, "value", "option1");
                builder.AddContent(2, "Option 1");
                builder.CloseElement();
                builder.OpenElement(3, "option");
                builder.AddAttribute(4, "value", "option2");
                builder.AddContent(5, "Option 2");
                builder.CloseElement();
            })));

        var select = cut.Find("select");

        // Act
        select.Change("option2");

        // Assert
        newValue.Should().Be("option2");
    }

    /// <summary>
    /// Tests that the Select component applies custom CSS classes.
    /// </summary>
    [Fact]
    public void Select_AppliesCustomCssClasses()
    {
        // Arrange & Act
        var cut = RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.Class, "custom-select"));

        // Assert
        var select = cut.Find("select");
        select.ClassList.Should().Contain("custom-select");
    }

    /// <summary>
    /// Tests that the Select component does not show placeholder when ShowPlaceholder is false.
    /// </summary>
    [Fact]
    public void Select_DoesNotShowPlaceholder_WhenShowPlaceholderIsFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<Select<string>>(parameters => parameters
            .Add(p => p.Placeholder, "Select option")
            .Add(p => p.ShowPlaceholder, false));

        // Assert
        var placeholders = cut.FindAll("option[disabled]");
        placeholders.Should().BeEmpty();
    }
}
