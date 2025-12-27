using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for getting a portfolio snapshot with current market prices.
/// Orchestrates portfolio loading, price fetching, and valuation calculation.
/// Uses Result pattern for consistent error handling without exceptions.
/// </summary>
public class GetPortfolioSnapshotUseCase : IGetPortfolioSnapshotUseCase
{
    private readonly IPortfolioPort _portfolioPort;
    private readonly IMarketDataPort _marketDataPort;
    private readonly MarketPriceService _priceService;
    private readonly PortfolioValuationService _valuationService;

    public GetPortfolioSnapshotUseCase(
        IPortfolioPort portfolioPort,
        IMarketDataPort marketDataPort,
        MarketPriceService priceService,
        PortfolioValuationService valuationService)
    {
        _portfolioPort = portfolioPort ?? throw new ArgumentNullException(nameof(portfolioPort));
        _marketDataPort = marketDataPort ?? throw new ArgumentNullException(nameof(marketDataPort));
        _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
        _valuationService = valuationService ?? throw new ArgumentNullException(nameof(valuationService));
    }

    /// <inheritdoc />
    public async Task<Result<PortfolioSnapshot>> ExecuteAsync(
        int portfolioId,
        IProgress<string>? progress = null)
    {
        progress?.Report("Loading portfolio...");

        // Load portfolio with positions
        Portfolio? portfolio = await _portfolioPort.GetPortfolioByIdAsync(portfolioId);
        if (portfolio == null)
        {
            return Result<PortfolioSnapshot>.Failure(
                Error.NotFound($"Portfolio {portfolioId} not found", "PORTFOLIO_NOT_FOUND"));
        }

        // Handle empty portfolio (cash only)
        if (!portfolio.Positions.Any())
        {
            progress?.Report("Portfolio has no positions");
            PortfolioSnapshot emptySnapshot = new PortfolioSnapshot(
                portfolio.Id,
                portfolio.Name,
                DateTime.UtcNow,
                portfolio.Cash,
                new List<PositionSnapshot>(),
                portfolio.Cash,
                portfolio.Cash,
                0m,
                0m);

            return Result<PortfolioSnapshot>.Success(emptySnapshot);
        }

        progress?.Report("Fetching current market prices...");

        // Get unique tickers
        List<string> tickers = portfolio.Positions.Select(p => p.Ticker).Distinct().ToList();

        // Fetch current prices using the centralized service
        Result<Dictionary<string, decimal>> priceResult = await _priceService.GetCurrentPricesAsync(
            tickers,
            _marketDataPort,
            progress);

        if (priceResult.IsFailure)
        {
            return Result<PortfolioSnapshot>.Failure(priceResult.Errors);
        }

        Dictionary<string, decimal> currentPrices = priceResult.Value;

        progress?.Report("Calculating portfolio valuation...");

        // Calculate snapshot using domain service
        Result<PortfolioSnapshot> result = _valuationService.CalculateSnapshot(portfolio, currentPrices);

        if (result.IsSuccess)
        {
            progress?.Report("Portfolio snapshot complete");
        }

        return result;
    }
}
