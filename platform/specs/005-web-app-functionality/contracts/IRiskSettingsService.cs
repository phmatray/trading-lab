// <copyright file="IRiskSettingsService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Services;

using TradingBot.Core.Models.Configuration;

/// <summary>
/// Service for managing risk management settings.
/// </summary>
public interface IRiskSettingsService
{
    /// <summary>
    /// Retrieves the current risk settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current risk settings, or default values if none exist.</returns>
    /// <remarks>
    /// Risk settings are stored as a singleton (single row in database with fixed ID).
    /// Default values: MaxPositionSize=10%, StopLoss=2%, TakeProfit=5%, MaxOpenPositions=5, MaxDailyLoss=5%
    /// </remarks>
    Task<RiskSettings> GetRiskSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the risk settings.
    /// </summary>
    /// <param name="settings">The new risk settings to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if settings were saved successfully, false if validation failed.</returns>
    /// <remarks>
    /// This method:
    /// 1. Validates all percentage fields are within acceptable ranges (0.1-100.0)
    /// 2. Validates MaxOpenPositions is between 1-100
    /// 3. Updates the singleton RiskSettings row in database
    /// 4. Reloads settings in RiskManager service
    /// 5. Publishes SignalR event OnRiskSettingsChanged
    ///
    /// Validation rules:
    /// - MaxPositionSizePercent: 0.1-100.0
    /// - StopLossPercent: 0.1-50.0
    /// - TakeProfitPercent: 0.1-100.0
    /// - MaxOpenPositions: 1-100
    /// - MaxDailyLossPercent: 0.1-100.0
    ///
    /// Settings take effect immediately for new order validations.
    /// Existing open positions are not affected by changes.
    /// </remarks>
    Task<bool> SaveRiskSettingsAsync(RiskSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets risk settings to default values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default risk settings after reset.</returns>
    /// <remarks>
    /// Default values:
    /// - MaxPositionSizePercent: 10.0
    /// - StopLossPercent: 2.0
    /// - TakeProfitPercent: 5.0
    /// - MaxOpenPositions: 5
    /// - MaxDailyLossPercent: 5.0
    ///
    /// These values align with the project constitution's risk management standards.
    /// </remarks>
    Task<RiskSettings> ResetToDefaultsAsync(CancellationToken cancellationToken = default);
}
