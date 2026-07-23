using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for getting portfolio top bar metrics efficiently.
/// Calculates portfolio value, today's return (vs yesterday), and win rate.
/// </summary>
public class GetPortfolioTopBarMetricsUseCase : IGetPortfolioTopBarMetricsUseCase
{
    private readonly IPortfolioPort _portfolioPort;
    private readonly IMarketDataPort _marketDataPort;
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly MarketPriceService _priceService;
    private readonly PortfolioValuationService _valuationService;

    public GetPortfolioTopBarMetricsUseCase(
        IPortfolioPort portfolioPort,
        IMarketDataPort marketDataPort,
        IHistoricalDataPort historicalDataPort,
        MarketPriceService priceService,
        PortfolioValuationService valuationService)
    {
        _portfolioPort = portfolioPort ?? throw new ArgumentNullException(nameof(portfolioPort));
        _marketDataPort = marketDataPort ?? throw new ArgumentNullException(nameof(marketDataPort));
        _historicalDataPort = historicalDataPort ?? throw new ArgumentNullException(nameof(historicalDataPort));
        _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
        _valuationService = valuationService ?? throw new ArgumentNullException(nameof(valuationService));
    }

    /// <inheritdoc />
    public async Task<Result<TopBarMetrics>> ExecuteAsync(
        int portfolioId,
        IProgress<string>? progress = null)
    {
        try
        {
            progress?.Report("Loading portfolio...");

            // Load portfolio with positions
            Portfolio? portfolio = await _portfolioPort.GetPortfolioByIdAsync(portfolioId);
            if (portfolio is null)
            {
                return Result<TopBarMetrics>.Failure(
                    Error.NotFound($"Portfolio {portfolioId} not found", ErrorCodes.Portfolio.NotFound));
            }

            // Handle empty portfolio (cash only)
            if (!portfolio.Positions.Any())
            {
                return Result<TopBarMetrics>.Success(new TopBarMetrics(
                    totalValue: portfolio.Cash,
                    todayReturnDollars: 0m,
                    todayReturnPercentage: 0m,
                    winRatePercentage: 0m,
                    winningPositions: 0,
                    totalPositions: 0));
            }

            progress?.Report("Fetching current prices...");

            // Get unique tickers
            List<string> tickers = portfolio.Positions.Select(p => p.Ticker).Distinct().ToList();

            // Fetch current prices
            Result<Dictionary<string, decimal>> currentPriceResult = await _priceService.GetCurrentPricesAsync(
                tickers,
                _marketDataPort,
                progress);

            if (currentPriceResult.IsFailure)
            {
                return Result<TopBarMetrics>.Failure(currentPriceResult.Errors);
            }

            Dictionary<string, decimal> currentPrices = currentPriceResult.Value;

            progress?.Report("Calculating current portfolio value...");

            // Calculate current snapshot
            Result<PortfolioSnapshot> snapshotResult = _valuationService.CalculateSnapshot(portfolio, currentPrices);
            if (snapshotResult.IsFailure)
            {
                return Result<TopBarMetrics>.Failure(snapshotResult.Errors);
            }

            PortfolioSnapshot currentSnapshot = snapshotResult.Value;
            decimal currentValue = currentSnapshot.TotalValue;

            progress?.Report("Calculating win rate...");

            // Calculate win rate
            (decimal winRatePercentage, int winningPositions, int totalPositions) =
                _valuationService.CalculateWinRate(currentSnapshot);

            progress?.Report("Fetching yesterday's prices for return calculation...");

            // Calculate yesterday's date (handle weekends)
            DateTime yesterdayDate = GetPreviousTradingDay(DateTime.Today);

            // Fetch yesterday's prices from historical data
            Dictionary<string, decimal> yesterdayPrices = new();
            bool hasCompleteYesterdayData = true;

            foreach (string ticker in tickers)
            {
                List<HistoricalPrice> yesterdayData = await _historicalDataPort
                    .GetHistoricalDataAsync(ticker, TimeFrame.D1, yesterdayDate, yesterdayDate);

                if (yesterdayData.Count == 0 || !yesterdayData[0].Close.HasValue)
                {
                    hasCompleteYesterdayData = false;
                    break;
                }

                yesterdayPrices[ticker] = yesterdayData[0].Close!.Value;
            }

            decimal todayReturnDollars = 0m;
            decimal todayReturnPercentage = 0m;

            if (hasCompleteYesterdayData)
            {
                progress?.Report("Calculating today's return...");

                // Calculate yesterday's portfolio value
                decimal yesterdayValue = _valuationService.CalculateTotalValue(
                    portfolio.Cash,
                    portfolio.Positions,
                    yesterdayPrices);

                // Calculate today's return
                todayReturnDollars = currentValue - yesterdayValue;
                if (yesterdayValue > 0)
                {
                    todayReturnPercentage = (todayReturnDollars / yesterdayValue) * 100m;
                }
            }
            else
            {
                progress?.Report("Insufficient historical data for today's return calculation");
            }

            progress?.Report("Top bar metrics complete");

            return Result<TopBarMetrics>.Success(new TopBarMetrics(
                totalValue: currentValue,
                todayReturnDollars: todayReturnDollars,
                todayReturnPercentage: todayReturnPercentage,
                winRatePercentage: winRatePercentage,
                winningPositions: winningPositions,
                totalPositions: totalPositions));
        }
        catch (Exception ex)
        {
            return Result<TopBarMetrics>.Failure(
                Error.BusinessRule($"Failed to calculate top bar metrics: {ex.Message}", ErrorCodes.TopBar.MetricsCalculationFailed));
        }
    }

    /// <summary>
    /// Gets the previous trading day, handling weekends.
    /// </summary>
    /// <param name="date">The current date.</param>
    /// <returns>The previous trading day.</returns>
    private static DateTime GetPreviousTradingDay(DateTime date)
    {
        DateTime previousDay = date.AddDays(-1);

        // If Saturday (6) or Sunday (0), go back to Friday
        while (previousDay.DayOfWeek == DayOfWeek.Saturday || previousDay.DayOfWeek == DayOfWeek.Sunday)
        {
            previousDay = previousDay.AddDays(-1);
        }

        return previousDay;
    }
}
