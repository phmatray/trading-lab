using TradingStrat.Application.Commands;
using TradingStrat.Domain.Common;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Command use case for managing custom trading strategies.
/// Write operations (Create, Update, Delete, Clone) following CQRS-lite pattern.
/// Separated from query operations for better separation of concerns.
/// </summary>
public interface ICustomStrategyCommandUseCase
{
    /// <summary>
    /// Creates a new custom strategy with validation.
    /// </summary>
    /// <param name="command">Command containing strategy details and definition.</param>
    /// <returns>Result containing created strategy or error.</returns>
    Task<Result<CustomStrategyResult>> CreateStrategyAsync(CreateCustomStrategyCommand command);

    /// <summary>
    /// Updates an existing custom strategy with validation.
    /// </summary>
    /// <param name="command">Command containing updated strategy details.</param>
    /// <returns>Result containing updated strategy or error.</returns>
    Task<Result<CustomStrategyResult>> UpdateStrategyAsync(UpdateCustomStrategyCommand command);

    /// <summary>
    /// Deletes a custom strategy by its unique identifier.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy to delete.</param>
    /// <returns>Result indicating success or error.</returns>
    Task<Result<bool>> DeleteStrategyAsync(int strategyId);

    /// <summary>
    /// Clones an existing strategy with a new name.
    /// Creates a copy of the strategy definition with updated metadata.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy to clone.</param>
    /// <param name="newName">The name for the cloned strategy.</param>
    /// <returns>Result containing cloned strategy or error.</returns>
    Task<Result<CustomStrategyResult>> CloneStrategyAsync(int strategyId, string newName);
}
