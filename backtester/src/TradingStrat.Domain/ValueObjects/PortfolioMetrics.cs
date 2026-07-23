using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Portfolio-level performance metrics.
/// </summary>
public sealed class PortfolioMetrics : ValueObject
{
    /// <summary>Total portfolio value (cash + all positions).</summary>
    public decimal TotalValue { get; init; }

    /// <summary>Total cost basis.</summary>
    public decimal TotalCost { get; init; }

    /// <summary>Total unrealized gain/loss in dollars.</summary>
    public decimal TotalReturn { get; init; }

    /// <summary>Total return percentage.</summary>
    public decimal TotalReturnPercentage { get; init; }

    /// <summary>Daily return in dollars.</summary>
    public decimal DailyReturn { get; init; }

    /// <summary>Daily return percentage.</summary>
    public decimal DailyReturnPercentage { get; init; }

    /// <summary>Number of positions held.</summary>
    public int NumberOfPositions { get; init; }

    /// <summary>Percentage of portfolio held in cash.</summary>
    public decimal CashPercentage { get; init; }

    /// <summary>Allocation percentage of largest position.</summary>
    public decimal LargestPositionPercentage { get; init; }

    /// <summary>Ticker of the most valuable position.</summary>
    public string MostValuablePosition { get; init; }

    /// <summary>Annualized portfolio volatility.</summary>
    public decimal PortfolioVolatility { get; init; }

    /// <summary>Sharpe ratio (excess return per unit risk).</summary>
    public decimal PortfolioSharpeRatio { get; init; }

    /// <summary>Diversification metric (1/HHI where HHI is Herfindahl-Hirschman Index).</summary>
    public decimal DiversificationRatio { get; init; }

    /// <summary>Average correlation between positions (null if less than 2 positions).</summary>
    public decimal? AverageCorrelation { get; init; }

    /// <summary>Beta of each position relative to equal-weighted portfolio (null if insufficient data).</summary>
    public Dictionary<string, decimal>? PositionBetas { get; init; }

    public PortfolioMetrics(
        decimal totalValue,
        decimal totalCost,
        decimal totalReturn,
        decimal totalReturnPercentage,
        decimal dailyReturn,
        decimal dailyReturnPercentage,
        int numberOfPositions,
        decimal cashPercentage,
        decimal largestPositionPercentage,
        string mostValuablePosition,
        decimal portfolioVolatility,
        decimal portfolioSharpeRatio,
        decimal diversificationRatio,
        decimal? averageCorrelation,
        Dictionary<string, decimal>? positionBetas)
    {
        TotalValue = totalValue;
        TotalCost = totalCost;
        TotalReturn = totalReturn;
        TotalReturnPercentage = totalReturnPercentage;
        DailyReturn = dailyReturn;
        DailyReturnPercentage = dailyReturnPercentage;
        NumberOfPositions = numberOfPositions;
        CashPercentage = cashPercentage;
        LargestPositionPercentage = largestPositionPercentage;
        MostValuablePosition = mostValuablePosition;
        PortfolioVolatility = portfolioVolatility;
        PortfolioSharpeRatio = portfolioSharpeRatio;
        DiversificationRatio = diversificationRatio;
        AverageCorrelation = averageCorrelation;
        PositionBetas = positionBetas;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalValue;
        yield return TotalCost;
        yield return TotalReturn;
        yield return TotalReturnPercentage;
        yield return DailyReturn;
        yield return DailyReturnPercentage;
        yield return NumberOfPositions;
        yield return CashPercentage;
        yield return LargestPositionPercentage;
        yield return MostValuablePosition;
        yield return PortfolioVolatility;
        yield return PortfolioSharpeRatio;
        yield return DiversificationRatio;
        yield return AverageCorrelation ?? 0m;
        if (PositionBetas is not null)
        {
            foreach (string key in PositionBetas.Keys.OrderBy(k => k))
            {
                yield return key;
                yield return PositionBetas[key];
            }
        }
    }
}

/// <summary>
/// Single data point in portfolio performance history.
/// </summary>
public sealed class PortfolioPerformancePoint : ValueObject
{
    /// <summary>The date of this data point.</summary>
    public DateTime Date { get; init; }

    /// <summary>Total portfolio value on this date.</summary>
    public decimal TotalValue { get; init; }

    /// <summary>Cash balance on this date.</summary>
    public decimal Cash { get; init; }

    /// <summary>Total value of equity positions (TotalValue - Cash).</summary>
    public decimal EquityValue { get; init; }

    /// <summary>Daily return (change from previous day).</summary>
    public decimal DailyReturn { get; init; }

    public PortfolioPerformancePoint(
        DateTime date,
        decimal totalValue,
        decimal cash,
        decimal equityValue,
        decimal dailyReturn)
    {
        Date = date;
        TotalValue = totalValue;
        Cash = cash;
        EquityValue = equityValue;
        DailyReturn = dailyReturn;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Date;
        yield return TotalValue;
        yield return Cash;
        yield return EquityValue;
        yield return DailyReturn;
    }
}

/// <summary>
/// Historical performance data for a portfolio over a time period.
/// </summary>
public sealed class PortfolioPerformanceHistory : ValueObject
{
    /// <summary>The portfolio identifier.</summary>
    public int PortfolioId { get; init; }

    /// <summary>Start date of the period.</summary>
    public DateTime StartDate { get; init; }

    /// <summary>End date of the period.</summary>
    public DateTime EndDate { get; init; }

    /// <summary>Daily performance data points.</summary>
    public List<PortfolioPerformancePoint> DataPoints { get; init; }

    /// <summary>Current portfolio metrics.</summary>
    public PortfolioMetrics CurrentMetrics { get; init; }

    public PortfolioPerformanceHistory(
        int portfolioId,
        DateTime startDate,
        DateTime endDate,
        List<PortfolioPerformancePoint> dataPoints,
        PortfolioMetrics currentMetrics)
    {
        PortfolioId = portfolioId;
        StartDate = startDate;
        EndDate = endDate;
        DataPoints = dataPoints;
        CurrentMetrics = currentMetrics;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return PortfolioId;
        yield return StartDate;
        yield return EndDate;
        foreach (PortfolioPerformancePoint point in DataPoints)
        {
            yield return point;
        }
        yield return CurrentMetrics;
    }
}
