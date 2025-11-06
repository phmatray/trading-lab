// <copyright file="MetricsCalculator.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Analytics;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Analytics;

/// <summary>
/// Calculates comprehensive trading metrics and analytics.
/// </summary>
public sealed class MetricsCalculator
{
    private const decimal RiskFreeRate = 0.02m; // 2% annual risk-free rate
    private const decimal TradingDaysPerYear = 252m;

    /// <summary>
    /// Calculates the Sharpe ratio from returns.
    /// </summary>
    /// <param name="returns">List of period returns.</param>
    /// <param name="riskFreeRate">Risk-free rate (default 2%).</param>
    /// <returns>Sharpe ratio.</returns>
    public decimal CalculateSharpeRatio(IReadOnlyList<decimal> returns, decimal riskFreeRate = RiskFreeRate)
    {
        if (returns == null || returns.Count == 0)
        {
            return 0m;
        }

        var avgReturn = returns.Average();
        var stdDev = CalculateStandardDeviation(returns);

        if (stdDev == 0m)
        {
            return 0m;
        }

        // Annualize the Sharpe ratio
        var excessReturn = avgReturn - (riskFreeRate / TradingDaysPerYear);
        return excessReturn / stdDev * (decimal)Math.Sqrt((double)TradingDaysPerYear);
    }

    /// <summary>
    /// Calculates the Sortino ratio (downside risk-adjusted return).
    /// </summary>
    /// <param name="returns">List of period returns.</param>
    /// <param name="riskFreeRate">Risk-free rate (default 2%).</param>
    /// <returns>Sortino ratio.</returns>
    public decimal CalculateSortinoRatio(IReadOnlyList<decimal> returns, decimal riskFreeRate = RiskFreeRate)
    {
        if (returns == null || returns.Count == 0)
        {
            return 0m;
        }

        var avgReturn = returns.Average();
        var downsideReturns = returns.Where(r => r < 0).ToList();

        if (downsideReturns.Count == 0)
        {
            return decimal.MaxValue; // No downside risk
        }

        var downsideDeviation = CalculateStandardDeviation(downsideReturns);

        if (downsideDeviation == 0m)
        {
            return 0m;
        }

        var excessReturn = avgReturn - (riskFreeRate / TradingDaysPerYear);
        return excessReturn / downsideDeviation * (decimal)Math.Sqrt((double)TradingDaysPerYear);
    }

    /// <summary>
    /// Calculates the Calmar ratio (return / max drawdown).
    /// </summary>
    /// <param name="annualizedReturn">Annualized return percentage.</param>
    /// <param name="maxDrawdown">Maximum drawdown percentage.</param>
    /// <returns>Calmar ratio.</returns>
    public decimal CalculateCalmarRatio(decimal annualizedReturn, decimal maxDrawdown)
    {
        if (maxDrawdown == 0m)
        {
            return annualizedReturn > 0 ? decimal.MaxValue : 0m;
        }

        return annualizedReturn / maxDrawdown;
    }

    /// <summary>
    /// Calculates rolling Sharpe ratio over a specified window.
    /// </summary>
    /// <param name="equityCurve">Equity curve points.</param>
    /// <param name="windowDays">Rolling window in days (default 30).</param>
    /// <returns>List of rolling Sharpe ratios.</returns>
    public List<(DateTime Date, decimal SharpeRatio)> CalculateRollingSharpe(
        IReadOnlyList<EquityPoint> equityCurve,
        int windowDays = 30)
    {
        if (equityCurve == null || equityCurve.Count < windowDays)
        {
            return new List<(DateTime, decimal)>();
        }

        var results = new List<(DateTime, decimal)>();
        var returns = CalculateReturns(equityCurve);

        for (int i = windowDays; i < returns.Count; i++)
        {
            var windowReturns = returns.Skip(i - windowDays).Take(windowDays).ToList();
            var sharpe = CalculateSharpeRatio(windowReturns);
            results.Add((equityCurve[i].Timestamp, sharpe));
        }

        return results;
    }

    /// <summary>
    /// Calculates profit factor (gross profit / gross loss).
    /// </summary>
    /// <param name="trades">List of trades.</param>
    /// <returns>Profit factor.</returns>
    public decimal CalculateProfitFactor(IReadOnlyList<Trade> trades)
    {
        if (trades == null || trades.Count == 0)
        {
            return 0m;
        }

        var grossProfit = trades.Where(t => t.RealizedPnL > 0).Sum(t => t.RealizedPnL);
        var grossLoss = Math.Abs(trades.Where(t => t.RealizedPnL < 0).Sum(t => t.RealizedPnL));

        if (grossLoss == 0m)
        {
            return grossProfit > 0 ? decimal.MaxValue : 0m;
        }

        return grossProfit / grossLoss;
    }

    /// <summary>
    /// Calculates win rate percentage.
    /// </summary>
    /// <param name="trades">List of trades.</param>
    /// <returns>Win rate percentage.</returns>
    public decimal CalculateWinRate(IReadOnlyList<Trade> trades)
    {
        if (trades == null || trades.Count == 0)
        {
            return 0m;
        }

        var winningTrades = trades.Count(t => t.RealizedPnL > 0);
        return ((decimal)winningTrades / trades.Count) * 100m;
    }

    /// <summary>
    /// Calculates expectancy (average trade profit).
    /// </summary>
    /// <param name="trades">List of trades.</param>
    /// <returns>Expectancy value.</returns>
    public decimal CalculateExpectancy(IReadOnlyList<Trade> trades)
    {
        if (trades == null || trades.Count == 0)
        {
            return 0m;
        }

        return trades.Average(t => t.RealizedPnL);
    }

    /// <summary>
    /// Calculates the standard deviation of returns.
    /// </summary>
    /// <param name="returns">List of returns.</param>
    /// <returns>Standard deviation.</returns>
    private static decimal CalculateStandardDeviation(IReadOnlyList<decimal> returns)
    {
        if (returns.Count == 0)
        {
            return 0m;
        }

        var avg = returns.Average();
        var sumSquaredDiffs = returns.Sum(r => (r - avg) * (r - avg));
        var variance = sumSquaredDiffs / returns.Count;
        return (decimal)Math.Sqrt((double)variance);
    }

    /// <summary>
    /// Calculates period-to-period returns from equity curve.
    /// </summary>
    /// <param name="equityCurve">Equity curve points.</param>
    /// <returns>List of returns.</returns>
    private static List<decimal> CalculateReturns(IReadOnlyList<EquityPoint> equityCurve)
    {
        var returns = new List<decimal>();

        for (int i = 1; i < equityCurve.Count; i++)
        {
            var prevEquity = equityCurve[i - 1].Equity;
            var currentEquity = equityCurve[i].Equity;

            if (prevEquity > 0)
            {
                var periodReturn = ((currentEquity - prevEquity) / prevEquity) * 100m;
                returns.Add(periodReturn);
            }
        }

        return returns;
    }
}
