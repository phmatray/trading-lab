using TradingStrat.Domain.Strategies;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Represents a variant of a strategy with specific parameters for A/B testing.
/// Immutable value object that encapsulates strategy configuration.
/// </summary>
public record StrategyVariant(
    string Label,
    StrategyType StrategyType,
    Dictionary<string, object> Parameters,
    string Description)
{
    /// <summary>
    /// Creates a default variant with empty parameters.
    /// </summary>
    public StrategyVariant() : this("Default", StrategyType.MovingAverageCrossover, new Dictionary<string, object>(), "Default configuration")
    {
    }

    /// <summary>
    /// Creates a user-friendly display name combining label and description.
    /// Example: "Variant A: MA Crossover (Fast: 10, Slow: 30)"
    /// </summary>
    public string DisplayName => $"{Label}: {Description}";
}
