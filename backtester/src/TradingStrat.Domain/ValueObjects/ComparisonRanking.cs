using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Value object containing ranking logic and scores for strategy comparison.
/// Uses weighted scoring across multiple performance metrics.
/// </summary>
public sealed class ComparisonRanking : ValueObject
{
    public decimal VariantAScore { get; init; }
    public decimal VariantBScore { get; init; }
    public Dictionary<string, MetricComparison> MetricBreakdown { get; init; }
    public int WinnerIndex { get; init; }

    public ComparisonRanking(
        decimal variantAScore,
        decimal variantBScore,
        Dictionary<string, MetricComparison> metricBreakdown,
        int winnerIndex)
    {
        VariantAScore = variantAScore;
        VariantBScore = variantBScore;
        MetricBreakdown = metricBreakdown;
        WinnerIndex = winnerIndex;
    }

    /// <summary>
    /// Calculates ranking between two performance metrics using weighted scoring.
    /// Weights: Sharpe Ratio (40%), Annualized Return (30%), Max Drawdown (20%), Win Rate (10%)
    /// </summary>
    public static ComparisonRanking CalculateRanking(
        PerformanceMetrics metricsA,
        PerformanceMetrics metricsB)
    {
        Dictionary<string, MetricComparison> breakdown = new Dictionary<string, MetricComparison>();

        // Key metrics for ranking (selected based on user requirements)
        breakdown["Sharpe Ratio"] = CompareMetric(
            metricsA.SharpeRatio,
            metricsB.SharpeRatio,
            higherIsBetter: true,
            weight: 0.40m);

        breakdown["Annualized Return"] = CompareMetric(
            metricsA.AnnualizedReturn,
            metricsB.AnnualizedReturn,
            higherIsBetter: true,
            weight: 0.30m);

        breakdown["Max Drawdown %"] = CompareMetric(
            metricsA.MaxDrawdownPercentage,
            metricsB.MaxDrawdownPercentage,
            higherIsBetter: true,  // Higher (closer to zero) is better for negative values
            weight: 0.20m);

        breakdown["Win Rate"] = CompareMetric(
            metricsA.WinRate,
            metricsB.WinRate,
            higherIsBetter: true,
            weight: 0.10m);

        // Additional metrics for display (no weight in ranking)
        breakdown["Total Return %"] = CompareMetric(
            metricsA.TotalReturnPercentage,
            metricsB.TotalReturnPercentage,
            higherIsBetter: true,
            weight: 0m);

        breakdown["Profit Factor"] = CompareMetric(
            metricsA.ProfitFactor,
            metricsB.ProfitFactor,
            higherIsBetter: true,
            weight: 0m);

        // Calculate weighted scores
        decimal scoreA = breakdown.Values.Sum(m => m.VariantAPoints);
        decimal scoreB = breakdown.Values.Sum(m => m.VariantBPoints);

        // Determine winner (require 5% difference to avoid ties on minor differences)
        int winner = 0;
        if (scoreA > scoreB * 1.05m)
        {
            winner = 1;
        }
        else if (scoreB > scoreA * 1.05m)
        {
            winner = 2;
        }

        return new ComparisonRanking(scoreA, scoreB, breakdown, winner);
    }

    private static MetricComparison CompareMetric(
        decimal valueA,
        decimal valueB,
        bool higherIsBetter,
        decimal weight)
    {
        decimal pointsA = 0;
        decimal pointsB = 0;

        if (weight > 0)
        {
            if (higherIsBetter)
            {
                if (valueA > valueB)
                {
                    pointsA = weight;
                }
                else if (valueB > valueA)
                {
                    pointsB = weight;
                }
            }
            else  // Lower is better (e.g., drawdown)
            {
                if (valueA < valueB)
                {
                    pointsA = weight;
                }
                else if (valueB < valueA)
                {
                    pointsB = weight;
                }
            }
        }

        return new MetricComparison(
            valueA,
            valueB,
            pointsA,
            pointsB,
            higherIsBetter);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return VariantAScore;
        yield return VariantBScore;
        foreach (string key in MetricBreakdown.Keys.OrderBy(k => k))
        {
            yield return key;
            yield return MetricBreakdown[key];
        }
        yield return WinnerIndex;
    }
}

/// <summary>
/// Represents the comparison of a single metric between two variants.
/// </summary>
public sealed class MetricComparison : ValueObject
{
    public decimal VariantAValue { get; init; }
    public decimal VariantBValue { get; init; }
    public decimal VariantAPoints { get; init; }
    public decimal VariantBPoints { get; init; }
    public bool HigherIsBetter { get; init; }

    public MetricComparison(
        decimal variantAValue,
        decimal variantBValue,
        decimal variantAPoints,
        decimal variantBPoints,
        bool higherIsBetter)
    {
        VariantAValue = variantAValue;
        VariantBValue = variantBValue;
        VariantAPoints = variantAPoints;
        VariantBPoints = variantBPoints;
        HigherIsBetter = higherIsBetter;
    }

    public decimal Difference => VariantAValue - VariantBValue;
    public decimal PercentageDifference => VariantBValue != 0
        ? ((VariantAValue - VariantBValue) / Math.Abs(VariantBValue)) * 100m
        : 0m;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return VariantAValue;
        yield return VariantBValue;
        yield return VariantAPoints;
        yield return VariantBPoints;
        yield return HigherIsBetter;
    }
}
