using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Target allocation weights for portfolio rebalancing.
/// </summary>
public class AllocationWeights : ValueObject
{
    /// <summary>
    /// Dictionary of ticker symbol to target allocation percentage (all non-negative).
    /// </summary>
    public Dictionary<string, decimal> TargetPercentages { get; init; }

    /// <summary>
    /// Target cash percentage (non-negative).
    /// </summary>
    public decimal CashPercentage { get; init; }

    public AllocationWeights(Dictionary<string, decimal> TargetPercentages, decimal CashPercentage)
    {
        // Validate dictionary is not null
        ValidationGuard.Require(TargetPercentages).NotNull();

        // Validate cash percentage is non-negative
        ValidationGuard.Require(CashPercentage).GreaterThanOrEqual(0m);

        // Validate all target percentages are non-negative
        foreach (KeyValuePair<string, decimal> kvp in TargetPercentages)
        {
            if (kvp.Value < 0)
            {
                throw new ArgumentException($"Target percentage for {kvp.Key} cannot be negative: {kvp.Value}%", nameof(TargetPercentages));
            }
        }

        this.TargetPercentages = TargetPercentages;
        this.CashPercentage = CashPercentage;
    }

    /// <summary>
    /// Validates that all allocations sum to 100%.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid()
    {
        decimal total = TargetPercentages.Values.Sum() + CashPercentage;
        return Math.Abs(total - 100m) < 0.01m; // Allow small rounding errors
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CashPercentage;

        // Sort keys for consistent equality comparison
        foreach (string key in TargetPercentages.Keys.OrderBy(k => k))
        {
            yield return key;
            yield return TargetPercentages[key];
        }
    }
}
