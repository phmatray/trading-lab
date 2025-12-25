using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for getting a portfolio snapshot with current market prices.
/// Orchestrates portfolio loading, price fetching, and valuation calculation.
/// </summary>
public class GetPortfolioSnapshotUseCase : IGetPortfolioSnapshotUseCase
{
    private readonly IPortfolioPort _portfolioPort;
    private readonly IMarketDataPort _marketDataPort;
    private readonly PortfolioValuationService _valuationService;

    public GetPortfolioSnapshotUseCase(
        IPortfolioPort portfolioPort,
        IMarketDataPort marketDataPort,
        PortfolioValuationService valuationService)
    {
        _portfolioPort = portfolioPort ?? throw new ArgumentNullException(nameof(portfolioPort));
        _marketDataPort = marketDataPort ?? throw new ArgumentNullException(nameof(marketDataPort));
        _valuationService = valuationService ?? throw new ArgumentNullException(nameof(valuationService));
    }

    /// <inheritdoc />
    public async Task<PortfolioSnapshot> ExecuteAsync(
        int portfolioId,
        IProgress<string>? progress = null)
    {
        progress?.Report("Loading portfolio...");

        // Load portfolio with positions
        var portfolio = await _portfolioPort.GetPortfolioByIdAsync(portfolioId);
        if (portfolio == null)
        {
            throw new InvalidOperationException($"Portfolio {portfolioId} not found");
        }

        // Handle empty portfolio (cash only)
        if (!portfolio.Positions.Any())
        {
            progress?.Report("Portfolio has no positions");
            return new PortfolioSnapshot(
                portfolio.Id,
                portfolio.Name,
                DateTime.UtcNow,
                portfolio.Cash,
                new List<PositionSnapshot>(),
                portfolio.Cash,
                portfolio.Cash,
                0m,
                0m);
        }

        progress?.Report("Fetching current market prices...");

        // Get unique tickers
        var tickers = portfolio.Positions.Select(p => p.Ticker).Distinct().ToList();
        var currentPrices = new Dictionary<string, decimal>();

        // Fetch current prices for each ticker
        foreach (string ticker in tickers)
        {
            progress?.Report($"Fetching price for {ticker}...");

            try
            {
                // Fetch recent data (last 7 days to ensure we get the latest price)
                var historicalData = await _marketDataPort.FetchHistoricalDataAsync(
                    ticker,
                    Domain.ValueObjects.TimeFrame.D1,
                    DateTime.Today.AddDays(-7),
                    DateTime.Today);

                if (historicalData.Any())
                {
                    // Get most recent closing price
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

        progress?.Report("Calculating portfolio valuation...");

        // Calculate snapshot using domain service
        var snapshot = _valuationService.CalculateSnapshot(portfolio, currentPrices);

        progress?.Report("Portfolio snapshot complete");

        return snapshot;
    }
}
