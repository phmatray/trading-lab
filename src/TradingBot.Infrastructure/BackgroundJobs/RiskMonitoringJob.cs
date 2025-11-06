// <copyright file="RiskMonitoringJob.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;

namespace TradingBot.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job to monitor risk limits and halt trading if breached.
/// </summary>
public sealed class RiskMonitoringJob : IJob
{
    private readonly ILogger<RiskMonitoringJob> _logger;
    private readonly IRiskManager _riskManager;
    private readonly IPortfolioManager _portfolioManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskMonitoringJob"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="riskManager">Risk manager.</param>
    /// <param name="portfolioManager">Portfolio manager.</param>
    public RiskMonitoringJob(
        ILogger<RiskMonitoringJob> logger,
        IRiskManager riskManager,
        IPortfolioManager portfolioManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
    }

    /// <inheritdoc/>
    public string JobName => "Risk Monitoring";

    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var riskSettings = await _riskManager.GetRiskSettingsAsync(cancellationToken);

            if (!riskSettings.RiskLimitsEnabled)
            {
                _logger.LogDebug("Risk limits are disabled - skipping monitoring");
                return;
            }

            var account = await _portfolioManager.GetAccountAsync(cancellationToken);
            var positions = await _portfolioManager.GetPositionsAsync(cancellationToken);

            // Check daily loss limit
            if (riskSettings.DailyLossLimit > 0)
            {
                var dailyLoss = Math.Abs(Math.Min(0, account.RealizedPnL + account.UnrealizedPnL));

                if (dailyLoss >= riskSettings.DailyLossLimit)
                {
                    _logger.LogWarning(
                        "Daily loss limit breached: ${DailyLoss:N2} >= ${Limit:N2}",
                        dailyLoss,
                        riskSettings.DailyLossLimit);

                    // Close all positions
                    foreach (var position in positions)
                    {
                        await _portfolioManager.ClosePositionAsync(position.Symbol, cancellationToken);
                    }

                    _logger.LogWarning("All positions closed due to daily loss limit breach");
                }
            }

            // Check maximum drawdown
            var metrics = await _portfolioManager.GetPerformanceMetricsAsync(cancellationToken);
            if (riskSettings.MaxDrawdownPercent > 0 && metrics.MaxDrawdown >= riskSettings.MaxDrawdownPercent)
            {
                _logger.LogWarning(
                    "Maximum drawdown limit breached: {Drawdown:F2}% >= {Limit:F2}%",
                    metrics.MaxDrawdown,
                    riskSettings.MaxDrawdownPercent);

                // Close all positions
                foreach (var position in positions)
                {
                    await _portfolioManager.ClosePositionAsync(position.Symbol, cancellationToken);
                }

                _logger.LogWarning("All positions closed due to maximum drawdown breach");
            }

            // Check leverage
            var currentLeverage = account.PositionValue / account.Equity;
            if (currentLeverage > riskSettings.Leverage)
            {
                _logger.LogWarning(
                    "Leverage exceeded: {Current:F2}x > {Max:F2}x",
                    currentLeverage,
                    riskSettings.Leverage);
            }

            // Check individual position sizes
            foreach (var position in positions)
            {
                var positionSizePercent = (position.Quantity * position.CurrentPrice / account.Equity) * 100;

                if (positionSizePercent > riskSettings.MaxPositionSizePercent)
                {
                    _logger.LogWarning(
                        "Position {Symbol} exceeds max size: {Size:F2}% > {Max:F2}%",
                        position.Symbol,
                        positionSizePercent,
                        riskSettings.MaxPositionSizePercent);
                }
            }

            _logger.LogDebug("Risk monitoring completed - all checks passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during risk monitoring: {Message}", ex.Message);
            throw;
        }
    }
}
