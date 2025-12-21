namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Portfolio-level performance metrics.
/// </summary>
/// <param name="TotalValue">Total portfolio value (cash + all positions).</param>
/// <param name="TotalCost">Total cost basis.</param>
/// <param name="TotalReturn">Total unrealized gain/loss in dollars.</param>
/// <param name="TotalReturnPercentage">Total return percentage.</param>
/// <param name="DailyReturn">Daily return in dollars.</param>
/// <param name="DailyReturnPercentage">Daily return percentage.</param>
/// <param name="NumberOfPositions">Number of positions held.</param>
/// <param name="CashPercentage">Percentage of portfolio held in cash.</param>
/// <param name="LargestPositionPercentage">Allocation percentage of largest position.</param>
/// <param name="MostValuablePosition">Ticker of the most valuable position.</param>
/// <param name="PortfolioVolatility">Annualized portfolio volatility.</param>
/// <param name="PortfolioSharpeRatio">Sharpe ratio (excess return per unit risk).</param>
/// <param name="DiversificationRatio">Diversification metric (1/HHI where HHI is Herfindahl-Hirschman Index).</param>
/// <param name="AverageCorrelation">Average correlation between positions (null if less than 2 positions).</param>
/// <param name="PositionBetas">Beta of each position relative to equal-weighted portfolio (null if insufficient data).</param>
public record PortfolioMetrics(
    decimal TotalValue,
    decimal TotalCost,
    decimal TotalReturn,
    decimal TotalReturnPercentage,
    decimal DailyReturn,
    decimal DailyReturnPercentage,
    int NumberOfPositions,
    decimal CashPercentage,
    decimal LargestPositionPercentage,
    string MostValuablePosition,
    decimal PortfolioVolatility,
    decimal PortfolioSharpeRatio,
    decimal DiversificationRatio,
    decimal? AverageCorrelation,
    Dictionary<string, decimal>? PositionBetas
);

/// <summary>
/// Single data point in portfolio performance history.
/// </summary>
/// <param name="Date">The date of this data point.</param>
/// <param name="TotalValue">Total portfolio value on this date.</param>
/// <param name="Cash">Cash balance on this date.</param>
/// <param name="EquityValue">Total value of equity positions (TotalValue - Cash).</param>
/// <param name="DailyReturn">Daily return (change from previous day).</param>
public record PortfolioPerformancePoint(
    DateTime Date,
    decimal TotalValue,
    decimal Cash,
    decimal EquityValue,
    decimal DailyReturn
);

/// <summary>
/// Historical performance data for a portfolio over a time period.
/// </summary>
/// <param name="PortfolioId">The portfolio identifier.</param>
/// <param name="StartDate">Start date of the period.</param>
/// <param name="EndDate">End date of the period.</param>
/// <param name="DataPoints">Daily performance data points.</param>
/// <param name="CurrentMetrics">Current portfolio metrics.</param>
public record PortfolioPerformanceHistory(
    int PortfolioId,
    DateTime StartDate,
    DateTime EndDate,
    List<PortfolioPerformancePoint> DataPoints,
    PortfolioMetrics CurrentMetrics
);
