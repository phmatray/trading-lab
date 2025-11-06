// <copyright file="EndOfDayJob.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;

namespace TradingBot.Infrastructure.BackgroundJobs;

/// <summary>
/// Background job for end-of-day processing and summaries.
/// </summary>
public sealed class EndOfDayJob : IJob
{
    private readonly ILogger<EndOfDayJob> _logger;
    private readonly IPortfolioManager _portfolioManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndOfDayJob"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="portfolioManager">Portfolio manager.</param>
    public EndOfDayJob(
        ILogger<EndOfDayJob> logger,
        IPortfolioManager portfolioManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
    }

    /// <inheritdoc/>
    public string JobName => "End of Day";

    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting end-of-day processing");

            // Get current account state
            var account = await _portfolioManager.GetAccountAsync(cancellationToken);
            var positions = await _portfolioManager.GetPositionsAsync(cancellationToken);
            var metrics = await _portfolioManager.GetPerformanceMetricsAsync(cancellationToken);

            // Calculate daily P&L
            var dailyPnL = account.RealizedPnL + account.UnrealizedPnL;
            var dailyReturn = (dailyPnL / account.Equity) * 100;

            // Log daily summary
            _logger.LogInformation(
                "End of Day Summary - Date: {Date:yyyy-MM-dd} | " +
                "Equity: ${Equity:N2} | " +
                "Daily P&L: ${DailyPnL:N2} ({DailyReturn:F2}%) | " +
                "Total Trades: {TotalTrades} | " +
                "Open Positions: {OpenPositions} | " +
                "Win Rate: {WinRate:F1}% | " +
                "Total Return: {TotalReturn:F2}%",
                DateTime.UtcNow,
                account.Equity,
                dailyPnL,
                dailyReturn,
                metrics.TotalTrades,
                positions.Count,
                metrics.WinRate,
                metrics.TotalReturn);

            // Check if today had any trades
            var todayTrades = await _portfolioManager.GetTradeHistoryAsync(
                startDate: DateTime.UtcNow.Date,
                endDate: DateTime.UtcNow,
                cancellationToken: cancellationToken);

            if (todayTrades.Count > 0)
            {
                var winningTrades = todayTrades.Count(t => t.RealizedPnL > 0);
                var losingTrades = todayTrades.Count(t => t.RealizedPnL < 0);
                var todayWinRate = (decimal)winningTrades / todayTrades.Count * 100;

                _logger.LogInformation(
                    "Today's Trading Activity - " +
                    "Trades: {TodayTrades} | " +
                    "Wins: {Wins} | " +
                    "Losses: {Losses} | " +
                    "Win Rate: {TodayWinRate:F1}%",
                    todayTrades.Count,
                    winningTrades,
                    losingTrades,
                    todayWinRate);
            }
            else
            {
                _logger.LogInformation("No trades executed today");
            }

            // Log warning if any risk limits were approached
            if (metrics.MaxDrawdown > 15)
            {
                _logger.LogWarning("Approaching high drawdown: {Drawdown:F2}%", metrics.MaxDrawdown);
            }

            _logger.LogInformation("End-of-day processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during end-of-day processing: {Message}", ex.Message);
            throw;
        }
    }
}
