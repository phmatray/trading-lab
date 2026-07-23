// <copyright file="IRiskManager.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Configuration;

namespace TradingBot.Core.Interfaces;

/// <summary>
/// Service for managing trading risk parameters and limits.
/// </summary>
public interface IRiskManager
{
    /// <summary>
    /// Gets the current risk settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current risk settings.</returns>
    Task<RiskSettings> GetRiskSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the account leverage.
    /// </summary>
    /// <param name="leverage">Leverage multiplier (e.g., 2.0 for 2x).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetLeverageAsync(decimal leverage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the default stop-loss percentage.
    /// </summary>
    /// <param name="stopLossPercent">Stop-loss percentage (e.g., 2.0 for 2%).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetStopLossAsync(decimal stopLossPercent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the default take-profit percentage.
    /// </summary>
    /// <param name="takeProfitPercent">Take-profit percentage (e.g., 5.0 for 5%).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetTakeProfitAsync(decimal takeProfitPercent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the maximum daily loss limit.
    /// </summary>
    /// <param name="dailyLossLimit">Maximum daily loss amount.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetDailyLossLimitAsync(decimal dailyLossLimit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the maximum drawdown percentage limit.
    /// </summary>
    /// <param name="maxDrawdownPercent">Maximum drawdown percentage (e.g., 10.0 for 10%).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetMaxDrawdownAsync(decimal maxDrawdownPercent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the maximum position size percentage.
    /// </summary>
    /// <param name="maxPositionSizePercent">Maximum position size as percentage of equity (e.g., 10.0 for 10%).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetMaxPositionSizeAsync(decimal maxPositionSizePercent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all risk settings to their default values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetToDefaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a proposed position size complies with risk limits.
    /// </summary>
    /// <param name="positionValue">Proposed position value.</param>
    /// <param name="accountEquity">Current account equity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if position size is allowed, false otherwise.</returns>
    Task<bool> ValidatePositionSizeAsync(
        decimal positionValue,
        decimal accountEquity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if daily loss limit has been exceeded.
    /// </summary>
    /// <param name="currentDailyLoss">Current daily loss amount.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if daily loss limit exceeded, false otherwise.</returns>
    Task<bool> IsDailyLossLimitExceededAsync(
        decimal currentDailyLoss,
        CancellationToken cancellationToken = default);
}
