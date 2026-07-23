// <copyright file="EquityCurveGenerator.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Analytics;

namespace TradingBot.Analytics;

/// <summary>
/// Generates equity curves from trade history.
/// </summary>
public sealed class EquityCurveGenerator
{
    private readonly IPortfolioManager _portfolioManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EquityCurveGenerator"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    public EquityCurveGenerator(IPortfolioManager portfolioManager)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
    }

    /// <summary>
    /// Generates an equity curve from trade history.
    /// </summary>
    /// <param name="initialCapital">Initial capital amount.</param>
    /// <param name="startDate">Start date filter (optional).</param>
    /// <param name="endDate">End date filter (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of equity curve points.</returns>
    public async Task<IReadOnlyList<EquityPoint>> GenerateEquityCurveAsync(
        decimal initialCapital,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var trades = await _portfolioManager.GetTradeHistoryAsync(
            startDate,
            endDate,
            cancellationToken: cancellationToken);

        if (trades.Count == 0)
        {
            return new List<EquityPoint>
            {
                new EquityPoint
                {
                    Timestamp = DateTime.UtcNow,
                    Equity = initialCapital,
                    Drawdown = 0m,
                    Peak = initialCapital,
                    ReturnPercent = 0m,
                },
            };
        }

        var points = new List<EquityPoint>();
        var equity = initialCapital;
        var peak = initialCapital;

        // Add initial point
        points.Add(new EquityPoint
        {
            Timestamp = trades[0].EntryTime,
            Equity = initialCapital,
            Drawdown = 0m,
            Peak = initialCapital,
            ReturnPercent = 0m,
        });

        // Process each trade chronologically
        foreach (var trade in trades.OrderBy(t => t.ExitTime))
        {
            // Update equity with realized P&L (already includes commission)
            equity += trade.RealizedPnL;

            // Update peak
            peak = Math.Max(peak, equity);

            // Calculate drawdown
            var drawdown = peak > 0 ? ((peak - equity) / peak) * 100m : 0m;

            // Calculate return from initial
            var returnPercent = initialCapital > 0 ? ((equity - initialCapital) / initialCapital) * 100m : 0m;

            points.Add(new EquityPoint
            {
                Timestamp = trade.ExitTime,
                Equity = equity,
                Drawdown = drawdown,
                Peak = peak,
                ReturnPercent = returnPercent,
            });
        }

        return points;
    }

    /// <summary>
    /// Generates a daily equity curve by resampling to end-of-day values.
    /// </summary>
    /// <param name="initialCapital">Initial capital amount.</param>
    /// <param name="startDate">Start date filter (optional).</param>
    /// <param name="endDate">End date filter (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of daily equity curve points.</returns>
    public async Task<IReadOnlyList<EquityPoint>> GenerateDailyEquityCurveAsync(
        decimal initialCapital,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var allPoints = await GenerateEquityCurveAsync(initialCapital, startDate, endDate, cancellationToken);

        if (allPoints.Count == 0)
        {
            return allPoints;
        }

        // Group by date and take the last point of each day
        var dailyPoints = allPoints
            .GroupBy(p => p.Timestamp.Date)
            .Select(g => g.OrderBy(p => p.Timestamp).Last())
            .OrderBy(p => p.Timestamp)
            .ToList();

        return dailyPoints;
    }
}
