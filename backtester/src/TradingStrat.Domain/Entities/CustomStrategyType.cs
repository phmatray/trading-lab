namespace TradingStrat.Domain.Entities;

/// <summary>
/// Discriminator enum for custom strategy implementation types.
/// Determines whether a custom strategy uses rule-based configuration or Python code.
/// </summary>
public enum CustomStrategyType
{
    /// <summary>
    /// Rule-based strategy using visual editor with entry/exit rules.
    /// Strategy logic is defined via StrategyDefinition JSON.
    /// </summary>
    RuleBased,

    /// <summary>
    /// Python code-based strategy using Monaco editor.
    /// Strategy logic is defined via Python script with generate_signal() function.
    /// </summary>
    Python
}
