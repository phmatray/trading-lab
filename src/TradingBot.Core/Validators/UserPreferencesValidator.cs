// <copyright file="UserPreferencesValidator.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Entities;

namespace TradingBot.Core.Validators;

/// <summary>
/// Validator for UserPreferences entity.
/// </summary>
public class UserPreferencesValidator
{
    /// <summary>
    /// Validates a UserPreferences instance.
    /// </summary>
    /// <param name="preferences">The preferences to validate.</param>
    /// <returns>A validation result.</returns>
    public ValidationResult Validate(UserPreferences preferences)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(preferences.UserId))
        {
            errors.Add("User ID is required.");
        }

        if (preferences.DashboardRefreshInterval < 1 || preferences.DashboardRefreshInterval > 300)
        {
            errors.Add($"Dashboard refresh interval must be between 1 and 300 seconds. You entered {preferences.DashboardRefreshInterval} seconds. Please enter a value within the valid range.");
        }

        if (preferences.NotificationDuration < 2 || preferences.NotificationDuration > 10)
        {
            errors.Add($"Notification duration must be between 2 and 10 seconds. You entered {preferences.NotificationDuration} seconds. Please enter a value within the valid range.");
        }

        if (preferences.Theme == null)
        {
            errors.Add("Theme is required. Please select either Light or Dark theme.");
        }

        return errors.Any()
            ? ValidationResult.Error(errors)
            : ValidationResult.Success();
    }
}
