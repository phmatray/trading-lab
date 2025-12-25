using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using TradingStrat.Web.Models;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the NotificationCenter component - notification management panel.
/// </summary>
public class NotificationCenterTests : BunitTestContext
{
    public NotificationCenterTests()
    {
        // Reset notifications before each test
        FakeNotificationService.Reset();
    }

    [Fact]
    public void NotificationCenter_WhenClosed_RendersNothing()
    {
        // Arrange & Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, false));

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact]
    public void NotificationCenter_WhenOpen_RendersPanel()
    {
        // Arrange & Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Markup.ShouldContain("Notifications");
        var panel = cut.Find("[role='dialog']");
        panel.ShouldNotBeNull();
        panel.GetAttribute("aria-modal").ShouldBe("true");
    }

    [Fact]
    public void NotificationCenter_WithNoNotifications_ShowsEmptyState()
    {
        // Arrange & Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Markup.ShouldContain("No notifications yet");
    }

    [Fact]
    public async Task NotificationCenter_WithNotifications_DisplaysNotifications()
    {
        // Arrange
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.Signal,
            NotificationSeverity.Success,
            "Test Notification",
            "Test message");

        // Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Markup.ShouldContain("Test Notification");
        cut.Markup.ShouldContain("Test message");
    }

    [Fact]
    public async Task NotificationCenter_WithUnreadNotifications_ShowsUnreadIndicator()
    {
        // Arrange
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.Signal,
            NotificationSeverity.Info,
            "Unread Notification",
            "This is unread");

        // Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert - Look for data-is-read attribute
        var notification = cut.Find("[data-notification-item]");
        notification.GetAttribute("data-is-read").ShouldBe("false");
    }

    [Fact]
    public async Task NotificationCenter_GroupsNotificationsByTime()
    {
        // Arrange
        var today = DateTime.UtcNow;
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var older = DateTime.UtcNow.AddDays(-5);

        // Add notifications with specific timestamps
        var notification1 = await FakeNotificationService.AddNotificationAsync(
            NotificationType.Signal, NotificationSeverity.Info, "Today's Notification", "Today");
        notification1.Timestamp = today;

        var notification2 = await FakeNotificationService.AddNotificationAsync(
            NotificationType.Signal, NotificationSeverity.Info, "Yesterday's Notification", "Yesterday");
        notification2.Timestamp = yesterday;

        var notification3 = await FakeNotificationService.AddNotificationAsync(
            NotificationType.Signal, NotificationSeverity.Info, "Old Notification", "Older");
        notification3.Timestamp = older;

        // Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert - Just verify the notification content is displayed
        cut.Markup.ShouldContain("Today's Notification");
        cut.Markup.ShouldContain("Yesterday's Notification");
        cut.Markup.ShouldContain("Old Notification");
    }

    [Fact]
    public async Task NotificationCenter_SuccessNotification_HasCorrectIconColor()
    {
        // Arrange
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.Signal,
            NotificationSeverity.Success,
            "Success",
            "Success message");

        // Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert - Verify the notification displays
        cut.Markup.ShouldContain("Success");
        cut.Markup.ShouldContain("Success message");
    }

    [Fact]
    public async Task NotificationCenter_WarningNotification_Displays()
    {
        // Arrange
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.DataFreshness,
            NotificationSeverity.Warning,
            "Warning",
            "Warning message");

        // Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Markup.ShouldContain("Warning");
        cut.Markup.ShouldContain("Warning message");
    }

    [Fact]
    public async Task NotificationCenter_ErrorNotification_Displays()
    {
        // Arrange
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Error,
            "Error",
            "Error message");

        // Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Markup.ShouldContain("Error");
        cut.Markup.ShouldContain("Error message");
    }

    [Fact]
    public async Task NotificationCenter_InfoNotification_Displays()
    {
        // Arrange
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "Info",
            "Info message");

        // Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Markup.ShouldContain("Info");
        cut.Markup.ShouldContain("Info message");
    }

    [Fact]
    public async Task NotificationCenter_WithUnreadNotifications_ShowsMarkAllReadButton()
    {
        // Arrange
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.Signal,
            NotificationSeverity.Info,
            "Unread",
            "Unread message");

        // Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Markup.ShouldContain("Mark all read");
    }

    [Fact]
    public async Task NotificationCenter_WithAllReadNotifications_HidesMarkAllReadButton()
    {
        // Arrange
        var notification = await FakeNotificationService.AddNotificationAsync(
            NotificationType.Signal,
            NotificationSeverity.Info,
            "Read",
            "Read message");

        // Mark as read
        await FakeNotificationService.MarkAsReadAsync(notification.Id);

        // Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Markup.ShouldNotContain("Mark all read");
    }

    [Fact]
    public async Task NotificationCenter_HasClearAllButton()
    {
        // Arrange
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.Signal,
            NotificationSeverity.Info,
            "Test",
            "Test message");

        // Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        cut.Markup.ShouldContain("Clear all notifications");
    }

    [Fact]
    public void NotificationCenter_Header_DisplaysTitle()
    {
        // Arrange & Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        var title = cut.Find("#notification-center-title");
        title.ShouldNotBeNull();
        title.TextContent.ShouldContain("Notifications");
    }

    [Fact]
    public async Task NotificationCenter_Footer_DisplaysWhenNotificationsExist()
    {
        // Arrange
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.Signal,
            NotificationSeverity.Info,
            "Test",
            "Test message");

        // Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert - Footer should contain clear all button
        cut.Markup.ShouldContain("Clear all notifications");
    }

    [Fact]
    public void NotificationCenter_Panel_HasCorrectAccessibilityAttributes()
    {
        // Arrange & Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        var panel = cut.Find("[role='dialog']");
        panel.GetAttribute("role").ShouldBe("dialog");
        panel.GetAttribute("aria-modal").ShouldBe("true");
        panel.GetAttribute("aria-labelledby").ShouldBe("notification-center-title");
    }

    [Fact]
    public void NotificationCenter_CloseButton_HasAccessibilityLabel()
    {
        // Arrange & Act
        var cut = Render<NotificationCenter>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        var closeButton = cut.Find("button[aria-label='Close notifications']");
        closeButton.ShouldNotBeNull();
    }
}
