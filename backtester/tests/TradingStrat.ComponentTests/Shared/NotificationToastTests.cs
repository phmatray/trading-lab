using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using TradingStrat.Web.Models;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the NotificationToast component.
/// </summary>
public class NotificationToastTests : BunitTestContext
{
    [Fact]
    public void NotificationToast_WithNotification_DisplaysTitleAndMessage()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Test Title",
            Message = "Test message content",
            Severity = NotificationSeverity.Info
        };

        // Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        cut.Markup.ShouldContain("Test Title");
        cut.Markup.ShouldContain("Test message content");
    }

    [Fact]
    public void NotificationToast_Success_AppliesGreenStyling()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Success",
            Message = "Operation completed",
            Severity = NotificationSeverity.Success
        };

        // Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        IElement container = cut.Find("[data-testid='notification-toast']");
        container.ClassList.ShouldContain("bg-green-50");
        container.ClassList.ShouldContain("border-green-200");
    }

    [Fact]
    public void NotificationToast_Warning_AppliesYellowStyling()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Warning",
            Message = "Be careful",
            Severity = NotificationSeverity.Warning
        };

        // Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        IElement container = cut.Find("[data-testid='notification-toast']");
        container.ClassList.ShouldContain("bg-yellow-50");
        container.ClassList.ShouldContain("border-yellow-200");
    }

    [Fact]
    public void NotificationToast_Error_AppliesRedStyling()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Error",
            Message = "Something went wrong",
            Severity = NotificationSeverity.Error
        };

        // Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        IElement container = cut.Find("[data-testid='notification-toast']");
        container.ClassList.ShouldContain("bg-red-50");
        container.ClassList.ShouldContain("border-red-200");
    }

    [Fact]
    public void NotificationToast_Info_AppliesBlueStyling()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Info",
            Message = "Informational message",
            Severity = NotificationSeverity.Info
        };

        // Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        IElement container = cut.Find("[data-testid='notification-toast']");
        container.ClassList.ShouldContain("bg-blue-50");
        container.ClassList.ShouldContain("border-blue-200");
    }

    [Theory]
    [InlineData(NotificationSeverity.Success)]
    [InlineData(NotificationSeverity.Warning)]
    [InlineData(NotificationSeverity.Error)]
    [InlineData(NotificationSeverity.Info)]
    public void NotificationToast_AllSeverities_DisplayIcon(NotificationSeverity severity)
    {
        // Arrange
        var notification = new Notification
        {
            Title = $"{severity} Title",
            Message = "Message",
            Severity = severity
        };

        // Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        IElement svg = cut.Find("svg");
        svg.ShouldNotBeNull();
        svg.ClassList.ShouldContain("w-6");
        svg.ClassList.ShouldContain("h-6");
    }

    [Fact]
    public void NotificationToast_WithAction_DisplaysActionButton()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Action Required",
            Message = "Click to view",
            Severity = NotificationSeverity.Info,
            Action = new NotificationAction
            {
                Label = "View Details",
                TargetPage = "/details"
            }
        };

        // Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        IElement actionButton = cut.Find("button[class*='underline']");
        actionButton.ShouldNotBeNull();
        actionButton.TextContent.ShouldBe("View Details");
    }

    [Fact]
    public void NotificationToast_WithoutAction_DoesNotDisplayActionButton()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "No Action",
            Message = "Just a notification",
            Severity = NotificationSeverity.Info
        };

        // Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        IReadOnlyList<IElement> actionButtons = cut.FindAll("button[class*='underline']");
        actionButtons.ShouldBeEmpty();
    }

    [Fact]
    public void NotificationToast_DismissButton_InvokesOnDismissCallback()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Test",
            Message = "Test",
            Severity = NotificationSeverity.Info
        };

        bool dismissCalled = false;

        // Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification)
            .Add(p => p.OnDismiss, () => dismissCalled = true));

        IElement dismissButton = cut.Find("[data-testid='dismiss-button']");
        dismissButton.Click();

        // Assert
        dismissCalled.ShouldBeTrue();
    }

    [Fact]
    public void NotificationToast_ActionButton_InvokesOnDismissCallback()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Action",
            Message = "Test",
            Severity = NotificationSeverity.Info,
            Action = new NotificationAction { Label = "Click Me", TargetPage = "/page" }
        };

        bool dismissCalled = false;

        // Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification)
            .Add(p => p.OnDismiss, () => dismissCalled = true));

        IElement actionButton = cut.Find("button[class*='underline']");
        actionButton.Click();

        // Assert
        dismissCalled.ShouldBeTrue();
    }

    [Fact]
    public void NotificationToast_HasAccessibilityAttributes()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Accessible",
            Message = "Test",
            Severity = NotificationSeverity.Info
        };

        // Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        IElement container = cut.Find("[role='alert']");
        container.ShouldNotBeNull();
        container.GetAttribute("aria-live").ShouldBe("assertive");
        container.GetAttribute("aria-atomic").ShouldBe("true");
    }

    [Fact]
    public void NotificationToast_DismissButton_HasAriaLabel()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "Test",
            Message = "Test",
            Severity = NotificationSeverity.Info
        };

        // Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification));

        // Assert
        IElement dismissButton = cut.Find("[data-testid='dismiss-button']");
        dismissButton.GetAttribute("aria-label").ShouldBe("Dismiss");
    }

    [Fact]
    public void NotificationToast_WithAutoDismissDisabled_DoesNotAutoDismiss()
    {
        // Arrange
        var notification = new Notification
        {
            Title = "No Auto Dismiss",
            Message = "Test",
            Severity = NotificationSeverity.Info
        };

        // Act - AutoDismissMs = 0 disables auto-dismiss
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, notification)
            .Add(p => p.AutoDismissMs, 0));

        // Assert - component should render without throwing
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact]
    public void NotificationToast_WithNullNotification_RendersEmpty()
    {
        // Arrange & Act
        IRenderedComponent<NotificationToast> cut = Render<NotificationToast>(parameters => parameters
            .Add(p => p.Notification, null));

        // Assert
        // Component should render container but without content
        IElement container = cut.Find("[data-testid='notification-toast']");
        container.ShouldNotBeNull();
    }
}
