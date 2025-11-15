// <copyright file="RiskManager.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Configuration;

namespace TradingBot.Engine;

/// <summary>
/// Service for managing trading risk parameters and limits.
/// </summary>
public sealed class RiskManager : IRiskManager
{
    private readonly ILogger<RiskManager> _logger;
    private readonly RiskSettings _settings;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public RiskManager(ILogger<RiskManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = new RiskSettings();
    }

    /// <inheritdoc/>
    public async Task<RiskSettings> GetRiskSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return new RiskSettings
            {
                Leverage = _settings.Leverage,
                StopLossPercent = _settings.StopLossPercent,
                TakeProfitPercent = _settings.TakeProfitPercent,
                DailyLossLimit = _settings.DailyLossLimit,
                MaxDrawdownPercent = _settings.MaxDrawdownPercent,
                MaxPositionSizePercent = _settings.MaxPositionSizePercent,
                RiskLimitsEnabled = _settings.RiskLimitsEnabled,
                LastModified = _settings.LastModified,
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetLeverageAsync(decimal leverage, CancellationToken cancellationToken = default)
    {
        if (leverage < 1.0m || leverage > 10.0m)
        {
            throw new ArgumentException("Leverage must be between 1.0 and 10.0", nameof(leverage));
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _settings.Leverage = leverage;
            _settings.LastModified = DateTime.UtcNow;
            _logger.LogInformation("Leverage set to {Leverage}x", leverage);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetStopLossAsync(decimal stopLossPercent, CancellationToken cancellationToken = default)
    {
        if (stopLossPercent < 0.1m || stopLossPercent > 20.0m)
        {
            throw new ArgumentException("Stop-loss must be between 0.1% and 20%", nameof(stopLossPercent));
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _settings.StopLossPercent = stopLossPercent;
            _settings.LastModified = DateTime.UtcNow;
            _logger.LogInformation("Stop-loss set to {StopLoss}%", stopLossPercent);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetTakeProfitAsync(decimal takeProfitPercent, CancellationToken cancellationToken = default)
    {
        if (takeProfitPercent < 0.1m || takeProfitPercent > 50.0m)
        {
            throw new ArgumentException("Take-profit must be between 0.1% and 50%", nameof(takeProfitPercent));
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _settings.TakeProfitPercent = takeProfitPercent;
            _settings.LastModified = DateTime.UtcNow;
            _logger.LogInformation("Take-profit set to {TakeProfit}%", takeProfitPercent);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetDailyLossLimitAsync(decimal dailyLossLimit, CancellationToken cancellationToken = default)
    {
        if (dailyLossLimit <= 0)
        {
            throw new ArgumentException("Daily loss limit must be positive", nameof(dailyLossLimit));
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _settings.DailyLossLimit = dailyLossLimit;
            _settings.LastModified = DateTime.UtcNow;
            _logger.LogInformation("Daily loss limit set to ${DailyLossLimit:N2}", dailyLossLimit);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetMaxDrawdownAsync(decimal maxDrawdownPercent, CancellationToken cancellationToken = default)
    {
        if (maxDrawdownPercent < 1.0m || maxDrawdownPercent > 50.0m)
        {
            throw new ArgumentException("Max drawdown must be between 1% and 50%", nameof(maxDrawdownPercent));
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _settings.MaxDrawdownPercent = maxDrawdownPercent;
            _settings.LastModified = DateTime.UtcNow;
            _logger.LogInformation("Max drawdown set to {MaxDrawdown}%", maxDrawdownPercent);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetMaxPositionSizeAsync(decimal maxPositionSizePercent, CancellationToken cancellationToken = default)
    {
        if (maxPositionSizePercent < 1.0m || maxPositionSizePercent > 100.0m)
        {
            throw new ArgumentException("Max position size must be between 1% and 100%", nameof(maxPositionSizePercent));
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _settings.MaxPositionSizePercent = maxPositionSizePercent;
            _settings.LastModified = DateTime.UtcNow;
            _logger.LogInformation("Max position size set to {MaxPositionSize}%", maxPositionSizePercent);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _settings.Leverage = 1.0m;
            _settings.StopLossPercent = 2.0m;
            _settings.TakeProfitPercent = 5.0m;
            _settings.DailyLossLimit = 1000m;
            _settings.MaxDrawdownPercent = 10.0m;
            _settings.MaxPositionSizePercent = 10.0m;
            _settings.RiskLimitsEnabled = true;
            _settings.LastModified = DateTime.UtcNow;

            _logger.LogInformation("Risk settings reset to defaults");
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidatePositionSizeAsync(
        decimal positionValue,
        decimal accountEquity,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_settings.RiskLimitsEnabled)
            {
                return true;
            }

            var positionPercent = (positionValue / accountEquity) * 100m;
            var isValid = positionPercent <= _settings.MaxPositionSizePercent;

            if (!isValid)
            {
                _logger.LogWarning(
                    "Position size {PositionPercent:F2}% exceeds maximum of {MaxPositionSize}%",
                    positionPercent,
                    _settings.MaxPositionSizePercent);
            }

            return isValid;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsDailyLossLimitExceededAsync(
        decimal currentDailyLoss,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_settings.RiskLimitsEnabled)
            {
                return false;
            }

            var exceeded = Math.Abs(currentDailyLoss) >= _settings.DailyLossLimit;

            if (exceeded)
            {
                _logger.LogWarning(
                    "Daily loss ${CurrentLoss:N2} exceeds limit of ${Limit:N2}",
                    Math.Abs(currentDailyLoss),
                    _settings.DailyLossLimit);
            }

            return exceeded;
        }
        finally
        {
            _lock.Release();
        }
    }
}
