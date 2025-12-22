using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Commands;

/// <summary>
/// Command to optimize parameters for a custom strategy.
/// </summary>
public record OptimizeParametersCommand(
    int CustomStrategyId,
    OptimizationType Type,
    Dictionary<string, ParameterRange> ParameterRanges,
    OptimizationObjective Objective,
    BacktestConfig BacktestSettings,
    GeneticAlgorithmSettings? GeneticSettings = null
);

/// <summary>
/// Type of optimization algorithm to use.
/// </summary>
public enum OptimizationType
{
    /// <summary>Exhaustive grid search over all parameter combinations.</summary>
    GridSearch,

    /// <summary>Evolutionary genetic algorithm optimization.</summary>
    Genetic
}

/// <summary>
/// Configuration for genetic algorithm optimization (application layer wrapper).
/// </summary>
public record GeneticAlgorithmSettings(
    int PopulationSize = 50,
    int Generations = 100,
    decimal MutationRate = 0.1m,
    int EliteCount = 5,
    decimal CrossoverRate = 0.8m
)
{
    /// <summary>
    /// Converts to domain GeneticAlgorithmConfig.
    /// </summary>
    public GeneticAlgorithmConfig ToDomainConfig()
    {
        return new GeneticAlgorithmConfig(
            PopulationSize: PopulationSize,
            Generations: Generations,
            MutationRate: MutationRate,
            EliteCount: EliteCount,
            CrossoverRate: CrossoverRate
        );
    }
}
