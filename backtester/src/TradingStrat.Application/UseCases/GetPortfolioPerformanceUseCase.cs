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
        if (portfolio is null)
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
            Dictionary<string, decimal>? pricesForDate = GetPricesForDate(date, allHistoricalData, tickers);

            if (pricesForDate is null)
            {
                continue; // Skip dates with incomplete price data
            }

            PortfolioPerformancePoint? point = CalculatePerformancePointForDate(
                date,
                portfolio,
                pricesForDate,
                previousValue,
                progress);

            if (point is not null)
            {
                performancePoints.Add(point);
                previousValue = point.TotalValue;
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

    /// <summary>
    /// Gets prices for all tickers on a specific date.
    /// Returns null if any ticker is missing price data for the date.
    /// </summary>
    private static Dictionary<string, decimal>? GetPricesForDate(
        DateTime date,
        Dictionary<string, List<HistoricalPrice>> allHistoricalData,
        List<string> tickers)
    {
        var pricesForDate = new Dictionary<string, decimal>();

        foreach (string ticker in tickers)
        {
            if (!allHistoricalData.TryGetValue(ticker, out List<HistoricalPrice>? tickerData))
            {
                return null; // Missing ticker data
            }

            HistoricalPrice? priceData = tickerData.FirstOrDefault(p => p.DateTime.Date == date);

            if (priceData?.Close.HasValue != true)
            {
                return null; // Missing price for this date
            }

            pricesForDate[ticker] = priceData.Close!.Value;
        }

        return pricesForDate;
    }

    /// <summary>
    /// Calculates a performance point for a specific date.
    /// Returns null if calculation fails.
    /// </summary>
    private PortfolioPerformancePoint? CalculatePerformancePointForDate(
        DateTime date,
        Portfolio portfolio,
        Dictionary<string, decimal> pricesForDate,
        decimal previousValue,
        IProgress<string>? progress)
    {
        try
        {
            Result<PortfolioSnapshot> result = _valuationService.CalculateSnapshot(portfolio, pricesForDate);

            if (result.IsFailure)
            {
                progress?.Report($"Warning: Skipped {date:yyyy-MM-dd} - failed to calculate snapshot");
                return null;
            }

            PortfolioSnapshot snapshot = result.Value;
            decimal dailyReturn = previousValue > 0
                ? (snapshot.TotalValue - previousValue) / previousValue
                : 0m;

            return new PortfolioPerformancePoint(
                date,
                snapshot.TotalValue,
                snapshot.Cash,
                snapshot.TotalValue - snapshot.Cash,
                dailyReturn);
        }
        catch (Exception ex)
        {
            progress?.Report($"Warning: Skipped {date:yyyy-MM-dd} due to error: {ex.Message}");
            return null;
        }
    }
}
