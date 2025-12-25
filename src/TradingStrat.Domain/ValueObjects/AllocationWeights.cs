using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Target allocation weights for portfolio rebalancing.
/// </summary>
public record AllocationWeights
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

    /// <summary>
    /// Custom equality implementation that compares dictionary contents (value equality).
    /// </summary>
    public virtual bool Equals(AllocationWeights? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (CashPercentage != other.CashPercentage)
        {
            return false;
        }

        if (TargetPercentages.Count != other.TargetPercentages.Count)
        {
            return false;
        }

        foreach (var kvp in TargetPercentages)
        {
            if (!other.TargetPercentages.TryGetValue(kvp.Key, out decimal otherValue))
            {
                return false;
            }

            if (kvp.Value != otherValue)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Custom hash code implementation that includes dictionary contents.
    /// </summary>
    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(CashPercentage);

        // Sort keys for consistent hash code
        foreach (string key in TargetPercentages.Keys.OrderBy(k => k))
        {
            hash.Add(key);
            hash.Add(TargetPercentages[key]);
        }

        return hash.ToHashCode();
    }
}
