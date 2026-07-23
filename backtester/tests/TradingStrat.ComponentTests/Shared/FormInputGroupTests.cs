using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the FormInputGroup component.
/// </summary>
public class FormInputGroupTests : BunitTestContext
{
    [Fact]
    public void FormInputGroup_WithLabel_DisplaysLabel()
    {
        // Arrange & Act
        IRenderedComponent<FormInputGroup> cut = Render<FormInputGroup>(parameters => parameters
            .Add(p => p.Label, "Username")
            .Add(p => p.Id, "username-input"));

        // Assert
        cut.Markup.ShouldContain("Username");
        IElement label = cut.Find("label");
        label.ShouldNotBeNull();
        label.GetAttribute("for").ShouldBe("username-input");
    }

    [Fact]
    public void FormInputGroup_WithIsOptionalTrue_DisplaysOptionalIndicator()
    {
        // Arrange & Act
        IRenderedComponent<FormInputGroup> cut = Render<FormInputGroup>(parameters => parameters
            .Add(p => p.Label, "Optional Field")
            .Add(p => p.Id, "optional-field")
            .Add(p => p.IsOptional, true));

        // Assert
        cut.Markup.ShouldContain("(Optional)");
    }

    [Fact]
    public void FormInputGroup_WithIsOptionalFalse_DoesNotDisplayOptionalIndicator()
    {
        // Arrange & Act
        IRenderedComponent<FormInputGroup> cut = Render<FormInputGroup>(parameters => parameters
            .Add(p => p.Label, "Required Field")
            .Add(p => p.Id, "required-field")
            .Add(p => p.IsOptional, false));

        // Assert
        cut.Markup.ShouldNotContain("(Optional)");
    }

    [Fact]
    public void FormInputGroup_WithHelpText_DisplaysHelpText()
    {
        // Arrange & Act
        IRenderedComponent<FormInputGroup> cut = Render<FormInputGroup>(parameters => parameters
            .Add(p => p.Label, "Email")
            .Add(p => p.Id, "email")
            .Add(p => p.HelpText, "We'll never share your email"));

        // Assert
        cut.Markup.ShouldContain("We'll never share your email");
        IElement helpText = cut.Find("p.text-xs");
        helpText.ShouldNotBeNull();
    }

    [Fact]
    public void FormInputGroup_WithoutHelpText_DoesNotDisplayHelpText()
    {
        // Arrange & Act
        IRenderedComponent<FormInputGroup> cut = Render<FormInputGroup>(parameters => parameters
            .Add(p => p.Label, "Email")
            .Add(p => p.Id, "email"));

        // Assert
        IReadOnlyList<IElement> helpTexts = cut.FindAll("p.text-xs");
        helpTexts.ShouldBeEmpty();
    }

    [Fact]
    public void FormInputGroup_RendersChildContent()
    {
        // Arrange & Act
        IRenderedComponent<FormInputGroup> cut = Render<FormInputGroup>(parameters => parameters
            .Add(p => p.Label, "Input")
            .Add(p => p.Id, "input")
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "id", "test-input");
                builder.AddAttribute(2, "class", "form-input");
                builder.CloseElement();
            }));

        // Assert
        IElement input = cut.Find("input#test-input");
        input.ShouldNotBeNull();
        input.ClassList.ShouldContain("form-input");
    }

    [Fact]
    public void FormInputGroup_WithAllFeatures_RendersCorrectly()
    {
        // Arrange & Act
        IRenderedComponent<FormInputGroup> cut = Render<FormInputGroup>(parameters => parameters
            .Add(p => p.Label, "Full Name")
            .Add(p => p.Id, "fullname")
            .Add(p => p.IsOptional, true)
            .Add(p => p.HelpText, "Enter your full legal name")
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "id", "fullname");
                builder.CloseElement();
            }));

        // Assert
        cut.Markup.ShouldContain("Full Name");
        cut.Markup.ShouldContain("(Optional)");
        cut.Markup.ShouldContain("Enter your full legal name");
        IElement input = cut.Find("input#fullname");
        input.ShouldNotBeNull();
    }

    [Fact]
    public void FormInputGroup_LabelHasCorrectForAttribute()
    {
        // Arrange & Act
        IRenderedComponent<FormInputGroup> cut = Render<FormInputGroup>(parameters => parameters
            .Add(p => p.Label, "Password")
            .Add(p => p.Id, "password-field"));

        // Assert
        IElement label = cut.Find("label");
        label.GetAttribute("for").ShouldBe("password-field");
    }

    [Fact]
    public void FormInputGroup_LabelHasCorrectStyling()
    {
        // Arrange & Act
        IRenderedComponent<FormInputGroup> cut = Render<FormInputGroup>(parameters => parameters
            .Add(p => p.Label, "Test")
            .Add(p => p.Id, "test"));

        // Assert
        IElement label = cut.Find("label");
        label.ClassList.ShouldContain("block");
        label.ClassList.ShouldContain("text-sm");
        label.ClassList.ShouldContain("font-medium");
    }
}
