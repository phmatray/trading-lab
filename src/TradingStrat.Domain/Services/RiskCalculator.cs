namespace TradingStrat.Domain.Services;

/// <summary>
/// Domain service focused on calculating risk metrics.
/// Computes Sharpe ratio, volatility, and statistical measures.
/// </summary>
public class RiskCalculator
{
    /// <summary>
    /// Calculates the annualized Sharpe ratio from daily returns.
    /// Assumes risk-free rate of 0 for simplicity.
    /// </summary>
    /// <param name="dailyReturns">List of daily returns as decimals.</param>
    /// <returns>Annualized Sharpe ratio (higher is better, typically > 1 is good).</returns>
    public decimal CalculateSharpeRatio(List<decimal> dailyReturns)
    {
        if (dailyReturns.Count < 2)
        {
            return 0;
        }

        decimal averageReturn = dailyReturns.Average();
        decimal stdDev = CalculateStandardDeviation(dailyReturns);

        if (stdDev == 0)
        {
            return 0;
        }

        // Annualize: multiply by sqrt(252) trading days
        decimal sharpeRatio = averageReturn / stdDev;
        return sharpeRatio * (decimal)Math.Sqrt(252);
    }

    /// <summary>
    /// Calculates the annualized volatility from daily returns.
    /// </summary>
    /// <param name="dailyReturns">List of daily returns as decimals.</param>
    /// <returns>Annualized volatility as a percentage.</returns>
    public decimal CalculateVolatility(List<decimal> dailyReturns)
    {
        if (dailyReturns.Count < 2)
        {
            return 0;
        }

        decimal stdDev = CalculateStandardDeviation(dailyReturns);
        // Annualize: multiply by sqrt(252) trading days, then convert to percentage
        return stdDev * (decimal)Math.Sqrt(252) * 100;
    }

    /// <summary>
    /// Calculates the standard deviation of a set of values.
    /// Uses sample standard deviation (N-1 denominator).
    /// </summary>
    /// <param name="values">List of values to calculate standard deviation for.</param>
    /// <returns>Standard deviation.</returns>
    public decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        decimal average = values.Average();
        double sumOfSquares = values.Sum(v => Math.Pow((double)(v - average), 2));
        double variance = sumOfSquares / (values.Count - 1);
        return (decimal)Math.Sqrt(variance);
    }

    /// <summary>
    /// Analyzes risk metrics from daily returns.
    /// </summary>
    /// <param name="dailyReturns">List of daily returns as decimals.</param>
    /// <returns>Risk metrics including Sharpe ratio and volatility.</returns>
    public RiskMetrics Analyze(List<decimal> dailyReturns)
    {
        decimal sharpeRatio = CalculateSharpeRatio(dailyReturns);
        decimal volatility = CalculateVolatility(dailyReturns);

        return new RiskMetrics(
            SharpeRatio: sharpeRatio,
            Volatility: volatility
        );
    }
}

/// <summary>
/// Immutable record containing risk analysis results.
/// </summary>
public record RiskMetrics(
    decimal SharpeRatio,
    decimal Volatility
);
