// <copyright file="UserPreferencesService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Entities;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Validators;

namespace TradingBot.Infrastructure.Services;

/// <summary>
/// Service implementation for managing user preferences.
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly IUserPreferencesRepository _repository;
    private readonly UserPreferencesValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferencesService"/> class.
    /// </summary>
    /// <param name="repository">The user preferences repository.</param>
    public UserPreferencesService(IUserPreferencesRepository repository)
    {
        _repository = repository;
        _validator = new UserPreferencesValidator();
    }

    /// <summary>
    /// Occurs when user preferences are changed.
    /// </summary>
    public event EventHandler? PreferencesChanged;

    /// <inheritdoc/>
    public async Task<UserPreferences> GetPreferencesAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetByUserIdAsync("default", cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> UpdatePreferencesAsync(
        UserPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(preferences);

        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        await _repository.SaveAsync(preferences, cancellationToken);
        OnPreferencesChanged();
        return ValidationResult.Success();
    }

    /// <inheritdoc/>
    public async Task<UserPreferences> ResetToDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.ResetToDefaultAsync("default", cancellationToken);
    }

    /// <summary>
    /// Raises the PreferencesChanged event.
    /// </summary>
    protected virtual void OnPreferencesChanged()
    {
        PreferencesChanged?.Invoke(this, EventArgs.Empty);
    }
}
