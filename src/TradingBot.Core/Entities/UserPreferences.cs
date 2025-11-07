// <copyright file="UserPreferences.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.ValueObjects;

namespace TradingBot.Core.Entities;

/// <summary>
/// Represents user-specific preferences for UI customization.
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// For single-user systems, this defaults to "default".
    /// </summary>
    public string UserId { get; set; } = "default";

    /// <summary>
    /// Gets or sets the UI theme preference (Light or Dark).
    /// </summary>
    public Theme Theme { get; set; } = Theme.Light;

    /// <summary>
    /// Gets or sets the dashboard refresh interval in seconds (1-300).
    /// </summary>
    public int DashboardRefreshInterval { get; set; } = 5;

    /// <summary>
    /// Gets or sets the notification display duration in seconds (2-10).
    /// </summary>
    public int NotificationDuration { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether success notifications are displayed.
    /// </summary>
    public bool ShowSuccessNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether error notifications are displayed.
    /// </summary>
    public bool ShowErrorNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether info notifications are displayed.
    /// </summary>
    public bool ShowInfoNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether warning notifications are displayed.
    /// </summary>
    public bool ShowWarningNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets custom settings as JSON for future extensibility.
    /// </summary>
    public string? CustomSettings { get; set; }

    /// <summary>
    /// Gets or sets the record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
