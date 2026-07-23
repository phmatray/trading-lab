using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Domain service focused on analyzing equity curve data.
/// Calculates drawdown metrics and daily returns from equity history.
/// </summary>
public class EquityCurveAnalyzer
{
    /// <summary>
    /// Calculates the maximum drawdown from an equity curve.
    /// </summary>
    /// <param name="equityCurve">List of equity points over time.</param>
    /// <returns>Tuple of (max drawdown in dollars, max drawdown percentage).</returns>
    public (decimal maxDrawdown, decimal maxDrawdownPercentage) CalculateMaxDrawdown(List<EquityPoint> equityCurve)
    {
        if (equityCurve.Count == 0)
        {
            return (0, 0);
        }

        decimal maxEquity = equityCurve[0].Equity;
        decimal maxDrawdown = 0;
        decimal maxDrawdownPercentage = 0;

        foreach (EquityPoint point in equityCurve)
        {
            if (point.Equity > maxEquity)
            {
                maxEquity = point.Equity;
            }

            decimal drawdown = maxEquity - point.Equity;
            decimal drawdownPercentage = maxEquity > 0 ? (drawdown / maxEquity) * 100 : 0;

            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
                maxDrawdownPercentage = drawdownPercentage;
            }
        }

        return (maxDrawdown, maxDrawdownPercentage);
    }

    /// <summary>
    /// Calculates daily returns from an equity curve.
    /// </summary>
    /// <param name="equityCurve">List of equity points over time.</param>
    /// <returns>List of daily returns as decimals (e.g., 0.02 = 2% gain).</returns>
    public List<decimal> CalculateDailyReturns(List<EquityPoint> equityCurve)
    {
        var returns = new List<decimal>();

        for (int i = 1; i < equityCurve.Count; i++)
        {
            decimal previousEquity = equityCurve[i - 1].Equity;
            decimal currentEquity = equityCurve[i].Equity;

            if (previousEquity > 0)
            {
                decimal dailyReturn = (currentEquity - previousEquity) / previousEquity;
                returns.Add(dailyReturn);
            }
        }

        return returns;
    }

    /// <summary>
    /// Analyzes the equity curve and returns comprehensive statistics.
    /// </summary>
    /// <param name="equityCurve">List of equity points over time.</param>
    /// <param name="totalDays">Total number of days in the period.</param>
    /// <returns>Equity curve statistics including drawdown and market exposure.</returns>
    public EquityCurveStatistics Analyze(List<EquityPoint> equityCurve, int totalDays)
    {
        if (equityCurve.Count == 0)
        {
            return new EquityCurveStatistics(
                MaxDrawdown: 0,
                MaxDrawdownPercentage: 0,
                DaysInMarket: 0,
                MarketExposurePercentage: 0
            );
        }

        (decimal maxDrawdown, decimal maxDrawdownPercentage) = CalculateMaxDrawdown(equityCurve);

        int daysInMarket = equityCurve.Count(e => e.Position > 0);
        decimal marketExposurePercentage = totalDays > 0 ? (decimal)daysInMarket / totalDays * 100 : 0;

        return new EquityCurveStatistics(
            MaxDrawdown: maxDrawdown,
            MaxDrawdownPercentage: maxDrawdownPercentage,
            DaysInMarket: daysInMarket,
            MarketExposurePercentage: marketExposurePercentage
        );
    }
}

/// <summary>
/// Immutable record containing equity curve analysis results.
/// </summary>
public record EquityCurveStatistics(
    decimal MaxDrawdown,
    decimal MaxDrawdownPercentage,
    int DaysInMarket,
    decimal MarketExposurePercentage
);
