using TradingStrat.Application.Commands;
using TradingStrat.Domain.Common;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Query use case for retrieving custom trading strategies.
/// Read-only operations following CQRS-lite pattern.
/// Separated from command operations for better separation of concerns.
/// </summary>
public interface ICustomStrategyQueryUseCase
{
    /// <summary>
    /// Retrieves a custom strategy by its unique identifier.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy to retrieve.</param>
    /// <returns>Result containing the strategy details or error.</returns>
    Task<Result<CustomStrategyResult>> GetStrategyByIdAsync(int strategyId);

    /// <summary>
    /// Retrieves all custom strategies, optionally filtered by category.
    /// </summary>
    /// <param name="category">Optional category filter (e.g., "Momentum", "Mean Reversion").</param>
    /// <returns>Result containing list of strategies or error.</returns>
    Task<Result<List<CustomStrategyResult>>> GetAllStrategiesAsync(string? category = null);
}
