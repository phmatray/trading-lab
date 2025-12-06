using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Tests.TestDoubles;

/// <summary>
/// In-memory implementation of IHistoricalDataPort for testing.
/// Perfect for unit tests - no actual database needed.
/// </summary>
public class InMemoryHistoricalDataRepository : IHistoricalDataPort
{
    private readonly Dictionary<string, List<HistoricalPrice>> _data = new();

    public Task SaveHistoricalDataAsync(string ticker, string? isin, IEnumerable<HistoricalPrice> data)
    {
        if (!_data.ContainsKey(ticker))
        {
            _data[ticker] = new List<HistoricalPrice>();
        }

        var dataList = data.ToList();

        // Update ticker and ISIN for all entries
        foreach (var price in dataList)
        {
            price.Ticker = ticker;
            price.ISIN = isin;
            price.CreatedAt = DateTime.UtcNow;
        }

        // Filter out duplicates (same date)
        var existingDates = _data[ticker].Select(p => p.DateTime).ToHashSet();
        var newRecords = dataList.Where(p => !existingDates.Contains(p.DateTime)).ToList();

        _data[ticker].AddRange(newRecords);
        _data[ticker] = _data[ticker].OrderBy(p => p.DateTime).ToList();

        return Task.CompletedTask;
    }

    public Task<DateTime?> GetLatestDataDateAsync(string ticker)
    {
        if (!_data.ContainsKey(ticker) || !_data[ticker].Any())
        {
            return Task.FromResult<DateTime?>(null);
        }

        return Task.FromResult<DateTime?>(_data[ticker].Max(p => p.DateTime));
    }

    public Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker)
    {
        if (!_data.ContainsKey(ticker))
        {
            return Task.FromResult(new List<HistoricalPrice>());
        }

        return Task.FromResult(_data[ticker].OrderBy(p => p.DateTime).ToList());
    }

    public Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker, DateTime start, DateTime end)
    {
        if (!_data.ContainsKey(ticker))
        {
            return Task.FromResult(new List<HistoricalPrice>());
        }

        var filtered = _data[ticker]
            .Where(p => p.DateTime >= start && p.DateTime <= end)
            .OrderBy(p => p.DateTime)
            .ToList();

        return Task.FromResult(filtered);
    }

    public Task<DataSummaryResult> GetDataSummaryAsync(string ticker)
    {
        if (!_data.ContainsKey(ticker) || !_data[ticker].Any())
        {
            return Task.FromResult(new DataSummaryResult(ticker, null, 0, 0, null, null, null, null, null));
        }

        var data = _data[ticker];
        var isin = data.FirstOrDefault()?.ISIN;
        var totalRecords = data.Count;
        var oldestDate = data.Min(p => p.DateTime);
        var latestDate = data.Max(p => p.DateTime);
        var minPrice = data.Where(p => p.Low.HasValue).Min(p => p.Low);
        var maxPrice = data.Where(p => p.High.HasValue).Max(p => p.High);
        var latestClose = data.OrderByDescending(p => p.DateTime).FirstOrDefault()?.Close;

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

    // Test helper methods
    public void Clear() => _data.Clear();

    public void SeedData(string ticker, List<HistoricalPrice> prices)
    {
        _data[ticker] = prices.OrderBy(p => p.DateTime).ToList();
    }
}
