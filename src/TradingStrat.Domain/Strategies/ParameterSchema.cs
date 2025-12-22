namespace TradingStrat.Domain.Strategies;

/// <summary>
/// Describes a single strategy parameter with metadata for validation and UI rendering.
/// Immutable value object following domain-driven design principles.
/// </summary>
public sealed record ParameterSchema
{
    /// <summary>
    /// Parameter name (matches strategy constructor parameter name)
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Parameter type (e.g., typeof(int), typeof(decimal))
    /// </summary>
    public required Type ParameterType { get; init; }

    /// <summary>
    /// Default value for the parameter
    /// </summary>
    public required object DefaultValue { get; init; }

    /// <summary>
    /// Display name for UI labels (optional, defaults to Name if not provided)
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Description for tooltips and help text
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Minimum allowed value for numeric parameters
    /// </summary>
    public object? MinValue { get; init; }

    /// <summary>
    /// Maximum allowed value for numeric parameters
    /// </summary>
    public object? MaxValue { get; init; }

    /// <summary>
    /// Step increment for UI sliders/spinners (for decimal/int parameters)
    /// </summary>
    public decimal? Step { get; init; }
}
