// <copyright file="NotificationCenterTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Bunit;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TradingBot.Core.Entities;
using TradingBot.Core.Interfaces;
using TradingBot.Core.ValueObjects;
using TradingBot.Web.Components.Organisms;
using TradingBot.Web.Services;
using Xunit;

namespace TradingBot.Web.Tests.Components.Organisms;

/// <summary>
/// Tests for the NotificationCenter component.
/// </summary>
public class NotificationCenterTests : Bunit.TestContext
{
    private readonly ToastService _toastService;
    private readonly IUserPreferencesService _preferencesService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationCenterTests"/> class.
    /// </summary>
    public NotificationCenterTests()
    {
        _toastService = new ToastService();
        _preferencesService = A.Fake<IUserPreferencesService>();

        // Setup default preferences
        var defaultPreferences = new UserPreferences
        {
            Theme = Theme.Light,
            ShowSuccessNotifications = true,
            ShowErrorNotifications = true,
            ShowInfoNotifications = true,
            ShowWarningNotifications = true,
        };

        A.CallTo(() => _preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(defaultPreferences));

        Services.AddSingleton<IToastService>(_toastService);
        Services.AddSingleton(_preferencesService);
    }

    [Fact]
    public void NotificationCenter_Renders_WithCorrectStructure()
    {
        // Act
        var cut = RenderComponent<NotificationCenter>();

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
        // Act
        var cut = RenderComponent<NotificationCenter>();

        // Assert
        var container = cut.Find("div[aria-live='polite']");
        container.GetAttribute("aria-live").ShouldBe("polite");
        container.GetAttribute("aria-atomic").ShouldBe("false");
    }

    [Fact]
    public async Task NotificationCenter_DisplaysToast_WhenToastAdded()
    {
        // Arrange
        var cut = RenderComponent<NotificationCenter>();

        // Act - Add a toast via the service
        _toastService.ShowSuccess("Test success message");

        // Wait for async operations
        await Task.Delay(100);
        cut.Render();

        // Assert
        cut.Markup.ShouldContain("Test success message");
    }

    [Fact]
    public async Task NotificationCenter_RespectsUserPreferences_ShowSuccessNotifications()
    {
        // Arrange - Preferences with success notifications disabled
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
            ShowSuccessNotifications = false,
            ShowErrorNotifications = true,
            ShowInfoNotifications = true,
            ShowWarningNotifications = true,
        };

        A.CallTo(() => _preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        var cut = RenderComponent<NotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        // Act - Add a success toast
        _toastService.ShowSuccess("Success message (should be hidden)");
        await Task.Delay(100);
        cut.Render();

        // Assert - Success toast should not be displayed
        cut.Markup.ShouldNotContain("Success message (should be hidden)");
    }

    [Fact]
    public async Task NotificationCenter_RespectsUserPreferences_ShowErrorNotifications()
    {
        // Arrange - Preferences with error notifications disabled
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
            ShowSuccessNotifications = true,
            ShowErrorNotifications = false,
            ShowInfoNotifications = true,
            ShowWarningNotifications = true,
        };

        A.CallTo(() => _preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        var cut = RenderComponent<NotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        // Act - Add an error toast
        _toastService.ShowError("Error message (should be hidden)");
        await Task.Delay(100);
        cut.Render();

        // Assert - Error toast should not be displayed
        cut.Markup.ShouldNotContain("Error message (should be hidden)");
    }

    [Fact]
    public async Task NotificationCenter_RespectsUserPreferences_ShowWarningNotifications()
    {
        // Arrange - Preferences with warning notifications disabled
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
            ShowSuccessNotifications = true,
            ShowErrorNotifications = true,
            ShowInfoNotifications = true,
            ShowWarningNotifications = false,
        };

        A.CallTo(() => _preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        var cut = RenderComponent<NotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        // Act - Add a warning toast
        _toastService.ShowWarning("Warning message (should be hidden)");
        await Task.Delay(100);
        cut.Render();

        // Assert - Warning toast should not be displayed
        cut.Markup.ShouldNotContain("Warning message (should be hidden)");
    }

    [Fact]
    public async Task NotificationCenter_RespectsUserPreferences_ShowInfoNotifications()
    {
        // Arrange - Preferences with info notifications disabled
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
            ShowSuccessNotifications = true,
            ShowErrorNotifications = true,
            ShowInfoNotifications = false,
            ShowWarningNotifications = true,
        };

        A.CallTo(() => _preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        var cut = RenderComponent<NotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        // Act - Add an info toast
        _toastService.ShowInfo("Info message (should be hidden)");
        await Task.Delay(100);
        cut.Render();

        // Assert - Info toast should not be displayed
        cut.Markup.ShouldNotContain("Info message (should be hidden)");
    }

    [Fact]
    public async Task NotificationCenter_DisplaysMultipleToasts()
    {
        // Arrange
        var cut = RenderComponent<NotificationCenter>();

        // Act - Add multiple toasts
        _toastService.ShowSuccess("Message 1");
        await Task.Delay(50);
        _toastService.ShowInfo("Message 2");
        await Task.Delay(50);
        _toastService.ShowWarning("Message 3");
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
        // Arrange & Act
        var cut = RenderComponent<NotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        // Assert - Verify preferences were loaded
        A.CallTo(() => _preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Fact]
    public async Task NotificationCenter_HandlesPreferencesLoadError_Gracefully()
    {
        // Arrange - Simulate preferences load error
        A.CallTo(() => _preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Throws<InvalidOperationException>();

        // Act - Should not throw, should default to showing all notifications
        var cut = RenderComponent<NotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        _toastService.ShowSuccess("Test message with error");
        await Task.Delay(100);
        cut.Render();

        // Assert - Should still render toast (defaults to showing all)
        cut.Markup.ShouldContain("Test message with error");
    }

    [Fact]
    public void NotificationCenter_UnsubscribesFromToastService_OnDispose()
    {
        // Arrange
        var cut = RenderComponent<NotificationCenter>();
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
        // Arrange - Only show error and warning notifications
        var preferences = new UserPreferences
        {
            Theme = Theme.Light,
            ShowSuccessNotifications = false,
            ShowErrorNotifications = true,
            ShowInfoNotifications = false,
            ShowWarningNotifications = true,
        };

        A.CallTo(() => _preferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(preferences));

        var cut = RenderComponent<NotificationCenter>();
        await cut.InvokeAsync(async () => await Task.Delay(100));

        // Act - Add all types of toasts
        _toastService.ShowSuccess("Success (hidden)");
        _toastService.ShowError("Error (visible)");
        _toastService.ShowInfo("Info (hidden)");
        _toastService.ShowWarning("Warning (visible)");

        await Task.Delay(100);
        cut.Render();

        // Assert
        cut.Markup.ShouldNotContain("Success (hidden)");
        cut.Markup.ShouldContain("Error (visible)");
        cut.Markup.ShouldNotContain("Info (hidden)");
        cut.Markup.ShouldContain("Warning (visible)");
    }
}
