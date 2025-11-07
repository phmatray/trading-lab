// <copyright file="IDashboardService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Web.Models;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for retrieving dashboard data.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets aggregated dashboard data including account, positions, trades, metrics, and settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard view model with all required data.</returns>
    Task<DashboardViewModel> GetDashboardDataAsync(CancellationToken cancellationToken = default);
}
