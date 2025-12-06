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
        progress?.Report("Initializing data fetch...");

        var ticker = await ResolveTicker(command.Ticker, command.Isin, progress);
        var (startDate, endDate, isUpToDate) = await DetermineDateRange(ticker, command, progress);

        if (isUpToDate)
        {
            progress?.Report("Database is up to date");
            return await _historicalDataPort.GetDataSummaryAsync(ticker);
        }

        var historicalData = await FetchMarketData(ticker, startDate, endDate, progress);

        if (!historicalData.Any())
        {
            progress?.Report("No new data available");
            return await _historicalDataPort.GetDataSummaryAsync(ticker);
        }

        await SaveData(ticker, command.Isin, historicalData, progress);

        progress?.Report("Data fetch complete");
        return await _historicalDataPort.GetDataSummaryAsync(ticker);
    }

    private async Task<string> ResolveTicker(string? ticker, string? isin, IProgress<string>? progress)
    {
        if (string.IsNullOrEmpty(isin))
            return ticker ?? throw new ArgumentException("Either ticker or ISIN must be provided");

        var possibleTickers = _tickerResolver.GetAllTickersForIsin(isin);

        if (possibleTickers == null || !possibleTickers.Any())
            throw new InvalidOperationException($"Could not resolve ISIN {isin} to Yahoo ticker");

        progress?.Report($"Found possible tickers: {string.Join(", ", possibleTickers)}");

        var workingTicker = await FindWorkingTicker(possibleTickers, progress);

        if (workingTicker == null)
        {
            throw new InvalidOperationException(
                "Could not fetch data with any available ticker. " +
                "This may be due to Yahoo Finance API rate limiting or the security not being available.");
        }

        return workingTicker;
    }

    private async Task<string?> FindWorkingTicker(IEnumerable<string> possibleTickers, IProgress<string>? progress)
    {
        foreach (var candidateTicker in possibleTickers)
        {
            progress?.Report($"Testing {candidateTicker}...");

            if (await IsTickerWorking(candidateTicker))
            {
                progress?.Report($"Successfully connected with {candidateTicker}");
                return candidateTicker;
            }
        }

        return null;
    }

    private async Task<bool> IsTickerWorking(string ticker)
    {
        try
        {
            var testData = await _marketDataPort.FetchHistoricalDataAsync(
                ticker,
                DateTime.Today.AddDays(-7),
                DateTime.Today);

            return testData.Any();
        }
        catch
        {
            return false;
        }
    }

    private async Task<(DateTime startDate, DateTime endDate, bool isUpToDate)> DetermineDateRange(
        string ticker,
        FetchDataCommand command,
        IProgress<string>? progress)
    {
        var latestDate = await _historicalDataPort.GetLatestDataDateAsync(ticker);
        var startDate = command.StartDate ?? latestDate?.AddDays(1) ?? new DateTime(2021, 12, 10);
        var endDate = command.EndDate ?? DateTime.Today;

        if (latestDate.HasValue)
        {
            progress?.Report($"Latest data in database: {latestDate:yyyy-MM-dd}");

            if (startDate > endDate)
                return (startDate, endDate, isUpToDate: true);

            progress?.Report($"Fetching new data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        }
        else
        {
            progress?.Report($"No existing data found. Fetching all historical data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        }

        return (startDate, endDate, isUpToDate: false);
    }

    private async Task<IReadOnlyList<Domain.Entities.HistoricalPrice>> FetchMarketData(
        string ticker,
        DateTime startDate,
        DateTime endDate,
        IProgress<string>? progress)
    {
        progress?.Report("Fetching data from Yahoo Finance...");
        var historicalData = await _marketDataPort.FetchHistoricalDataAsync(ticker, startDate, endDate);

        if (historicalData.Any())
            progress?.Report($"Retrieved {historicalData.Count} records from Yahoo Finance");

        return historicalData;
    }

    private async Task SaveData(
        string ticker,
        string? isin,
        IReadOnlyList<Domain.Entities.HistoricalPrice> historicalData,
        IProgress<string>? progress)
    {
        progress?.Report("Saving to database...");
        await _historicalDataPort.SaveHistoricalDataAsync(ticker, isin, historicalData);
        progress?.Report("Data saved successfully");
    }
}
