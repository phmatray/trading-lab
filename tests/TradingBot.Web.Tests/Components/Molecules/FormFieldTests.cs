// <copyright file="FormFieldTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Microsoft.AspNetCore.Components;
using TradingBot.Web.Components.Molecules;

namespace TradingBot.Web.Tests.Components.Molecules;

/// <summary>
/// Tests for the FormField component.
/// </summary>
public class FormFieldTests
{
    /// <summary>
    /// Tests that the FormField component renders with default values.
    /// </summary>
    [Fact]
    public void FormField_RendersWithDefaults()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbFormField>();

        // Assert
        var container = cut.Find(".form-field");
        container.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the FormField component renders with a label.
    /// </summary>
    [Fact]
    public void FormField_RendersWithLabel()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbFormField>(parameters => parameters
            .Add(p => p.Label, "Username")
            .Add(p => p.InputId, "username-input"));

        // Assert
        var label = cut.Find("label");
        label.TextContent.ShouldContain("Username");
        label.GetAttribute("for").ShouldBe("username-input");
    }

    /// <summary>
    /// Tests that the FormField component shows required indicator when IsRequired is true.
    /// </summary>
    [Fact]
    public void FormField_ShowsRequiredIndicator_WhenIsRequiredIsTrue()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbFormField>(parameters => parameters
            .Add(p => p.Label, "Email")
            .Add(p => p.IsRequired, true));

        // Assert
        var requiredSpan = cut.Find("span.text-red-500");
        requiredSpan.TextContent.ShouldContain("*");
        requiredSpan.GetAttribute("aria-label").ShouldBe("required");
    }

    /// <summary>
    /// Tests that the FormField component renders with help text.
    /// </summary>
    [Fact]
    public void FormField_RendersWithHelpText()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbFormField>(parameters => parameters
            .Add(p => p.InputId, "test-input")
            .Add(p => p.HelpText, "Enter your username"));

        // Assert
        var helpText = cut.Find("p#test-input-help");
        helpText.TextContent.ShouldBe("Enter your username");
        helpText.ClassList.ShouldContain("text-sm");
        helpText.ClassList.ShouldContain("text-gray-500");
    }

    /// <summary>
    /// Tests that the FormField component renders with error message.
    /// </summary>
    [Fact]
    public void FormField_RendersWithErrorMessage()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbFormField>(parameters => parameters
            .Add(p => p.InputId, "test-input")
            .Add(p => p.ErrorMessage, "This field is required"));

        // Assert
        var error = cut.Find("p#test-input-error");
        error.TextContent.ShouldBe("This field is required");
        error.ClassList.ShouldContain("text-red-600");
        error.GetAttribute("role").ShouldBe("alert");
    }

    /// <summary>
    /// Tests that the FormField component hides help text when error message is present.
    /// </summary>
    [Fact]
    public void FormField_HidesHelpText_WhenErrorMessageIsPresent()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbFormField>(parameters => parameters
            .Add(p => p.InputId, "test-input")
            .Add(p => p.HelpText, "This is help text")
            .Add(p => p.ErrorMessage, "This is an error"));

        // Assert
        var helpTexts = cut.FindAll("p#test-input-help");
        helpTexts.ShouldBeEmpty();

        var errors = cut.FindAll("p#test-input-error");
        errors.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the FormField component renders with input content.
    /// </summary>
    [Fact]
    public void FormField_RendersWithInputContent()
    {
        // Arrange
        using var ctx = new BunitContext();
        RenderFragment inputContent = builder =>
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "type", "text");
            builder.AddAttribute(2, "class", "test-input");
            builder.CloseElement();
        };

        // Act
        var cut = ctx.Render<TbFormField>(
            parameters => parameters.Add(p => p.InputContent, inputContent));

        // Assert
        var input = cut.Find("input.test-input");
        input.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the FormField component applies custom CSS classes.
    /// </summary>
    [Fact]
    public void FormField_AppliesCustomCssClasses()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbFormField>(parameters => parameters
            .Add(p => p.Class, "custom-field"));

        // Assert
        var container = cut.Find(".form-field");
        container.ClassList.ShouldContain("custom-field");
    }

    /// <summary>
    /// Tests that the FormField component does not render label when Label is null.
    /// </summary>
    [Fact]
    public void FormField_DoesNotRenderLabel_WhenLabelIsNull()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut = ctx.Render<TbFormField>(parameters => parameters
            .Add(p => p.Label, null));

        // Assert
        var labels = cut.FindAll("label");
        labels.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that the FormField component generates unique input ID when not provided.
    /// </summary>
    [Fact]
    public void FormField_GeneratesUniqueInputId_WhenNotProvided()
    {
        // Arrange
        using var ctx = new BunitContext();

        // Act
        var cut1 = ctx.Render<TbFormField>(parameters => parameters
            .Add(p => p.HelpText, "Help 1"));
        var cut2 = ctx.Render<TbFormField>(parameters => parameters
            .Add(p => p.HelpText, "Help 2"));

        // Assert
        var helpText1 = cut1.Find("p");
        var helpText2 = cut2.Find("p");

        var id1 = helpText1.GetAttribute("id");
        var id2 = helpText2.GetAttribute("id");

        id1.ShouldNotBe(id2);
    }
}
