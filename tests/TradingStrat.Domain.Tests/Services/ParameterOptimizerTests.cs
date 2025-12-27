using Shouldly;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Services;

/// <summary>
/// Unit tests for ParameterOptimizer domain service.
/// Tests grid search and genetic algorithm optimization.
/// </summary>
public class ParameterOptimizerTests
{
    private readonly ParameterOptimizer _optimizer;

    public ParameterOptimizerTests()
    {
        _optimizer = new ParameterOptimizer();
    }

    #region Grid Search Tests

    [Fact]
    public async Task OptimizeGridSearch_WithSingleParameter_FindsOptimum()
    {
        // Arrange
        var parameterRanges = new Dictionary<string, ParameterRange>
        {
            ["Period"] = new ParameterRange(Min: 10, Max: 20, Step: 2)
        };

        // Evaluator returns better results for Period = 14
        static Task<(decimal, decimal, decimal, int)> evaluator(Dictionary<string, decimal> parameters)
        {
            decimal period = parameters["Period"];
            // Quadratic function with optimum at 14
            decimal totalReturn = -(period - 14) * (period - 14) + 100;
            return Task.FromResult((totalReturn, 1.5m, 10m, 50));
        }

        // Act
        OptimizationResult result = await _optimizer.OptimizeGridSearchAsync(
            parameterRanges,
            OptimizationObjective.MaximizeTotalReturn,
            evaluator);

        // Assert
        result.ShouldNotBeNull();
        result.BestParameters["Period"].ShouldBe(14m);
        result.TotalIterations.ShouldBe(6); // (20-10)/2 + 1 = 6 values
        result.AllIterations.Count.ShouldBe(6);
        result.BestScore.ShouldBe(100m); // -(14-14)^2 + 100
    }

    [Fact]
    public async Task OptimizeGridSearch_WithMultipleParameters_ExploresAllCombinations()
    {
        // Arrange
        var parameterRanges = new Dictionary<string, ParameterRange>
        {
            ["Period"] = new ParameterRange(Min: 10, Max: 14, Step: 2), // 3 values: 10, 12, 14
            ["Threshold"] = new ParameterRange(Min: 20, Max: 30, Step: 5) // 3 values: 20, 25, 30
        };

        // Evaluator returns better results for Period=14, Threshold=25
        static Task<(decimal, decimal, decimal, int)> evaluator(Dictionary<string, decimal> parameters)
        {
            decimal period = parameters["Period"];
            decimal threshold = parameters["Threshold"];
            decimal totalReturn = -(period - 14) * (period - 14) - (threshold - 25) * (threshold - 25) + 100;
            return Task.FromResult((totalReturn, 1.5m, 10m, 50));
        }

        // Act
        OptimizationResult result = await _optimizer.OptimizeGridSearchAsync(
            parameterRanges,
            OptimizationObjective.MaximizeTotalReturn,
            evaluator);

        // Assert
        result.TotalIterations.ShouldBe(9); // 3 * 3 = 9 combinations
        result.BestParameters["Period"].ShouldBe(14m);
        result.BestParameters["Threshold"].ShouldBe(25m);
        result.BestScore.ShouldBe(100m);
        result.AllIterations.Count.ShouldBe(9);
    }

    [Fact]
    public async Task OptimizeGridSearch_WithDifferentObjectives_CalculatesCorrectScore()
    {
        // Arrange
        var parameterRanges = new Dictionary<string, ParameterRange>
        {
            ["Period"] = new ParameterRange(Min: 10, Max: 14, Step: 2)
        };

        static Task<(decimal, decimal, decimal, int)> evaluator(Dictionary<string, decimal> parameters)
        {
            return Task.FromResult((
                totalReturn: 50m,
                sharpeRatio: 2.5m,
                maxDrawdown: 15m,
                tradeCount: 100
            ));
        }

        // Act - Test MaximizeSharpeRatio
        OptimizationResult result = await _optimizer.OptimizeGridSearchAsync(
            parameterRanges,
            OptimizationObjective.MaximizeSharpeRatio,
            evaluator);

        // Assert
        result.BestScore.ShouldBe(2.5m); // Sharpe ratio is the score
        result.Objective.ShouldBe(OptimizationObjective.MaximizeSharpeRatio);
    }

    [Fact]
    public async Task OptimizeGridSearch_WithMinimizeDrawdown_UsesNegativeScore()
    {
        // Arrange
        var parameterRanges = new Dictionary<string, ParameterRange>
        {
            ["Period"] = new ParameterRange(Min: 10, Max: 14, Step: 2)
        };

        static Task<(decimal, decimal, decimal, int)> evaluator(Dictionary<string, decimal> parameters)
        {
            decimal period = parameters["Period"];
            // Lower period = lower drawdown (better)
            decimal drawdown = period;
            return Task.FromResult((50m, 1.5m, drawdown, 100));
        }

        // Act
        OptimizationResult result = await _optimizer.OptimizeGridSearchAsync(
            parameterRanges,
            OptimizationObjective.MinimizeDrawdown,
            evaluator);

        // Assert
        result.BestParameters["Period"].ShouldBe(10m); // Lowest period = lowest drawdown
        result.BestScore.ShouldBe(-10m); // Negative of drawdown
    }

    [Fact]
    public async Task OptimizeGridSearch_ReportsProgress()
    {
        // Arrange
        var parameterRanges = new Dictionary<string, ParameterRange>
        {
            ["Period"] = new ParameterRange(Min: 10, Max: 30, Step: 2) // 11 values
        };

        static Task<(decimal, decimal, decimal, int)> evaluator(Dictionary<string, decimal> parameters)
        {
            return Task.FromResult((50m, 1.5m, 10m, 100));
        }

        var progressReports = new List<OptimizationProgress>();
        var progress = new Progress<OptimizationProgress>(p => progressReports.Add(p));

        // Act
        await _optimizer.OptimizeGridSearchAsync(
            parameterRanges,
            OptimizationObjective.MaximizeTotalReturn,
            evaluator,
            progress);

        // Assert
        progressReports.ShouldNotBeEmpty();
        progressReports.Count.ShouldBeGreaterThan(0);
        progressReports.Last().Current.ShouldBe(11);
        progressReports.Last().Total.ShouldBe(11);
    }

    #endregion

    #region Genetic Algorithm Tests

    [Fact]
    public async Task OptimizeGenetic_ConvergesToOptimum()
    {
        // Arrange
        var parameterRanges = new Dictionary<string, ParameterRange>
        {
            ["Period"] = new ParameterRange(Min: 5, Max: 30, Step: 1)
        };

        // Evaluator with clear optimum at Period = 14
        static Task<(decimal, decimal, decimal, int)> evaluator(Dictionary<string, decimal> parameters)
        {
            decimal period = parameters["Period"];
            decimal totalReturn = -(period - 14) * (period - 14) + 100;
            return Task.FromResult((totalReturn, 1.5m, 10m, 50));
        }

        var config = new GeneticAlgorithmConfig(
            PopulationSize: 20,
            Generations: 30,
            MutationRate: 0.2m,
            EliteCount: 2,
            CrossoverRate: 0.8m
        );

        // Act
        OptimizationResult result = await _optimizer.OptimizeGeneticAsync(
            parameterRanges,
            OptimizationObjective.MaximizeTotalReturn,
            evaluator,
            config);

        // Assert
        result.ShouldNotBeNull();
        result.TotalIterations.ShouldBe(600); // 20 * 30 = 600

        // Genetic algorithm should find value close to optimum (within 2 steps)
        decimal bestPeriod = result.BestParameters["Period"];
        Math.Abs(bestPeriod - 14).ShouldBeLessThanOrEqualTo(2m);

        // Score should be close to optimal (100)
        result.BestScore.ShouldBeGreaterThan(95m);
    }

    [Fact]
    public async Task OptimizeGenetic_WithElitism_PreservesBestSolutions()
    {
        // Arrange
        var parameterRanges = new Dictionary<string, ParameterRange>
        {
            ["Period"] = new ParameterRange(Min: 5, Max: 30, Step: 1)
        };

        static Task<(decimal, decimal, decimal, int)> evaluator(Dictionary<string, decimal> parameters)
        {
            decimal period = parameters["Period"];
            decimal totalReturn = -(period - 15) * (period - 15) + 100;
            return Task.FromResult((totalReturn, 1.5m, 10m, 50));
        }

        var config = new GeneticAlgorithmConfig(
            PopulationSize: 10,
            Generations: 10,
            MutationRate: 0.1m,
            EliteCount: 3, // Keep top 3
            CrossoverRate: 0.8m
        );

        // Act
        OptimizationResult result = await _optimizer.OptimizeGeneticAsync(
            parameterRanges,
            OptimizationObjective.MaximizeTotalReturn,
            evaluator,
            config);

        // Assert
        // Best score should never decrease across generations (due to elitism)
        decimal maxScoreSoFar = decimal.MinValue;
        foreach (OptimizationIteration iteration in result.AllIterations.OrderBy(i => i.IterationNumber))
        {
            if (iteration.Score > maxScoreSoFar)
            {
                maxScoreSoFar = iteration.Score;
            }
        }

        result.BestScore.ShouldBe(maxScoreSoFar);
    }

    [Fact]
    public async Task OptimizeGenetic_WithMultipleParameters_Converges()
    {
        // Arrange
        var parameterRanges = new Dictionary<string, ParameterRange>
        {
            ["Period"] = new ParameterRange(Min: 5, Max: 30, Step: 1),
            ["Threshold"] = new ParameterRange(Min: 20, Max: 80, Step: 1)
        };

        // Optimum at Period=14, Threshold=50
        static Task<(decimal, decimal, decimal, int)> evaluator(Dictionary<string, decimal> parameters)
        {
            decimal period = parameters["Period"];
            decimal threshold = parameters["Threshold"];
            decimal totalReturn = -(period - 14) * (period - 14) - (threshold - 50) * (threshold - 50) + 200;
            return Task.FromResult((totalReturn, 1.5m, 10m, 50));
        }

        var config = new GeneticAlgorithmConfig(
            PopulationSize: 30,
            Generations: 50,
            MutationRate: 0.15m,
            EliteCount: 5,
            CrossoverRate: 0.8m
        );

        // Act
        OptimizationResult result = await _optimizer.OptimizeGeneticAsync(
            parameterRanges,
            OptimizationObjective.MaximizeTotalReturn,
            evaluator,
            config);

        // Assert
        decimal bestPeriod = result.BestParameters["Period"];
        decimal bestThreshold = result.BestParameters["Threshold"];

        // Should be close to optimum
        Math.Abs(bestPeriod - 14).ShouldBeLessThanOrEqualTo(3m);
        Math.Abs(bestThreshold - 50).ShouldBeLessThanOrEqualTo(5m);

        // Score should be reasonably high
        result.BestScore.ShouldBeGreaterThan(180m); // Close to 200
    }

    [Fact]
    public async Task OptimizeGenetic_ReportsProgressPerGeneration()
    {
        // Arrange
        var parameterRanges = new Dictionary<string, ParameterRange>
        {
            ["Period"] = new ParameterRange(Min: 10, Max: 20, Step: 1)
        };

        static Task<(decimal, decimal, decimal, int)> evaluator(Dictionary<string, decimal> parameters)
        {
            return Task.FromResult((50m, 1.5m, 10m, 100));
        }

        var config = new GeneticAlgorithmConfig(
            PopulationSize: 10,
            Generations: 5,
            MutationRate: 0.1m,
            EliteCount: 2,
            CrossoverRate: 0.8m
        );

        var progressReports = new List<OptimizationProgress>();
        var progress = new Progress<OptimizationProgress>(p => progressReports.Add(p));

        // Act
        await _optimizer.OptimizeGeneticAsync(
            parameterRanges,
            OptimizationObjective.MaximizeTotalReturn,
            evaluator,
            config,
            progress);

        // Assert
        progressReports.ShouldNotBeEmpty();
        progressReports.Count.ShouldBe(5); // One per generation
        progressReports.Last().Current.ShouldBe(50); // 10 * 5
        progressReports.Last().Total.ShouldBe(50);
    }

    [Fact]
    public void GeneticAlgorithmConfig_WithInvalidPopulationSize_ThrowsException()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig(
            PopulationSize: 5, // Too small
            Generations: 10
        );

        // Act & Assert
        Should.Throw<ArgumentException>(() => config.Validate());
    }

    [Fact]
    public void GeneticAlgorithmConfig_WithInvalidMutationRate_ThrowsException()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig(
            PopulationSize: 20,
            Generations: 10,
            MutationRate: 1.5m // > 1
        );

        // Act & Assert
        Should.Throw<ArgumentException>(() => config.Validate());
    }

    [Fact]
    public void GeneticAlgorithmConfig_WithInvalidEliteCount_ThrowsException()
    {
        // Arrange
        var config = new GeneticAlgorithmConfig(
            PopulationSize: 20,
            Generations: 10,
            EliteCount: 25 // Greater than population size
        );

        // Act & Assert
        Should.Throw<ArgumentException>(() => config.Validate());
    }

    #endregion

    #region Score Calculation Tests

    [Fact]
    public void CalculateScore_WithMaximizeTotalReturn_ReturnsCorrectScore()
    {
        // Arrange
        var iteration = new OptimizationIteration(
            IterationNumber: 1,
            Parameters: new Dictionary<string, decimal> { ["Period"] = 14 },
            Score: 0m,
            TotalReturn: 25.5m,
            SharpeRatio: 1.8m,
            MaxDrawdown: 12.3m,
            TradeCount: 50
        );

        // Act
        decimal score = _optimizer.CalculateScore(iteration, OptimizationObjective.MaximizeTotalReturn);

        // Assert
        score.ShouldBe(25.5m);
    }

    [Fact]
    public void CalculateScore_WithMaximizeSharpeRatio_ReturnsCorrectScore()
    {
        // Arrange
        var iteration = new OptimizationIteration(
            IterationNumber: 1,
            Parameters: new Dictionary<string, decimal> { ["Period"] = 14 },
            Score: 0m,
            TotalReturn: 25.5m,
            SharpeRatio: 1.8m,
            MaxDrawdown: 12.3m,
            TradeCount: 50
        );

        // Act
        decimal score = _optimizer.CalculateScore(iteration, OptimizationObjective.MaximizeSharpeRatio);

        // Assert
        score.ShouldBe(1.8m);
    }

    [Fact]
    public void CalculateScore_WithMinimizeDrawdown_ReturnsNegativeDrawdown()
    {
        // Arrange
        var iteration = new OptimizationIteration(
            IterationNumber: 1,
            Parameters: new Dictionary<string, decimal> { ["Period"] = 14 },
            Score: 0m,
            TotalReturn: 25.5m,
            SharpeRatio: 1.8m,
            MaxDrawdown: 12.3m,
            TradeCount: 50
        );

        // Act
        decimal score = _optimizer.CalculateScore(iteration, OptimizationObjective.MinimizeDrawdown);

        // Assert
        score.ShouldBe(-12.3m); // Negative because we want to minimize
    }

    #endregion

    #region Parameter Range Tests

    [Fact]
    public void ParameterRange_GetValues_ReturnsCorrectSequence()
    {
        // Arrange
        var range = new ParameterRange(Min: 10, Max: 20, Step: 2.5m);

        // Act
        List<decimal> values = range.GetValues();

        // Assert
        values.ShouldBe(new[] { 10m, 12.5m, 15m, 17.5m, 20m });
    }

    [Fact]
    public void ParameterRange_StepCount_CalculatesCorrectly()
    {
        // Arrange
        var range = new ParameterRange(Min: 10, Max: 20, Step: 2);

        // Act
        int stepCount = range.StepCount;

        // Assert
        stepCount.ShouldBe(6); // 10, 12, 14, 16, 18, 20
    }

    [Fact]
    public void ParameterRange_WithInvalidStep_ThrowsException()
    {
        // Arrange
        var range = new ParameterRange(Min: 10, Max: 20, Step: 0);

        // Act & Assert
        Should.Throw<ArgumentException>(() => range.GetValues());
    }

    #endregion

    #region OptimizationResult Tests

    [Fact]
    public void OptimizationResult_BestIteration_ReturnsBestScore()
    {
        // Arrange
        var iterations = new List<OptimizationIteration>
        {
            new(1, new Dictionary<string, decimal>(), 50m, 10m, 1.5m, 10m, 50),
            new(2, new Dictionary<string, decimal>(), 75m, 15m, 2.0m, 8m, 60),
            new(3, new Dictionary<string, decimal>(), 60m, 12m, 1.8m, 9m, 55)
        };

        var result = new OptimizationResult(
            BestParameters: new Dictionary<string, decimal>(),
            BestScore: 75m,
            AllIterations: iterations,
            Duration: TimeSpan.FromSeconds(10),
            TotalIterations: 3,
            Objective: OptimizationObjective.MaximizeTotalReturn
        );

        // Act
        OptimizationIteration bestIteration = result.BestIteration;

        // Assert
        bestIteration.IterationNumber.ShouldBe(2);
        bestIteration.Score.ShouldBe(75m);
    }

    [Fact]
    public void OptimizationResult_GetTopIterations_ReturnsTopN()
    {
        // Arrange
        var iterations = new List<OptimizationIteration>
        {
            new(1, new Dictionary<string, decimal>(), 50m, 10m, 1.5m, 10m, 50),
            new(2, new Dictionary<string, decimal>(), 75m, 15m, 2.0m, 8m, 60),
            new(3, new Dictionary<string, decimal>(), 60m, 12m, 1.8m, 9m, 55),
            new(4, new Dictionary<string, decimal>(), 80m, 16m, 2.1m, 7m, 65),
            new(5, new Dictionary<string, decimal>(), 55m, 11m, 1.6m, 10m, 52)
        };

        var result = new OptimizationResult(
            BestParameters: new Dictionary<string, decimal>(),
            BestScore: 80m,
            AllIterations: iterations,
            Duration: TimeSpan.FromSeconds(10),
            TotalIterations: 5,
            Objective: OptimizationObjective.MaximizeTotalReturn
        );

        // Act
        List<OptimizationIteration> top3 = result.GetTopIterations(3);

        // Assert
        top3.Count.ShouldBe(3);
        top3[0].Score.ShouldBe(80m);
        top3[1].Score.ShouldBe(75m);
        top3[2].Score.ShouldBe(60m);
    }

    #endregion

    #region OptimizationProgress Tests

    [Fact]
    public void OptimizationProgress_PercentComplete_CalculatesCorrectly()
    {
        // Arrange
        var progress = new OptimizationProgress(
            Current: 25,
            Total: 100,
            IterationsCompleted: 25,
            CurrentBestScore: 75m,
            CurrentBestParameters: new Dictionary<string, decimal>(),
            Message: "Progress"
        );

        // Act
        int percent = progress.PercentComplete;

        // Assert
        percent.ShouldBe(25);
    }

    [Fact]
    public void OptimizationProgress_WithZeroTotal_ReturnsZeroPercent()
    {
        // Arrange
        var progress = new OptimizationProgress(
            Current: 0,
            Total: 0,
            IterationsCompleted: 0,
            CurrentBestScore: null,
            CurrentBestParameters: null,
            Message: "Starting"
        );

        // Act
        int percent = progress.PercentComplete;

        // Assert
        percent.ShouldBe(0);
    }

    #endregion
}
