using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Domain service for calculating portfolio rebalancing signals.
/// Pure business logic with no external dependencies.
/// </summary>
public class PortfolioRebalancingService
{
    /// <summary>
    /// Calculates a complete rebalancing plan to reach target allocation weights.
    /// </summary>
    /// <param name="currentSnapshot">Current portfolio snapshot.</param>
    /// <param name="targetWeights">Target allocation weights.</param>
    /// <param name="currentPrices">Dictionary of ticker to current market price.</param>
    /// <param name="commissionPercentage">Commission rate as a decimal (e.g., 0.001 for 0.1%).</param>
    /// <param name="minimumCommission">Minimum commission amount.</param>
    /// <returns>Complete rebalancing plan.</returns>
    /// <exception cref="ArgumentException">If target weights are invalid.</exception>
    /// <exception cref="InvalidOperationException">If price is not available for a target ticker.</exception>
    public RebalancingPlan CalculateRebalancing(
        PortfolioSnapshot currentSnapshot,
        AllocationWeights targetWeights,
        Dictionary<string, decimal> currentPrices,
        decimal commissionPercentage,
        decimal minimumCommission)
    {
        if (!targetWeights.IsValid())
        {
            throw new ArgumentException("Target allocations must sum to 100%", nameof(targetWeights));
        }

        var signals = new List<RebalancingSignal>();
        decimal totalValue = currentSnapshot.TotalValue;
        decimal requiredCash = 0m;

        // Calculate signals for each target ticker
        foreach (var (ticker, targetPercent) in targetWeights.TargetPercentages)
        {
            if (!currentPrices.TryGetValue(ticker, out decimal currentPrice))
            {
                throw new InvalidOperationException(
                    $"No current price available for {ticker}");
            }

            decimal targetValue = totalValue * (targetPercent / 100m);
            int targetQuantity = (int)(targetValue / currentPrice);

            var currentPosition = currentSnapshot.Positions
                .FirstOrDefault(p => p.Ticker == ticker);
            int currentQuantity = currentPosition?.Quantity ?? 0;
            decimal currentAllocation = currentPosition?.AllocationPercentage ?? 0m;

            int quantityDelta = targetQuantity - currentQuantity;
            var action = quantityDelta > 0 ? RebalancingAction.Buy
                       : quantityDelta < 0 ? RebalancingAction.Sell
                       : RebalancingAction.Hold;

            decimal grossCost = Math.Abs(quantityDelta) * currentPrice;
            decimal commission = Math.Max(
                grossCost * commissionPercentage,
                minimumCommission);

            decimal estimatedCost = action == RebalancingAction.Buy
                ? grossCost + commission
                : action == RebalancingAction.Sell
                    ? -(grossCost - commission)
                    : 0;

            if (action == RebalancingAction.Buy)
            {
                requiredCash += estimatedCost;
            }

            signals.Add(new RebalancingSignal(
                ticker,
                action,
                currentQuantity,
                targetQuantity,
                quantityDelta,
                currentAllocation,
                targetPercent,
                estimatedCost
            ));
        }

        // Handle positions that need to be sold but are not in target allocation
        foreach (var position in currentSnapshot.Positions)
        {
            if (!targetWeights.TargetPercentages.ContainsKey(position.Ticker))
            {
                if (!currentPrices.TryGetValue(position.Ticker, out decimal currentPrice))
                {
                    throw new InvalidOperationException(
                        $"No current price available for {position.Ticker}");
                }

                decimal grossCost = position.Quantity * currentPrice;
                decimal commission = Math.Max(
                    grossCost * commissionPercentage,
                    minimumCommission);

                signals.Add(new RebalancingSignal(
                    position.Ticker,
                    RebalancingAction.Sell,
                    position.Quantity,
                    0,
                    -position.Quantity,
                    position.AllocationPercentage,
                    0m,
                    -(grossCost - commission)
                ));
            }
        }

        bool isExecutable = currentSnapshot.Cash >= requiredCash;

        return new RebalancingPlan(
            currentSnapshot.PortfolioId,
            DateTime.UtcNow,
            signals.OrderByDescending(s => Math.Abs(s.EstimatedCost)).ToList(),
            requiredCash,
            currentSnapshot.Cash,
            isExecutable
        );
    }
}
