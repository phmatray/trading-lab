using TradingStrat.Application.Commands;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Inbound port for managing custom trading strategies.
/// Provides CRUD operations and validation for user-defined strategies.
/// </summary>
public interface ICustomStrategyManagementUseCase
{
    /// <summary>
    /// Creates a new custom strategy after validating the definition.
    /// </summary>
    /// <param name="command">Command containing strategy metadata and definition.</param>
    /// <returns>The created strategy with generated ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the strategy definition is invalid.</exception>
    Task<CustomStrategyResult> CreateStrategyAsync(CreateCustomStrategyCommand command);

    /// <summary>
    /// Updates an existing custom strategy.
    /// </summary>
    /// <param name="command">Command containing updated strategy data.</param>
    /// <returns>The updated strategy.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the strategy definition is invalid.</exception>
    Task<CustomStrategyResult> UpdateStrategyAsync(UpdateCustomStrategyCommand command);

    /// <summary>
    /// Deletes a custom strategy by ID.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy to delete.</param>
    Task DeleteStrategyAsync(int strategyId);

    /// <summary>
    /// Retrieves a custom strategy by ID.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy to retrieve.</param>
    /// <returns>The strategy if found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the strategy is not found.</exception>
    Task<CustomStrategyResult> GetStrategyByIdAsync(int strategyId);

    /// <summary>
    /// Retrieves all custom strategies, optionally filtered by category.
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    /// <returns>List of strategies matching the criteria.</returns>
    Task<List<CustomStrategyResult>> GetAllStrategiesAsync(string? category = null);

    /// <summary>
    /// Clones an existing strategy with a new name.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy to clone.</param>
    /// <param name="newName">The name for the cloned strategy.</param>
    /// <returns>The newly created clone.</returns>
    Task<CustomStrategyResult> CloneStrategyAsync(int strategyId, string newName);

    /// <summary>
    /// Validates a strategy definition without persisting it.
    /// Useful for real-time validation in the UI.
    /// </summary>
    /// <param name="definition">The strategy definition to validate.</param>
    /// <returns>Validation result with any errors found.</returns>
    Task<ValidationResult> ValidateStrategyDefinitionAsync(StrategyDefinition definition);
}
