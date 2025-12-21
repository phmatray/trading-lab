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
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

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

        if (historicalPoints != null && historicalPoints.Count > 2)
        {
            var returns = historicalPoints
                .OrderBy(p => p.Date)
                .Select(p => (double)p.DailyReturn)
                .ToList();

            volatility = CalculateStandardDeviation(returns) * (decimal)Math.Sqrt(252);
            sharpeRatio = CalculateSharpeRatio(returns);

            // Get most recent daily return
            var lastPoint = historicalPoints.OrderByDescending(p => p.Date).FirstOrDefault();
            if (lastPoint != null)
            {
                dailyReturn = lastPoint.DailyReturn;
                dailyReturnPercentage = lastPoint.TotalValue > 0
                    ? (dailyReturn / (lastPoint.TotalValue - dailyReturn)) * 100
                    : 0;
            }
        }

        // Calculate average correlation (requires at least 2 positions and historical data)
        decimal? averageCorrelation = null;
        Dictionary<string, decimal>? positionBetas = null;

        // TODO: Implement correlation and beta calculations when historical position-level data is available

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
