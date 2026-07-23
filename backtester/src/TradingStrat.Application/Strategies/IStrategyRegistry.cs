using TradingStrat.Domain.Strategies;

namespace TradingStrat.Application.Strategies;

/// <summary>
/// Port for accessing strategy metadata.
/// Provides type-safe access to strategy descriptors and string parsing for backward compatibility.
/// </summary>
public interface IStrategyRegistry
{
    /// <summary>
    /// Gets all registered strategy descriptors.
    /// </summary>
    /// <returns>Read-only collection of all strategy descriptors</returns>
    IReadOnlyCollection<StrategyDescriptor> GetAll();

    /// <summary>
    /// Gets the descriptor for a specific strategy type.
    /// </summary>
    /// <param name="type">The strategy type</param>
    /// <returns>Strategy descriptor with metadata</returns>
    /// <exception cref="ArgumentException">Thrown if strategy type is not registered</exception>
    StrategyDescriptor GetDescriptor(StrategyType type);

    /// <summary>
    /// Parses a strategy key string (e.g., "ma", "rsi") to a StrategyType enum.
    /// Supports primary keys and aliases for backward compatibility.
    /// Case-insensitive matching.
    /// </summary>
    /// <param name="strategyKey">String key to parse (e.g., "ma", "movingaverage", "RSI")</param>
    /// <returns>Corresponding StrategyType enum value</returns>
    /// <exception cref="ArgumentException">Thrown if key is not recognized</exception>
    StrategyType ParseStrategyType(string strategyKey);

    /// <summary>
    /// Tries to parse a strategy key string to a StrategyType enum.
    /// Returns false if key is not recognized.
    /// </summary>
    /// <param name="strategyKey">String key to parse</param>
    /// <param name="type">Output parameter for parsed strategy type</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    bool TryParseStrategyType(string strategyKey, out StrategyType type);
}
