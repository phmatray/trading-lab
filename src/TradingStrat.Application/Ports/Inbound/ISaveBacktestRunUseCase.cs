using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for saving a backtest run to the archive.
/// </summary>
public interface ISaveBacktestRunUseCase
{
    /// <summary>
    /// Executes the use case to save a backtest run.
    /// </summary>
    /// <param name="command">The command containing backtest execution details.</param>
    /// <returns>The saved backtest run with assigned ID.</returns>
    Task<BacktestRun> ExecuteAsync(SaveBacktestRunCommand command);
}

/// <summary>
/// Command for saving a backtest run to the archive.
/// </summary>
public sealed record SaveBacktestRunCommand(
    string Ticker,
    string StrategyType,
    string StrategyName,
    BacktestConfig Config,
    BacktestResult Result,
    Dictionary<string, object> StrategyParameters,
    int ExecutionTimeMs,
    string Status = "Success",
    string? ErrorMessage = null,
    string? Tags = null
);
