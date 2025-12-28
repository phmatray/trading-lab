using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Immutable snapshot of a single position at a point in time.
/// </summary>
public sealed class PositionSnapshot : ValueObject
{
    /// <summary>The ticker symbol.</summary>
    public string Ticker { get; init; }

    /// <summary>Number of shares held.</summary>
    public int Quantity { get; init; }

    /// <summary>Average entry price per share.</summary>
    public decimal EntryPrice { get; init; }

    /// <summary>Current market price per share.</summary>
    public decimal CurrentPrice { get; init; }

    /// <summary>Total market value (Quantity * CurrentPrice).</summary>
    public decimal MarketValue { get; init; }

    /// <summary>Total cost basis (Quantity * EntryPrice).</summary>
    public decimal CostBasis { get; init; }

    /// <summary>Unrealized gain or loss (MarketValue - CostBasis).</summary>
    public decimal UnrealizedGainLoss { get; init; }

    /// <summary>Unrealized return percentage.</summary>
    public decimal UnrealizedGainLossPercentage { get; init; }

    /// <summary>Percentage of total portfolio value.</summary>
    public decimal AllocationPercentage { get; init; }

    public PositionSnapshot(
        string Ticker,
        int Quantity,
        decimal EntryPrice,
        decimal CurrentPrice,
        decimal MarketValue,
        decimal CostBasis,
        decimal UnrealizedGainLoss,
        decimal UnrealizedGainLossPercentage,
        decimal AllocationPercentage)
    {
        this.Ticker = Ticker;
        this.Quantity = Quantity;
        this.EntryPrice = EntryPrice;
        this.CurrentPrice = CurrentPrice;
        this.MarketValue = MarketValue;
        this.CostBasis = CostBasis;
        this.UnrealizedGainLoss = UnrealizedGainLoss;
        this.UnrealizedGainLossPercentage = UnrealizedGainLossPercentage;
        this.AllocationPercentage = AllocationPercentage;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Ticker;
        yield return Quantity;
        yield return EntryPrice;
        yield return CurrentPrice;
        yield return MarketValue;
        yield return CostBasis;
        yield return UnrealizedGainLoss;
        yield return UnrealizedGainLossPercentage;
        yield return AllocationPercentage;
    }
}

/// <summary>
/// Immutable snapshot of a portfolio at a specific point in time.
/// </summary>
public sealed class PortfolioSnapshot : ValueObject
{
    /// <summary>The portfolio identifier.</summary>
    public int PortfolioId { get; init; }

    /// <summary>The portfolio name.</summary>
    public string PortfolioName { get; init; }

    /// <summary>The date and time of the snapshot.</summary>
    public DateTime SnapshotDate { get; init; }

    /// <summary>Cash balance in the portfolio.</summary>
    public decimal Cash { get; init; }

    /// <summary>List of position snapshots.</summary>
    public List<PositionSnapshot> Positions { get; init; }

    /// <summary>Total portfolio value (Cash + all positions market value).</summary>
    public decimal TotalValue { get; init; }

    /// <summary>Total cost basis (Cash + all positions cost basis).</summary>
    public decimal TotalCost { get; init; }

    /// <summary>Total unrealized gain or loss.</summary>
    public decimal UnrealizedGainLoss { get; init; }

    /// <summary>Total unrealized return percentage.</summary>
    public decimal UnrealizedGainLossPercentage { get; init; }

    public PortfolioSnapshot(
        int PortfolioId,
        string PortfolioName,
        DateTime SnapshotDate,
        decimal Cash,
        List<PositionSnapshot> Positions,
        decimal TotalValue,
        decimal TotalCost,
        decimal UnrealizedGainLoss,
        decimal UnrealizedGainLossPercentage)
    {
        this.PortfolioId = PortfolioId;
        this.PortfolioName = PortfolioName;
        this.SnapshotDate = SnapshotDate;
        this.Cash = Cash;
        this.Positions = Positions;
        this.TotalValue = TotalValue;
        this.TotalCost = TotalCost;
        this.UnrealizedGainLoss = UnrealizedGainLoss;
        this.UnrealizedGainLossPercentage = UnrealizedGainLossPercentage;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return PortfolioId;
        yield return PortfolioName;
        yield return SnapshotDate;
        yield return Cash;
        foreach (PositionSnapshot position in Positions)
        {
            yield return position;
        }
        yield return TotalValue;
        yield return TotalCost;
        yield return UnrealizedGainLoss;
        yield return UnrealizedGainLossPercentage;
    }
}
