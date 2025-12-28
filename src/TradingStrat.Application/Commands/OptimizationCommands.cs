using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Commands;

/// <summary>
/// Command to optimize parameters for a custom strategy.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record OptimizeParametersCommand
{
    public int CustomStrategyId { get; init; }
    public OptimizationType Type { get; init; }
    public Dictionary<string, ParameterRange> ParameterRanges { get; init; }
    public OptimizationObjective Objective { get; init; }
    public BacktestConfig BacktestSettings { get; init; }
    public GeneticAlgorithmSettings? GeneticSettings { get; init; }

    public OptimizeParametersCommand(
        int CustomStrategyId,
        OptimizationType Type,
        Dictionary<string, ParameterRange> ParameterRanges,
        OptimizationObjective Objective,
        BacktestConfig BacktestSettings,
        GeneticAlgorithmSettings? GeneticSettings = null)
    {
        // Validate parameters
        ValidationGuard.Require(CustomStrategyId).GreaterThan(0, "Custom strategy ID must be positive");
        ValidationGuard.Require(ParameterRanges).NotNull();
        ValidationGuard.Require(ParameterRanges.Count > 0, "Parameter ranges cannot be empty", nameof(ParameterRanges));
        ValidationGuard.Require(BacktestSettings).NotNull();

        // Validate genetic settings if optimization type is Genetic
        if (Type == OptimizationType.Genetic)
        {
            ValidationGuard.Require(GeneticSettings).NotNull();
        }

        // Assign validated values
        this.CustomStrategyId = CustomStrategyId;
        this.Type = Type;
        this.ParameterRanges = ParameterRanges;
        this.Objective = Objective;
        this.BacktestSettings = BacktestSettings;
        this.GeneticSettings = GeneticSettings;
    }
}

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
            populationSize: PopulationSize,
            generations: Generations,
            mutationRate: MutationRate,
            eliteCount: EliteCount,
            crossoverRate: CrossoverRate
        );
    }
}
