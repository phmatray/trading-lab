// <copyright file="PerformanceService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Portfolio;
using TradingBot.Web.Models;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for retrieving performance metrics and analytics.
/// </summary>
public sealed class PerformanceService : IPerformanceService
{
    private readonly IPortfolioManager _portfolioManager;
    private readonly ILogger<PerformanceService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceService"/> class.
    /// </summary>
    /// <param name="portfolioManager">The portfolio manager.</param>
    /// <param name="logger">The logger instance.</param>
    public PerformanceService(
        IPortfolioManager portfolioManager,
        ILogger<PerformanceService> logger)
    {
        _portfolioManager = portfolioManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<PerformanceMetrics> GetCurrentMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading performance metrics");

            var metrics = await _portfolioManager.GetPerformanceMetricsAsync(cancellationToken);

            _logger.LogInformation("Performance metrics loaded successfully");

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading performance metrics");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<EquityCurveDataPoint>> GetEquityCurveAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Loading equity curve: StartDate={StartDate}, EndDate={EndDate}",
                startDate,
                endDate);

            // Get trade history
            var trades = await _portfolioManager.GetTradeHistoryAsync(
                startDate,
                endDate,
                symbol: null,
                strategyName: null,
                cancellationToken);

            // Get current account state
            var account = await _portfolioManager.GetAccountAsync(cancellationToken);

            // Build equity curve from trades
            var equityCurve = new List<EquityCurveDataPoint>();

            // Start with initial capital (can be retrieved from config or calculated)
            var initialCapital = account.Equity - account.RealizedPnL - account.UnrealizedPnL;
            var currentEquity = initialCapital;

            // Add starting point
            var firstTradeDate = trades.Any() ? trades.Min(t => t.EntryTime) : DateTime.UtcNow;
            equityCurve.Add(new EquityCurveDataPoint
            {
                Timestamp = firstTradeDate,
                EquityValue = currentEquity,
            });

            // Add a point for each closed trade
            foreach (var trade in trades.OrderBy(t => t.ExitTime))
            {
                currentEquity += trade.RealizedPnL;
                equityCurve.Add(new EquityCurveDataPoint
                {
                    Timestamp = trade.ExitTime,
                    EquityValue = currentEquity,
                });
            }

            // Add current equity as the last point
            equityCurve.Add(new EquityCurveDataPoint
            {
                Timestamp = DateTime.UtcNow,
                EquityValue = account.Equity,
            });

            _logger.LogInformation("Equity curve loaded: {Count} data points", equityCurve.Count);

            return equityCurve;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading equity curve");
            throw;
        }
    }
}
