// <copyright file="FormFieldTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Tests.Components.Molecules;

using Bunit;
using Microsoft.AspNetCore.Components;
using TradingBot.Web.Components.Molecules;
using Xunit;

/// <summary>
/// Tests for the FormField component.
/// </summary>
public class FormFieldTests : Bunit.TestContext
{
    /// <summary>
    /// Tests that the FormField component renders with default values.
    /// </summary>
    [Fact]
    public void FormField_RendersWithDefaults()
    {
        // Arrange & Act
        var cut = RenderComponent<FormField>();

        // Assert
        var container = cut.Find(".form-field");
        container.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the FormField component renders with a label.
    /// </summary>
    [Fact]
    public void FormField_RendersWithLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<FormField>(parameters => parameters
            .Add(p => p.Label, "Username")
            .Add(p => p.InputId, "username-input"));

        // Assert
        var label = cut.Find("label");
        label.TextContent.Should().Contain("Username");
        label.GetAttribute("for").Should().Be("username-input");
    }

    /// <summary>
    /// Tests that the FormField component shows required indicator when IsRequired is true.
    /// </summary>
    [Fact]
    public void FormField_ShowsRequiredIndicator_WhenIsRequiredIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<FormField>(parameters => parameters
            .Add(p => p.Label, "Email")
            .Add(p => p.IsRequired, true));

        // Assert
        var requiredSpan = cut.Find("span.text-red-500");
        requiredSpan.TextContent.Should().Contain("*");
        requiredSpan.GetAttribute("aria-label").Should().Be("required");
    }

    /// <summary>
    /// Tests that the FormField component renders with help text.
    /// </summary>
    [Fact]
    public void FormField_RendersWithHelpText()
    {
        // Arrange & Act
        var cut = RenderComponent<FormField>(parameters => parameters
            .Add(p => p.InputId, "test-input")
            .Add(p => p.HelpText, "Enter your username"));

        // Assert
        var helpText = cut.Find("p#test-input-help");
        helpText.TextContent.Should().Be("Enter your username");
        helpText.ClassList.Should().Contain("text-sm");
        helpText.ClassList.Should().Contain("text-gray-500");
    }

    /// <summary>
    /// Tests that the FormField component renders with error message.
    /// </summary>
    [Fact]
    public void FormField_RendersWithErrorMessage()
    {
        // Arrange & Act
        var cut = RenderComponent<FormField>(parameters => parameters
            .Add(p => p.InputId, "test-input")
            .Add(p => p.ErrorMessage, "This field is required"));

        // Assert
        var error = cut.Find("p#test-input-error");
        error.TextContent.Should().Be("This field is required");
        error.ClassList.Should().Contain("text-red-600");
        error.GetAttribute("role").Should().Be("alert");
    }

    /// <summary>
    /// Tests that the FormField component hides help text when error message is present.
    /// </summary>
    [Fact]
    public void FormField_HidesHelpText_WhenErrorMessageIsPresent()
    {
        // Arrange & Act
        var cut = RenderComponent<FormField>(parameters => parameters
            .Add(p => p.InputId, "test-input")
            .Add(p => p.HelpText, "This is help text")
            .Add(p => p.ErrorMessage, "This is an error"));

        // Assert
        var helpTexts = cut.FindAll("p#test-input-help");
        helpTexts.Should().BeEmpty();

        var errors = cut.FindAll("p#test-input-error");
        errors.Should().HaveCount(1);
    }

    /// <summary>
    /// Tests that the FormField component renders with input content.
    /// </summary>
    [Fact]
    public void FormField_RendersWithInputContent()
    {
        // Arrange & Act
        var cut = RenderComponent<FormField>(parameters => parameters
            .Add(p => p.InputContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "text");
                builder.AddAttribute(2, "class", "test-input");
                builder.CloseElement();
            })));

        // Assert
        var input = cut.Find("input.test-input");
        input.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the FormField component applies custom CSS classes.
    /// </summary>
    [Fact]
    public void FormField_AppliesCustomCssClasses()
    {
        // Arrange & Act
        var cut = RenderComponent<FormField>(parameters => parameters
            .Add(p => p.Class, "custom-field"));

        // Assert
        var container = cut.Find(".form-field");
        container.ClassList.Should().Contain("custom-field");
    }

    /// <summary>
    /// Tests that the FormField component does not render label when Label is null.
    /// </summary>
    [Fact]
    public void FormField_DoesNotRenderLabel_WhenLabelIsNull()
    {
        // Arrange & Act
        var cut = RenderComponent<FormField>(parameters => parameters
            .Add(p => p.Label, (string?)null));

        // Assert
        var labels = cut.FindAll("label");
        labels.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the FormField component generates unique input ID when not provided.
    /// </summary>
    [Fact]
    public void FormField_GeneratesUniqueInputId_WhenNotProvided()
    {
        // Arrange & Act
        var cut1 = RenderComponent<FormField>(parameters => parameters
            .Add(p => p.HelpText, "Help 1"));
        var cut2 = RenderComponent<FormField>(parameters => parameters
            .Add(p => p.HelpText, "Help 2"));

        // Assert
        var helpText1 = cut1.Find("p");
        var helpText2 = cut2.Find("p");

        var id1 = helpText1.GetAttribute("id");
        var id2 = helpText2.GetAttribute("id");

        id1.Should().NotBe(id2);
    }
}
