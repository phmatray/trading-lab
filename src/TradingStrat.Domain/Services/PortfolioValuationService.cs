using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Domain service for calculating portfolio valuation and snapshots.
/// Pure business logic with no external dependencies.
/// </summary>
public class PortfolioValuationService
{
    /// <summary>
    /// Calculates a complete portfolio snapshot with current market prices.
    /// </summary>
    /// <param name="portfolio">The portfolio to value.</param>
    /// <param name="currentPrices">Dictionary of ticker to current market price.</param>
    /// <returns>Immutable portfolio snapshot.</returns>
    /// <exception cref="ArgumentNullException">If portfolio or currentPrices is null.</exception>
    /// <exception cref="InvalidOperationException">If current price is not available for a position.</exception>
    public PortfolioSnapshot CalculateSnapshot(
        Portfolio portfolio,
        Dictionary<string, decimal> currentPrices)
    {
        if (portfolio == null)
        {
            throw new ArgumentNullException(nameof(portfolio));
        }

        if (currentPrices == null)
        {
            throw new ArgumentNullException(nameof(currentPrices));
        }

        var positionSnapshots = new List<PositionSnapshot>();
        decimal totalMarketValue = portfolio.Cash;
        decimal totalCost = portfolio.Cash;

        foreach (var position in portfolio.Positions)
        {
            if (!currentPrices.TryGetValue(position.Ticker, out decimal currentPrice))
            {
                throw new InvalidOperationException(
                    $"No current price available for {position.Ticker}");
            }

            decimal marketValue = position.Quantity * currentPrice;
            decimal costBasis = position.Quantity * position.EntryPrice;
            decimal gainLoss = marketValue - costBasis;
            decimal gainLossPercent = costBasis > 0 ? (gainLoss / costBasis) * 100 : 0;

            positionSnapshots.Add(new PositionSnapshot(
                position.Ticker,
                position.Quantity,
                position.EntryPrice,
                currentPrice,
                marketValue,
                costBasis,
                gainLoss,
                gainLossPercent,
                0m  // Allocation calculated after total value is known
            ));

            totalMarketValue += marketValue;
            totalCost += costBasis;
        }

        // Calculate allocation percentages now that we know total value
        var updatedSnapshots = positionSnapshots
            .Select(p => p with
            {
                AllocationPercentage = totalMarketValue > 0
                    ? (p.MarketValue / totalMarketValue) * 100
                    : 0
            })
            .ToList();

        decimal totalGainLoss = totalMarketValue - totalCost;
        decimal totalGainLossPercent = totalCost > 0
            ? (totalGainLoss / totalCost) * 100
            : 0;

        return new PortfolioSnapshot(
            portfolio.Id,
            portfolio.Name,
            DateTime.UtcNow,
            portfolio.Cash,
            updatedSnapshots,
            totalMarketValue,
            totalCost,
            totalGainLoss,
            totalGainLossPercent
        );
    }

    /// <summary>
    /// Calculates the total value of a portfolio given current market prices.
    /// </summary>
    /// <param name="cash">Cash balance.</param>
    /// <param name="positions">List of positions.</param>
    /// <param name="currentPrices">Dictionary of ticker to current price.</param>
    /// <returns>Total portfolio value.</returns>
    public decimal CalculateTotalValue(
        decimal cash,
        IEnumerable<Position> positions,
        Dictionary<string, decimal> currentPrices)
    {
        decimal totalValue = cash;

        foreach (var position in positions)
        {
            if (currentPrices.TryGetValue(position.Ticker, out decimal currentPrice))
            {
                totalValue += position.Quantity * currentPrice;
            }
        }

        return totalValue;
    }
}
