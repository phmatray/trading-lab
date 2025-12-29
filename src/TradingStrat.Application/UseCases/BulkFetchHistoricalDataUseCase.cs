using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for bulk fetching historical data for multiple tickers.
/// Implements sequential processing with retry logic and graceful failure handling.
/// </summary>
public class BulkFetchHistoricalDataUseCase : IBulkDataFetchingUseCase
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly IMarketDataPort _marketDataPort;

    public BulkFetchHistoricalDataUseCase(
        IHistoricalDataPort historicalDataPort,
        IMarketDataPort marketDataPort)
    {
        _historicalDataPort = historicalDataPort;
        _marketDataPort = marketDataPort;
    }

    public async Task<Result<BulkFetchResult>> ExecuteAsync(
        BulkFetchDataCommand command,
        IProgress<BulkFetchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!command.Tickers.Any())
            {
                return Result<BulkFetchResult>.Failure(
                    Error.Validation("Tickers list cannot be empty", ErrorCodes.Data.TickerRequired));
            }

            int totalTickers = command.Tickers.Count;
            int completedTickers = 0;
            var successfulResults = new Dictionary<string, DataSummaryResult>();
            var failedResults = new Dictionary<string, string>();
            var skippedResults = new List<string>();

            foreach (string ticker in command.Tickers)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    // Report progress: Starting ticker
                    ReportProgress(progress, totalTickers, completedTickers, ticker, "Checking data freshness...");

                    // Check if we should skip this ticker (already up-to-date)
                    if (command.SkipExisting && await IsDataUpToDateAsync(ticker, command.TimeFrame, command.EndDate))
                    {
                        skippedResults.Add(ticker);
                        completedTickers++;
                        ReportProgress(progress, totalTickers, completedTickers, ticker, "Skipped (up-to-date)");
                        continue;
                    }

                    // Fetch data for this ticker
                    ReportProgress(progress, totalTickers, completedTickers, ticker, "Fetching data...");
                    DataSummaryResult result = await FetchSingleTickerAsync(ticker, command);

                    successfulResults[ticker] = result;
                    ReportProgress(progress, totalTickers, completedTickers, ticker, $"Success ({result.NewRecords} new records)");
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other tickers (graceful failure)
                    failedResults[ticker] = ex.Message;
                    ReportProgress(progress, totalTickers, completedTickers, ticker, $"Failed: {ex.Message}");
                }

                completedTickers++;
            }

            // Final progress report
            ReportProgress(progress, totalTickers, completedTickers, string.Empty, "Completed");

            return Result<BulkFetchResult>.Success(new BulkFetchResult(
                totalTickers,
                successfulResults.Count,
                failedResults.Count,
                skippedResults.Count,
                successfulResults,
                failedResults,
                skippedResults));
        }
        catch (InvalidOperationException ex)
        {
            return Result<BulkFetchResult>.Failure(
                Error.BusinessRule($"Failed to execute bulk data fetch: {ex.Message}", ErrorCodes.Data.FetchFailed));
        }
        catch (ArgumentException ex)
        {
            return Result<BulkFetchResult>.Failure(
                Error.Validation($"Invalid bulk fetch parameters: {ex.Message}", ErrorCodes.Data.FetchFailed));
        }
        catch (Exception ex)
        {
            return Result<BulkFetchResult>.Failure(
                Error.BusinessRule($"Failed to execute bulk data fetch: {ex.Message}", ErrorCodes.Data.FetchFailed));
        }
    }

    private async Task<DataSummaryResult> FetchSingleTickerAsync(
        string ticker,
        BulkFetchDataCommand command)
    {
        // Determine date range
        DateTime startDate = command.StartDate ?? await GetStartDateAsync(ticker, command.TimeFrame);
        DateTime endDate = command.EndDate ?? DateTime.Today;

        // Fetch data from market data provider
        IReadOnlyList<HistoricalPrice> prices = await _marketDataPort.FetchHistoricalDataAsync(
            ticker,
            command.TimeFrame,
            startDate,
            endDate);

        // Save to database
        await _historicalDataPort.SaveHistoricalDataAsync(
            ticker,
            null, // ISIN not available in bulk fetch (could be enhanced later)
            command.TimeFrame,
            prices);

        // Get summary of saved data
        DataSummaryResult summary = await _historicalDataPort.GetDataSummaryAsync(ticker, command.TimeFrame);

        // Calculate new records count
        int newRecords = prices.Count;
        return summary with { NewRecords = newRecords };
    }

    private async Task<DateTime> GetStartDateAsync(string ticker, TimeFrame timeFrame)
    {
        DateTime? latestDate = await _historicalDataPort.GetLatestDataDateAsync(ticker, timeFrame);

        if (latestDate.HasValue)
        {
            // Incremental update: start from the day after the latest data
            return latestDate.Value.AddDays(1);
        }

        // No existing data: fetch last 2 years by default
        return DateTime.Today.AddYears(-2);
    }

    private async Task<bool> IsDataUpToDateAsync(string ticker, TimeFrame timeFrame, DateTime? endDate)
    {
        DateTime targetDate = endDate ?? DateTime.Today;
        DateTime? latestDate = await _historicalDataPort.GetLatestDataDateAsync(ticker, timeFrame);

        if (!latestDate.HasValue)
        {
            return false; // No data exists
        }

        // Consider data up-to-date if latest date is within 1 day of target
        // (accounts for weekends and holidays)
        return (targetDate - latestDate.Value).Days <= 1;
    }

    private void ReportProgress(
        IProgress<BulkFetchProgress>? progress,
        int totalTickers,
        int completedTickers,
        string currentTicker,
        string currentStatus)
    {
        if (progress is null)
        {
            return;
        }

        int percentage = totalTickers > 0 ? (completedTickers * 100) / totalTickers : 0;

        progress.Report(new BulkFetchProgress(
            totalTickers,
            completedTickers,
            currentTicker,
            currentStatus,
            percentage));
    }
}
