using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Delegate for evaluating a parameter set and returning metrics.
/// This allows the optimizer to remain pure (no dependencies on backtesting infrastructure).
/// </summary>
public delegate Task<(decimal totalReturn, decimal sharpeRatio, decimal maxDrawdown, int tradeCount)>
    ParameterEvaluator(Dictionary<string, decimal> parameters);

/// <summary>
/// Domain service for optimizing strategy parameters.
/// Provides grid search and genetic algorithm optimization with zero external dependencies.
/// Uses a delegate pattern to remain independent of backtesting infrastructure.
/// </summary>
public interface IParameterOptimizer
{
    /// <summary>
    /// Performs grid search optimization over parameter ranges.
    /// Exhaustively tests all parameter combinations.
    /// </summary>
    /// <param name="parameterRanges">Parameter ranges to search.</param>
    /// <param name="objective">Optimization objective.</param>
    /// <param name="evaluator">Function that evaluates a parameter set and returns metrics.</param>
    /// <param name="progress">Optional progress reporting.</param>
    /// <returns>Optimization result with best parameters.</returns>
    Task<OptimizationResult> OptimizeGridSearchAsync(
        Dictionary<string, ParameterRange> parameterRanges,
        OptimizationObjective objective,
        ParameterEvaluator evaluator,
        IProgress<OptimizationProgress>? progress = null);

    /// <summary>
    /// Performs genetic algorithm optimization.
    /// Uses evolutionary approach to find optimal parameters.
    /// </summary>
    /// <param name="parameterRanges">Parameter ranges to search.</param>
    /// <param name="objective">Optimization objective.</param>
    /// <param name="evaluator">Function that evaluates a parameter set and returns metrics.</param>
    /// <param name="geneticConfig">Genetic algorithm configuration.</param>
    /// <param name="progress">Optional progress reporting.</param>
    /// <returns>Optimization result with best parameters.</returns>
    Task<OptimizationResult> OptimizeGeneticAsync(
        Dictionary<string, ParameterRange> parameterRanges,
        OptimizationObjective objective,
        ParameterEvaluator evaluator,
        GeneticAlgorithmConfig geneticConfig,
        IProgress<OptimizationProgress>? progress = null);

    /// <summary>
    /// Calculates the score for an iteration based on the objective.
    /// </summary>
    decimal CalculateScore(OptimizationIteration iteration, OptimizationObjective objective);
}
