using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Tests.TestDoubles;

/// <summary>
/// In-memory implementation of IHistoricalDataPort for testing.
/// Perfect for unit tests - no actual database needed.
/// </summary>
public class InMemoryHistoricalDataRepository : IHistoricalDataPort
{
    // Dictionary key is (Ticker, TimeFrame) tuple
    private readonly Dictionary<(string ticker, TimeFrameUnit timeFrame), List<HistoricalPrice>> _data = new();

    public Task SaveHistoricalDataAsync(string ticker, string? isin, TimeFrame timeFrame, IEnumerable<HistoricalPrice> data)
    {
        (string ticker, TimeFrameUnit Unit) key = (ticker, timeFrame.Unit);
        if (!_data.ContainsKey(key))
        {
            _data[key] = new List<HistoricalPrice>();
        }

        var dataList = data.ToList();

        // Update ticker, ISIN, and TimeFrame for all entries
        foreach (HistoricalPrice price in dataList)
        {
            price.Ticker = ticker;
            price.ISIN = isin;
            price.TimeFrame = timeFrame.Unit;
            price.CreatedAt = DateTime.UtcNow;
        }

        // Filter out duplicates (same date and timeframe)
        var existingDates = _data[key].Select(p => p.DateTime).ToHashSet();
        var newRecords = dataList.Where(p => !existingDates.Contains(p.DateTime)).ToList();

        _data[key].AddRange(newRecords);
        _data[key] = _data[key].OrderBy(p => p.DateTime).ToList();

        return Task.CompletedTask;
    }

    public Task<DateTime?> GetLatestDataDateAsync(string ticker, TimeFrame timeFrame)
    {
        (string ticker, TimeFrameUnit Unit) key = (ticker, timeFrame.Unit);
        if (!_data.ContainsKey(key) || !_data[key].Any())
        {
            return Task.FromResult<DateTime?>(null);
        }

        return Task.FromResult<DateTime?>(_data[key].Max(p => p.DateTime));
    }

    public Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker, TimeFrame timeFrame)
    {
        (string ticker, TimeFrameUnit Unit) key = (ticker, timeFrame.Unit);
        if (!_data.ContainsKey(key))
        {
            return Task.FromResult(new List<HistoricalPrice>());
        }

        return Task.FromResult(_data[key].OrderBy(p => p.DateTime).ToList());
    }

    public Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker, TimeFrame timeFrame, DateTime start, DateTime end)
    {
        (string ticker, TimeFrameUnit Unit) key = (ticker, timeFrame.Unit);
        if (!_data.ContainsKey(key))
        {
            return Task.FromResult(new List<HistoricalPrice>());
        }

        var filtered = _data[key]
            .Where(p => p.DateTime >= start && p.DateTime <= end)
            .OrderBy(p => p.DateTime)
            .ToList();

        return Task.FromResult(filtered);
    }

    public Task<DataSummaryResult> GetDataSummaryAsync(string ticker, TimeFrame timeFrame)
    {
        (string ticker, TimeFrameUnit Unit) key = (ticker, timeFrame.Unit);
        if (!_data.ContainsKey(key) || !_data[key].Any())
        {
            return Task.FromResult(new DataSummaryResult(ticker, null, 0, 0, null, null, null, null, null));
        }

        List<HistoricalPrice> data = _data[key];
        string? isin = data.FirstOrDefault()?.ISIN;
        int totalRecords = data.Count;
        DateTime oldestDate = data.Min(p => p.DateTime);
        DateTime latestDate = data.Max(p => p.DateTime);
        decimal? minPrice = data.Where(p => p.Low.HasValue).Min(p => p.Low);
        decimal? maxPrice = data.Where(p => p.High.HasValue).Max(p => p.High);
        decimal? latestClose = data.OrderByDescending(p => p.DateTime).FirstOrDefault()?.Close;

        return Task.FromResult(new DataSummaryResult(
            ticker,
            isin,
            totalRecords,
            0,
            oldestDate,
            latestDate,
            minPrice,
            maxPrice,
            latestClose));
    }

    public Task<List<TimeFrame>> GetAvailableTimeFramesAsync(string ticker)
    {
        var timeFrames = _data.Keys
            .Where(k => k.ticker == ticker)
            .Select(k => new TimeFrame { Unit = k.timeFrame })
            .OrderBy(tf => tf.ToMinutes())
            .ToList();

        return Task.FromResult(timeFrames);
    }

    public Task<List<string>> GetAllTickersAsync()
    {
        var tickers = _data.Keys
            .Select(k => k.ticker)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        return Task.FromResult(tickers);
    }

    public async Task<Dictionary<string, DataSummaryResult>> GetDataSummariesAsync(
        IEnumerable<string> tickers,
        TimeFrame timeFrame)
    {
        var result = new Dictionary<string, DataSummaryResult>();

        foreach (string ticker in tickers)
        {
            DataSummaryResult summary = await GetDataSummaryAsync(ticker, timeFrame);
            result[ticker] = summary;
        }

        return result;
    }

    public async Task BulkSaveHistoricalDataAsync(
        Dictionary<string, (string? isin, IEnumerable<HistoricalPrice> data)> tickerDataMap,
        TimeFrame timeFrame,
        IProgress<BulkSaveProgress>? progress = null)
    {
        int totalTickers = tickerDataMap.Count;
        int completedTickers = 0;
        int totalRecordsSaved = 0;

        foreach (KeyValuePair<string, (string? isin, IEnumerable<HistoricalPrice> data)> kvp in tickerDataMap)
        {
            string ticker = kvp.Key;
            string? isin = kvp.Value.isin;
            IEnumerable<HistoricalPrice> data = kvp.Value.data;

            progress?.Report(new BulkSaveProgress(
                totalTickers,
                completedTickers,
                ticker,
                totalRecordsSaved));

            await SaveHistoricalDataAsync(ticker, isin, timeFrame, data);

            completedTickers++;
            totalRecordsSaved += data.Count();
        }

        progress?.Report(new BulkSaveProgress(
            totalTickers,
            completedTickers,
            string.Empty,
            totalRecordsSaved));
    }

    public Task<int> DeleteTickerDataAsync(string ticker, TimeFrame? timeFrame = null)
    {
        int deletedCount = 0;

        if (timeFrame is null)
        {
            // Delete all timeframes for this ticker
            var keysToRemove = _data.Keys
                .Where(k => k.ticker == ticker)
                .ToList();

            foreach ((string ticker, TimeFrameUnit timeFrame) key in keysToRemove)
            {
                deletedCount += _data[key].Count;
                _data.Remove(key);
            }
        }
        else
        {
            // Delete specific timeframe
            (string ticker, TimeFrameUnit Unit) key = (ticker, timeFrame.Unit);
            if (_data.ContainsKey(key))
            {
                deletedCount = _data[key].Count;
                _data.Remove(key);
            }
        }

        return Task.FromResult(deletedCount);
    }

    public Task<int> DeleteDateRangeAsync(
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate)
    {
        (string ticker, TimeFrameUnit Unit) key = (ticker, timeFrame.Unit);
        if (!_data.ContainsKey(key))
        {
            return Task.FromResult(0);
        }

        var recordsToKeep = _data[key]
            .Where(p => p.DateTime < startDate || p.DateTime > endDate)
            .ToList();

        int deletedCount = _data[key].Count - recordsToKeep.Count;
        _data[key] = recordsToKeep;

        return Task.FromResult(deletedCount);
    }

    public Task<List<TickerSummary>> GetAllTickerSummariesAsync(TimeFrame timeFrame)
    {
        var summaries = _data
            .Where(kvp => kvp.Key.timeFrame == timeFrame.Unit)
            .Select(kvp =>
            {
                List<HistoricalPrice> data = kvp.Value;
                return new TickerSummary(
                    kvp.Key.ticker,
                    data.FirstOrDefault()?.ISIN,
                    data.Count,
                    data.Any() ? data.Min(p => p.DateTime) : null,
                    data.Any() ? data.Max(p => p.DateTime) : null);
            })
            .OrderBy(t => t.Ticker)
            .ToList();

        return Task.FromResult(summaries);
    }

    public Task<DateTime?> GetDatabaseLastModifiedAsync()
    {
        if (!_data.Any())
        {
            return Task.FromResult<DateTime?>(null);
        }

        DateTime? maxCreatedAt = _data.Values
            .SelectMany(list => list)
            .Max(p => (DateTime?)p.CreatedAt);

        return Task.FromResult(maxCreatedAt);
    }

    // Test helper methods
    public void Clear() => _data.Clear();

    public void SeedData(string ticker, TimeFrameUnit timeFrame, List<HistoricalPrice> prices)
    {
        (string ticker, TimeFrameUnit timeFrame) key = (ticker, timeFrame);
        _data[key] = prices.OrderBy(p => p.DateTime).ToList();
    }
}
