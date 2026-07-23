using TradingStrat.Domain.Common;
using TradingStrat.Domain.Strategies;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Represents a variant of a strategy with specific parameters for A/B testing.
/// Immutable value object that encapsulates strategy configuration.
/// </summary>
public sealed class StrategyVariant : ValueObject
{
    public string Label { get; init; }
    public StrategyType StrategyType { get; init; }
    public Dictionary<string, object> Parameters { get; init; }
    public string Description { get; init; }

    /// <summary>
    /// Creates a default variant with empty parameters.
    /// </summary>
    public StrategyVariant() : this("Default", StrategyType.MovingAverageCrossover, new Dictionary<string, object>(), "Default configuration")
    {
    }

    public StrategyVariant(
        string label,
        StrategyType strategyType,
        Dictionary<string, object> parameters,
        string description)
    {
        Label = label;
        StrategyType = strategyType;
        Parameters = parameters;
        Description = description;
    }

    /// <summary>
    /// Creates a user-friendly display name combining label and description.
    /// Example: "Variant A: MA Crossover (Fast: 10, Slow: 30)"
    /// </summary>
    public string DisplayName => $"{Label}: {Description}";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Label;
        yield return StrategyType;
        foreach (string key in Parameters.Keys.OrderBy(k => k))
        {
            yield return key;
            yield return Parameters[key];
        }
        yield return Description;
    }
}
