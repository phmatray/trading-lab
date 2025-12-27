using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the FormWrapper component.
/// </summary>
public class FormWrapperTests : BunitTestContext
{
    [Fact]
    public void FormWrapper_RendersChildContent()
    {
        // Arrange & Act
        IRenderedComponent<FormWrapper> cut = Render<FormWrapper>(parameters => parameters
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "test-child");
                builder.AddContent(2, "Test Content");
                builder.CloseElement();
            }));

        // Assert
        IElement child = cut.Find("div.test-child");
        child.ShouldNotBeNull();
        child.TextContent.ShouldBe("Test Content");
    }

    [Fact]
    public void FormWrapper_WithErrorMessage_DisplaysErrorAlert()
    {
        // Arrange & Act
        IRenderedComponent<FormWrapper> cut = Render<FormWrapper>(parameters => parameters
            .Add(p => p.ErrorMessage, "An error occurred"));

        // Assert
        cut.Markup.ShouldContain("An error occurred");
        IRenderedComponent<AlertMessage> alert = cut.FindComponent<AlertMessage>();
        alert.ShouldNotBeNull();
    }

    [Fact]
    public void FormWrapper_WithWarningMessage_DisplaysWarningAlert()
    {
        // Arrange & Act
        IRenderedComponent<FormWrapper> cut = Render<FormWrapper>(parameters => parameters
            .Add(p => p.WarningMessage, "This is a warning"));

        // Assert
        cut.Markup.ShouldContain("This is a warning");
        IRenderedComponent<AlertMessage> alert = cut.FindComponent<AlertMessage>();
        alert.ShouldNotBeNull();
    }

    [Fact]
    public void FormWrapper_WithSuccessMessage_DisplaysSuccessAlert()
    {
        // Arrange & Act
        IRenderedComponent<FormWrapper> cut = Render<FormWrapper>(parameters => parameters
            .Add(p => p.SuccessMessage, "Operation successful"));

        // Assert
        cut.Markup.ShouldContain("Operation successful");
        IRenderedComponent<AlertMessage> alert = cut.FindComponent<AlertMessage>();
        alert.ShouldNotBeNull();
    }

    [Fact]
    public void FormWrapper_WithoutMessages_DoesNotDisplayAlerts()
    {
        // Arrange & Act
        IRenderedComponent<FormWrapper> cut = Render<FormWrapper>();

        // Assert
        IReadOnlyList<IRenderedComponent<AlertMessage>> alerts = cut.FindComponents<AlertMessage>();
        alerts.ShouldBeEmpty();
    }

    [Fact]
    public void FormWrapper_WithShowProgressIndicatorTrue_DisplaysProgressIndicator()
    {
        // Arrange & Act
        IRenderedComponent<FormWrapper> cut = Render<FormWrapper>(parameters => parameters
            .Add(p => p.ShowProgressIndicator, true));

        // Assert
        IRenderedComponent<ProgressIndicator> progressIndicator = cut.FindComponent<ProgressIndicator>();
        progressIndicator.ShouldNotBeNull();
    }

    [Fact]
    public void FormWrapper_WithShowProgressIndicatorFalse_DoesNotDisplayProgressIndicator()
    {
        // Arrange & Act
        IRenderedComponent<FormWrapper> cut = Render<FormWrapper>(parameters => parameters
            .Add(p => p.ShowProgressIndicator, false));

        // Assert
        IReadOnlyList<IRenderedComponent<ProgressIndicator>> progressIndicators = cut.FindComponents<ProgressIndicator>();
        progressIndicators.ShouldBeEmpty();
    }

    [Fact]
    public void FormWrapper_WithMultipleMessages_DisplaysAllMessages()
    {
        // Arrange & Act
        IRenderedComponent<FormWrapper> cut = Render<FormWrapper>(parameters => parameters
            .Add(p => p.ErrorMessage, "Error occurred")
            .Add(p => p.WarningMessage, "Warning message")
            .Add(p => p.SuccessMessage, "Success message"));

        // Assert
        cut.Markup.ShouldContain("Error occurred");
        cut.Markup.ShouldContain("Warning message");
        cut.Markup.ShouldContain("Success message");
    }

    [Fact]
    public void FormWrapper_MessagesDisplayInCorrectOrder()
    {
        // Arrange & Act
        IRenderedComponent<FormWrapper> cut = Render<FormWrapper>(parameters => parameters
            .Add(p => p.ErrorMessage, "Error")
            .Add(p => p.WarningMessage, "Warning")
            .Add(p => p.SuccessMessage, "Success"));

        // Assert - Error should appear before Warning, Warning before Success
        string markup = cut.Markup;
        int errorIndex = markup.IndexOf("Error");
        int warningIndex = markup.IndexOf("Warning");
        int successIndex = markup.IndexOf("Success");

        errorIndex.ShouldBeLessThan(warningIndex);
        warningIndex.ShouldBeLessThan(successIndex);
    }

    [Fact]
    public void FormWrapper_WithEmptyStringMessages_DoesNotDisplayAlerts()
    {
        // Arrange & Act
        IRenderedComponent<FormWrapper> cut = Render<FormWrapper>(parameters => parameters
            .Add(p => p.ErrorMessage, "")
            .Add(p => p.WarningMessage, "")
            .Add(p => p.SuccessMessage, ""));

        // Assert
        IReadOnlyList<IRenderedComponent<AlertMessage>> alerts = cut.FindComponents<AlertMessage>();
        alerts.ShouldBeEmpty();
    }
}
