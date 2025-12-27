using System.Text.Json;
using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for retrieving top performing strategies based on backtest results.
/// Uses BaseUseCase to eliminate try-catch boilerplate.
/// </summary>
public class GetTopStrategiesUseCase : BaseUseCase<int, List<TopStrategyResult>>, IGetTopStrategiesUseCase
{
    private readonly IBacktestArchivePort _backtestArchivePort;

    public GetTopStrategiesUseCase(IBacktestArchivePort backtestArchivePort)
    {
        _backtestArchivePort = backtestArchivePort;
    }

    public Task<Result<List<TopStrategyResult>>> ExecuteAsync(int limit = 5)
        => base.ExecuteAsync(limit, ExecuteCoreAsync, ErrorCodes.Strategy.RetrievalFailed);

    private async Task<List<TopStrategyResult>> ExecuteCoreAsync(int limit)
    {
        List<BacktestRun> topBacktests = await _backtestArchivePort.GetTopBacktestRunsAsync(limit);

        var results = new List<TopStrategyResult>();

        foreach (BacktestRun backtest in topBacktests)
        {
            try
            {
                // Deserialize BacktestResult from JSON
                BacktestResult? backtestResult = JsonSerializer.Deserialize<BacktestResult>(backtest.ResultsJson);

                if (backtestResult != null)
                {
                    results.Add(new TopStrategyResult(
                        StrategyName: backtest.StrategyName,
                        Ticker: backtest.Ticker,
                        TotalReturn: backtestResult.Metrics.TotalReturnPercentage,
                        SharpeRatio: backtestResult.Metrics.SharpeRatio,
                        MaxDrawdown: backtestResult.Metrics.MaxDrawdownPercentage,
                        TotalTrades: backtestResult.Metrics.TotalTrades,
                        LastBacktestDate: backtest.ExecutedAt
                    ));
                }
            }
            catch (JsonException ex)
            {
                // Log error but continue processing other results
                Console.WriteLine($"Error deserializing backtest results for {backtest.StrategyName}: {ex.Message}");
            }
        }

        return results;
    }
}
