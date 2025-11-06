// <copyright file="PerformanceCalculator.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Analytics;

/// <summary>
/// Calculates comprehensive performance metrics from trading results.
/// </summary>
public sealed class PerformanceCalculator
{
    /// <summary>
    /// Calculates performance metrics from a list of trades.
    /// </summary>
    /// <param name="trades">List of completed trades.</param>
    /// <param name="initialCapital">Initial capital amount.</param>
    /// <param name="finalEquity">Final equity amount.</param>
    /// <param name="equityCurve">Equity curve data points.</param>
    /// <returns>Calculated performance metrics.</returns>
    public PerformanceMetrics CalculateMetrics(
        IReadOnlyList<Trade> trades,
        decimal initialCapital,
        decimal finalEquity,
        IReadOnlyList<(DateTime Date, decimal Equity)> equityCurve)
    {
        if (trades == null || trades.Count == 0)
        {
            return CreateEmptyMetrics();
        }

        var winningTrades = trades.Where(t => t.RealizedPnL > 0).ToList();
        var losingTrades = trades.Where(t => t.RealizedPnL < 0).ToList();

        var totalReturn = initialCapital > 0 ? ((finalEquity - initialCapital) / initialCapital) * 100m : 0m;

        var avgWin = winningTrades.Count > 0 ? winningTrades.Average(t => t.RealizedPnL) : 0m;
        var avgLoss = losingTrades.Count > 0 ? Math.Abs(losingTrades.Average(t => t.RealizedPnL)) : 0m;

        var profitFactor = losingTrades.Sum(t => Math.Abs(t.RealizedPnL)) > 0
            ? winningTrades.Sum(t => t.RealizedPnL) / losingTrades.Sum(t => Math.Abs(t.RealizedPnL))
            : winningTrades.Count > 0 ? decimal.MaxValue : 0m;

        var (maxDrawdown, maxDrawdownPercent) = CalculateMaxDrawdown(equityCurve);

        var sharpeRatio = CalculateSharpeRatio(equityCurve);
        var sortinoRatio = CalculateSortinoRatio(equityCurve);
        var annualizedReturn = CalculateAnnualizedReturn(totalReturn, equityCurve);
        var calmarRatio = maxDrawdownPercent > 0 ? annualizedReturn / maxDrawdownPercent : 0m;

        return new PerformanceMetrics
        {
            TotalReturn = totalReturn,
            AnnualizedReturn = annualizedReturn,
            TotalTrades = trades.Count,
            WinningTrades = winningTrades.Count,
            LosingTrades = losingTrades.Count,
            AverageWin = avgWin,
            AverageLoss = avgLoss,
            ProfitFactor = profitFactor,
            MaxDrawdown = maxDrawdownPercent,
            SharpeRatio = sharpeRatio,
            SortinoRatio = sortinoRatio,
            CalmarRatio = calmarRatio,
        };
    }

    private static PerformanceMetrics CreateEmptyMetrics()
    {
        return new PerformanceMetrics
        {
            TotalReturn = 0m,
            AnnualizedReturn = 0m,
            TotalTrades = 0,
            WinningTrades = 0,
            LosingTrades = 0,
            AverageWin = 0m,
            AverageLoss = 0m,
            ProfitFactor = 0m,
            MaxDrawdown = 0m,
            SharpeRatio = 0m,
            SortinoRatio = 0m,
            CalmarRatio = 0m,
        };
    }

    private static (decimal MaxDrawdown, decimal MaxDrawdownPercent) CalculateMaxDrawdown(
        IReadOnlyList<(DateTime Date, decimal Equity)> equityCurve)
    {
        if (equityCurve == null || equityCurve.Count == 0)
        {
            return (0m, 0m);
        }

        decimal maxDrawdownPercent = 0m;
        decimal peak = equityCurve[0].Equity;

        foreach (var point in equityCurve)
        {
            if (point.Equity > peak)
            {
                peak = point.Equity;
            }

            var drawdown = peak - point.Equity;
            var drawdownPercent = peak > 0 ? (drawdown / peak) * 100m : 0m;

            if (drawdownPercent > maxDrawdownPercent)
            {
                maxDrawdownPercent = drawdownPercent;
            }
        }

        return (0m, maxDrawdownPercent);
    }

    private static decimal CalculateAnnualizedReturn(
        decimal totalReturn,
        IReadOnlyList<(DateTime Date, decimal Equity)> equityCurve)
    {
        if (equityCurve == null || equityCurve.Count < 2)
        {
            return totalReturn;
        }

        var startDate = equityCurve[0].Date;
        var endDate = equityCurve[^1].Date;
        var daysElapsed = (endDate - startDate).TotalDays;

        if (daysElapsed == 0)
        {
            return totalReturn;
        }

        var years = (decimal)(daysElapsed / 365.25);
        var returnFactor = 1m + (totalReturn / 100m);

        return years > 0 ? (((decimal)Math.Pow((double)returnFactor, (double)(1m / years)) - 1m) * 100m) : totalReturn;
    }

    private static decimal CalculateSharpeRatio(
        IReadOnlyList<(DateTime Date, decimal Equity)> equityCurve,
        decimal riskFreeRate = 0.02m)
    {
        if (equityCurve == null || equityCurve.Count < 2)
        {
            return 0m;
        }

        var returns = new List<decimal>();
        for (int i = 1; i < equityCurve.Count; i++)
        {
            var previousEquity = equityCurve[i - 1].Equity;
            var currentEquity = equityCurve[i].Equity;

            if (previousEquity > 0)
            {
                var dailyReturn = (currentEquity - previousEquity) / previousEquity;
                returns.Add(dailyReturn);
            }
        }

        if (returns.Count == 0)
        {
            return 0m;
        }

        var avgReturn = returns.Average();
        var stdDev = CalculateStandardDeviation(returns);

        if (stdDev == 0)
        {
            return 0m;
        }

        var annualizedReturn = avgReturn * 252m;
        var annualizedStdDev = stdDev * (decimal)Math.Sqrt(252);

        return annualizedStdDev > 0 ? (annualizedReturn - riskFreeRate) / annualizedStdDev : 0m;
    }

    private static decimal CalculateSortinoRatio(
        IReadOnlyList<(DateTime Date, decimal Equity)> equityCurve,
        decimal riskFreeRate = 0.02m)
    {
        if (equityCurve == null || equityCurve.Count < 2)
        {
            return 0m;
        }

        var returns = new List<decimal>();
        var negativeReturns = new List<decimal>();

        for (int i = 1; i < equityCurve.Count; i++)
        {
            var previousEquity = equityCurve[i - 1].Equity;
            var currentEquity = equityCurve[i].Equity;

            if (previousEquity > 0)
            {
                var dailyReturn = (currentEquity - previousEquity) / previousEquity;
                returns.Add(dailyReturn);

                if (dailyReturn < 0)
                {
                    negativeReturns.Add(dailyReturn);
                }
            }
        }

        if (returns.Count == 0 || negativeReturns.Count == 0)
        {
            return 0m;
        }

        var avgReturn = returns.Average();
        var downsideDeviation = CalculateStandardDeviation(negativeReturns);

        if (downsideDeviation == 0)
        {
            return 0m;
        }

        var annualizedReturn = avgReturn * 252m;
        var annualizedDownsideDev = downsideDeviation * (decimal)Math.Sqrt(252);

        return annualizedDownsideDev > 0 ? (annualizedReturn - riskFreeRate) / annualizedDownsideDev : 0m;
    }

    private static decimal CalculateStandardDeviation(IReadOnlyList<decimal> values)
    {
        if (values == null || values.Count < 2)
        {
            return 0m;
        }

        var avg = values.Average();
        var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
        var variance = sumOfSquares / (values.Count - 1);

        return (decimal)Math.Sqrt((double)variance);
    }
}
