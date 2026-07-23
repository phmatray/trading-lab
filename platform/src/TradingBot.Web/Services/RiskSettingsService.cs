// <copyright file="RiskSettingsService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.SignalR;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Configuration;
using TradingBot.Web.Hubs;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for managing risk settings with persistence and real-time updates.
/// </summary>
public sealed class RiskSettingsService : IRiskSettingsService
{
    private readonly IRiskSettingsRepository _repository;
    private readonly IHubContext<TradingHub, ITradingClient> _hubContext;
    private readonly ILogger<RiskSettingsService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskSettingsService"/> class.
    /// </summary>
    /// <param name="repository">The risk settings repository.</param>
    /// <param name="hubContext">The SignalR hub context.</param>
    /// <param name="logger">The logger instance.</param>
    public RiskSettingsService(
        IRiskSettingsRepository repository,
        IHubContext<TradingHub, ITradingClient> hubContext,
        ILogger<RiskSettingsService> logger)
    {
        _repository = repository;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<RiskSettings> GetRiskSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading current risk settings");

            var settings = await _repository.GetAsync(cancellationToken);

            if (settings == null)
            {
                _logger.LogWarning("No risk settings found in database, returning defaults");
                return GetDefaultSettings();
            }

            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading risk settings");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SaveRiskSettingsAsync(RiskSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Saving risk settings");

            // Validate settings
            if (!ValidateSettings(settings, out var validationError))
            {
                _logger.LogWarning("Risk settings validation failed: {Error}", validationError);
                return false;
            }

            // Save to database
            await _repository.UpdateAsync(settings, cancellationToken);

            _logger.LogInformation("Risk settings saved successfully");

            // Broadcast update to all connected clients via SignalR
            await _hubContext.Clients.All.ReceiveRiskSettingsUpdate(settings);
            _logger.LogInformation("Risk settings update broadcast to all clients");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving risk settings");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<RiskSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Resetting risk settings to defaults");

            var defaultSettings = GetDefaultSettings();

            // Save defaults to database
            await _repository.UpdateAsync(defaultSettings, cancellationToken);

            _logger.LogInformation("Risk settings reset to defaults successfully");

            // Broadcast update to all connected clients via SignalR
            await _hubContext.Clients.All.ReceiveRiskSettingsUpdate(defaultSettings);
            _logger.LogInformation("Risk settings reset broadcast to all clients");

            return defaultSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting risk settings to defaults");
            throw;
        }
    }

    private static RiskSettings GetDefaultSettings()
    {
        return new RiskSettings
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            MaxPositionSizePercent = 10.0m,
            StopLossPercent = 2.0m,
            TakeProfitPercent = 5.0m,
            MaxOpenPositions = 5,
            MaxDailyLossPercent = 5.0m,
            LastModified = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private bool ValidateSettings(RiskSettings settings, out string? error)
    {
        error = null;

        if (settings.MaxPositionSizePercent < 0.1m || settings.MaxPositionSizePercent > 100.0m)
        {
            error = "Max position size percentage must be between 0.1% and 100%";
            return false;
        }

        if (settings.StopLossPercent < 0.1m || settings.StopLossPercent > 50.0m)
        {
            error = "Stop-loss percentage must be between 0.1% and 50%";
            return false;
        }

        if (settings.TakeProfitPercent < 0.1m || settings.TakeProfitPercent > 100.0m)
        {
            error = "Take-profit percentage must be between 0.1% and 100%";
            return false;
        }

        if (settings.MaxOpenPositions < 1 || settings.MaxOpenPositions > 100)
        {
            error = "Max open positions must be between 1 and 100";
            return false;
        }

        if (settings.MaxDailyLossPercent < 0.1m || settings.MaxDailyLossPercent > 100.0m)
        {
            error = "Max daily loss percentage must be between 0.1% and 100%";
            return false;
        }

        return true;
    }
}
