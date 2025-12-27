using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using TradingStrat.Web.Models;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the NotificationBell component.
/// </summary>
public class NotificationBellTests : BunitTestContext
{
    [Fact]
    public void NotificationBell_Renders_Successfully()
    {
        // Arrange & Act
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>();

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement bell = cut.Find("[data-testid='notification-bell']");
        bell.ShouldNotBeNull();
    }

    [Fact]
    public void NotificationBell_HasBellIcon()
    {
        // Arrange & Act
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>();

        // Assert
        IElement svg = cut.Find("svg");
        svg.ShouldNotBeNull();
        svg.ClassList.ShouldContain("w-6");
        svg.ClassList.ShouldContain("h-6");
    }

    [Fact]
    public void NotificationBell_HasAccessibilityAttributes()
    {
        // Arrange & Act
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>();

        // Assert
        IElement button = cut.Find("[data-testid='notification-bell']");
        button.GetAttribute("aria-label").ShouldBe("Notifications");
        button.GetAttribute("type").ShouldBe("button");
        button.GetAttribute("aria-describedby").ShouldNotBeNullOrEmpty();
        button.GetAttribute("aria-expanded").ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task NotificationBell_WithUnreadNotifications_DisplaysBadge()
    {
        // Arrange - Add unread notifications to the service
        await FakeNotificationService.AddNotificationAsync(
            Web.Models.NotificationType.System,
            Web.Models.NotificationSeverity.Info,
            "Test 1",
            "Message 1");

        await FakeNotificationService.AddNotificationAsync(
            Web.Models.NotificationType.System,
            Web.Models.NotificationSeverity.Info,
            "Test 2",
            "Message 2");

        // Act
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>();

        // Wait for OnAfterRenderAsync to complete
        await Task.Delay(100);
        cut.Render();

        // Assert
        IElement badge = cut.Find("[data-testid='notification-badge']");
        badge.ShouldNotBeNull();
        badge.TextContent.ShouldBe("2");
    }

    [Fact]
    public async Task NotificationBell_WithNoUnreadNotifications_DoesNotDisplayBadge()
    {
        // Arrange - Add a notification and mark it as read
        Notification notification = await FakeNotificationService.AddNotificationAsync(
            Web.Models.NotificationType.System,
            Web.Models.NotificationSeverity.Info,
            "Read",
            "Already read");
        await FakeNotificationService.MarkAsReadAsync(notification.Id);

        // Act
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>();
        await Task.Delay(100);
        cut.Render();

        // Assert
        IReadOnlyList<IElement> badges = cut.FindAll("[data-testid='notification-badge']");
        badges.ShouldBeEmpty();
    }

    [Fact]
    public async Task NotificationBell_WithMoreThan99Unread_DisplaysPlus()
    {
        // Arrange - Add 100+ unread notifications
        for (int i = 0; i < 100; i++)
        {
            await FakeNotificationService.AddNotificationAsync(
                Web.Models.NotificationType.System,
                Web.Models.NotificationSeverity.Info,
                $"Test {i}",
                $"Message {i}");
        }

        // Act
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>();
        await Task.Delay(100);
        cut.Render();

        // Assert
        IElement badge = cut.Find("[data-testid='notification-badge']");
        badge.TextContent.ShouldBe("99+");
    }

    [Fact]
    public void NotificationBell_Click_TogglesPanel()
    {
        // Arrange
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>();
        IElement button = cut.Find("[data-testid='notification-bell']");

        // Get initial aria-expanded state
        string? initialExpanded = button.GetAttribute("aria-expanded");

        // Act - First click
        button.Click();
        string? afterFirstClick = button.GetAttribute("aria-expanded");

        // Act - Second click
        button.Click();
        string? afterSecondClick = button.GetAttribute("aria-expanded");

        // Assert
        initialExpanded.ShouldBe("false");
        afterFirstClick.ShouldBe("true");
        afterSecondClick.ShouldBe("false");
    }

    [Fact]
    public async Task NotificationBell_Badge_HasAriaLive()
    {
        // Arrange - Setup notifications first
        await FakeNotificationService.AddNotificationAsync(
            Web.Models.NotificationType.System,
            Web.Models.NotificationSeverity.Info,
            "Test",
            "Test");

        // Act
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>();
        await Task.Delay(100);
        cut.Render();

        // Assert
        IElement badge = cut.Find("[data-testid='notification-badge']");
        badge.GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public async Task NotificationBell_BadgeCount_HasCorrectStyling()
    {
        // Arrange
        await FakeNotificationService.AddNotificationAsync(
            Web.Models.NotificationType.System,
            Web.Models.NotificationSeverity.Info,
            "Test",
            "Test");

        // Act
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>();
        await Task.Delay(100);
        cut.Render();

        // Assert
        IElement badge = cut.Find("[data-testid='notification-badge']");
        badge.ClassList.ShouldContain("absolute");
        badge.ClassList.ShouldContain("bg-red-600");
        badge.ClassList.ShouldContain("text-white");
        badge.ClassList.ShouldContain("rounded-full");
    }

    [Fact]
    public async Task NotificationBell_UpdatesCountWhenNotificationServiceChanges()
    {
        // Arrange
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>();
        await Task.Delay(100);
        cut.Render();

        // Initially no badge
        IReadOnlyList<IElement> initialBadges = cut.FindAll("[data-testid='notification-badge']");
        initialBadges.ShouldBeEmpty();

        // Act - Add a notification
        await FakeNotificationService.AddNotificationAsync(
            Web.Models.NotificationType.System,
            Web.Models.NotificationSeverity.Info,
            "New",
            "New message");

        // Give the component time to react to the event
        await Task.Delay(100);
        cut.Render();

        // Assert - Badge should now appear
        IElement badge = cut.Find("[data-testid='notification-badge']");
        badge.ShouldNotBeNull();
        badge.TextContent.ShouldBe("1");
    }

    [Fact]
    public void NotificationBell_RendersNotificationCenter()
    {
        // Arrange
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>();
        IElement button = cut.Find("[data-testid='notification-bell']");

        // Act - Click to open the notification center
        button.Click();

        // Assert - NotificationCenter panel is now visible
        IElement dialog = cut.Find("[role='dialog']");
        dialog.ShouldNotBeNull();
        dialog.GetAttribute("aria-labelledby").ShouldBe("notification-center-title");
    }

    [Fact]
    public void NotificationBell_ButtonHasHoverStyles()
    {
        // Arrange & Act
        IRenderedComponent<NotificationBell> cut = Render<NotificationBell>();

        // Assert
        IElement button = cut.Find("[data-testid='notification-bell']");
        button.ClassList.ShouldContain("hover:bg-gray-100");
        button.ClassList.ShouldContain("focus:outline-none");
        button.ClassList.ShouldContain("focus:ring-2");
        button.ClassList.ShouldContain("focus:ring-trading-blue");
    }
}
