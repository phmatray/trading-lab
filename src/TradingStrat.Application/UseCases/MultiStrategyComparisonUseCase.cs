using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Strategies;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case implementation for comparing multiple trading strategies.
/// </summary>
public class MultiStrategyComparisonUseCase : IMultiStrategyComparisonUseCase
{
    private readonly IBacktestUseCase _backtestUseCase;

    public MultiStrategyComparisonUseCase(IBacktestUseCase backtestUseCase)
    {
        _backtestUseCase = backtestUseCase;
    }

    public async Task<MultiStrategyComparisonResult> ExecuteAsync(
        MultiStrategyComparisonCommand command,
        IProgress<string>? progress = null)
    {
        if (command.Strategies.Count == 0)
        {
            throw new ArgumentException("At least one strategy must be provided for comparison", nameof(command));
        }

        if (command.Strategies.Count > 10)
        {
            throw new ArgumentException("Maximum 10 strategies can be compared at once", nameof(command));
        }

        List<StrategyComparisonItem> comparisonItems = new();
        int currentStrategy = 0;
        int totalStrategies = command.Strategies.Count;

        // Run backtest for each strategy
        foreach (StrategyConfiguration strategyConfig in command.Strategies)
        {
            currentStrategy++;
            progress?.Report($"Running backtest {currentStrategy}/{totalStrategies}: {strategyConfig.StrategyType}...");

            // Determine strategy type enum
            StrategyType strategyType = ParseStrategyType(strategyConfig.StrategyType);

            // Create backtest command
            BacktestCommand backtestCommand = new(
                Ticker: command.Ticker,
                StrategyType: strategyType,
                StrategyParameters: strategyConfig.Parameters,
                InitialCapital: command.InitialCapital,
                CommissionPercentage: command.CommissionPercentage,
                MinimumCommission: command.MinimumCommission,
                StartDate: command.StartDate,
                EndDate: command.EndDate,
                TimeFrame: null,
                TradingStyle: null,
                CustomStrategyId: strategyConfig.CustomStrategyId
            );

            // Execute backtest
            var backtestResult = await _backtestUseCase.ExecuteAsync(backtestCommand, progress: null);

            if (backtestResult.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Backtest failed for strategy {strategyConfig.StrategyType}: {string.Join(", ", backtestResult.Errors.Select(e => e.Message))}");
            }

            BacktestResult result = backtestResult.Value;

            // Count winning/losing trades
            int winningTrades = result.Trades.Count(t => t.ProfitLoss > 0);
            int losingTrades = result.Trades.Count(t => t.ProfitLoss < 0);

            // Create comparison item
            comparisonItems.Add(new StrategyComparisonItem(
                StrategyName: result.StrategyName,
                StrategyType: strategyConfig.StrategyType,
                Parameters: strategyConfig.Parameters,
                Metrics: result.Metrics,
                EquityCurve: result.EquityCurve,
                TotalTrades: result.Trades.Count,
                WinningTrades: winningTrades,
                LosingTrades: losingTrades
            ));
        }

        // Identify best performers
        StrategyComparisonItem? bestByReturn = comparisonItems
            .OrderByDescending(s => s.Metrics.TotalReturnPercentage)
            .FirstOrDefault();

        StrategyComparisonItem? bestBySharpe = comparisonItems
            .OrderByDescending(s => s.Metrics.SharpeRatio)
            .FirstOrDefault();

        StrategyComparisonItem? bestByDrawdown = comparisonItems
            .OrderBy(s => s.Metrics.MaxDrawdown)  // Lower drawdown is better
            .FirstOrDefault();

        progress?.Report($"Comparison complete: {totalStrategies} strategies analyzed");

        return new MultiStrategyComparisonResult(
            Ticker: command.Ticker,
            StartDate: command.StartDate,
            EndDate: command.EndDate,
            Strategies: comparisonItems,
            BestByReturn: bestByReturn,
            BestBySharpe: bestBySharpe,
            BestByDrawdown: bestByDrawdown
        );
    }

    private static StrategyType ParseStrategyType(string strategyType)
    {
        return strategyType.ToLowerInvariant() switch
        {
            "ma" => StrategyType.MovingAverageCrossover,
            "rsi" => StrategyType.RSI,
            "macd" => StrategyType.MACD,
            "ml" => StrategyType.MachineLearning,
            "custom" => StrategyType.RSI, // Placeholder - CustomStrategyId will be used instead
            _ => throw new ArgumentException($"Unknown strategy type: {strategyType}", nameof(strategyType))
        };
    }
}
