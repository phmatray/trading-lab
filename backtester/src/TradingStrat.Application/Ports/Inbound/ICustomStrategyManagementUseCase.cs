using TradingStrat.Application.Commands;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;
using AppPythonValidationResult = TradingStrat.Application.Commands.PythonValidationResult;

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
    /// <returns>Result containing the created strategy with generated ID, or errors if validation fails.</returns>
    Task<Result<CustomStrategyResult>> CreateStrategyAsync(CreateCustomStrategyCommand command);

    /// <summary>
    /// Updates an existing custom strategy.
    /// </summary>
    /// <param name="command">Command containing updated strategy data.</param>
    /// <returns>Result containing the updated strategy, or errors if validation fails or strategy not found.</returns>
    Task<Result<CustomStrategyResult>> UpdateStrategyAsync(UpdateCustomStrategyCommand command);

    /// <summary>
    /// Deletes a custom strategy by ID.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy to delete.</param>
    /// <returns>Result indicating success or failure of the deletion.</returns>
    Task<Result<bool>> DeleteStrategyAsync(int strategyId);

    /// <summary>
    /// Retrieves a custom strategy by ID.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy to retrieve.</param>
    /// <returns>Result containing the strategy if found, or error if not found.</returns>
    Task<Result<CustomStrategyResult>> GetStrategyByIdAsync(int strategyId);

    /// <summary>
    /// Retrieves all custom strategies, optionally filtered by category.
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    /// <returns>Result containing list of strategies matching the criteria.</returns>
    Task<Result<List<CustomStrategyResult>>> GetAllStrategiesAsync(string? category = null);

    /// <summary>
    /// Clones an existing strategy with a new name.
    /// </summary>
    /// <param name="strategyId">The ID of the strategy to clone.</param>
    /// <param name="newName">The name for the cloned strategy.</param>
    /// <returns>Result containing the newly created clone, or error if source strategy not found.</returns>
    Task<Result<CustomStrategyResult>> CloneStrategyAsync(int strategyId, string newName);

    /// <summary>
    /// Validates a strategy definition without persisting it.
    /// Useful for real-time validation in the UI.
    /// </summary>
    /// <param name="definition">The strategy definition to validate.</param>
    /// <returns>Result containing validation result with any errors found.</returns>
    Task<Result<ValidationResult>> ValidateStrategyDefinitionAsync(StrategyDefinition definition);

    /// <summary>
    /// Validates Python code syntax and required functions without persisting it.
    /// Checks for required generate_signal() function and optional initialize() function.
    /// </summary>
    /// <param name="command">Command containing Python code to validate.</param>
    /// <returns>Result containing validation result with syntax errors if any.</returns>
    Task<Result<AppPythonValidationResult>> ValidatePythonCodeAsync(ValidatePythonCodeCommand command);

    /// <summary>
    /// Dry-runs a Python strategy on historical data without persisting it.
    /// Validates syntax, fetches historical data, and executes a backtest.
    /// Useful for testing Python code before saving the strategy.
    /// </summary>
    /// <param name="command">Command containing Python code, ticker, and backtest parameters.</param>
    /// <returns>Result containing validation errors and/or backtest results.</returns>
    Task<Result<DryRunResult>> DryRunPythonStrategyAsync(DryRunPythonStrategyCommand command);
}
