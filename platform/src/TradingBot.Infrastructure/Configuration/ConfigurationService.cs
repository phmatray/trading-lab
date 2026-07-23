// <copyright file="ConfigurationService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;

namespace TradingBot.Infrastructure.Configuration;

/// <summary>
/// Service for managing application configuration stored in a JSON file.
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Store config in user's app data directory
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var tradingBotPath = Path.Combine(appDataPath, "TradingBot");

        Directory.CreateDirectory(tradingBotPath);
        _configFilePath = Path.Combine(tradingBotPath, "config.json");

        _logger.LogDebug("Configuration file path: {Path}", _configFilePath);
    }

    /// <inheritdoc/>
    public async Task<string?> GetAsync(string key)
    {
        await _lock.WaitAsync();
        try
        {
            var config = await LoadConfigAsync();
            return config.TryGetValue(key, out var value) ? value : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync(string key, string value)
    {
        await _lock.WaitAsync();
        try
        {
            var config = await LoadConfigAsync();
            config[key] = value;
            await SaveConfigAsync(config);

            _logger.LogInformation("Configuration updated: {Key}", key);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return await LoadConfigAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string key)
    {
        await _lock.WaitAsync();
        try
        {
            var config = await LoadConfigAsync();
            if (config.Remove(key))
            {
                await SaveConfigAsync(config);
                _logger.LogInformation("Configuration deleted: {Key}", key);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Dictionary<string, string>> LoadConfigAsync()
    {
        if (!File.Exists(_configFilePath))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configFilePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from {Path}", _configFilePath);
            return new Dictionary<string, string>();
        }
    }

    private async Task SaveConfigAsync(Dictionary<string, string> config)
    {
        try
        {
            // Create backup before saving
            if (File.Exists(_configFilePath))
            {
                var backupPath = $"{_configFilePath}.bak";
                File.Copy(_configFilePath, backupPath, overwrite: true);
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            await File.WriteAllTextAsync(_configFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to {Path}", _configFilePath);
            throw;
        }
    }
}
