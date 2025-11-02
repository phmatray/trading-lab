// <copyright file="IConfigurationService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service for managing application configuration.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <returns>Configuration value or null if not found.</returns>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <param name="value">Configuration value.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync(string key, string value);

    /// <summary>
    /// Gets all configuration values.
    /// </summary>
    /// <returns>Dictionary of all configuration values.</returns>
    Task<Dictionary<string, string>> GetAllAsync();

    /// <summary>
    /// Deletes a configuration value.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(string key);
}
