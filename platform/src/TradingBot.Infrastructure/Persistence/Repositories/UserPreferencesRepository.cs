// <copyright file="UserPreferencesRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Entities;
using TradingBot.Core.Interfaces;
using TradingBot.Core.ValueObjects;

namespace TradingBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for UserPreferences.
/// </summary>
public class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly TradingBotDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferencesRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UserPreferencesRepository(TradingBotDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<UserPreferences> GetByUserIdAsync(
        string userId = "default",
        CancellationToken cancellationToken = default)
    {
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (preferences == null)
        {
            // Return default preferences if not found
            preferences = new UserPreferences
            {
                UserId = userId,
                Theme = Theme.Light,
                DashboardRefreshInterval = 5,
                NotificationDuration = 5,
                ShowSuccessNotifications = true,
                ShowErrorNotifications = true,
                ShowInfoNotifications = true,
                ShowWarningNotifications = true,
            };
        }

        return preferences;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        preferences.UpdatedAt = DateTime.UtcNow;

        var existing = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == preferences.UserId, cancellationToken);

        if (existing == null)
        {
            preferences.CreatedAt = DateTime.UtcNow;
            await _context.UserPreferences.AddAsync(preferences, cancellationToken);
        }
        else
        {
            existing.Theme = preferences.Theme;
            existing.DashboardRefreshInterval = preferences.DashboardRefreshInterval;
            existing.NotificationDuration = preferences.NotificationDuration;
            existing.ShowSuccessNotifications = preferences.ShowSuccessNotifications;
            existing.ShowErrorNotifications = preferences.ShowErrorNotifications;
            existing.ShowInfoNotifications = preferences.ShowInfoNotifications;
            existing.ShowWarningNotifications = preferences.ShowWarningNotifications;
            existing.CustomSettings = preferences.CustomSettings;
            existing.UpdatedAt = preferences.UpdatedAt;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UserPreferences> ResetToDefaultAsync(
        string userId = "default",
        CancellationToken cancellationToken = default)
    {
        var defaultPreferences = new UserPreferences
        {
            UserId = userId,
            Theme = Theme.Light,
            DashboardRefreshInterval = 5,
            NotificationDuration = 5,
            ShowSuccessNotifications = true,
            ShowErrorNotifications = true,
            ShowInfoNotifications = true,
            ShowWarningNotifications = true,
        };

        await SaveAsync(defaultPreferences, cancellationToken);
        return defaultPreferences;
    }
}
