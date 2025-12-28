using System.Text.Json;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for optimizing custom strategy parameters using grid search or genetic algorithms.
/// Orchestrates multiple backtests with different parameter combinations to find optimal values.
/// </summary>
public class OptimizeStrategyParametersUseCase : IOptimizeStrategyParametersUseCase
{
    private readonly ICustomStrategyPort _customStrategyPort;
    private readonly IParameterOptimizer _parameterOptimizer;
    private readonly BacktestEngine _backtestEngine;
    private readonly IIndicatorCalculator _indicatorCalculator;

    public OptimizeStrategyParametersUseCase(
        ICustomStrategyPort customStrategyPort,
        IParameterOptimizer parameterOptimizer,
        BacktestEngine backtestEngine,
        IIndicatorCalculator indicatorCalculator)
    {
        _customStrategyPort = customStrategyPort;
        _parameterOptimizer = parameterOptimizer;
        _backtestEngine = backtestEngine;
        _indicatorCalculator = indicatorCalculator;
    }

    public async Task<Result<OptimizationResult>> ExecuteAsync(
        OptimizeParametersCommand command,
        IProgress<Domain.ValueObjects.OptimizationProgress>? progress = null)
    {
        try
        {
            // Load custom strategy from repository
            CustomStrategy? strategy = await _customStrategyPort.GetByIdAsync(command.CustomStrategyId);
            if (strategy is null)
            {
                return Result<OptimizationResult>.Failure(
                    Error.NotFound($"Custom strategy with ID {command.CustomStrategyId} not found", ErrorCodes.Strategy.NotFound));
            }

            // Deserialize strategy definition
            StrategyDefinition? baseDefinition = JsonSerializer.Deserialize<StrategyDefinition>(strategy.DefinitionJson);
            if (baseDefinition is null)
            {
                return Result<OptimizationResult>.Failure(
                    Error.BusinessRule("Failed to deserialize strategy definition", ErrorCodes.Strategy.InvalidDefinition));
            }

            // Convert BacktestConfig to BacktestConfiguration
            BacktestConfiguration backtestConfig = new(
                Ticker: command.BacktestSettings.Ticker,
                StartDate: command.BacktestSettings.StartDate,
                EndDate: command.BacktestSettings.EndDate,
                InitialCapital: command.BacktestSettings.InitialCapital,
                CommissionPercentage: command.BacktestSettings.CommissionPercentage,
                MinimumCommission: command.BacktestSettings.MinimumCommission,
                TimeFrame: TimeFrame.D1
            );

            // Create parameter evaluator function that runs backtests
            async Task<(decimal totalReturn, decimal sharpeRatio, decimal maxDrawdown, int tradeCount)> Evaluator(
                Dictionary<string, decimal> parameters)
            {
                // Modify strategy definition with new parameters
                StrategyDefinition modifiedDefinition = ApplyParametersToDefinition(baseDefinition, parameters);

                // Create strategy instance with modified definition
                CustomRuleBasedStrategy customStrategy = new(
                    _indicatorCalculator,
                    modifiedDefinition,
                    strategy.Name,
                    strategy.Description
                );

                // Run backtest
                BacktestResult result = await _backtestEngine.RunBacktestAsync(
                    customStrategy,
                    backtestConfig,
                    progress: null // Don't report individual backtest progress to avoid noise
                );

                // Extract metrics for optimization
                decimal totalReturn = result.Metrics.TotalReturn;
                decimal sharpeRatio = result.Metrics.SharpeRatio;
                decimal maxDrawdown = result.Metrics.MaxDrawdown;
                int tradeCount = result.Trades.Count;

                return (totalReturn, sharpeRatio, maxDrawdown, tradeCount);
            }

            // Run optimization based on type
            OptimizationResult optimizationResult = command.Type switch
            {
                OptimizationType.GridSearch => await _parameterOptimizer.OptimizeGridSearchAsync(
                    command.ParameterRanges,
                    command.Objective,
                    Evaluator,
                    progress),

                OptimizationType.Genetic => await _parameterOptimizer.OptimizeGeneticAsync(
                    command.ParameterRanges,
                    command.Objective,
                    Evaluator,
                    command.GeneticSettings?.ToDomainConfig() ?? new GeneticAlgorithmConfig(),
                    progress),

                _ => throw new ArgumentOutOfRangeException(nameof(command.Type), command.Type, "Unknown optimization type")
            };

            return Result<OptimizationResult>.Success(optimizationResult);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result<OptimizationResult>.Failure(
                Error.Validation($"Invalid optimization type: {ex.Message}", ErrorCodes.Analysis.OptimizationFailed));
        }
        catch (InvalidOperationException ex)
        {
            return Result<OptimizationResult>.Failure(
                Error.BusinessRule($"Optimization failed: {ex.Message}", ErrorCodes.Analysis.OptimizationFailed));
        }
        catch (ArgumentException ex)
        {
            return Result<OptimizationResult>.Failure(
                Error.Validation($"Invalid optimization parameters: {ex.Message}", ErrorCodes.Analysis.OptimizationFailed));
        }
        catch (Exception ex)
        {
            return Result<OptimizationResult>.Failure(
                Error.BusinessRule($"Optimization failed: {ex.Message}", ErrorCodes.Analysis.OptimizationFailed));
        }
    }

    /// <summary>
    /// Applies parameter values to strategy definition by modifying rule parameters.
    /// </summary>
    private StrategyDefinition ApplyParametersToDefinition(
        StrategyDefinition baseDefinition,
        Dictionary<string, decimal> parameters)
    {
        // Create deep copy of entry rules with modified parameters
        List<StrategyRule> modifiedEntryRules = baseDefinition.EntryRules
            .Select(rule => ApplyParametersToRule(rule, parameters))
            .ToList();

        // Create deep copy of exit rules with modified parameters
        List<StrategyRule> modifiedExitRules = baseDefinition.ExitRules
            .Select(rule => ApplyParametersToRule(rule, parameters))
            .ToList();

        // Return modified definition
        return new StrategyDefinition(
            EntryRules: modifiedEntryRules,
            ExitRules: modifiedExitRules,
            SizingMode: baseDefinition.SizingMode,
            SizingParameters: new Dictionary<string, decimal>(baseDefinition.SizingParameters)
        );
    }

    /// <summary>
    /// Applies parameter values to a single rule by updating indicator parameters.
    /// </summary>
    private StrategyRule ApplyParametersToRule(StrategyRule rule, Dictionary<string, decimal> parameters)
    {
        // Modify indicator parameters if they match optimization parameters
        Dictionary<string, object> modifiedParams = ModifyParameters(rule.IndicatorParameters, parameters);

        // Modify second indicator parameters if present
        Dictionary<string, object>? modifiedSecondParams = rule.SecondIndicatorParameters is not null
            ? ModifyParameters(rule.SecondIndicatorParameters, parameters)
            : null;

        // Return modified rule
        return new StrategyRule(
            rule.IndicatorName,
            modifiedParams,
            rule.Operator,
            rule.ValueType,
            rule.ConstantValue,
            rule.SecondIndicatorName,
            modifiedSecondParams,
            rule.LogicalOperator);
    }

    /// <summary>
    /// Modifies indicator parameter dictionary by replacing values with optimization parameter values.
    /// </summary>
    private Dictionary<string, object> ModifyParameters(
        Dictionary<string, object> indicatorParams,
        Dictionary<string, decimal> optimizationParams)
    {
        Dictionary<string, object> modified = new(indicatorParams);

        foreach ((string key, object value) in indicatorParams)
        {
            // If this parameter is being optimized, replace its value
            if (optimizationParams.ContainsKey(key))
            {
                // Convert decimal to appropriate type (int for periods, decimal for thresholds)
                object newValue = value is int
                    ? (int)optimizationParams[key]
                    : optimizationParams[key];

                modified[key] = newValue;
            }
        }

        return modified;
    }
}
