using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for calculating portfolio rebalancing plan.
/// Orchestrates snapshot retrieval, price fetching for target tickers, and rebalancing calculation.
/// </summary>
public class CalculateRebalancingUseCase : ICalculateRebalancingUseCase
{
    private readonly IGetPortfolioSnapshotUseCase _snapshotUseCase;
    private readonly IMarketDataPort _marketDataPort;
    private readonly PortfolioRebalancingService _rebalancingService;

    public CalculateRebalancingUseCase(
        IGetPortfolioSnapshotUseCase snapshotUseCase,
        IMarketDataPort marketDataPort,
        PortfolioRebalancingService rebalancingService)
    {
        _snapshotUseCase = snapshotUseCase ?? throw new ArgumentNullException(nameof(snapshotUseCase));
        _marketDataPort = marketDataPort ?? throw new ArgumentNullException(nameof(marketDataPort));
        _rebalancingService = rebalancingService ?? throw new ArgumentNullException(nameof(rebalancingService));
    }

    /// <inheritdoc />
    public async Task<RebalancingPlan> ExecuteAsync(
        RebalancingCommand command,
        IProgress<string>? progress = null)
    {
        // Validate input
        if (!command.TargetWeights.IsValid())
        {
            throw new ArgumentException("Target allocations must sum to 100%", nameof(command));
        }

        progress?.Report("Getting current portfolio snapshot...");

        // Get current portfolio snapshot (with current prices for existing positions)
        var snapshot = await _snapshotUseCase.ExecuteAsync(
            command.PortfolioId,
            progress);

        progress?.Report("Fetching prices for target positions...");

        // Build complete price dictionary (existing positions + new target tickers)
        var currentPrices = snapshot.Positions.ToDictionary(
            p => p.Ticker,
            p => p.CurrentPrice);

        // Fetch prices for any target tickers not already in the portfolio
        var targetTickers = command.TargetWeights.TargetPercentages.Keys.ToList();
        var newTickers = targetTickers.Except(currentPrices.Keys).ToList();

        foreach (string ticker in newTickers)
        {
            progress?.Report($"Fetching price for {ticker}...");

            try
            {
                var historicalData = await _marketDataPort.FetchHistoricalDataAsync(
                    ticker,
                    Domain.ValueObjects.TimeFrame.D1,
                    DateTime.Today.AddDays(-7),
                    DateTime.Today);

                if (historicalData.Any())
                {
                    var latestPrice = historicalData
                        .OrderByDescending(p => p.DateTime)
                        .First();

                    if (latestPrice.Close.HasValue)
                    {
                        currentPrices[ticker] = latestPrice.Close.Value;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"No closing price available for {ticker}");
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unable to fetch price data for {ticker}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to fetch price for {ticker}: {ex.Message}", ex);
            }
        }

        progress?.Report("Calculating rebalancing plan...");

        // Calculate rebalancing plan using domain service
        var plan = _rebalancingService.CalculateRebalancing(
            snapshot,
            command.TargetWeights,
            currentPrices,
            command.CommissionPercentage,
            command.MinimumCommission);

        progress?.Report("Rebalancing calculation complete");

        return plan;
    }
}
