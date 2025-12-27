using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

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

    public async Task<Result<DataSummaryResult>> ExecuteAsync(
        FetchDataCommand command,
        IProgress<string>? progress = null)
    {
        try
        {
            // Default to D1 (daily) if no timeframe specified
            TimeFrame timeFrame = command.TimeFrame ?? Domain.ValueObjects.TimeFrame.D1;

            progress?.Report("Initializing data fetch...");

            var tickerResult = await ResolveTicker(command.Ticker, command.Isin, timeFrame, progress);
            if (tickerResult.IsFailure)
            {
                return Result<DataSummaryResult>.Failure(tickerResult.Errors);
            }

            string ticker = tickerResult.Value;
        (DateTime startDate, DateTime endDate, bool isUpToDate) = await DetermineDateRange(ticker, timeFrame, command, progress);

            if (isUpToDate)
            {
                progress?.Report("Database is up to date");
                DataSummaryResult summary = await _historicalDataPort.GetDataSummaryAsync(ticker, timeFrame);
                return Result<DataSummaryResult>.Success(summary);
            }

            IReadOnlyList<HistoricalPrice> historicalData = await FetchMarketData(ticker, timeFrame, startDate, endDate, progress);

            if (!historicalData.Any())
            {
                progress?.Report("No new data available");
                DataSummaryResult summary = await _historicalDataPort.GetDataSummaryAsync(ticker, timeFrame);
                return Result<DataSummaryResult>.Success(summary);
            }

            await SaveData(ticker, command.Isin, timeFrame, historicalData, progress);

            progress?.Report("Data fetch complete");
            DataSummaryResult finalSummary = await _historicalDataPort.GetDataSummaryAsync(ticker, timeFrame);
            return Result<DataSummaryResult>.Success(finalSummary);
        }
        catch (Exception ex)
        {
            return Result<DataSummaryResult>.Failure(
                Error.BusinessRule($"Failed to fetch historical data: {ex.Message}", "DATA_FETCH_FAILED"));
        }
    }

    private async Task<Result<string>> ResolveTicker(string? ticker, string? isin, TimeFrame timeFrame, IProgress<string>? progress)
    {
        if (string.IsNullOrEmpty(isin))
        {
            if (string.IsNullOrEmpty(ticker))
            {
                return Result<string>.Failure(
                    Error.Validation("Either ticker or ISIN must be provided", "TICKER_OR_ISIN_REQUIRED"));
            }
            return Result<string>.Success(ticker);
        }

        List<string>? possibleTickers = _tickerResolver.GetAllTickersForIsin(isin);

        if (possibleTickers == null || !possibleTickers.Any())
        {
            return Result<string>.Failure(
                Error.NotFound($"Could not resolve ISIN {isin} to Yahoo ticker", "ISIN_NOT_RESOLVED"));
        }

        progress?.Report($"Found possible tickers: {string.Join(", ", possibleTickers)}");

        string? workingTicker = await FindWorkingTicker(possibleTickers, timeFrame, progress);

        if (workingTicker == null)
        {
            return Result<string>.Failure(
                Error.BusinessRule(
                    "Could not fetch data with any available ticker. This may be due to Yahoo Finance API rate limiting or the security not being available.",
                    "NO_WORKING_TICKER"));
        }

        return Result<string>.Success(workingTicker);
    }

    private async Task<string?> FindWorkingTicker(IEnumerable<string> possibleTickers, TimeFrame timeFrame, IProgress<string>? progress)
    {
        foreach (string candidateTicker in possibleTickers)
        {
            progress?.Report($"Testing {candidateTicker}...");

            if (await IsTickerWorking(candidateTicker, timeFrame))
            {
                progress?.Report($"Successfully connected with {candidateTicker}");
                return candidateTicker;
            }
        }

        return null;
    }

    private async Task<bool> IsTickerWorking(string ticker, TimeFrame timeFrame)
    {
        try
        {
            IReadOnlyList<HistoricalPrice> testData = await _marketDataPort.FetchHistoricalDataAsync(
                ticker,
                timeFrame,
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
        TimeFrame timeFrame,
        FetchDataCommand command,
        IProgress<string>? progress)
    {
        DateTime? latestDate = await _historicalDataPort.GetLatestDataDateAsync(ticker, timeFrame);
        DateTime startDate = command.StartDate ?? latestDate?.AddDays(1) ?? new DateTime(2021, 12, 10);
        DateTime endDate = command.EndDate ?? DateTime.Today;

        if (latestDate.HasValue)
        {
            progress?.Report($"Latest data in database: {latestDate:yyyy-MM-dd}");

            if (startDate > endDate)
            {
                return (startDate, endDate, isUpToDate: true);
            }

            progress?.Report($"Fetching new data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        }
        else
        {
            progress?.Report($"No existing data found. Fetching all historical data from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        }

        return (startDate, endDate, isUpToDate: false);
    }

    private async Task<IReadOnlyList<HistoricalPrice>> FetchMarketData(
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate,
        IProgress<string>? progress)
    {
        progress?.Report("Fetching data from market data provider...");
        IReadOnlyList<HistoricalPrice> historicalData = await _marketDataPort.FetchHistoricalDataAsync(ticker, timeFrame, startDate, endDate);

        if (historicalData.Any())
        {
            progress?.Report($"Retrieved {historicalData.Count} records from market data provider");
        }

        return historicalData;
    }

    private async Task SaveData(
        string ticker,
        string? isin,
        TimeFrame timeFrame,
        IReadOnlyList<HistoricalPrice> historicalData,
        IProgress<string>? progress)
    {
        progress?.Report("Saving to database...");
        await _historicalDataPort.SaveHistoricalDataAsync(ticker, isin, timeFrame, historicalData);
        progress?.Report("Data saved successfully");
    }
}
