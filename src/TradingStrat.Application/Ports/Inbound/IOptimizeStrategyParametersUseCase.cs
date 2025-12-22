using TradingStrat.Application.Commands;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for optimizing custom strategy parameters using grid search or genetic algorithms.
/// Orchestrates the parameter optimization process by running multiple backtests
/// with different parameter combinations to find optimal values.
/// </summary>
public interface IOptimizeStrategyParametersUseCase
{
    /// <summary>
    /// Optimizes parameters for a custom strategy using the specified algorithm.
    /// </summary>
    /// <param name="command">Command containing strategy, parameter ranges, and optimization settings.</param>
    /// <param name="progress">Optional progress reporter for UI updates.</param>
    /// <returns>Optimization result with best parameters and all iteration data.</returns>
    Task<OptimizationResult> ExecuteAsync(
        OptimizeParametersCommand command,
        IProgress<Domain.ValueObjects.OptimizationProgress>? progress = null);
}
