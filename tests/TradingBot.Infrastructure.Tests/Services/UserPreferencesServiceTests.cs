// <copyright file="UserPreferencesServiceTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using FakeItEasy;
using Shouldly;
using TradingBot.Core.Entities;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Validators;
using TradingBot.Core.ValueObjects;
using TradingBot.Infrastructure.Services;
using Xunit;

namespace TradingBot.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for UserPreferencesService.
/// Tests must achieve 100% coverage as this is a critical path service.
/// </summary>
public class UserPreferencesServiceTests
{
    private readonly IUserPreferencesRepository _fakeRepository;
    private readonly UserPreferencesService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferencesServiceTests"/> class.
    /// </summary>
    public UserPreferencesServiceTests()
    {
        _fakeRepository = A.Fake<IUserPreferencesRepository>();
        _service = new UserPreferencesService(_fakeRepository);
    }

    [Fact]
    public async Task GetPreferencesAsync_ShouldReturnPreferencesFromRepository()
    {
        // Arrange
        var expectedPreferences = CreateSamplePreferences();
        A.CallTo(() => _fakeRepository.GetByUserIdAsync("default", A<CancellationToken>._))
            .Returns(expectedPreferences);

        // Act
        var result = await _service.GetPreferencesAsync();

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe("default");
        result.Theme.ShouldBe(Theme.Light);
        A.CallTo(() => _fakeRepository.GetByUserIdAsync("default", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_ValidPreferences_ShouldSaveAndReturnSuccess()
    {
        // Arrange
        var preferences = CreateSamplePreferences();
        A.CallTo(() => _fakeRepository.SaveAsync(preferences, A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdatePreferencesAsync(preferences);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        A.CallTo(() => _fakeRepository.SaveAsync(preferences, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_InvalidDashboardRefreshInterval_ShouldReturnError()
    {
        // Arrange
        var preferences = CreateSamplePreferences();
        preferences.DashboardRefreshInterval = 500; // Invalid (must be 1-300)

        // Act
        var result = await _service.UpdatePreferencesAsync(preferences);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("Dashboard refresh interval must be between 1 and 300 seconds"));
        A.CallTo(() => _fakeRepository.SaveAsync(A<UserPreferences>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_InvalidNotificationDuration_ShouldReturnError()
    {
        // Arrange
        var preferences = CreateSamplePreferences();
        preferences.NotificationDuration = 1; // Invalid (must be 2-10)

        // Act
        var result = await _service.UpdatePreferencesAsync(preferences);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("Notification duration must be between 2 and 10 seconds"));
        A.CallTo(() => _fakeRepository.SaveAsync(A<UserPreferences>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_MissingUserId_ShouldReturnError()
    {
        // Arrange
        var preferences = CreateSamplePreferences();
        preferences.UserId = string.Empty;

        // Act
        var result = await _service.UpdatePreferencesAsync(preferences);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("User ID is required"));
        A.CallTo(() => _fakeRepository.SaveAsync(A<UserPreferences>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_NullTheme_ShouldReturnError()
    {
        // Arrange
        var preferences = CreateSamplePreferences();
        preferences.Theme = null!;

        // Act
        var result = await _service.UpdatePreferencesAsync(preferences);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("Theme is required"));
        A.CallTo(() => _fakeRepository.SaveAsync(A<UserPreferences>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_MultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var preferences = CreateSamplePreferences();
        preferences.UserId = string.Empty;
        preferences.Theme = null!;
        preferences.DashboardRefreshInterval = 0;
        preferences.NotificationDuration = 11;

        // Act
        var result = await _service.UpdatePreferencesAsync(preferences);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count.ShouldBe(4);
        A.CallTo(() => _fakeRepository.SaveAsync(A<UserPreferences>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ResetToDefaultAsync_ShouldCallRepositoryResetMethod()
    {
        // Arrange
        var defaultPreferences = CreateSamplePreferences();
        A.CallTo(() => _fakeRepository.ResetToDefaultAsync("default", A<CancellationToken>._))
            .Returns(defaultPreferences);

        // Act
        var result = await _service.ResetToDefaultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Theme.ShouldBe(Theme.Light);
        result.DashboardRefreshInterval.ShouldBe(10);
        A.CallTo(() => _fakeRepository.ResetToDefaultAsync("default", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var preferences = CreateSamplePreferences();
        var cts = new CancellationTokenSource();
        A.CallTo(() => _fakeRepository.SaveAsync(preferences, cts.Token))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdatePreferencesAsync(preferences, cts.Token);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        A.CallTo(() => _fakeRepository.SaveAsync(preferences, cts.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetPreferencesAsync_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var preferences = CreateSamplePreferences();
        var cts = new CancellationTokenSource();
        A.CallTo(() => _fakeRepository.GetByUserIdAsync("default", cts.Token))
            .Returns(preferences);

        // Act
        var result = await _service.GetPreferencesAsync(cts.Token);

        // Assert
        result.ShouldNotBeNull();
        A.CallTo(() => _fakeRepository.GetByUserIdAsync("default", cts.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ResetToDefaultAsync_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var preferences = CreateSamplePreferences();
        var cts = new CancellationTokenSource();
        A.CallTo(() => _fakeRepository.ResetToDefaultAsync("default", cts.Token))
            .Returns(preferences);

        // Act
        var result = await _service.ResetToDefaultAsync(cts.Token);

        // Assert
        result.ShouldNotBeNull();
        A.CallTo(() => _fakeRepository.ResetToDefaultAsync("default", cts.Token))
            .MustHaveHappenedOnceExactly();
    }

    private static UserPreferences CreateSamplePreferences()
    {
        return new UserPreferences
        {
            UserId = "default",
            Theme = Theme.Light,
            DashboardRefreshInterval = 10,
            NotificationDuration = 5,
            ShowSuccessNotifications = true,
            ShowErrorNotifications = true,
            ShowInfoNotifications = true,
            ShowWarningNotifications = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}
