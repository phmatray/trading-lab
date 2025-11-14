// <copyright file="UserPreferencesRepositoryTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradingBot.Core.Entities;
using TradingBot.Core.ValueObjects;
using TradingBot.Infrastructure.Persistence;
using TradingBot.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TradingBot.Infrastructure.Tests.Persistence.Repositories;

/// <summary>
/// Unit tests for UserPreferencesRepository.
/// </summary>
public class UserPreferencesRepositoryTests : IDisposable
{
    private readonly TradingBotDbContext _context;
    private readonly UserPreferencesRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferencesRepositoryTests"/> class.
    /// </summary>
    public UserPreferencesRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TradingBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TradingBotDbContext(options);
        _repository = new UserPreferencesRepository(_context);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenPreferencesExist_ShouldReturnPreferences()
    {
        // Arrange
        var preferences = CreateSamplePreferences("user123");
        await _context.UserPreferences.AddAsync(preferences);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync("user123");

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe("user123");
        result.Theme.ShouldBe(Theme.Light);
        result.DashboardRefreshInterval.ShouldBe(10);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenPreferencesDoNotExist_ShouldReturnDefaultPreferences()
    {
        // Act
        var result = await _repository.GetByUserIdAsync("nonexistent");

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe("nonexistent");
        result.Theme.ShouldBe(Theme.Light);
        result.DashboardRefreshInterval.ShouldBe(5);
        result.NotificationDuration.ShouldBe(5);
        result.ShowSuccessNotifications.ShouldBeTrue();
        result.ShowErrorNotifications.ShouldBeTrue();
        result.ShowInfoNotifications.ShouldBeTrue();
        result.ShowWarningNotifications.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveAsync_NewPreferences_ShouldAddToDatabase()
    {
        // Arrange
        var preferences = CreateSamplePreferences("newuser");

        // Act
        await _repository.SaveAsync(preferences);

        // Assert
        var result = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == "newuser");
        result.ShouldNotBeNull();
        result.UserId.ShouldBe("newuser");
        result.Theme.ShouldBe(Theme.Light);
    }

    [Fact]
    public async Task SaveAsync_ExistingPreferences_ShouldUpdateInDatabase()
    {
        // Arrange
        var preferences = CreateSamplePreferences("user123");
        await _context.UserPreferences.AddAsync(preferences);
        await _context.SaveChangesAsync();

        // Modify preferences
        preferences.Theme = Theme.Dark;
        preferences.DashboardRefreshInterval = 30;
        preferences.NotificationDuration = 8;

        // Act
        await _repository.SaveAsync(preferences);

        // Assert
        var result = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == "user123");
        result.ShouldNotBeNull();
        result.Theme.ShouldBe(Theme.Dark);
        result.DashboardRefreshInterval.ShouldBe(30);
        result.NotificationDuration.ShouldBe(8);
        result.UpdatedAt.ShouldBeGreaterThan(result.CreatedAt);
    }

    [Fact]
    public async Task ResetToDefaultAsync_ShouldResetPreferencesToDefaults()
    {
        // Arrange
        var preferences = CreateSamplePreferences("user123");
        preferences.Theme = Theme.Dark;
        preferences.DashboardRefreshInterval = 100;
        preferences.NotificationDuration = 10;
        preferences.ShowInfoNotifications = false;
        await _context.UserPreferences.AddAsync(preferences);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ResetToDefaultAsync("user123");

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe("user123");
        result.Theme.ShouldBe(Theme.Light);
        result.DashboardRefreshInterval.ShouldBe(5);
        result.NotificationDuration.ShouldBe(5);
        result.ShowSuccessNotifications.ShouldBeTrue();
        result.ShowErrorNotifications.ShouldBeTrue();
        result.ShowInfoNotifications.ShouldBeTrue();
        result.ShowWarningNotifications.ShouldBeTrue();
    }

    [Fact]
    public async Task ResetToDefaultAsync_WhenPreferencesDoNotExist_ShouldCreateDefaultPreferences()
    {
        // Act
        var result = await _repository.ResetToDefaultAsync("newuser");

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe("newuser");
        result.Theme.ShouldBe(Theme.Light);
        result.DashboardRefreshInterval.ShouldBe(5);

        // Verify it was saved to database
        var saved = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == "newuser");
        saved.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_DefaultUser_ShouldReturnDefaultUserPreferences()
    {
        // Arrange
        var preferences = CreateSamplePreferences("default");
        await _context.UserPreferences.AddAsync(preferences);
        await _context.SaveChangesAsync();

        // Act (calling without parameter should default to "default")
        var result = await _repository.GetByUserIdAsync();

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe("default");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private static UserPreferences CreateSamplePreferences(string userId)
    {
        return new UserPreferences
        {
            UserId = userId,
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
