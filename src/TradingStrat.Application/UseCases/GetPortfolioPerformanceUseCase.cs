using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for getting portfolio performance history and metrics.
/// Orchestrates historical data loading, daily valuation calculation, and performance metrics.
/// Uses BaseProgressUseCase to eliminate try-catch boilerplate.
/// </summary>
public class GetPortfolioPerformanceUseCase : BaseProgressUseCase<PortfolioPerformanceQuery, PortfolioPerformanceHistory>, IGetPortfolioPerformanceUseCase
{
    private readonly IPortfolioPort _portfolioPort;
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly IGetPortfolioSnapshotUseCase _snapshotUseCase;
    private readonly PortfolioValuationService _valuationService;
    private readonly PortfolioPerformanceService _performanceService;

    public GetPortfolioPerformanceUseCase(
        IPortfolioPort portfolioPort,
        IHistoricalDataPort historicalDataPort,
        IGetPortfolioSnapshotUseCase snapshotUseCase,
        PortfolioValuationService valuationService,
        PortfolioPerformanceService performanceService)
    {
        _portfolioPort = portfolioPort ?? throw new ArgumentNullException(nameof(portfolioPort));
        _historicalDataPort = historicalDataPort ?? throw new ArgumentNullException(nameof(historicalDataPort));
        _snapshotUseCase = snapshotUseCase ?? throw new ArgumentNullException(nameof(snapshotUseCase));
        _valuationService = valuationService ?? throw new ArgumentNullException(nameof(valuationService));
        _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
    }

    /// <inheritdoc />
    public Task<Result<PortfolioPerformanceHistory>> ExecuteAsync(
        PortfolioPerformanceQuery query,
        IProgress<string>? progress = null)
        => base.ExecuteAsync(query, progress, ExecuteCoreAsync, ErrorCodes.Portfolio.PerformanceFailed);

    private async Task<PortfolioPerformanceHistory> ExecuteCoreAsync(
        PortfolioPerformanceQuery query,
        IProgress<string>? progress)
    {
        progress?.Report("Loading portfolio...");

        // Load portfolio with positions
        Portfolio? portfolio = await _portfolioPort.GetPortfolioByIdAsync(query.PortfolioId);
        if (portfolio == null)
        {
            throw new InvalidOperationException($"Portfolio {query.PortfolioId} not found");
        }

        // Set date range (default to last year)
        DateTime startDate = query.StartDate ?? DateTime.Today.AddYears(-1);
        DateTime endDate = query.EndDate ?? DateTime.Today;

        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be before end date");
        }

        // Handle empty portfolio
        if (!portfolio.Positions.Any())
        {
            progress?.Report("Portfolio has no positions");

            Result<PortfolioSnapshot> snapshotResult = await _snapshotUseCase.ExecuteAsync(query.PortfolioId);

            if (snapshotResult.IsFailure)
            {
                throw new InvalidOperationException(snapshotResult.Errors.First().Message);
            }

            PortfolioSnapshot currentSnapshot = snapshotResult.Value;
            PortfolioMetrics emptyMetrics = _performanceService.CalculateMetrics(currentSnapshot);

            return new PortfolioPerformanceHistory(
                portfolio.Id,
                startDate,
                endDate,
                new List<PortfolioPerformancePoint>(),
                emptyMetrics);
        }

        progress?.Report("Loading historical price data...");

        // Get unique tickers
        var tickers = portfolio.Positions.Select(p => p.Ticker).Distinct().ToList();
        var allHistoricalData = new Dictionary<string, List<HistoricalPrice>>();

        // Load historical data for each ticker
        foreach (string ticker in tickers)
        {
            progress?.Report($"Loading data for {ticker}...");

            List<HistoricalPrice> data = await _historicalDataPort.GetHistoricalDataAsync(
                ticker,
                TimeFrame.D1,
                startDate,
                endDate);

            if (data.Any())
            {
                allHistoricalData[ticker] = data;
            }
            else
            {
                progress?.Report($"Warning: No historical data found for {ticker}");
            }
        }

        // Get all unique dates where we have complete data for all positions
        progress?.Report("Calculating daily portfolio values...");

        var allDates = allHistoricalData.Values
            .SelectMany(d => d.Select(p => p.DateTime.Date))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var performancePoints = new List<PortfolioPerformancePoint>();
        decimal previousValue = 0m;

        foreach (DateTime date in allDates)
        {
            // Build price dictionary for this date
            var pricesForDate = new Dictionary<string, decimal>();
            bool hasAllPrices = true;

            foreach (string ticker in tickers)
            {
                if (allHistoricalData.TryGetValue(ticker, out List<HistoricalPrice>? tickerData))
                {
                    HistoricalPrice? priceData = tickerData.FirstOrDefault(p => p.DateTime.Date == date);

                    if (priceData?.Close.HasValue == true)
                    {
                        pricesForDate[ticker] = priceData.Close.Value;
                    }
                    else
                    {
                        hasAllPrices = false;
                        break;
                    }
                }
                else
                {
                    hasAllPrices = false;
                    break;
                }
            }

            // Only calculate if we have prices for all positions
            if (hasAllPrices)
            {
                try
                {
                    Result<PortfolioSnapshot> result = _valuationService.CalculateSnapshot(portfolio, pricesForDate);

                    if (result.IsFailure)
                    {
                        progress?.Report($"Warning: Skipped {date:yyyy-MM-dd} - failed to calculate snapshot");
                        continue;
                    }

                    PortfolioSnapshot snapshot = result.Value;
                    decimal dailyReturn = previousValue > 0
                        ? (snapshot.TotalValue - previousValue) / previousValue
                        : 0m;

                    performancePoints.Add(new PortfolioPerformancePoint(
                        date,
                        snapshot.TotalValue,
                        snapshot.Cash,
                        snapshot.TotalValue - snapshot.Cash,
                        dailyReturn));

                    previousValue = snapshot.TotalValue;
                }
                catch (Exception ex)
                {
                    // Log or skip problematic dates
                    progress?.Report($"Warning: Skipped {date:yyyy-MM-dd} due to error: {ex.Message}");
                }
            }
        }

        progress?.Report("Calculating performance metrics...");

        // Get current snapshot for metrics
        Result<PortfolioSnapshot> metricsSnapshotResult = await _snapshotUseCase.ExecuteAsync(query.PortfolioId);

        if (metricsSnapshotResult.IsFailure)
        {
            throw new InvalidOperationException(metricsSnapshotResult.Errors.First().Message);
        }

        PortfolioSnapshot currentSnapshotForMetrics = metricsSnapshotResult.Value;

        // Calculate metrics with historical data
        PortfolioMetrics metrics = _performanceService.CalculateMetrics(
            currentSnapshotForMetrics,
            performancePoints);

        progress?.Report("Performance analysis complete");

        return new PortfolioPerformanceHistory(
            portfolio.Id,
            startDate,
            endDate,
            performancePoints,
            metrics);
    }
}
