using System.Diagnostics;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Domain service implementing parameter optimization algorithms.
/// Pure domain logic with zero external dependencies - uses delegate pattern for evaluation.
/// </summary>
public class ParameterOptimizer : IParameterOptimizer
{
    private readonly Random _random = new();

    /// <inheritdoc />
    public async Task<OptimizationResult> OptimizeGridSearchAsync(
        Dictionary<string, ParameterRange> parameterRanges,
        OptimizationObjective objective,
        ParameterEvaluator evaluator,
        IProgress<OptimizationProgress>? progress = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        var allIterations = new List<OptimizationIteration>();

        // Generate all parameter combinations
        List<Dictionary<string, decimal>> parameterCombinations = GenerateParameterCombinations(parameterRanges);
        int totalCombinations = parameterCombinations.Count;

        int iterationNumber = 0;
        decimal bestScore = decimal.MinValue;
        Dictionary<string, decimal>? bestParameters = null;

        foreach (Dictionary<string, decimal> parameters in parameterCombinations)
        {
            iterationNumber++;

            // Evaluate this parameter set
            (decimal totalReturn, decimal sharpeRatio, decimal maxDrawdown, int tradeCount) = await evaluator(parameters);

            var iteration = new OptimizationIteration(
                iterationNumber: iterationNumber,
                parameters: new Dictionary<string, decimal>(parameters),
                score: 0m, // Will be calculated below
                totalReturn: totalReturn,
                sharpeRatio: sharpeRatio,
                maxDrawdown: maxDrawdown,
                tradeCount: tradeCount
            );

            // Calculate score based on objective
            decimal score = CalculateScore(iteration, objective);
            iteration = new OptimizationIteration(
                iteration.IterationNumber,
                iteration.Parameters,
                score,
                iteration.TotalReturn,
                iteration.SharpeRatio,
                iteration.MaxDrawdown,
                iteration.TradeCount);

            allIterations.Add(iteration);

            // Track best
            if (score > bestScore)
            {
                bestScore = score;
                bestParameters = parameters;

                progress?.Report(new OptimizationProgress(
                    current: iterationNumber,
                    total: totalCombinations,
                    iterationsCompleted: iterationNumber,
                    currentBestScore: bestScore,
                    currentBestParameters: bestParameters,
                    message: $"New best score: {bestScore:F2} (iteration {iterationNumber}/{totalCombinations})"
                ));
            }
            else if (iterationNumber % 10 == 0)
            {
                progress?.Report(new OptimizationProgress(
                    current: iterationNumber,
                    total: totalCombinations,
                    iterationsCompleted: iterationNumber,
                    currentBestScore: bestScore,
                    currentBestParameters: bestParameters,
                    message: $"Completed {iterationNumber}/{totalCombinations} iterations"
                ));
            }
        }

        // Report final progress if not already reported
        if (iterationNumber % 10 != 0 && iterationNumber > 0)
        {
            progress?.Report(new OptimizationProgress(
                current: iterationNumber,
                total: totalCombinations,
                iterationsCompleted: iterationNumber,
                currentBestScore: bestScore,
                currentBestParameters: bestParameters,
                message: $"Completed {iterationNumber}/{totalCombinations} iterations"
            ));
        }

        stopwatch.Stop();

        return new OptimizationResult(
            bestParameters: bestParameters!,
            bestScore: bestScore,
            allIterations: allIterations,
            duration: stopwatch.Elapsed,
            totalIterations: totalCombinations,
            objective: objective
        );
    }

    /// <inheritdoc />
    public async Task<OptimizationResult> OptimizeGeneticAsync(
        Dictionary<string, ParameterRange> parameterRanges,
        OptimizationObjective objective,
        ParameterEvaluator evaluator,
        GeneticAlgorithmConfig geneticConfig,
        IProgress<OptimizationProgress>? progress = null)
    {
        geneticConfig.Validate();
        Stopwatch stopwatch = Stopwatch.StartNew();
        var allIterations = new List<OptimizationIteration>();

        // Initialize population with random parameters
        List<Dictionary<string, decimal>> population = InitializePopulation(
            parameterRanges,
            geneticConfig.PopulationSize);

        int iterationNumber = 0;
        decimal bestScore = decimal.MinValue;
        Dictionary<string, decimal>? bestParameters = null;

        for (int generation = 0; generation < geneticConfig.Generations; generation++)
        {
            // Evaluate fitness for all individuals
            var evaluatedPopulation = new List<(Dictionary<string, decimal> parameters, decimal score)>();

            foreach (Dictionary<string, decimal> parameters in population)
            {
                iterationNumber++;

                // Evaluate this parameter set
                (decimal totalReturn, decimal sharpeRatio, decimal maxDrawdown, int tradeCount) = await evaluator(parameters);

                var iteration = new OptimizationIteration(
                    iterationNumber: iterationNumber,
                    parameters: new Dictionary<string, decimal>(parameters),
                    score: 0m, // Will be calculated below
                    totalReturn: totalReturn,
                    sharpeRatio: sharpeRatio,
                    maxDrawdown: maxDrawdown,
                    tradeCount: tradeCount
                );

                decimal score = CalculateScore(iteration, objective);
                iteration = new OptimizationIteration(
                    iteration.IterationNumber,
                    iteration.Parameters,
                    score,
                    iteration.TotalReturn,
                    iteration.SharpeRatio,
                    iteration.MaxDrawdown,
                    iteration.TradeCount);

                allIterations.Add(iteration);
                evaluatedPopulation.Add((parameters, score));

                // Track best
                if (score > bestScore)
                {
                    bestScore = score;
                    bestParameters = new Dictionary<string, decimal>(parameters);
                }
            }

            // Report progress
            int totalIterations = geneticConfig.PopulationSize * geneticConfig.Generations;
            progress?.Report(new OptimizationProgress(
                current: iterationNumber,
                total: totalIterations,
                iterationsCompleted: iterationNumber,
                currentBestScore: bestScore,
                currentBestParameters: bestParameters,
                message: $"Generation {generation + 1}/{geneticConfig.Generations} - Best score: {bestScore:F2}"
            ));

            // If not last generation, create next generation
            if (generation < geneticConfig.Generations - 1)
            {
                population = CreateNextGeneration(
                    evaluatedPopulation,
                    parameterRanges,
                    geneticConfig);
            }
        }

        stopwatch.Stop();

        return new OptimizationResult(
            bestParameters: bestParameters!,
            bestScore: bestScore,
            allIterations: allIterations,
            duration: stopwatch.Elapsed,
            totalIterations: iterationNumber,
            objective: objective
        );
    }

    /// <inheritdoc />
    public decimal CalculateScore(OptimizationIteration iteration, OptimizationObjective objective)
    {
        return objective switch
        {
            OptimizationObjective.MaximizeTotalReturn => iteration.TotalReturn,
            OptimizationObjective.MaximizeSharpeRatio => iteration.SharpeRatio,
            OptimizationObjective.MinimizeDrawdown => -iteration.MaxDrawdown, // Negative because we minimize
            OptimizationObjective.MaximizeWinRate => CalculateWinRate(iteration),
            OptimizationObjective.MaximizeProfitFactor => CalculateProfitFactor(iteration),
            _ => throw new ArgumentOutOfRangeException(nameof(objective), objective, null)
        };
    }

    #region Grid Search Helpers

    private List<Dictionary<string, decimal>> GenerateParameterCombinations(
        Dictionary<string, ParameterRange> parameterRanges)
    {
        var combinations = new List<Dictionary<string, decimal>>();

        // Get all parameter names and their values
        var parameterNames = parameterRanges.Keys.ToList();
        var parameterValues = parameterNames
            .Select(name => parameterRanges[name].GetValues())
            .ToList();

        // Generate all combinations using recursion
        GenerateCombinationsRecursive(
            parameterNames,
            parameterValues,
            new Dictionary<string, decimal>(),
            0,
            combinations);

        return combinations;
    }

    private void GenerateCombinationsRecursive(
        List<string> parameterNames,
        List<List<decimal>> parameterValues,
        Dictionary<string, decimal> currentCombination,
        int index,
        List<Dictionary<string, decimal>> combinations)
    {
        if (index == parameterNames.Count)
        {
            combinations.Add(new Dictionary<string, decimal>(currentCombination));
            return;
        }

        string paramName = parameterNames[index];
        foreach (decimal value in parameterValues[index])
        {
            currentCombination[paramName] = value;
            GenerateCombinationsRecursive(
                parameterNames,
                parameterValues,
                currentCombination,
                index + 1,
                combinations);
        }
    }

    #endregion

    #region Genetic Algorithm Helpers

    private List<Dictionary<string, decimal>> InitializePopulation(
        Dictionary<string, ParameterRange> parameterRanges,
        int populationSize)
    {
        var population = new List<Dictionary<string, decimal>>();

        for (int i = 0; i < populationSize; i++)
        {
            var individual = new Dictionary<string, decimal>();
            foreach ((string paramName, ParameterRange range) in parameterRanges)
            {
                // Random value within range
                decimal randomValue = range.Min + (decimal)_random.NextDouble() * (range.Max - range.Min);
                // Round to nearest step
                randomValue = Math.Round(randomValue / range.Step) * range.Step;
                individual[paramName] = Math.Clamp(randomValue, range.Min, range.Max);
            }
            population.Add(individual);
        }

        return population;
    }

    private List<Dictionary<string, decimal>> CreateNextGeneration(
        List<(Dictionary<string, decimal> parameters, decimal score)> evaluatedPopulation,
        Dictionary<string, ParameterRange> parameterRanges,
        GeneticAlgorithmConfig config)
    {
        var nextGeneration = new List<Dictionary<string, decimal>>();

        // Sort by fitness (score)
        var sorted = evaluatedPopulation.OrderByDescending(x => x.score).ToList();

        // Elitism - keep best individuals
        for (int i = 0; i < config.EliteCount && i < sorted.Count; i++)
        {
            nextGeneration.Add(new Dictionary<string, decimal>(sorted[i].parameters));
        }

        // Create rest of population through crossover and mutation
        while (nextGeneration.Count < config.PopulationSize)
        {
            // Tournament selection for parents
            Dictionary<string, decimal> parent1 = TournamentSelection(sorted, 3);
            Dictionary<string, decimal> parent2 = TournamentSelection(sorted, 3);

            // Crossover
            Dictionary<string, decimal> offspring = _random.NextDouble() < (double)config.CrossoverRate
                ? Crossover(parent1, parent2)
                : new Dictionary<string, decimal>(parent1);

            // Mutation
            Mutate(offspring, parameterRanges, config.MutationRate);

            nextGeneration.Add(offspring);
        }

        return nextGeneration;
    }

    private Dictionary<string, decimal> TournamentSelection(
        List<(Dictionary<string, decimal> parameters, decimal score)> population,
        int tournamentSize)
    {
        var tournament = new List<(Dictionary<string, decimal> parameters, decimal score)>();

        for (int i = 0; i < tournamentSize; i++)
        {
            int index = _random.Next(population.Count);
            tournament.Add(population[index]);
        }

        return tournament.OrderByDescending(x => x.score).First().parameters;
    }

    private Dictionary<string, decimal> Crossover(
        Dictionary<string, decimal> parent1,
        Dictionary<string, decimal> parent2)
    {
        Dictionary<string, decimal> offspring = new();

        foreach (string paramName in parent1.Keys)
        {
            // Uniform crossover - randomly pick from either parent
            offspring[paramName] = _random.NextDouble() < 0.5
                ? parent1[paramName]
                : parent2[paramName];
        }

        return offspring;
    }

    private void Mutate(
        Dictionary<string, decimal> individual,
        Dictionary<string, ParameterRange> parameterRanges,
        decimal mutationRate)
    {
        foreach (string paramName in individual.Keys.ToList())
        {
            if (_random.NextDouble() < (double)mutationRate)
            {
                ParameterRange range = parameterRanges[paramName];

                // Gaussian mutation - add random value from normal distribution
                decimal currentValue = individual[paramName];
                decimal mutationAmount = (range.Max - range.Min) * 0.1m * (decimal)(_random.NextDouble() * 2 - 1);

                decimal newValue = currentValue + mutationAmount;
                newValue = Math.Round(newValue / range.Step) * range.Step;
                individual[paramName] = Math.Clamp(newValue, range.Min, range.Max);
            }
        }
    }

    #endregion

    #region Score Calculation Helpers

    private decimal CalculateWinRate(OptimizationIteration iteration)
    {
        // Win rate calculation would need individual trade data
        // For now, estimate based on total return and trade count
        // This will be enhanced in the application layer with actual trade data
        if (iteration.TradeCount == 0)
        {
            return 0m;
        }

        // Rough estimate: if total return is positive, assume ~60% win rate, else ~40%
        return iteration.TotalReturn > 0 ? 60m : 40m;
    }

    private decimal CalculateProfitFactor(OptimizationIteration iteration)
    {
        // Profit factor calculation would need individual trade P&L data
        // For now, estimate based on total return
        // This will be enhanced in the application layer with actual trade data
        if (iteration.TotalReturn <= 0)
        {
            return 0m;
        }

        // Rough estimate based on total return
        return Math.Abs(iteration.TotalReturn) + 1;
    }

    #endregion
}
