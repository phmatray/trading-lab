namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing the complete definition of a custom trading strategy.
/// Contains entry rules, exit rules, and position sizing configuration.
/// Serialized to JSON for persistence in the database.
/// </summary>
public sealed record StrategyDefinition(
    List<StrategyRule> EntryRules,
    List<StrategyRule> ExitRules,
    PositionSizingMode SizingMode,
    Dictionary<string, decimal> SizingParameters
);

/// <summary>
/// Represents a single condition-based rule in a custom strategy.
/// Rules compare indicators to constants, price, or other indicators.
/// Multiple rules can be combined with AND/OR logic.
/// </summary>
public sealed record StrategyRule(
    string IndicatorName,
    Dictionary<string, object> IndicatorParameters,
    ComparisonOperator Operator,
    RuleValueType ValueType,
    decimal? ConstantValue,
    string? SecondIndicatorName,
    Dictionary<string, object>? SecondIndicatorParameters,
    LogicalOperator LogicalOperator
);

/// <summary>
/// Defines how to compare an indicator value with a threshold.
/// Supports standard comparisons and crossover detection.
/// </summary>
public enum ComparisonOperator
{
    /// <summary>Greater than (&gt;)</summary>
    GreaterThan,

    /// <summary>Greater than or equal (&gt;=)</summary>
    GreaterThanOrEqual,

    /// <summary>Less than (&lt;)</summary>
    LessThan,

    /// <summary>Less than or equal (&lt;=)</summary>
    LessThanOrEqual,

    /// <summary>Equal (=)</summary>
    Equal,

    /// <summary>
    /// Detects bullish crossover: indicator[i] &gt; value AND indicator[i-1] &lt;= value
    /// </summary>
    CrossesAbove,

    /// <summary>
    /// Detects bearish crossover: indicator[i] &lt; value AND indicator[i-1] &gt;= value
    /// </summary>
    CrossesBelow
}

/// <summary>
/// Defines how to combine multiple rules in a strategy.
/// </summary>
public enum LogicalOperator
{
    /// <summary>No combination (last rule in list)</summary>
    None,

    /// <summary>Both this rule AND the next rule must be true</summary>
    And,

    /// <summary>Either this rule OR the next rule must be true</summary>
    Or
}

/// <summary>
/// Defines what type of value to compare the indicator against.
/// </summary>
public enum RuleValueType
{
    /// <summary>Compare to a fixed numerical constant</summary>
    Constant,

    /// <summary>Compare to another indicator's value</summary>
    Indicator,

    /// <summary>Compare to the current price (close)</summary>
    Price
}

/// <summary>
/// Defines the strategy for determining position size when entering a trade.
/// </summary>
public enum PositionSizingMode
{
    /// <summary>
    /// Use a fixed percentage of available cash.
    /// Requires 'Percentage' parameter (0.0-1.0).
    /// </summary>
    FixedPercentage,

    /// <summary>
    /// Buy a fixed number of shares.
    /// Requires 'Quantity' parameter.
    /// </summary>
    FixedQuantity,

    /// <summary>
    /// Size position based on risk (ATR-based stop distance).
    /// Requires 'RiskPercentage' parameter (0.0-1.0).
    /// </summary>
    RiskBased
}
