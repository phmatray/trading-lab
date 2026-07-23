namespace TradingStrat.Domain.Entities;

/// <summary>
/// Aggregate root representing a user-defined custom trading strategy.
/// Stores the strategy definition as serialized JSON and tracks usage statistics.
/// </summary>
public class CustomStrategy
{
    /// <summary>
    /// Unique identifier for the custom strategy.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User-defined name for the strategy.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description explaining the strategy's logic and intended use.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Name of the user who created this strategy.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Category for organizing strategies (e.g., "Momentum", "Mean Reversion", "Trend").
    /// </summary>
    public string Category { get; set; } = "Custom";

    /// <summary>
    /// Strategy implementation type (Rule-Based or Python).
    /// Determines which execution engine to use for generating trading signals.
    /// </summary>
    public CustomStrategyType StrategyType { get; set; } = CustomStrategyType.RuleBased;

    /// <summary>
    /// Timestamp when the strategy was first created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the strategy was last modified.
    /// Updated automatically on any change.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// JSON-serialized StrategyDefinition containing the complete strategy logic.
    /// Includes entry rules, exit rules, and position sizing configuration.
    /// Used for RuleBased strategy type.
    /// </summary>
    public string DefinitionJson { get; set; } = string.Empty;

    /// <summary>
    /// Python source code implementing the trading strategy.
    /// Must define generate_signal(index, price, cash, position) function.
    /// Optionally can define initialize(prices) for pre-calculation.
    /// Used for Python strategy type.
    /// </summary>
    public string? PythonCode { get; set; }

    /// <summary>
    /// Version number for Python code API compatibility.
    /// Allows for future migrations if the Python API changes.
    /// Null for RuleBased strategies.
    /// </summary>
    public int? PythonCodeVersion { get; set; }

    /// <summary>
    /// Number of times this strategy has been used in backtests or live trading.
    /// Incremented each time the strategy is executed.
    /// </summary>
    public int TimesUsed { get; set; } = 0;

    /// <summary>
    /// Total return percentage from the most recent backtest.
    /// Null if the strategy has never been backtested.
    /// </summary>
    public decimal? LastBacktestReturn { get; set; }

    /// <summary>
    /// Timestamp of the most recent backtest execution.
    /// Null if the strategy has never been backtested.
    /// </summary>
    public DateTime? LastBacktestDate { get; set; }
}
