namespace TradingStrat.Domain.Strategies;

/// <summary>
/// Describes a strategy with metadata for UI rendering, validation, and documentation.
/// Immutable value object following domain-driven design principles.
/// </summary>
public sealed record StrategyDescriptor
{
    /// <summary>
    /// Unique strategy type identifier (enum value)
    /// </summary>
    public required StrategyType Type { get; init; }

    /// <summary>
    /// Canonical string key for backward compatibility and config parsing (e.g., "ma", "rsi", "macd").
    /// Used for parsing config files, API requests, and user preferences stored as strings.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Display name for UI (e.g., "Moving Average Crossover", "RSI Strategy")
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Short description for tooltips and help text
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Aliases for flexible string parsing (e.g., ["ma", "movingaverage", "macrossover"]).
    /// Enables users to refer to strategies by multiple names.
    /// </summary>
    public required IReadOnlyList<string> Aliases { get; init; }

    /// <summary>
    /// Parameter schema describing expected parameters with types, defaults, and validation rules.
    /// Used for dynamic UI generation, validation, and documentation.
    /// </summary>
    public required IReadOnlyDictionary<string, ParameterSchema> Parameters { get; init; }

    /// <summary>
    /// Category for UI grouping (e.g., "Trend", "Momentum", "Machine Learning").
    /// Optional - can be used for filtering and organizing strategies in dropdowns.
    /// </summary>
    public string? Category { get; init; }
}
