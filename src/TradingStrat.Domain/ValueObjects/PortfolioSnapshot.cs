namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Immutable snapshot of a single position at a point in time.
/// </summary>
/// <param name="Ticker">The ticker symbol.</param>
/// <param name="Quantity">Number of shares held.</param>
/// <param name="EntryPrice">Average entry price per share.</param>
/// <param name="CurrentPrice">Current market price per share.</param>
/// <param name="MarketValue">Total market value (Quantity * CurrentPrice).</param>
/// <param name="CostBasis">Total cost basis (Quantity * EntryPrice).</param>
/// <param name="UnrealizedGainLoss">Unrealized gain or loss (MarketValue - CostBasis).</param>
/// <param name="UnrealizedGainLossPercentage">Unrealized return percentage.</param>
/// <param name="AllocationPercentage">Percentage of total portfolio value.</param>
public record PositionSnapshot(
    string Ticker,
    int Quantity,
    decimal EntryPrice,
    decimal CurrentPrice,
    decimal MarketValue,
    decimal CostBasis,
    decimal UnrealizedGainLoss,
    decimal UnrealizedGainLossPercentage,
    decimal AllocationPercentage
);

/// <summary>
/// Immutable snapshot of a portfolio at a specific point in time.
/// </summary>
/// <param name="PortfolioId">The portfolio identifier.</param>
/// <param name="PortfolioName">The portfolio name.</param>
/// <param name="SnapshotDate">The date and time of the snapshot.</param>
/// <param name="Cash">Cash balance in the portfolio.</param>
/// <param name="Positions">List of position snapshots.</param>
/// <param name="TotalValue">Total portfolio value (Cash + all positions market value).</param>
/// <param name="TotalCost">Total cost basis (Cash + all positions cost basis).</param>
/// <param name="UnrealizedGainLoss">Total unrealized gain or loss.</param>
/// <param name="UnrealizedGainLossPercentage">Total unrealized return percentage.</param>
public record PortfolioSnapshot(
    int PortfolioId,
    string PortfolioName,
    DateTime SnapshotDate,
    decimal Cash,
    List<PositionSnapshot> Positions,
    decimal TotalValue,
    decimal TotalCost,
    decimal UnrealizedGainLoss,
    decimal UnrealizedGainLossPercentage
);
