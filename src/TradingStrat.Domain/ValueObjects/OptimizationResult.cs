namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Represents a single optimization iteration with parameters and score.
/// </summary>
public sealed record OptimizationIteration(
    int IterationNumber,
    Dictionary<string, decimal> Parameters,
    decimal Score,
    decimal TotalReturn,
    decimal SharpeRatio,
    decimal MaxDrawdown,
    int TradeCount
);

/// <summary>
/// Complete optimization result with best parameters and all iterations.
/// </summary>
public sealed record OptimizationResult(
    Dictionary<string, decimal> BestParameters,
    decimal BestScore,
    List<OptimizationIteration> AllIterations,
    TimeSpan Duration,
    int TotalIterations,
    OptimizationObjective Objective
)
{
    /// <summary>
    /// Gets the iteration with the best score.
    /// </summary>
    public OptimizationIteration BestIteration =>
        AllIterations.OrderByDescending(i => i.Score).First();

    /// <summary>
    /// Gets the top N iterations by score.
    /// </summary>
    public List<OptimizationIteration> GetTopIterations(int count)
    {
        return AllIterations
            .OrderByDescending(i => i.Score)
            .Take(count)
            .ToList();
    }
}

/// <summary>
/// Defines the optimization objective function.
/// </summary>
public enum OptimizationObjective
{
    /// <summary>Maximize total return percentage.</summary>
    MaximizeTotalReturn,

    /// <summary>Maximize Sharpe ratio (risk-adjusted return).</summary>
    MaximizeSharpeRatio,

    /// <summary>Minimize maximum drawdown.</summary>
    MinimizeDrawdown,

    /// <summary>Maximize win rate (winning trades / total trades).</summary>
    MaximizeWinRate,

    /// <summary>Maximize profit factor (gross profit / gross loss).</summary>
    MaximizeProfitFactor
}

/// <summary>
/// Defines a parameter range for optimization.
/// </summary>
public sealed record ParameterRange(
    decimal Min,
    decimal Max,
    decimal Step
)
{
    /// <summary>
    /// Gets all values in this range.
    /// </summary>
    public List<decimal> GetValues()
    {
        if (Step <= 0)
        {
            throw new ArgumentException("Step must be positive", nameof(Step));
        }

        var values = new List<decimal>();
        for (decimal value = Min; value <= Max; value += Step)
        {
            values.Add(value);
        }
        return values;
    }

    /// <summary>
    /// Gets the number of steps in this range.
    /// </summary>
    public int StepCount => (int)Math.Ceiling((Max - Min) / Step) + 1;
}

/// <summary>
/// Progress update for optimization operations.
/// </summary>
public sealed record OptimizationProgress(
    int Current,
    int Total,
    int IterationsCompleted,
    decimal? CurrentBestScore,
    Dictionary<string, decimal>? CurrentBestParameters,
    string Message
)
{
    /// <summary>
    /// Gets the progress percentage.
    /// </summary>
    public int PercentComplete => Total > 0 ? (int)((double)Current / Total * 100) : 0;
}

/// <summary>
/// Configuration for genetic algorithm optimization.
/// </summary>
public sealed record GeneticAlgorithmConfig(
    int PopulationSize = 50,
    int Generations = 100,
    decimal MutationRate = 0.1m,
    int EliteCount = 5,
    decimal CrossoverRate = 0.8m
)
{
    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public void Validate()
    {
        if (PopulationSize < 10)
        {
            throw new ArgumentException("Population size must be at least 10", nameof(PopulationSize));
        }

        if (Generations < 1)
        {
            throw new ArgumentException("Generations must be at least 1", nameof(Generations));
        }

        if (MutationRate is < 0 or > 1)
        {
            throw new ArgumentException("Mutation rate must be between 0 and 1", nameof(MutationRate));
        }

        if (EliteCount < 0 || EliteCount >= PopulationSize)
        {
            throw new ArgumentException("Elite count must be between 0 and population size", nameof(EliteCount));
        }

        if (CrossoverRate is < 0 or > 1)
        {
            throw new ArgumentException("Crossover rate must be between 0 and 1", nameof(CrossoverRate));
        }
    }
}
