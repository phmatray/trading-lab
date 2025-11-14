// <copyright file="UserPreferencesValidatorTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Entities;
using TradingBot.Core.Validators;
using TradingBot.Core.ValueObjects;

namespace TradingBot.Core.Tests.Validators;

/// <summary>
/// Tests for <see cref="UserPreferencesValidator"/>.
/// </summary>
public class UserPreferencesValidatorTests
{
    private readonly UserPreferencesValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferencesValidatorTests"/> class.
    /// </summary>
    public UserPreferencesValidatorTests()
    {
        _validator = new UserPreferencesValidator();
    }

    [Fact]
    public void Validate_ValidPreferences_ReturnsSuccess()
    {
        // Arrange
        var preferences = new UserPreferences
        {
            UserId = "test-user",
            Theme = Theme.Light,
            DashboardRefreshInterval = 10,
            NotificationDuration = 5,
        };

        // Act
        var result = _validator.Validate(preferences);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    [InlineData(500)]
    public void Validate_InvalidDashboardRefreshInterval_ReturnsError(int interval)
    {
        // Arrange
        var preferences = new UserPreferences
        {
            UserId = "test-user",
            Theme = Theme.Light,
            DashboardRefreshInterval = interval,
            NotificationDuration = 5,
        };

        // Act
        var result = _validator.Validate(preferences);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("Dashboard refresh interval must be between 1 and 300 seconds"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(150)]
    [InlineData(300)]
    public void Validate_ValidDashboardRefreshInterval_ReturnsSuccess(int interval)
    {
        // Arrange
        var preferences = new UserPreferences
        {
            UserId = "test-user",
            Theme = Theme.Light,
            DashboardRefreshInterval = interval,
            NotificationDuration = 5,
        };

        // Act
        var result = _validator.Validate(preferences);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void Validate_InvalidNotificationDuration_ReturnsError(int duration)
    {
        // Arrange
        var preferences = new UserPreferences
        {
            UserId = "test-user",
            Theme = Theme.Light,
            DashboardRefreshInterval = 10,
            NotificationDuration = duration,
        };

        // Act
        var result = _validator.Validate(preferences);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("Notification duration must be between 2 and 10 seconds"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void Validate_ValidNotificationDuration_ReturnsSuccess(int duration)
    {
        // Arrange
        var preferences = new UserPreferences
        {
            UserId = "test-user",
            Theme = Theme.Light,
            DashboardRefreshInterval = 10,
            NotificationDuration = duration,
        };

        // Act
        var result = _validator.Validate(preferences);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validate_MissingUserId_ReturnsError(string? userId)
    {
        // Arrange
        var preferences = new UserPreferences
        {
            UserId = userId!,
            Theme = Theme.Light,
            DashboardRefreshInterval = 10,
            NotificationDuration = 5,
        };

        // Act
        var result = _validator.Validate(preferences);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("User ID is required"));
    }

    [Fact]
    public void Validate_NullTheme_ReturnsError()
    {
        // Arrange
        var preferences = new UserPreferences
        {
            UserId = "test-user",
            Theme = null!,
            DashboardRefreshInterval = 10,
            NotificationDuration = 5,
        };

        // Act
        var result = _validator.Validate(preferences);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("Theme is required"));
    }

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var preferences = new UserPreferences
        {
            UserId = string.Empty,
            Theme = null!,
            DashboardRefreshInterval = 500,
            NotificationDuration = 1,
        };

        // Act
        var result = _validator.Validate(preferences);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count.ShouldBe(4);
    }
}
