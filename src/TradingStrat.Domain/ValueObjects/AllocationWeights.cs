namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Target allocation weights for portfolio rebalancing.
/// </summary>
/// <param name="TargetPercentages">Dictionary of ticker symbol to target allocation percentage.</param>
/// <param name="CashPercentage">Target cash percentage.</param>
public record AllocationWeights(
    Dictionary<string, decimal> TargetPercentages,
    decimal CashPercentage
)
{
    /// <summary>
    /// Validates that all allocations sum to 100%.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid()
    {
        decimal total = TargetPercentages.Values.Sum() + CashPercentage;
        return Math.Abs(total - 100m) < 0.01m; // Allow small rounding errors
    }
}
