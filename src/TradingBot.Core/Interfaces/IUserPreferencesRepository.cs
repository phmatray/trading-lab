// <copyright file="IUserPreferencesRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Entities;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Repository interface for UserPreferences.
/// </summary>
public interface IUserPreferencesRepository
{
    /// <summary>
    /// Gets user preferences by user ID.
    /// </summary>
    /// <param name="userId">The user ID (default: "default").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user preferences, or default preferences if not found.</returns>
    Task<UserPreferences> GetByUserIdAsync(
        string userId = "default",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates user preferences.
    /// </summary>
    /// <param name="preferences">The preferences to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets user preferences to default values.
    /// </summary>
    /// <param name="userId">The user ID (default: "default").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reset preferences.</returns>
    Task<UserPreferences> ResetToDefaultAsync(
        string userId = "default",
        CancellationToken cancellationToken = default);
}
