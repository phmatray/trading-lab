using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;

namespace TradingStrat.Application.UseCases;

public class FetchHistoricalDataUseCase : IDataFetchingUseCase
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly IMarketDataPort _marketDataPort;
    private readonly ITickerResolver _tickerResolver;

    public FetchHistoricalDataUseCase(
        IHistoricalDataPort historicalDataPort,
        IMarketDataPort marketDataPort,
        ITickerResolver tickerResolver)
    {
        _historicalDataPort = historicalDataPort;
        _marketDataPort = marketDataPort;
        _tickerResolver = tickerResolver;
    }

    public async Task<DataSummaryResult> ExecuteAsync(
        FetchDataCommand command,
        IProgress<string>? progress = null)
    {
        var ticker = command.Ticker;
        var isin = command.Isin;

        progress?.Report("Initializing data fetch...");

        // If ISIN is provided, resolve to tickers and test connections
        if (!string.IsNullOrEmpty(isin))
        {
            var possibleTickers = _tickerResolver.GetAllTickersForIsin(isin);

            if (possibleTickers == null || !possibleTickers.Any())
            {
                throw new InvalidOperationException($"Could not resolve ISIN {isin} to Yahoo ticker");
            }

            progress?.Report($"Found possible tickers: {string.Join(", ", possibleTickers)}");

            // Test each ticker to find working one
            string? workingTicker = null;
            foreach (var candidateTicker in possibleTickers)
            {
                progress?.Report($"Testing {candidateTicker}...");
                try
                {
                    var testData = await _marketDataPort.FetchHistoricalDataAsync(
                        candidateTicker,
                        DateTime.Today.AddDays(-7),
                        DateTime.Today);

                    if (testData.Any())
                    {
                        workingTicker = candidateTicker;
                        progress?.Report($"Successfully connected with {workingTicker}");
                        break;
                    }
                }
                catch
                {
                    // Continue to next ticker
                }
            }

            if (workingTicker == null)
            {
                throw new InvalidOperationException(
                    "Could not fetch data with any available ticker. " +
                    "This may be due to Yahoo Finance API rate limiting or the security not being available.");
            }

            ticker = workingTicker;
        }

        // Determine date range
        var latestDate = await _historicalDataPort.GetLatestDataDateAsync(ticker);
        var startDate = command.StartDate ?? latestDate?.AddDays(1) ?? new DateTime(2021, 12, 10);
        var endDate = command.EndDate ?? DateTime.Today;

        if (latestDate.HasValue)
        {
            progress?.Report($"Latest data in database: {latestDate:yyyy-MM-dd}");

            if (startDate > endDate)
            {
                progress?.Report("Database is up to date");
                var existingSummary = await _historicalDataPort.GetDataSummaryAsync(ticker);
                return existingSummary;
            }

            progress?.Report($"Fetching new data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        }
        else
        {
            progress?.Report($"No existing data found. Fetching all historical data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        }

        // Fetch historical data
        progress?.Report("Fetching data from Yahoo Finance...");
        var historicalData = await _marketDataPort.FetchHistoricalDataAsync(ticker, startDate, endDate);

        if (!historicalData.Any())
        {
            progress?.Report("No new data available");
            var existingSummary = await _historicalDataPort.GetDataSummaryAsync(ticker);
            return existingSummary;
        }

        progress?.Report($"Retrieved {historicalData.Count} records from Yahoo Finance");

        // Save to database
        progress?.Report("Saving to database...");
        await _historicalDataPort.SaveHistoricalDataAsync(ticker, isin, historicalData);

        progress?.Report("Data saved successfully");

        // Get summary
        var summary = await _historicalDataPort.GetDataSummaryAsync(ticker);

        progress?.Report("Data fetch complete");

        return summary;
    }
}
