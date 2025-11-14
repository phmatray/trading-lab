// <copyright file="DashboardViewModel.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Risk;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Web.Models;

/// <summary>
/// Aggregates all dashboard data for efficient loading.
/// </summary>
public sealed class DashboardViewModel
{
    /// <summary>
    /// Gets or sets the current account state.
    /// </summary>
    public Account Account { get; set; } = null!;

    /// <summary>
    /// Gets or sets the top 10 positions by value.
    /// </summary>
    public List<Position> OpenPositions { get; set; } = new();

    /// <summary>
    /// Gets or sets the last 5 completed trades.
    /// </summary>
    public List<Trade> RecentTrades { get; set; } = new();

    /// <summary>
    /// Gets or sets the current performance statistics.
    /// </summary>
    public PerformanceMetrics PerformanceMetrics { get; set; } = null!;

    /// <summary>
    /// Gets or sets the current risk configuration.
    /// </summary>
    public RiskSettings RiskSettings { get; set; } = null!;

    /// <summary>
    /// Gets or sets the enabled strategies.
    /// </summary>
    public List<IStrategy> ActiveStrategies { get; set; } = new();

    /// <summary>
    /// Gets or sets the SignalR connection status.
    /// </summary>
    public string ConnectionStatus { get; set; } = "disconnected";

    /// <summary>
    /// Gets or sets the last data refresh timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
