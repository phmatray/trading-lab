// <copyright file="SettingsFormTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using TradingBot.Core.Entities;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Validators;
using TradingBot.Core.ValueObjects;
using TradingBot.Web.Components.Organisms;
using TradingBot.Web.Services;

namespace TradingBot.Web.Tests.Components.Organisms;

/// <summary>
/// Tests for the SettingsForm component.
/// </summary>
public class SettingsFormTests
{
    /// <summary>
    /// Tests that SettingsForm loads user preferences on initialization.
    /// </summary>
    [Fact]
    public void SettingsForm_LoadsUserPreferences_OnInitialization()
    {
        // Arrange
        using var ctx = new BunitContext();
        var mockPreferencesService = A.Fake<IUserPreferencesService>();
        var mockToastService = A.Fake<IToastService>();
        var mockJsRuntime = A.Fake<IJSRuntime>();

        var testPreferences = new UserPreferences
        {
            Id = 1,
            Theme = Theme.Dark,
            DashboardRefreshInterval = 10,
            NotificationDuration = 5,
            ShowSuccessNotifications = true,
            ShowErrorNotifications = true,
            ShowInfoNotifications = false,
            ShowWarningNotifications = true,
        };

        A.CallTo(() => mockPreferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(testPreferences));

        ctx.Services.AddSingleton(mockPreferencesService);
        ctx.Services.AddSingleton(mockToastService);
        ctx.Services.AddSingleton(mockJsRuntime);

        // Act
        var cut = ctx.Render<TbSettingsForm>();

        // Assert
        A.CallTo(() => mockPreferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        var themeSelect = cut.Find("#theme-select");
        themeSelect.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that SettingsForm displays validation error for invalid refresh interval.
    /// </summary>
    [Fact]
    public void SettingsForm_DisplaysValidationError_ForInvalidRefreshInterval()
    {
        // Arrange
        using var ctx = new BunitContext();
        var mockPreferencesService = A.Fake<IUserPreferencesService>();
        var mockToastService = A.Fake<IToastService>();
        var mockJsRuntime = A.Fake<IJSRuntime>();

        var testPreferences = new UserPreferences
        {
            Id = 1,
            Theme = Theme.Light,
            DashboardRefreshInterval = 5,
            NotificationDuration = 5,
        };

        A.CallTo(() => mockPreferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(testPreferences));

        ctx.Services.AddSingleton(mockPreferencesService);
        ctx.Services.AddSingleton(mockToastService);
        ctx.Services.AddSingleton(mockJsRuntime);

        var cut = ctx.Render<TbSettingsForm>();

        // Act - Set invalid value (out of 1-300 range)
        var refreshInput = cut.Find("#refresh-interval");
        refreshInput.Change("500");

        var form = cut.Find("form");
        form.Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var errorMessage = cut.Find("p#refresh-interval-error");
            errorMessage.ShouldNotBeNull();
            errorMessage.TextContent.ShouldContain("between 1 and 300");
        });

        A.CallTo(() => mockPreferencesService.UpdatePreferencesAsync(A<UserPreferences>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    /// <summary>
    /// Tests that SettingsForm displays validation error for invalid notification duration.
    /// </summary>
    [Fact]
    public void SettingsForm_DisplaysValidationError_ForInvalidNotificationDuration()
    {
        // Arrange
        using var ctx = new BunitContext();
        var mockPreferencesService = A.Fake<IUserPreferencesService>();
        var mockToastService = A.Fake<IToastService>();
        var mockJsRuntime = A.Fake<IJSRuntime>();

        var testPreferences = new UserPreferences
        {
            Id = 1,
            Theme = Theme.Light,
            DashboardRefreshInterval = 5,
            NotificationDuration = 5,
        };

        A.CallTo(() => mockPreferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(testPreferences));

        ctx.Services.AddSingleton(mockPreferencesService);
        ctx.Services.AddSingleton(mockToastService);
        ctx.Services.AddSingleton(mockJsRuntime);

        var cut = ctx.Render<TbSettingsForm>();

        // Act - Set invalid value (out of 2-10 range)
        var notificationInput = cut.Find("#notification-duration");
        notificationInput.Change("15");

        var form = cut.Find("form");
        form.Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var errorMessage = cut.Find("p#notification-duration-error");
            errorMessage.ShouldNotBeNull();
            errorMessage.TextContent.ShouldContain("between 2 and 10");
        });

        A.CallTo(() => mockPreferencesService.UpdatePreferencesAsync(A<UserPreferences>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    /// <summary>
    /// Tests that SettingsForm successfully saves valid preferences.
    /// </summary>
    [Fact]
    public void SettingsForm_SavesPreferences_WhenValidDataSubmitted()
    {
        // Arrange
        using var ctx = new BunitContext();
        var mockPreferencesService = A.Fake<IUserPreferencesService>();
        var mockToastService = A.Fake<IToastService>();
        var mockJsRuntime = A.Fake<IJSRuntime>();

        var testPreferences = new UserPreferences
        {
            Id = 1,
            Theme = Theme.Light,
            DashboardRefreshInterval = 5,
            NotificationDuration = 5,
        };

        A.CallTo(() => mockPreferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(testPreferences));

        A.CallTo(() => mockPreferencesService.UpdatePreferencesAsync(A<UserPreferences>._, A<CancellationToken>._))
            .Returns(Task.FromResult(ValidationResult.Success()));

        ctx.Services.AddSingleton(mockPreferencesService);
        ctx.Services.AddSingleton(mockToastService);
        ctx.Services.AddSingleton(mockJsRuntime);

        var cut = ctx.Render<TbSettingsForm>();

        // Act - Change values
        var refreshInput = cut.Find("#refresh-interval");
        refreshInput.Change("30");

        var notificationInput = cut.Find("#notification-duration");
        notificationInput.Change("8");

        var form = cut.Find("form");
        form.Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            A.CallTo(() => mockPreferencesService.UpdatePreferencesAsync(
                A<UserPreferences>.That.Matches(p =>
                    p.DashboardRefreshInterval == 30 &&
                    p.NotificationDuration == 8),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => mockToastService.ShowSuccess(
                A<string>.That.Contains("success"),
                A<string>._,
                A<int>._))
                .MustHaveHappenedOnceExactly();
        });
    }

    /// <summary>
    /// Tests that SettingsForm shows error toast when save fails.
    /// </summary>
    [Fact]
    public void SettingsForm_ShowsErrorToast_WhenSaveFails()
    {
        // Arrange
        using var ctx = new BunitContext();
        var mockPreferencesService = A.Fake<IUserPreferencesService>();
        var mockToastService = A.Fake<IToastService>();
        var mockJsRuntime = A.Fake<IJSRuntime>();

        var testPreferences = new UserPreferences
        {
            Id = 1,
            Theme = Theme.Light,
            DashboardRefreshInterval = 5,
            NotificationDuration = 5,
        };

        A.CallTo(() => mockPreferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(testPreferences));

        A.CallTo(() => mockPreferencesService.UpdatePreferencesAsync(A<UserPreferences>._, A<CancellationToken>._))
            .Returns(Task.FromResult(ValidationResult.Error(new[] { "Database error" })));

        ctx.Services.AddSingleton(mockPreferencesService);
        ctx.Services.AddSingleton(mockToastService);
        ctx.Services.AddSingleton(mockJsRuntime);

        var cut = ctx.Render<TbSettingsForm>();

        // Act
        var form = cut.Find("form");
        form.Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            A.CallTo(() => mockToastService.ShowError(
                A<string>.That.Contains("Failed to save"),
                A<string>._,
                A<int>._))
                .MustHaveHappenedOnceExactly();
        });
    }

    /// <summary>
    /// Tests that SettingsForm toggles notification preferences correctly.
    /// </summary>
    [Fact]
    public void SettingsForm_TogglesNotificationPreferences_Correctly()
    {
        // Arrange
        using var ctx = new BunitContext();
        var mockPreferencesService = A.Fake<IUserPreferencesService>();
        var mockToastService = A.Fake<IToastService>();
        var mockJsRuntime = A.Fake<IJSRuntime>();

        var testPreferences = new UserPreferences
        {
            Id = 1,
            Theme = Theme.Light,
            DashboardRefreshInterval = 5,
            NotificationDuration = 5,
            ShowSuccessNotifications = true,
            ShowErrorNotifications = true,
            ShowInfoNotifications = true,
            ShowWarningNotifications = true,
        };

        A.CallTo(() => mockPreferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(testPreferences));

        A.CallTo(() => mockPreferencesService.UpdatePreferencesAsync(A<UserPreferences>._, A<CancellationToken>._))
            .Returns(Task.FromResult(ValidationResult.Success()));

        ctx.Services.AddSingleton(mockPreferencesService);
        ctx.Services.AddSingleton(mockToastService);
        ctx.Services.AddSingleton(mockJsRuntime);

        var cut = ctx.Render<TbSettingsForm>();

        // Act - Toggle info notifications off
        var infoToggle = cut.Find("#toggle-info");
        infoToggle.Click();

        var form = cut.Find("form");
        form.Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            A.CallTo(() => mockPreferencesService.UpdatePreferencesAsync(
                A<UserPreferences>.That.Matches(p =>
                    p.ShowSuccessNotifications &&
                    p.ShowErrorNotifications &&
                    !p.ShowInfoNotifications &&
                    p.ShowWarningNotifications),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        });
    }

    /// <summary>
    /// Tests that SettingsForm opens reset confirmation modal.
    /// </summary>
    [Fact]
    public void SettingsForm_OpensResetConfirmationModal_WhenResetClicked()
    {
        // Arrange
        using var ctx = new BunitContext();
        var mockPreferencesService = A.Fake<IUserPreferencesService>();
        var mockToastService = A.Fake<IToastService>();
        var mockJsRuntime = A.Fake<IJSRuntime>();

        var testPreferences = new UserPreferences
        {
            Id = 1,
            Theme = Theme.Light,
            DashboardRefreshInterval = 5,
            NotificationDuration = 5,
        };

        A.CallTo(() => mockPreferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(testPreferences));

        ctx.Services.AddSingleton(mockPreferencesService);
        ctx.Services.AddSingleton(mockToastService);
        ctx.Services.AddSingleton(mockJsRuntime);

        var cut = ctx.Render<TbSettingsForm>();

        // Act - Click reset button
        var resetButtons = cut.FindAll("button");
        var resetButton = resetButtons.First(b => b.TextContent.Contains("Reset to Defaults"));
        resetButton.Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var modal = cut.Find("[role='dialog']");
            modal.ShouldNotBeNull();

            var modalTitle = cut.Find("#reset-dialog-title");
            modalTitle.TextContent.ShouldContain("Reset to Defaults");
        });
    }

    /// <summary>
    /// Tests that SettingsForm resets preferences when confirmed.
    /// </summary>
    [Fact]
    public void SettingsForm_ResetsPreferences_WhenConfirmed()
    {
        // Arrange
        using var ctx = new BunitContext();
        var mockPreferencesService = A.Fake<IUserPreferencesService>();
        var mockToastService = A.Fake<IToastService>();
        var mockJsRuntime = A.Fake<IJSRuntime>();

        var testPreferences = new UserPreferences
        {
            Id = 1,
            Theme = Theme.Dark,
            DashboardRefreshInterval = 30,
            NotificationDuration = 8,
        };

        var defaultPreferences = new UserPreferences
        {
            Id = 1,
            Theme = Theme.Light,
            DashboardRefreshInterval = 5,
            NotificationDuration = 5,
        };

        A.CallTo(() => mockPreferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(testPreferences));

        A.CallTo(() => mockPreferencesService.ResetToDefaultAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(defaultPreferences));

        ctx.Services.AddSingleton(mockPreferencesService);
        ctx.Services.AddSingleton(mockToastService);
        ctx.Services.AddSingleton(mockJsRuntime);

        var cut = ctx.Render<TbSettingsForm>();

        // Act - Open modal and confirm
        var resetButtons = cut.FindAll("button");
        var resetButton = resetButtons.First(b => b.TextContent.Contains("Reset to Defaults"));
        resetButton.Click();

        cut.WaitForAssertion(() =>
        {
            var confirmButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Reset");
            confirmButton.Click();
        });

        // Assert
        cut.WaitForAssertion(() =>
        {
            A.CallTo(() => mockPreferencesService.ResetToDefaultAsync(A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => mockToastService.ShowSuccess(
                A<string>.That.Contains("reset"),
                A<string>._,
                A<int>._))
                .MustHaveHappenedOnceExactly();
        });
    }

    /// <summary>
    /// Tests that SettingsForm cancels reset when modal is closed.
    /// </summary>
    [Fact]
    public void SettingsForm_CancelsReset_WhenModalClosed()
    {
        // Arrange
        using var ctx = new BunitContext();
        var mockPreferencesService = A.Fake<IUserPreferencesService>();
        var mockToastService = A.Fake<IToastService>();
        var mockJsRuntime = A.Fake<IJSRuntime>();

        var testPreferences = new UserPreferences
        {
            Id = 1,
            Theme = Theme.Light,
            DashboardRefreshInterval = 5,
            NotificationDuration = 5,
        };

        A.CallTo(() => mockPreferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(testPreferences));

        ctx.Services.AddSingleton(mockPreferencesService);
        ctx.Services.AddSingleton(mockToastService);
        ctx.Services.AddSingleton(mockJsRuntime);

        var cut = ctx.Render<TbSettingsForm>();

        // Act - Open modal and cancel
        var resetButtons = cut.FindAll("button");
        var resetButton = resetButtons.First(b => b.TextContent.Contains("Reset to Defaults"));
        resetButton.Click();

        cut.WaitForAssertion(() =>
        {
            var cancelButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel");
            cancelButton.Click();
        });

        // Assert
        cut.WaitForAssertion(() =>
        {
            var modals = cut.FindAll("[role='dialog']");
            modals.ShouldBeEmpty();
        });

        A.CallTo(() => mockPreferencesService.ResetToDefaultAsync(A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    /// <summary>
    /// Tests that SettingsForm changes theme selection correctly.
    /// </summary>
    [Fact]
    public void SettingsForm_ChangesTheme_Correctly()
    {
        // Arrange
        using var ctx = new BunitContext();
        var mockPreferencesService = A.Fake<IUserPreferencesService>();
        var mockToastService = A.Fake<IToastService>();
        var mockJsRuntime = A.Fake<IJSRuntime>();

        var testPreferences = new UserPreferences
        {
            Id = 1,
            Theme = Theme.Light,
            DashboardRefreshInterval = 5,
            NotificationDuration = 5,
        };

        A.CallTo(() => mockPreferencesService.GetPreferencesAsync(A<CancellationToken>._))
            .Returns(Task.FromResult(testPreferences));

        A.CallTo(() => mockPreferencesService.UpdatePreferencesAsync(A<UserPreferences>._, A<CancellationToken>._))
            .Returns(Task.FromResult(ValidationResult.Success()));

        ctx.Services.AddSingleton(mockPreferencesService);
        ctx.Services.AddSingleton(mockToastService);
        ctx.Services.AddSingleton(mockJsRuntime);

        var cut = ctx.Render<TbSettingsForm>();

        // Act - Change theme to Dark
        var themeSelect = cut.Find("#theme-select");
        themeSelect.Change("Dark");

        var form = cut.Find("form");
        form.Submit();

        // Assert
        cut.WaitForAssertion(() =>
        {
            A.CallTo(() => mockPreferencesService.UpdatePreferencesAsync(
                A<UserPreferences>.That.Matches(p => p.Theme == Theme.Dark),
                A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
        });
    }
}
