using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Represents a single optimization iteration with parameters and score.
/// </summary>
public sealed class OptimizationIteration : ValueObject
{
    public int IterationNumber { get; init; }
    public Dictionary<string, decimal> Parameters { get; init; }
    public decimal Score { get; init; }
    public decimal TotalReturn { get; init; }
    public decimal SharpeRatio { get; init; }
    public decimal MaxDrawdown { get; init; }
    public int TradeCount { get; init; }

    public OptimizationIteration(
        int iterationNumber,
        Dictionary<string, decimal> parameters,
        decimal score,
        decimal totalReturn,
        decimal sharpeRatio,
        decimal maxDrawdown,
        int tradeCount)
    {
        IterationNumber = iterationNumber;
        Parameters = parameters;
        Score = score;
        TotalReturn = totalReturn;
        SharpeRatio = sharpeRatio;
        MaxDrawdown = maxDrawdown;
        TradeCount = tradeCount;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return IterationNumber;
        foreach (string key in Parameters.Keys.OrderBy(k => k))
        {
            yield return key;
            yield return Parameters[key];
        }
        yield return Score;
        yield return TotalReturn;
        yield return SharpeRatio;
        yield return MaxDrawdown;
        yield return TradeCount;
    }
}

/// <summary>
/// Complete optimization result with best parameters and all iterations.
/// </summary>
public sealed class OptimizationResult : ValueObject
{
    public Dictionary<string, decimal> BestParameters { get; init; }
    public decimal BestScore { get; init; }
    public List<OptimizationIteration> AllIterations { get; init; }
    public TimeSpan Duration { get; init; }
    public int TotalIterations { get; init; }
    public OptimizationObjective Objective { get; init; }

    public OptimizationResult(
        Dictionary<string, decimal> bestParameters,
        decimal bestScore,
        List<OptimizationIteration> allIterations,
        TimeSpan duration,
        int totalIterations,
        OptimizationObjective objective)
    {
        BestParameters = bestParameters;
        BestScore = bestScore;
        AllIterations = allIterations;
        Duration = duration;
        TotalIterations = totalIterations;
        Objective = objective;
    }

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

    protected override IEnumerable<object> GetEqualityComponents()
    {
        foreach (string key in BestParameters.Keys.OrderBy(k => k))
        {
            yield return key;
            yield return BestParameters[key];
        }
        yield return BestScore;
        foreach (OptimizationIteration iteration in AllIterations)
        {
            yield return iteration;
        }
        yield return Duration;
        yield return TotalIterations;
        yield return Objective;
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
public sealed class ParameterRange : ValueObject
{
    public decimal Min { get; init; }
    public decimal Max { get; init; }
    public decimal Step { get; init; }

    public ParameterRange(
        decimal min,
        decimal max,
        decimal step)
    {
        Min = min;
        Max = max;
        Step = step;
    }

    /// <summary>
    /// Gets all values in this range.
    /// </summary>
    public List<decimal> GetValues()
    {
        if (Step <= 0)
        {
            throw new ArgumentException("Step must be positive", nameof(Step));
        }

        List<decimal> values = new List<decimal>();
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

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Min;
        yield return Max;
        yield return Step;
    }
}

/// <summary>
/// Progress update for optimization operations.
/// </summary>
public sealed class OptimizationProgress : ValueObject
{
    public int Current { get; init; }
    public int Total { get; init; }
    public int IterationsCompleted { get; init; }
    public decimal? CurrentBestScore { get; init; }
    public Dictionary<string, decimal>? CurrentBestParameters { get; init; }
    public string Message { get; init; }

    public OptimizationProgress(
        int current,
        int total,
        int iterationsCompleted,
        decimal? currentBestScore,
        Dictionary<string, decimal>? currentBestParameters,
        string message)
    {
        Current = current;
        Total = total;
        IterationsCompleted = iterationsCompleted;
        CurrentBestScore = currentBestScore;
        CurrentBestParameters = currentBestParameters;
        Message = message;
    }

    /// <summary>
    /// Gets the progress percentage.
    /// </summary>
    public int PercentComplete => Total > 0 ? (int)((double)Current / Total * 100) : 0;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Current;
        yield return Total;
        yield return IterationsCompleted;
        yield return CurrentBestScore ?? 0m;
        if (CurrentBestParameters is not null)
        {
            foreach (string key in CurrentBestParameters.Keys.OrderBy(k => k))
            {
                yield return key;
                yield return CurrentBestParameters[key];
            }
        }
        yield return Message;
    }
}

/// <summary>
/// Configuration for genetic algorithm optimization.
/// </summary>
public sealed class GeneticAlgorithmConfig : ValueObject
{
    public int PopulationSize { get; init; }
    public int Generations { get; init; }
    public decimal MutationRate { get; init; }
    public int EliteCount { get; init; }
    public decimal CrossoverRate { get; init; }

    public GeneticAlgorithmConfig(
        int populationSize = 50,
        int generations = 100,
        decimal mutationRate = 0.1m,
        int eliteCount = 5,
        decimal crossoverRate = 0.8m)
    {
        PopulationSize = populationSize;
        Generations = generations;
        MutationRate = mutationRate;
        EliteCount = eliteCount;
        CrossoverRate = crossoverRate;
    }

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

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return PopulationSize;
        yield return Generations;
        yield return MutationRate;
        yield return EliteCount;
        yield return CrossoverRate;
    }
}
