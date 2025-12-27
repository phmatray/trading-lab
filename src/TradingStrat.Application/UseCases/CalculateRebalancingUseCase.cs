using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Common;
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
    private readonly MarketPriceService _priceService;
    private readonly PortfolioRebalancingService _rebalancingService;

    public CalculateRebalancingUseCase(
        IGetPortfolioSnapshotUseCase snapshotUseCase,
        IMarketDataPort marketDataPort,
        MarketPriceService priceService,
        PortfolioRebalancingService rebalancingService)
    {
        _snapshotUseCase = snapshotUseCase ?? throw new ArgumentNullException(nameof(snapshotUseCase));
        _marketDataPort = marketDataPort ?? throw new ArgumentNullException(nameof(marketDataPort));
        _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
        _rebalancingService = rebalancingService ?? throw new ArgumentNullException(nameof(rebalancingService));
    }

    /// <inheritdoc />
    public async Task<Result<RebalancingPlan>> ExecuteAsync(
        RebalancingCommand command,
        IProgress<string>? progress = null)
    {
        // Command validation happens in constructor - command is guaranteed to be valid here

        // Validate target weights sum to 100%
        if (!command.TargetWeights.IsValid())
        {
            return Result<RebalancingPlan>.Failure(
                Error.Validation("Target allocations must sum to 100%", "INVALID_TARGET_WEIGHTS"));
        }

        progress?.Report("Getting current portfolio snapshot...");

        // Get current portfolio snapshot (with current prices for existing positions)
        Result<PortfolioSnapshot> snapshotResult = await _snapshotUseCase.ExecuteAsync(
            command.PortfolioId,
            progress);

        if (snapshotResult.IsFailure)
        {
            return Result<RebalancingPlan>.Failure(snapshotResult.Errors);
        }

        PortfolioSnapshot snapshot = snapshotResult.Value;

        progress?.Report("Fetching prices for target positions...");

        // Build complete price dictionary (existing positions + new target tickers)
        Dictionary<string, decimal> currentPrices = snapshot.Positions.ToDictionary(
            p => p.Ticker,
            p => p.CurrentPrice);

        // Fetch prices for any target tickers not already in the portfolio
        var targetTickers = command.TargetWeights.TargetPercentages.Keys.ToList();
        var newTickers = targetTickers.Except(currentPrices.Keys).ToList();

        if (newTickers.Any())
        {
            // Fetch prices for new tickers using the centralized service
            Result<Dictionary<string, decimal>> priceResult = await _priceService.GetCurrentPricesAsync(
                newTickers,
                _marketDataPort,
                progress);

            if (priceResult.IsFailure)
            {
                return Result<RebalancingPlan>.Failure(priceResult.Errors);
            }

            // Merge new prices into the current prices dictionary
            foreach ((string ticker, decimal price) in priceResult.Value)
            {
                currentPrices[ticker] = price;
            }
        }

        progress?.Report("Calculating rebalancing plan...");

        // Calculate rebalancing plan using domain service
        RebalancingPlan plan = _rebalancingService.CalculateRebalancing(
            snapshot,
            command.TargetWeights,
            currentPrices,
            command.CommissionPercentage,
            command.MinimumCommission);

        progress?.Report("Rebalancing calculation complete");

        return Result<RebalancingPlan>.Success(plan);
    }
}
