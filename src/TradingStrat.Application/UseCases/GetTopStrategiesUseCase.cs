using System.Text.Json;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for retrieving top performing strategies based on backtest results.
/// </summary>
public class GetTopStrategiesUseCase : IGetTopStrategiesUseCase
{
    private readonly IBacktestArchivePort _backtestArchivePort;

    public GetTopStrategiesUseCase(IBacktestArchivePort backtestArchivePort)
    {
        _backtestArchivePort = backtestArchivePort;
    }

    public async Task<Result<List<TopStrategyResult>>> ExecuteAsync(int limit = 5)
    {
        try
        {
            var topBacktests = await _backtestArchivePort.GetTopBacktestRunsAsync(limit);

            var results = new List<TopStrategyResult>();

            foreach (var backtest in topBacktests)
            {
                try
                {
                    // Deserialize BacktestResult from JSON
                    var backtestResult = JsonSerializer.Deserialize<BacktestResult>(backtest.ResultsJson);

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

            return Result<List<TopStrategyResult>>.Success(results);
        }
        catch (Exception ex)
        {
            return Result<List<TopStrategyResult>>.Failure(
                Error.BusinessRule($"Failed to retrieve top strategies: {ex.Message}", "TOP_STRATEGIES_FAILED"));
        }
    }
}
