using System.Text.Json;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case implementation for saving backtest runs to the archive.
/// </summary>
public class SaveBacktestRunUseCase : ISaveBacktestRunUseCase
{
    private readonly IBacktestArchivePort _backtestArchivePort;

    public SaveBacktestRunUseCase(IBacktestArchivePort backtestArchivePort)
    {
        _backtestArchivePort = backtestArchivePort;
    }

    public async Task<BacktestRun> ExecuteAsync(SaveBacktestRunCommand command)
    {
        // Create BacktestRun entity
        var backtestRun = new BacktestRun
        {
            Ticker = command.Ticker,
            StrategyType = command.StrategyType,
            StrategyName = command.StrategyName,
            StrategyParametersJson = JsonSerializer.Serialize(command.StrategyParameters),
            ConfigJson = JsonSerializer.Serialize(command.Config),
            ResultsJson = JsonSerializer.Serialize(command.Result),
            ExecutedAt = DateTime.UtcNow,
            ExecutionTimeMs = command.ExecutionTimeMs,
            Status = command.Status,
            ErrorMessage = command.ErrorMessage,
            Tags = command.Tags
        };

        // Save to repository
        return await _backtestArchivePort.SaveBacktestRunAsync(backtestRun);
    }
}
