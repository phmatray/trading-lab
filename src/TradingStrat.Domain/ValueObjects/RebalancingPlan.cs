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
/// <param name="Ticker">The ticker symbol.</param>
/// <param name="Action">The action to take (Buy, Sell, or Hold).</param>
/// <param name="CurrentQuantity">Current number of shares held.</param>
/// <param name="TargetQuantity">Target number of shares.</param>
/// <param name="QuantityDelta">Difference between target and current (positive for buy, negative for sell).</param>
/// <param name="CurrentAllocation">Current allocation percentage.</param>
/// <param name="TargetAllocation">Target allocation percentage.</param>
/// <param name="EstimatedCost">Estimated cost including commission (positive for buy, negative for sell).</param>
public record RebalancingSignal(
    string Ticker,
    RebalancingAction Action,
    int CurrentQuantity,
    int TargetQuantity,
    int QuantityDelta,
    decimal CurrentAllocation,
    decimal TargetAllocation,
    decimal EstimatedCost
);

/// <summary>
/// Complete rebalancing plan for a portfolio.
/// </summary>
/// <param name="PortfolioId">The portfolio identifier.</param>
/// <param name="CalculationDate">Date and time when the plan was calculated.</param>
/// <param name="Signals">List of rebalancing signals for each position.</param>
/// <param name="RequiredCash">Total cash needed to execute all buy orders.</param>
/// <param name="AvailableCash">Cash currently available in the portfolio.</param>
/// <param name="IsExecutable">True if the plan can be executed with available cash.</param>
public record RebalancingPlan(
    int PortfolioId,
    DateTime CalculationDate,
    List<RebalancingSignal> Signals,
    decimal RequiredCash,
    decimal AvailableCash,
    bool IsExecutable
);
