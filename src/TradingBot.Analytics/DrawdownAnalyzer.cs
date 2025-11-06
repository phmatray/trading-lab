// <copyright file="DrawdownAnalyzer.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Analytics;

namespace TradingBot.Analytics;

/// <summary>
/// Analyzes drawdown periods from equity curve data.
/// </summary>
public sealed class DrawdownAnalyzer
{
    /// <summary>
    /// Analyzes all drawdown periods from an equity curve.
    /// </summary>
    /// <param name="equityCurve">Equity curve points.</param>
    /// <returns>List of drawdown periods.</returns>
    public IReadOnlyList<DrawdownPeriod> AnalyzeDrawdowns(IReadOnlyList<EquityPoint> equityCurve)
    {
        if (equityCurve == null || equityCurve.Count == 0)
        {
            return Array.Empty<DrawdownPeriod>();
        }

        var drawdownPeriods = new List<DrawdownPeriod>();
        DrawdownPeriod? currentDrawdown = null;
        var peak = equityCurve[0].Equity;
        var peakDate = equityCurve[0].Timestamp;

        for (int i = 0; i < equityCurve.Count; i++)
        {
            var point = equityCurve[i];

            if (point.Equity >= peak)
            {
                // New peak - check if we're recovering from a drawdown
                if (currentDrawdown != null)
                {
                    // Mark recovery
                    currentDrawdown.RecoveryDate = point.Timestamp;
                    currentDrawdown.RecoveryDays = (int)(point.Timestamp - currentDrawdown.EndDate).TotalDays;
                    drawdownPeriods.Add(currentDrawdown);
                    currentDrawdown = null;
                }

                peak = point.Equity;
                peakDate = point.Timestamp;
            }
            else
            {
                // In drawdown
                if (currentDrawdown == null)
                {
                    // Start of new drawdown
                    currentDrawdown = new DrawdownPeriod
                    {
                        StartDate = peakDate,
                        EndDate = point.Timestamp,
                        PeakEquity = peak,
                        TroughEquity = point.Equity,
                    };
                }
                else
                {
                    // Update existing drawdown if this is a new low
                    if (point.Equity < currentDrawdown.TroughEquity)
                    {
                        currentDrawdown.TroughEquity = point.Equity;
                        currentDrawdown.EndDate = point.Timestamp;
                    }
                }

                // Update drawdown metrics
                if (currentDrawdown != null)
                {
                    currentDrawdown.MaxDrawdownPercent = currentDrawdown.PeakEquity > 0
                        ? ((currentDrawdown.PeakEquity - currentDrawdown.TroughEquity) / currentDrawdown.PeakEquity) * 100m
                        : 0m;
                    currentDrawdown.DurationDays = (int)(currentDrawdown.EndDate - currentDrawdown.StartDate).TotalDays;
                }
            }
        }

        // If still in drawdown at the end, add it without recovery
        if (currentDrawdown != null)
        {
            drawdownPeriods.Add(currentDrawdown);
        }

        return drawdownPeriods;
    }

    /// <summary>
    /// Gets the maximum drawdown from a list of drawdown periods.
    /// </summary>
    /// <param name="drawdownPeriods">Drawdown periods.</param>
    /// <returns>Maximum drawdown period.</returns>
    public DrawdownPeriod? GetMaxDrawdown(IReadOnlyList<DrawdownPeriod> drawdownPeriods)
    {
        return drawdownPeriods
            .OrderByDescending(d => d.MaxDrawdownPercent)
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets the longest drawdown period by duration.
    /// </summary>
    /// <param name="drawdownPeriods">Drawdown periods.</param>
    /// <returns>Longest drawdown period.</returns>
    public DrawdownPeriod? GetLongestDrawdown(IReadOnlyList<DrawdownPeriod> drawdownPeriods)
    {
        return drawdownPeriods
            .OrderByDescending(d => d.DurationDays)
            .FirstOrDefault();
    }

    /// <summary>
    /// Calculates the average drawdown percentage.
    /// </summary>
    /// <param name="drawdownPeriods">Drawdown periods.</param>
    /// <returns>Average drawdown percentage.</returns>
    public decimal GetAverageDrawdown(IReadOnlyList<DrawdownPeriod> drawdownPeriods)
    {
        if (drawdownPeriods.Count == 0)
        {
            return 0m;
        }

        return drawdownPeriods.Average(d => d.MaxDrawdownPercent);
    }

    /// <summary>
    /// Calculates the average recovery time in days.
    /// </summary>
    /// <param name="drawdownPeriods">Drawdown periods.</param>
    /// <returns>Average recovery time in days.</returns>
    public double GetAverageRecoveryTime(IReadOnlyList<DrawdownPeriod> drawdownPeriods)
    {
        var recoveredDrawdowns = drawdownPeriods.Where(d => d.IsRecovered && d.RecoveryDays.HasValue).ToList();

        if (recoveredDrawdowns.Count == 0)
        {
            return 0;
        }

        return recoveredDrawdowns.Average(d => d.RecoveryDays!.Value);
    }

    /// <summary>
    /// Gets the current drawdown status from the equity curve.
    /// </summary>
    /// <param name="equityCurve">Equity curve points.</param>
    /// <returns>Current drawdown percentage, or 0 if at peak.</returns>
    public decimal GetCurrentDrawdown(IReadOnlyList<EquityPoint> equityCurve)
    {
        if (equityCurve == null || equityCurve.Count == 0)
        {
            return 0m;
        }

        var latest = equityCurve[^1];
        return latest.Drawdown;
    }
}
