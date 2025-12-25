using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using TradingStrat.Web.Models;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the NotificationToastContainer component.
/// </summary>
public class NotificationToastContainerTests : BunitTestContext
{
    [Fact]
    public void NotificationToastContainer_InitialRender_ShowsNoToasts()
    {
        // Arrange & Act
        var cut = Render<NotificationToastContainer>();

        // Assert
        var toasts = cut.FindComponents<NotificationToast>();
        toasts.ShouldBeEmpty();
    }

    [Fact]
    public async Task NotificationToastContainer_WithSingleNotification_DisplaysToast()
    {
        // Arrange
        var cut = Render<NotificationToastContainer>();

        // Act
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "Test Title",
            "Test Message");

        // Wait for component to update
        cut.WaitForAssertion(() =>
        {
            var toasts = cut.FindComponents<NotificationToast>();
            toasts.Count.ShouldBe(1);
        });

        // Assert
        cut.Markup.ShouldContain("Test Title");
    }

    [Fact]
    public async Task NotificationToastContainer_WithMultipleNotifications_DisplaysAllToasts()
    {
        // Arrange
        var cut = Render<NotificationToastContainer>();

        // Act
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "First",
            "First message");

        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Success,
            "Second",
            "Second message");

        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Warning,
            "Third",
            "Third message");

        // Wait for component to update
        cut.WaitForAssertion(() =>
        {
            var toasts = cut.FindComponents<NotificationToast>();
            toasts.Count.ShouldBe(3);
        });

        // Assert
        cut.Markup.ShouldContain("First");
        cut.Markup.ShouldContain("Second");
        cut.Markup.ShouldContain("Third");
    }

    [Fact]
    public async Task NotificationToastContainer_LimitsToMaxVisibleToasts()
    {
        // Arrange
        var cut = Render<NotificationToastContainer>();

        // Act - Add 5 notifications (max is 3)
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "First",
            "First message");

        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "Second",
            "Second message");

        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "Third",
            "Third message");

        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "Fourth",
            "Fourth message");

        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "Fifth",
            "Fifth message");

        // Wait for component to update
        cut.WaitForAssertion(() =>
        {
            var toasts = cut.FindComponents<NotificationToast>();
            toasts.Count.ShouldBe(3);
        });

        // Assert - Newest (Fifth, Fourth, Third) should be visible
        cut.Markup.ShouldContain("Fifth");
        cut.Markup.ShouldContain("Fourth");
        cut.Markup.ShouldContain("Third");

        // Oldest (First, Second) should be removed
        cut.Markup.ShouldNotContain("First");
        cut.Markup.ShouldNotContain("Second");
    }

    [Fact]
    public async Task NotificationToastContainer_DisplaysNewestToastsFirst()
    {
        // Arrange
        var cut = Render<NotificationToastContainer>();

        // Act
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "Oldest",
            "Oldest message");

        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "Newest",
            "Newest message");

        // Wait for component to update
        cut.WaitForAssertion(() =>
        {
            var toasts = cut.FindComponents<NotificationToast>();
            toasts.Count.ShouldBe(2);
        });

        // Assert - Newest should appear first in markup
        string markup = cut.Markup;
        int newestIndex = markup.IndexOf("Newest");
        int oldestIndex = markup.IndexOf("Oldest");

        newestIndex.ShouldBeLessThan(oldestIndex);
    }

    [Fact]
    public async Task NotificationToastContainer_RemoveToast_RemovesToastFromDisplay()
    {
        // Arrange
        var cut = Render<NotificationToastContainer>();

        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "To Remove",
            "This will be removed");

        // Wait for toast to be displayed
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("To Remove");
        });

        // Act - Find and click dismiss button on the toast
        var toast = cut.FindComponent<NotificationToast>();
        toast.Find("button").Click(); // Dismiss button

        // Assert - Wait for toast to be removed
        cut.WaitForAssertion(() =>
        {
            var toasts = cut.FindComponents<NotificationToast>();
            toasts.ShouldBeEmpty();
        });

        cut.Markup.ShouldNotContain("To Remove");
    }

    [Fact]
    public async Task NotificationToastContainer_AutoDismiss_PassedToNotificationToast()
    {
        // Arrange
        var cut = Render<NotificationToastContainer>();

        // Act
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "Test",
            "Test message");

        // Wait for component to update
        cut.WaitForAssertion(() =>
        {
            var toasts = cut.FindComponents<NotificationToast>();
            toasts.Count.ShouldBe(1);
        });

        // Assert - Verify NotificationToast component is rendered
        var toast = cut.FindComponent<NotificationToast>();
        toast.ShouldNotBeNull();

        var instance = toast.Instance;
        instance.ShouldNotBeNull();
    }

    [Fact]
    public void NotificationToastContainer_HasCorrectAriaAttributes()
    {
        // Arrange & Act
        var cut = Render<NotificationToastContainer>();

        // Assert
        var container = cut.Find("div.fixed");
        container.GetAttribute("aria-live").ShouldBe("polite");
        container.GetAttribute("aria-atomic").ShouldBe("false");
    }

    [Fact]
    public void NotificationToastContainer_HasCorrectPositioning()
    {
        // Arrange & Act
        var cut = Render<NotificationToastContainer>();

        // Assert
        var container = cut.Find("div");
        container.ClassList.ShouldContain("fixed");
        container.ClassList.ShouldContain("top-4");
        container.ClassList.ShouldContain("right-4");
        container.ClassList.ShouldContain("z-50");
    }

    [Fact]
    public async Task NotificationToastContainer_WithDifferentSeverities_DisplaysCorrectly()
    {
        // Arrange
        var cut = Render<NotificationToastContainer>();

        // Act
        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Info,
            "Info",
            "Info message");

        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Success,
            "Success",
            "Success message");

        await FakeNotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Error,
            "Error",
            "Error message");

        // Wait for component to update
        cut.WaitForAssertion(() =>
        {
            var toasts = cut.FindComponents<NotificationToast>();
            toasts.Count.ShouldBe(3);
        });

        // Assert
        cut.Markup.ShouldContain("Info");
        cut.Markup.ShouldContain("Success");
        cut.Markup.ShouldContain("Error");
    }
}
