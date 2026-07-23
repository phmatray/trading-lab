using System.ComponentModel.DataAnnotations;
using TradingStrat.Application.Commands;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Web.Models;

/// <summary>
/// Form model for parameter optimization configuration.
/// </summary>
public class OptimizationFormModel
{
    [Required(ErrorMessage = "Custom strategy is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid custom strategy")]
    public int CustomStrategyId { get; set; }

    [Required(ErrorMessage = "Ticker is required")]
    [MinLength(1, ErrorMessage = "Ticker must be at least 1 character")]
    public string Ticker { get; set; } = "CON3.L";

    [Required(ErrorMessage = "Optimization type is required")]
    public OptimizationType OptimizationType { get; set; } = OptimizationType.GridSearch;

    [Required(ErrorMessage = "Optimization objective is required")]
    public OptimizationObjective Objective { get; set; } = OptimizationObjective.MaximizeSharpeRatio;

    public List<ParameterRangeModel> ParameterRanges { get; set; } = new();

    [Range(100, 1000000, ErrorMessage = "Initial capital must be between $100 and $1,000,000")]
    public decimal InitialCapital { get; set; } = 10000m;

    [Range(0, 0.1, ErrorMessage = "Commission must be between 0% and 10%")]
    public decimal CommissionPercentage { get; set; } = 0.001m;

    [Range(0, 100, ErrorMessage = "Minimum commission must be between $0 and $100")]
    public decimal MinimumCommission { get; set; } = 1.0m;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    // Genetic Algorithm Settings (only used if OptimizationType == Genetic)
    [Range(10, 1000, ErrorMessage = "Population size must be between 10 and 1000")]
    public int PopulationSize { get; set; } = 50;

    [Range(1, 500, ErrorMessage = "Generations must be between 1 and 500")]
    public int Generations { get; set; } = 100;

    [Range(0, 1, ErrorMessage = "Mutation rate must be between 0 and 1")]
    public decimal MutationRate { get; set; } = 0.1m;

    [Range(0, 100, ErrorMessage = "Elite count must be between 0 and 100")]
    public int EliteCount { get; set; } = 5;

    [Range(0, 1, ErrorMessage = "Crossover rate must be between 0 and 1")]
    public decimal CrossoverRate { get; set; } = 0.8m;

    /// <summary>
    /// Converts form model to command for use case.
    /// </summary>
    public OptimizeParametersCommand ToCommand()
    {
        // Convert parameter ranges to dictionary
        Dictionary<string, ParameterRange> parameterRanges = ParameterRanges
            .Where(p => p.IsEnabled)
            .ToDictionary(
                p => p.ParameterName,
                p => new ParameterRange(p.Min, p.Max, p.Step)
            );

        // Create backtest config
        BacktestConfig backtestConfig = new(
            ticker: Ticker,
            startDate: StartDate ?? DateTime.Today.AddYears(-2),
            endDate: EndDate ?? DateTime.Today,
            initialCapital: InitialCapital,
            commissionPercentage: CommissionPercentage,
            minimumCommission: MinimumCommission
        );

        // Create genetic settings if needed
        GeneticAlgorithmSettings? geneticSettings = OptimizationType == OptimizationType.Genetic
            ? new GeneticAlgorithmSettings(
                PopulationSize: PopulationSize,
                Generations: Generations,
                MutationRate: MutationRate,
                EliteCount: EliteCount,
                CrossoverRate: CrossoverRate)
            : null;

        return new OptimizeParametersCommand(
            CustomStrategyId: CustomStrategyId,
            Type: OptimizationType,
            ParameterRanges: parameterRanges,
            Objective: Objective,
            BacktestSettings: backtestConfig,
            GeneticSettings: geneticSettings
        );
    }
}

/// <summary>
/// Represents a single parameter range for optimization.
/// </summary>
public class ParameterRangeModel
{
    public string ParameterName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    [Range(-1000000, 1000000, ErrorMessage = "Min value must be between -1,000,000 and 1,000,000")]
    public decimal Min { get; set; }

    [Range(-1000000, 1000000, ErrorMessage = "Max value must be between -1,000,000 and 1,000,000")]
    public decimal Max { get; set; }

    [Range(0.001, 1000, ErrorMessage = "Step must be between 0.001 and 1000")]
    public decimal Step { get; set; } = 1m;

    public decimal CurrentValue { get; set; }
}
