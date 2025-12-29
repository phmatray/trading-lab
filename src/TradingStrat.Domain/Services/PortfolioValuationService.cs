using TradingStrat.Domain.Common;
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
    /// <returns>Result containing immutable portfolio snapshot or errors.</returns>
    public Result<PortfolioSnapshot> CalculateSnapshot(
        Portfolio? portfolio,
        Dictionary<string, decimal>? currentPrices)
    {
        List<Error> errors = new();

        if (portfolio is null)
        {
            errors.Add(Error.Validation("Portfolio is required", "PORTFOLIO_REQUIRED"));
        }

        if (currentPrices is null)
        {
            errors.Add(Error.Validation("Current prices are required", "CURRENT_PRICES_REQUIRED"));
        }

        if (errors.Any())
        {
            return Result<PortfolioSnapshot>.Failure(errors);
        }

        // Safe to use non-null assertion here because validation above ensures non-null
        var positionSnapshots = new List<PositionSnapshot>();
        decimal totalMarketValue = portfolio!.Cash;
        decimal totalCost = portfolio.Cash;

        foreach (Position position in portfolio.Positions)
        {
            if (!currentPrices!.TryGetValue(position.Ticker, out decimal currentPrice))
            {
                return Result<PortfolioSnapshot>.Failure(
                    Error.InsufficientData($"No current price available for {position.Ticker}"));
            }

            // Use enriched Position entity's domain behaviors
            decimal marketValue = position.CalculateMarketValue(currentPrice);
            decimal costBasis = position.CalculateCostBasis();
            decimal gainLoss = position.CalculateUnrealizedGainLoss(currentPrice);
            decimal gainLossPercent = position.CalculateGainLossPercentage(currentPrice);

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
            .Select(p => new PositionSnapshot(
                p.Ticker,
                p.Quantity,
                p.EntryPrice,
                p.CurrentPrice,
                p.MarketValue,
                p.CostBasis,
                p.UnrealizedGainLoss,
                p.UnrealizedGainLossPercentage,
                totalMarketValue > 0
                    ? (p.MarketValue / totalMarketValue) * 100
                    : 0))
            .ToList();

        decimal totalGainLoss = totalMarketValue - totalCost;
        decimal totalGainLossPercent = totalCost > 0
            ? (totalGainLoss / totalCost) * 100
            : 0;

        PortfolioSnapshot snapshot = new(
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

        return Result<PortfolioSnapshot>.Success(snapshot);
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

        foreach (Position position in positions)
        {
            if (currentPrices.TryGetValue(position.Ticker, out decimal currentPrice))
            {
                // Use enriched Position entity's domain behavior
                totalValue += position.CalculateMarketValue(currentPrice);
            }
        }

        return totalValue;
    }

    /// <summary>
    /// Calculates win rate from a portfolio snapshot.
    /// Win rate = percentage of positions with unrealized gains > 0.
    /// </summary>
    /// <param name="snapshot">The portfolio snapshot.</param>
    /// <returns>Tuple containing win rate percentage, winning positions count, and total positions count.</returns>
    public (decimal WinRatePercentage, int WinningPositions, int TotalPositions) CalculateWinRate(
        PortfolioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        int totalPositions = snapshot.Positions.Count;
        if (totalPositions == 0)
        {
            return (0m, 0, 0);
        }

        int winningPositions = snapshot.Positions
            .Count(p => p.UnrealizedGainLoss > 0);

        decimal winRatePercentage = (decimal)winningPositions / totalPositions * 100m;

        return (winRatePercentage, winningPositions, totalPositions);
    }
}
