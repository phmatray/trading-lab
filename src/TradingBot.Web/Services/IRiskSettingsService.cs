// <copyright file="IRiskSettingsService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Risk;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for managing risk settings.
/// </summary>
public interface IRiskSettingsService
{
    /// <summary>
    /// Gets the current risk management settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current risk settings.</returns>
    Task<RiskSettings> GetCurrentSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates risk management settings with validation.
    /// </summary>
    /// <param name="settings">New risk settings to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated risk settings after validation.</returns>
    /// <exception cref="ArgumentException">Thrown when settings fail validation.</exception>
    Task<RiskSettings> UpdateSettingsAsync(
        RiskSettings settings,
        CancellationToken cancellationToken = default);
}
