using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Outbound port for persisting and retrieving custom trading strategies.
/// Implemented by infrastructure layer (e.g., EF Core repository).
/// </summary>
public interface ICustomStrategyPort
{
    /// <summary>
    /// Creates a new custom strategy in the data store.
    /// </summary>
    /// <param name="strategy">The strategy to create.</param>
    /// <returns>The created strategy with generated ID.</returns>
    Task<CustomStrategy> CreateAsync(CustomStrategy strategy);

    /// <summary>
    /// Updates an existing custom strategy.
    /// </summary>
    /// <param name="strategy">The strategy to update.</param>
    /// <returns>The updated strategy.</returns>
    Task<CustomStrategy> UpdateAsync(CustomStrategy strategy);

    /// <summary>
    /// Deletes a custom strategy by ID.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy to delete.</param>
    Task DeleteAsync(int strategyId);

    /// <summary>
    /// Retrieves a custom strategy by ID.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy to retrieve.</param>
    /// <returns>The strategy if found, null otherwise.</returns>
    Task<CustomStrategy?> GetByIdAsync(int strategyId);

    /// <summary>
    /// Retrieves all custom strategies, optionally filtered by category.
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    /// <returns>List of strategies matching the criteria.</returns>
    Task<List<CustomStrategy>> GetAllAsync(string? category = null);

    /// <summary>
    /// Increments the usage count for a strategy.
    /// Called each time the strategy is used in a backtest or live trading.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy.</param>
    Task IncrementUsageCountAsync(int strategyId);

    /// <summary>
    /// Updates the backtest statistics for a strategy.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy.</param>
    /// <param name="returnPercentage">The total return percentage from the backtest.</param>
    /// <param name="backtestDate">The date the backtest was run.</param>
    Task UpdateBacktestStatsAsync(int strategyId, decimal returnPercentage, DateTime backtestDate);
}
