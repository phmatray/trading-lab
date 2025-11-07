// <copyright file="IUserPreferencesService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Entities;
using TradingBot.Core.Validators;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service interface for managing user preferences.
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Gets the current user's preferences.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user preferences.</returns>
    Task<UserPreferences> GetPreferencesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the current user's preferences.
    /// </summary>
    /// <param name="preferences">The updated preferences.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    Task<ValidationResult> UpdatePreferencesAsync(
        UserPreferences preferences,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets preferences to default values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reset preferences.</returns>
    Task<UserPreferences> ResetToDefaultAsync(CancellationToken cancellationToken = default);
}
