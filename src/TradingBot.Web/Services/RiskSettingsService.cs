// <copyright file="RiskSettingsService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradingBot.Core.Models.Risk;
using TradingBot.Web.Hubs;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for managing risk settings.
/// </summary>
public sealed class RiskSettingsService : IRiskSettingsService
{
    private readonly IHubContext<TradingHub, ITradingClient> _hubContext;
    private readonly ILogger<RiskSettingsService> _logger;
    private RiskSettings _currentSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskSettingsService"/> class.
    /// </summary>
    /// <param name="hubContext">The SignalR hub context.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="logger">The logger instance.</param>
    public RiskSettingsService(
        IHubContext<TradingHub, ITradingClient> hubContext,
        IConfiguration configuration,
        ILogger<RiskSettingsService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;

        // Initialize from configuration
        _currentSettings = new RiskSettings
        {
            Leverage = configuration.GetValue<decimal>("TradingBot:DefaultLeverage", 1.0m),
            StopLossPercent = configuration.GetValue<decimal>("TradingBot:StopLossPercent", 2.0m),
            TakeProfitPercent = configuration.GetValue<decimal>("TradingBot:TakeProfitPercent", 5.0m),
            DailyLossLimit = configuration.GetValue<decimal>("TradingBot:DailyLossLimit", 1000m),
            MaxDrawdownPercent = configuration.GetValue<decimal>("TradingBot:MaxDrawdownPercent", 10.0m),
            MaxPositionSizePercent = configuration.GetValue<decimal>("TradingBot:MaxPositionSize", 10.0m),
            RiskLimitsEnabled = configuration.GetValue<bool>("TradingBot:RiskLimitsEnabled", true),
            LastUpdated = DateTime.UtcNow,
        };
    }

    /// <inheritdoc/>
    public Task<RiskSettings> GetCurrentSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading current risk settings");

            return Task.FromResult(_currentSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading risk settings");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<RiskSettings> UpdateSettingsAsync(
        RiskSettings settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating risk settings");

            // Validate settings
            ValidateSettings(settings);

            // Update current settings
            _currentSettings = new RiskSettings
            {
                Leverage = settings.Leverage,
                StopLossPercent = settings.StopLossPercent,
                TakeProfitPercent = settings.TakeProfitPercent,
                DailyLossLimit = settings.DailyLossLimit,
                MaxDrawdownPercent = settings.MaxDrawdownPercent,
                MaxPositionSizePercent = settings.MaxPositionSizePercent,
                RiskLimitsEnabled = settings.RiskLimitsEnabled,
                LastUpdated = DateTime.UtcNow,
            };

            _logger.LogInformation("Risk settings updated successfully");

            // Broadcast update to all connected clients via SignalR
            await _hubContext.Clients.All.ReceiveRiskSettingsUpdate(_currentSettings);
            _logger.LogInformation("Risk settings update broadcast to all clients");

            return _currentSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating risk settings");
            throw;
        }
    }

    private void ValidateSettings(RiskSettings settings)
    {
        if (settings.Leverage < 1.0m || settings.Leverage > 10.0m)
        {
            throw new ArgumentException("Leverage must be between 1.0 and 10.0", nameof(settings));
        }

        if (settings.StopLossPercent < 0.1m || settings.StopLossPercent > 50.0m)
        {
            throw new ArgumentException("Stop-loss percentage must be between 0.1% and 50%", nameof(settings));
        }

        if (settings.TakeProfitPercent < 0.1m || settings.TakeProfitPercent > 100.0m)
        {
            throw new ArgumentException("Take-profit percentage must be between 0.1% and 100%", nameof(settings));
        }

        if (settings.MaxPositionSizePercent < 1.0m || settings.MaxPositionSizePercent > 100.0m)
        {
            throw new ArgumentException("Max position size percentage must be between 1% and 100%", nameof(settings));
        }

        if (settings.DailyLossLimit < 0)
        {
            throw new ArgumentException("Daily loss limit must be non-negative", nameof(settings));
        }

        if (settings.MaxDrawdownPercent < 0 || settings.MaxDrawdownPercent > 100.0m)
        {
            throw new ArgumentException("Max drawdown percentage must be between 0% and 100%", nameof(settings));
        }
    }
}
