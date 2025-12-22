using TradingStrat.Domain.Strategies;

namespace TradingStrat.Application.Factories;

public interface IStrategyFactory
{
    /// <summary>
    /// Creates a strategy instance using a type-safe enum.
    /// This is the preferred method for all new code.
    /// </summary>
    /// <param name="strategyType">The strategy type enum value</param>
    /// <param name="parameters">Optional dictionary of strategy parameters</param>
    /// <returns>Configured strategy instance</returns>
    IStrategy CreateStrategy(StrategyType strategyType, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Maps a strategy display name or partial name to its canonical type code.
    /// Handles variations like "Moving Average", "moving average crossover", etc.
    /// </summary>
    /// <param name="strategyName">The strategy name (case-insensitive).</param>
    /// <returns>The canonical strategy type code (e.g., "ma", "rsi", "macd").</returns>
    string MapStrategyNameToType(string strategyName);
}
