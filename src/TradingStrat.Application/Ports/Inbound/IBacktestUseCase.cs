using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for running strategy backtests on historical data.
/// Orchestrates historical data retrieval, strategy execution, and performance calculation.
/// Extracted from ProgramBacktest.RunAsync in original architecture.
/// </summary>
public interface IBacktestUseCase
{
    /// <summary>
    /// Executes a complete backtest of a trading strategy on historical data.
    /// </summary>
    /// <param name="command">Command containing strategy type, parameters, and backtest configuration.</param>
    /// <param name="progress">Optional progress reporter for UI updates during backtest execution.</param>
    /// <returns>Complete backtest results including trades, performance metrics, and equity curve.</returns>
    Task<BacktestResult> ExecuteAsync(
        BacktestCommand command,
        IProgress<BacktestProgress>? progress = null);
}

/// <summary>
/// Command object for executing a backtest.
/// </summary>
/// <param name="Ticker">Stock ticker symbol to backtest.</param>
/// <param name="StrategyType">Strategy type enum (e.g., MovingAverageCrossover, RSI, MACD, MachineLearning).</param>
/// <param name="StrategyParameters">Optional strategy-specific parameters (e.g., period lengths, thresholds).</param>
/// <param name="InitialCapital">Starting capital for the backtest (default $10,000).</param>
/// <param name="CommissionPercentage">Commission as a percentage of trade value (default 0.1%).</param>
/// <param name="MinimumCommission">Minimum commission per trade (default $1.00).</param>
/// <param name="StartDate">Optional start date for backtest period.</param>
/// <param name="EndDate">Optional end date for backtest period.</param>
/// <param name="TimeFrame">Timeframe for historical data (default D1 - daily). Determines bar granularity.</param>
/// <param name="TradingStyle">Optional trading style for applying intelligent defaults.</param>
/// <param name="CustomStrategyId">Optional custom strategy ID. If set, uses custom strategy instead of built-in StrategyType.</param>
public record BacktestCommand(
    string Ticker,
    StrategyType StrategyType,
    Dictionary<string, object>? StrategyParameters = null,
    decimal InitialCapital = 10000m,
    decimal CommissionPercentage = 0.001m,
    decimal MinimumCommission = 1.0m,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    TimeFrame? TimeFrame = null,
    TradingStyle? TradingStyle = null,
    int? CustomStrategyId = null);

/// <summary>
/// Progress update for backtest execution.
/// </summary>
/// <param name="Current">Current bar index being processed.</param>
/// <param name="Total">Total number of bars to process.</param>
/// <param name="Trades">Number of trades executed so far.</param>
public record BacktestProgress(
    int Current,
    int Total,
    int Trades);
