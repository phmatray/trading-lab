using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Action to take when rebalancing a position.
/// </summary>
public enum RebalancingAction
{
    /// <summary>
    /// Buy additional shares.
    /// </summary>
    Buy,

    /// <summary>
    /// Sell shares.
    /// </summary>
    Sell,

    /// <summary>
    /// Hold current position (no change needed).
    /// </summary>
    Hold
}

/// <summary>
/// Signal indicating the action needed to rebalance a single position.
/// </summary>
public sealed class RebalancingSignal : ValueObject
{
    /// <summary>The ticker symbol.</summary>
    public string Ticker { get; init; }

    /// <summary>The action to take (Buy, Sell, or Hold).</summary>
    public RebalancingAction Action { get; init; }

    /// <summary>Current number of shares held.</summary>
    public int CurrentQuantity { get; init; }

    /// <summary>Target number of shares.</summary>
    public int TargetQuantity { get; init; }

    /// <summary>Difference between target and current (positive for buy, negative for sell).</summary>
    public int QuantityDelta { get; init; }

    /// <summary>Current allocation percentage.</summary>
    public decimal CurrentAllocation { get; init; }

    /// <summary>Target allocation percentage.</summary>
    public decimal TargetAllocation { get; init; }

    /// <summary>Estimated cost including commission (positive for buy, negative for sell).</summary>
    public decimal EstimatedCost { get; init; }

    public RebalancingSignal(
        string Ticker,
        RebalancingAction Action,
        int CurrentQuantity,
        int TargetQuantity,
        int QuantityDelta,
        decimal CurrentAllocation,
        decimal TargetAllocation,
        decimal EstimatedCost)
    {
        this.Ticker = Ticker;
        this.Action = Action;
        this.CurrentQuantity = CurrentQuantity;
        this.TargetQuantity = TargetQuantity;
        this.QuantityDelta = QuantityDelta;
        this.CurrentAllocation = CurrentAllocation;
        this.TargetAllocation = TargetAllocation;
        this.EstimatedCost = EstimatedCost;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Ticker;
        yield return Action;
        yield return CurrentQuantity;
        yield return TargetQuantity;
        yield return QuantityDelta;
        yield return CurrentAllocation;
        yield return TargetAllocation;
        yield return EstimatedCost;
    }
}

/// <summary>
/// Complete rebalancing plan for a portfolio.
/// </summary>
public sealed class RebalancingPlan : ValueObject
{
    /// <summary>The portfolio identifier.</summary>
    public int PortfolioId { get; init; }

    /// <summary>Date and time when the plan was calculated.</summary>
    public DateTime CalculationDate { get; init; }

    /// <summary>List of rebalancing signals for each position.</summary>
    public List<RebalancingSignal> Signals { get; init; }

    /// <summary>Total cash needed to execute all buy orders.</summary>
    public decimal RequiredCash { get; init; }

    /// <summary>Cash currently available in the portfolio.</summary>
    public decimal AvailableCash { get; init; }

    /// <summary>True if the plan can be executed with available cash.</summary>
    public bool IsExecutable { get; init; }

    public RebalancingPlan(
        int PortfolioId,
        DateTime CalculationDate,
        List<RebalancingSignal> Signals,
        decimal RequiredCash,
        decimal AvailableCash,
        bool IsExecutable)
    {
        this.PortfolioId = PortfolioId;
        this.CalculationDate = CalculationDate;
        this.Signals = Signals;
        this.RequiredCash = RequiredCash;
        this.AvailableCash = AvailableCash;
        this.IsExecutable = IsExecutable;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return PortfolioId;
        yield return CalculationDate;
        foreach (RebalancingSignal signal in Signals)
        {
            yield return signal;
        }
        yield return RequiredCash;
        yield return AvailableCash;
        yield return IsExecutable;
    }
}
