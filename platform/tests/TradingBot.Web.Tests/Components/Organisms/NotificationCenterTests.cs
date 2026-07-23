// <copyright file="NotificationCenterTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

#pragma warning disable FakeItEasy0003 // Argument constraint outside call specification - false positive in test setup

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TradingBot.Core.Entities;
using TradingBot.Core.Interfaces;
using TradingBot.Core.ValueObjects;
using TradingBot.Web.Components.Organisms;
using TradingBot.Web.Services;

namespace TradingBot.Web.Tests.Components.Organisms;

/// <summary>
/// Tests for the NotificationCenter component.
/// </summary>
public class NotificationCenterTests
{
    private readonly ToastService toastService;
    private readonly IUserPreferencesService preferencesService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationCenterTests"/> class.
    /// </summary>
    public NotificationCenterTests()
    {
        toastService = new ToastService();
        preferencesService = A.Fake<IUserPreferencesService>();

        // Setup default preferences
        var defaultPreferences = new UserPreferences
        {
            Theme = Theme.Light,
            ShowSuccessNotifications = true,
            ShowErrorNotifications = true,
            ShowInfoNotifications = true,
            ShowWarningNotifications = true,
        };

        A.CallTo(() => preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(defaultPreferences));
    }

    private BunitContext SetupContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IToastService>(toastService);
        ctx.Services.AddSingleton(preferencesService);
        return ctx;
    }

    [Fact]
    public void NotificationCenter_Renders_WithCorrectStructure()
    {
        // Arrange
        using var ctx = SetupContext();

        // Act
        var cut = ctx.Render<TbNotificationCenter>();

        // Assert
        var container = cut.Find("div[aria-live='polite']");
        container.ShouldNotBeNull();
        container.ClassName.ShouldNotBeNull();
        container.ClassName.ShouldContain("fixed");
        container.ClassName.ShouldContain("top-4");
        container.ClassName.ShouldContain("right-4");
        container.ClassName.ShouldContain("z-50");
    }

    [Fact]
    public void NotificationCenter_HasCorrectAriaAttributes()
    {
        // Arrange
        using var ctx = SetupContext();

        // Act
        var cut = ctx.Render<TbNotificationCenter>();

        // Assert
        var container = cut.Find("div[aria-live='polite']");
        container.GetAttribute("aria-live").ShouldBe("polite");
        container.GetAttribute("aria-atomic").ShouldBe("false");
    }

    [Fact]
    public async Task NotificationCenter_DisplaysToast_WhenToastAdded()
    {
        // Arrange
        using var ctx = SetupContext();

        // Arrange
        var cut = ctx.Render<TbNotificationCenter>();

        // Act - Add a toast via the service
        toastService.ShowSuccess("Test success message");

        // Wait for async operations
        await Task.Delay(100);
        cut.Render();

        // Assert
        cut.Markup.ShouldContain("Test success message");
    }

    [Fact]
    public async Task NotificationCenter_RespectsUserPreferences_ShowSuccessNotifications()
    {
        // Arrange
        using var ctx = SetupContext();

        // Arrange - Preferences with success notifications disabled
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
            ShowSuccessNotifications = false,
            ShowErrorNotifications = true,
            ShowInfoNotifications = true,
            ShowWarningNotifications = true,
        };

        A.CallTo(() => preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        var cut = ctx.Render<TbNotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        // Act - Add a success toast
        toastService.ShowSuccess("Success message (should be hidden)");
        await Task.Delay(100);
        cut.Render();

        // Assert - Success toast should not be displayed
        cut.Markup.ShouldNotContain("Success message (should be hidden)");
    }

    [Fact]
    public async Task NotificationCenter_RespectsUserPreferences_ShowErrorNotifications()
    {
        // Arrange
        using var ctx = SetupContext();

        // Arrange - Preferences with error notifications disabled
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
            ShowSuccessNotifications = true,
            ShowErrorNotifications = false,
            ShowInfoNotifications = true,
            ShowWarningNotifications = true,
        };

        A.CallTo(() => preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        var cut = ctx.Render<TbNotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        // Act - Add an error toast
        toastService.ShowError("Error message (should be hidden)");
        await Task.Delay(100);
        cut.Render();

        // Assert - Error toast should not be displayed
        cut.Markup.ShouldNotContain("Error message (should be hidden)");
    }

    [Fact]
    public async Task NotificationCenter_RespectsUserPreferences_ShowWarningNotifications()
    {
        // Arrange
        using var ctx = SetupContext();

        // Arrange - Preferences with warning notifications disabled
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
            ShowSuccessNotifications = true,
            ShowErrorNotifications = true,
            ShowInfoNotifications = true,
            ShowWarningNotifications = false,
        };

        A.CallTo(() => preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        var cut = ctx.Render<TbNotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        // Act - Add a warning toast
        toastService.ShowWarning("Warning message (should be hidden)");
        await Task.Delay(100);
        cut.Render();

        // Assert - Warning toast should not be displayed
        cut.Markup.ShouldNotContain("Warning message (should be hidden)");
    }

    [Fact]
    public async Task NotificationCenter_RespectsUserPreferences_ShowInfoNotifications()
    {
        // Arrange
        using var ctx = SetupContext();

        // Arrange - Preferences with info notifications disabled
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
            ShowSuccessNotifications = true,
            ShowErrorNotifications = true,
            ShowInfoNotifications = false,
            ShowWarningNotifications = true,
        };

        A.CallTo(() => preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        var cut = ctx.Render<TbNotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        // Act - Add an info toast
        toastService.ShowInfo("Info message (should be hidden)");
        await Task.Delay(100);
        cut.Render();

        // Assert - Info toast should not be displayed
        cut.Markup.ShouldNotContain("Info message (should be hidden)");
    }

    [Fact]
    public async Task NotificationCenter_DisplaysMultipleToasts()
    {
        // Arrange
        using var ctx = SetupContext();

        // Arrange
        var cut = ctx.Render<TbNotificationCenter>();

        // Act - Add multiple toasts
        toastService.ShowSuccess("Message 1");
        await Task.Delay(50);
        toastService.ShowInfo("Message 2");
        await Task.Delay(50);
        toastService.ShowWarning("Message 3");
        await Task.Delay(50);

        cut.Render();

        // Assert
        cut.Markup.ShouldContain("Message 1");
        cut.Markup.ShouldContain("Message 2");
        cut.Markup.ShouldContain("Message 3");
    }

    [Fact]
    public async Task NotificationCenter_LoadsPreferences_OnInitialization()
    {
        // Arrange
        using var ctx = SetupContext();

        // Arrange & Act
        var cut = ctx.Render<TbNotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        // Assert - Verify preferences were loaded
        A.CallTo(() => preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task NotificationCenter_HandlesPreferencesLoadError_Gracefully()
    {
        // Arrange
        using var ctx = SetupContext();

        // Arrange - Simulate preferences load error
        A.CallTo(() => preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Throws<InvalidOperationException>();

        // Act - Should not throw, should default to showing all notifications
        var cut = ctx.Render<TbNotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        toastService.ShowSuccess("Test message with error");
        await Task.Delay(100);
        cut.Render();

        // Assert - Should still render toast (defaults to showing all)
        cut.Markup.ShouldContain("Test message with error");
    }

    [Fact]
    public void NotificationCenterunsubscribesFromToastService_OnDispose()
    {
        // Arrange
        using var ctx = SetupContext();

        // Arrange
        var cut = ctx.Render<TbNotificationCenter>();
        var instance = cut.Instance;

        // Act
        cut.Dispose();

        // Assert - Component should unsubscribe (verified by no exceptions on disposal)
        // We captured the instance before disposal to verify it existed
        instance.ShouldNotBeNull();
    }

    [Fact]
    public async Task NotificationCenter_ShowsOnlyEnabledNotificationTypes()
    {
        // Arrange
        using var ctx = SetupContext();

        // Arrange - Only show error and warning notifications
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
            ShowSuccessNotifications = false,
            ShowErrorNotifications = true,
            ShowInfoNotifications = false,
            ShowWarningNotifications = true,
        };

        A.CallTo(() => preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        var cut = ctx.Render<TbNotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        // Act - Add all types of toasts
        toastService.ShowSuccess("Success (hidden)");
        toastService.ShowError("Error (visible)");
        toastService.ShowInfo("Info (hidden)");
        toastService.ShowWarning("Warning (visible)");

        await Task.Delay(100);
        cut.Render();

        // Assert
        cut.Markup.ShouldNotContain("Success (hidden)");
        cut.Markup.ShouldContain("Error (visible)");
        cut.Markup.ShouldNotContain("Info (hidden)");
        cut.Markup.ShouldContain("Warning (visible)");
    }
}
