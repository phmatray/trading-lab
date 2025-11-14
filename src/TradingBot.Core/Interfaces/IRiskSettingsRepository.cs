// <copyright file="IRiskSettingsRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Interfaces;

using TradingBot.Core.Models.Configuration;

/// <summary>
/// Repository interface for managing risk settings persistence.
/// </summary>
public interface IRiskSettingsRepository
{
    /// <summary>
    /// Retrieves the singleton risk settings record.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The risk settings, or null if none exist.</returns>
    Task<RiskSettings?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the singleton risk settings record.
    /// If no record exists, creates a new one.
    /// </summary>
    /// <param name="settings">The risk settings to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(RiskSettings settings, CancellationToken cancellationToken = default);
}
