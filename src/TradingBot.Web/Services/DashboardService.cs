// <copyright file="DashboardService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Interfaces;
using TradingBot.Web.Models;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for retrieving dashboard data.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly IPortfolioManager _portfolioManager;
    private readonly IStrategyManagementService _strategyManagementService;
    private readonly ILogger<DashboardService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardService"/> class.
    /// </summary>
    /// <param name="portfolioManager">The portfolio manager.</param>
    /// <param name="strategyManagementService">The strategy management service.</param>
    /// <param name="logger">The logger instance.</param>
    public DashboardService(
        IPortfolioManager portfolioManager,
        IStrategyManagementService strategyManagementService,
        ILogger<DashboardService> logger)
    {
        _portfolioManager = portfolioManager;
        _strategyManagementService = strategyManagementService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DashboardViewModel> GetDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading dashboard data");

            // Fetch all data in parallel for efficiency
            var accountTask = _portfolioManager.GetAccountAsync(cancellationToken);
            var positionsTask = _portfolioManager.GetPositionsAsync(cancellationToken);
            var tradesTask = _portfolioManager.GetTradeHistoryAsync(
                startDate: DateTime.UtcNow.AddDays(-7),
                endDate: null,
                symbol: null,
                strategyName: null,
                cancellationToken: cancellationToken);
            var metricsTask = _portfolioManager.GetPerformanceMetricsAsync(cancellationToken);
            var strategiesTask = _strategyManagementService.GetAllStrategiesAsync(cancellationToken);

            await Task.WhenAll(
                accountTask,
                positionsTask,
                tradesTask,
                metricsTask,
                strategiesTask);

            var account = await accountTask;
            var positions = await positionsTask;
            var trades = await tradesTask;
            var metrics = await metricsTask;
            var strategies = await strategiesTask;

            // Use default risk settings for dashboard display
            // The actual risk settings are managed through the RiskSettingsPage
            var riskSettings = new TradingBot.Core.Models.Configuration.RiskSettings();

            // Take top 10 positions by value
            var topPositions = positions
                .OrderByDescending(p => p.PositionValue)
                .Take(10)
                .ToList();

            // Take last 5 trades
            var recentTrades = trades
                .OrderByDescending(t => t.ExitTime)
                .Take(5)
                .ToList();

            // Filter active strategies
            var activeStrategies = strategies
                .Where(s => s.IsEnabled)
                .ToList();

            var viewModel = new DashboardViewModel
            {
                Account = account,
                OpenPositions = topPositions,
                RecentTrades = recentTrades,
                PerformanceMetrics = metrics,
                RiskSettings = riskSettings,
                ActiveStrategies = activeStrategies,
                ConnectionStatus = "connected",
                LastUpdated = DateTime.UtcNow,
            };

            _logger.LogInformation("Dashboard data loaded successfully");

            return viewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            throw;
        }
    }
}
