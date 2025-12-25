using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Specifications;

/// <summary>
/// Specification that validates allocation weights sum to 100% and are non-negative.
/// </summary>
public class ValidAllocationWeightsSpecification : ISpecification<AllocationWeights>
{
    private const decimal Tolerance = 0.01m;

    /// <summary>
    /// Gets the reason why the specification was not satisfied.
    /// </summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>
    /// Checks whether the allocation weights are valid.
    /// </summary>
    /// <param name="candidate">The allocation weights to validate.</param>
    /// <returns>True if weights sum to 100% (within tolerance) and all are non-negative.</returns>
    public bool IsSatisfiedBy(AllocationWeights candidate)
    {
        Reason = string.Empty;

        if (candidate == null)
        {
            Reason = "Allocation weights cannot be null";
            return false;
        }

        // Check for negative percentages
        if (candidate.CashPercentage < 0)
        {
            Reason = "Cash percentage cannot be negative";
            return false;
        }

        foreach (decimal percentage in candidate.TargetPercentages.Values)
        {
            if (percentage < 0)
            {
                Reason = "Target percentages cannot be negative";
                return false;
            }
        }

        // Calculate total
        decimal total = candidate.TargetPercentages.Values.Sum() + candidate.CashPercentage;

        // Check if total is 100% within tolerance
        if (Math.Abs(total - 100m) > Tolerance)
        {
            Reason = $"Allocation weights must sum to 100%, got {total}%";
            return false;
        }

        return true;
    }
}
