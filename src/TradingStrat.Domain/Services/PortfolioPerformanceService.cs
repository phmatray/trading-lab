using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Domain service for calculating portfolio performance metrics.
/// Pure business logic with no external dependencies.
/// </summary>
public class PortfolioPerformanceService
{
    /// <summary>
    /// Calculates comprehensive portfolio metrics from snapshot and optional historical data.
    /// </summary>
    /// <param name="snapshot">Current portfolio snapshot.</param>
    /// <param name="historicalPoints">Optional historical performance points for volatility and Sharpe calculations.</param>
    /// <returns>Complete portfolio metrics.</returns>
    public PortfolioMetrics CalculateMetrics(
        PortfolioSnapshot snapshot,
        List<PortfolioPerformancePoint>? historicalPoints = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        int numPositions = snapshot.Positions.Count;
        decimal cashPercent = snapshot.TotalValue > 0
            ? (snapshot.Cash / snapshot.TotalValue) * 100
            : 0;

        decimal largestPositionPercent = numPositions > 0
            ? snapshot.Positions.Max(p => p.AllocationPercentage)
            : 0m;

        string mostValuablePosition = numPositions > 0
            ? snapshot.Positions.OrderByDescending(p => p.MarketValue).First().Ticker
            : "None";

        // Calculate diversification ratio (inverse of Herfindahl-Hirschman Index)
        decimal hhi = snapshot.Positions.Sum(p =>
            (decimal)Math.Pow((double)(p.AllocationPercentage / 100m), 2));
        decimal diversificationRatio = hhi > 0 ? 1m / hhi : 1m;

        // Calculate volatility and Sharpe from historical data if available
        decimal volatility = 0m;
        decimal sharpeRatio = 0m;
        decimal dailyReturn = 0m;
        decimal dailyReturnPercentage = 0m;

        if (historicalPoints is not null && historicalPoints.Count > 2)
        {
            var returns = historicalPoints
                .OrderBy(p => p.Date)
                .Select(p => (double)p.DailyReturn)
                .ToList();

            volatility = CalculateStandardDeviation(returns) * (decimal)Math.Sqrt(252);
            sharpeRatio = CalculateSharpeRatio(returns);

            // Get most recent daily return
            PortfolioPerformancePoint? lastPoint = historicalPoints.OrderByDescending(p => p.Date).FirstOrDefault();
            if (lastPoint is not null)
            {
                dailyReturn = lastPoint.DailyReturn;
                dailyReturnPercentage = lastPoint.TotalValue > 0
                    ? (dailyReturn / (lastPoint.TotalValue - dailyReturn)) * 100
                    : 0;
            }
        }

        // Future Enhancement: Correlation and Beta Calculations
        //
        // These advanced portfolio metrics require historical position-level data (individual security prices over time),
        // which is not currently tracked. The system currently only maintains portfolio-level historical data.
        //
        // AverageCorrelation:
        //   - Measures the average pairwise correlation between positions in the portfolio
        //   - Range: -1 (perfect negative correlation) to +1 (perfect positive correlation)
        //   - Lower correlation indicates better diversification
        //   - Requires: Daily returns for each position over a common time period (typically 30-90 days)
        //   - Calculation: Pearson correlation coefficient for each pair, then average
        //
        // PositionBetas:
        //   - Dictionary mapping each ticker to its beta coefficient
        //   - Beta measures a position's volatility relative to the overall portfolio
        //   - Beta > 1: More volatile than portfolio, Beta < 1: Less volatile than portfolio
        //   - Requires: Daily returns for each position and portfolio returns over same period
        //   - Calculation: Covariance(position_returns, portfolio_returns) / Variance(portfolio_returns)
        //
        // Implementation Prerequisites:
        //   1. Create PositionPerformancePoint value object (Ticker, Date, Price, DailyReturn)
        //   2. Track historical position data in PortfolioPerformancePoint
        //   3. Add GetPositionHistoricalData() method to IPortfolioPort
        //   4. Implement CalculateCorrelationMatrix() and CalculatePositionBetas() methods in this service
        //   5. Update PortfolioPerformancePoint to include position-level returns dictionary
        //
        // Benefits:
        //   - Identify positions driving portfolio volatility (high beta positions)
        //   - Assess true diversification (low average correlation)
        //   - Optimize position weights for risk-adjusted returns
        //   - Detect concentrated risk from highly correlated positions
        decimal? averageCorrelation = null;
        Dictionary<string, decimal>? positionBetas = null;

        return new PortfolioMetrics(
            TotalValue: snapshot.TotalValue,
            TotalCost: snapshot.TotalCost,
            TotalReturn: snapshot.UnrealizedGainLoss,
            TotalReturnPercentage: snapshot.UnrealizedGainLossPercentage,
            DailyReturn: dailyReturn,
            DailyReturnPercentage: dailyReturnPercentage,
            NumberOfPositions: numPositions,
            CashPercentage: cashPercent,
            LargestPositionPercentage: largestPositionPercent,
            MostValuablePosition: mostValuablePosition,
            PortfolioVolatility: volatility,
            PortfolioSharpeRatio: sharpeRatio,
            DiversificationRatio: diversificationRatio,
            AverageCorrelation: averageCorrelation,
            PositionBetas: positionBetas
        );
    }

    /// <summary>
    /// Calculates standard deviation of a series of values.
    /// </summary>
    /// <param name="values">List of values.</param>
    /// <returns>Standard deviation.</returns>
    private decimal CalculateStandardDeviation(List<double> values)
    {
        if (values.Count < 2)
        {
            return 0m;
        }

        double avg = values.Average();
        double sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
        double variance = sumOfSquares / (values.Count - 1);
        return (decimal)Math.Sqrt(variance);
    }

    /// <summary>
    /// Calculates Sharpe ratio from daily returns.
    /// Assumes risk-free rate of 0 for simplicity.
    /// </summary>
    /// <param name="dailyReturns">List of daily returns.</param>
    /// <returns>Annualized Sharpe ratio.</returns>
    private decimal CalculateSharpeRatio(List<double> dailyReturns)
    {
        if (dailyReturns.Count < 2)
        {
            return 0m;
        }

        double avgReturn = dailyReturns.Average();
        double stdDev = (double)CalculateStandardDeviation(dailyReturns);

        if (stdDev == 0)
        {
            return 0m;
        }

        // Annualize: multiply by sqrt(252) trading days
        return (decimal)(avgReturn / stdDev * Math.Sqrt(252));
    }
}
