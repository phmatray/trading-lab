// <copyright file="IPerformanceService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Portfolio;
using TradingBot.Web.Models;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for retrieving performance metrics and analytics.
/// </summary>
public interface IPerformanceService
{
    /// <summary>
    /// Gets current performance metrics for the portfolio.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Performance metrics including returns, ratios, and statistics.</returns>
    Task<PerformanceMetrics> GetCurrentMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets equity curve data points for charting.
    /// </summary>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of equity curve data points ordered by timestamp.</returns>
    Task<List<EquityCurveDataPoint>> GetEquityCurveAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}
