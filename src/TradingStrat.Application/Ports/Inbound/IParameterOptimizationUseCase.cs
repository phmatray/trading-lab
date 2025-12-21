using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for running A/B parameter optimization tests between two strategy variants.
/// Orchestrates dual backtests and compares results using ranking criteria.
/// </summary>
public interface IParameterOptimizationUseCase
{
    /// <summary>
    /// Executes A/B testing by running two backtests and comparing results.
    /// </summary>
    /// <param name="command">Command containing two strategy variants to compare.</param>
    /// <param name="progress">Optional progress reporter for UI updates.</param>
    /// <returns>Complete comparison results with ranking and metric breakdown.</returns>
    Task<ParameterOptimizationResult> ExecuteAsync(
        ParameterOptimizationCommand command,
        IProgress<OptimizationProgress>? progress = null);
}

/// <summary>
/// Command object for A/B parameter optimization.
/// </summary>
public record ParameterOptimizationCommand(
    string Ticker,
    StrategyVariant VariantA,
    StrategyVariant VariantB,
    decimal InitialCapital = 10000m,
    decimal CommissionPercentage = 0.001m,
    decimal MinimumCommission = 1.0m,
    DateTime? StartDate = null,
    DateTime? EndDate = null);

/// <summary>
/// Result object containing comparison between two strategy variants.
/// </summary>
public record ParameterOptimizationResult(
    StrategyComparison Comparison,
    TimeSpan ExecutionTime);

/// <summary>
/// Progress update for optimization execution.
/// </summary>
public record OptimizationProgress(
    string CurrentVariant,  // "Variant A" or "Variant B"
    int CurrentBar,
    int TotalBars,
    int Trades);
