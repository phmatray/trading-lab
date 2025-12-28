using Microsoft.EntityFrameworkCore;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Infrastructure.Persistence.EfCore;

public class HistoricalDataRepository : IHistoricalDataPort
{
    private readonly TradingContext _context;

    public HistoricalDataRepository(TradingContext context)
    {
        _context = context;
    }

    public async Task SaveHistoricalDataAsync(string ticker, string? isin, TimeFrame timeFrame, IEnumerable<HistoricalPrice> data)
    {
        var dataList = data.ToList();
        if (!dataList.Any())
        {
            return;
        }

        // Update ticker, ISIN, and TimeFrame for all entries
        foreach (HistoricalPrice price in dataList)
        {
            price.Ticker = ticker;
            price.ISIN = isin;
            price.TimeFrame = timeFrame.Unit;
            price.CreatedAt = DateTime.UtcNow;
        }

        // Get existing dates to filter out duplicates (now includes TimeFrame in uniqueness check)
        var dates = dataList.Select(p => p.DateTime).ToList();
        List<DateTime> existingDates = await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker && p.TimeFrame == timeFrame.Unit && dates.Contains(p.DateTime))
            .Select(p => p.DateTime)
            .ToListAsync();

        // Only add new records
        var newRecords = dataList
            .Where(p => !existingDates.Contains(p.DateTime))
            .ToList();

        if (newRecords.Any())
        {
            await _context.HistoricalPrices.AddRangeAsync(newRecords);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<DateTime?> GetLatestDataDateAsync(string ticker, TimeFrame timeFrame)
    {
        return await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker && p.TimeFrame == timeFrame.Unit)
            .MaxAsync(p => (DateTime?)p.DateTime);
    }

    public async Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker, TimeFrame timeFrame)
    {
        return await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker && p.TimeFrame == timeFrame.Unit)
            .OrderBy(p => p.DateTime)
            .ToListAsync();
    }

    public async Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker, TimeFrame timeFrame, DateTime start, DateTime end)
    {
        return await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker && p.TimeFrame == timeFrame.Unit && p.DateTime >= start && p.DateTime <= end)
            .OrderBy(p => p.DateTime)
            .ToListAsync();
    }

    public async Task<DataSummaryResult> GetDataSummaryAsync(string ticker, TimeFrame timeFrame)
    {
        List<HistoricalPrice> data = await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker && p.TimeFrame == timeFrame.Unit)
            .ToListAsync();

        if (!data.Any())
        {
            return new DataSummaryResult(ticker, null, 0, 0, null, null, null, null, null);
        }

        string? isin = data.FirstOrDefault()?.ISIN;
        int totalRecords = data.Count;
        DateTime oldestDate = data.Min(p => p.DateTime);
        DateTime latestDate = data.Max(p => p.DateTime);
        decimal? minPrice = data.Where(p => p.Low.HasValue).Min(p => p.Low);
        decimal? maxPrice = data.Where(p => p.High.HasValue).Max(p => p.High);
        decimal? latestClose = data
            .OrderByDescending(p => p.DateTime)
            .FirstOrDefault()?.Close;

        return new DataSummaryResult(
            ticker,
            isin,
            totalRecords,
            0, // NewRecords will be set by use case
            oldestDate,
            latestDate,
            minPrice,
            maxPrice,
            latestClose);
    }

    public async Task<List<TimeFrame>> GetAvailableTimeFramesAsync(string ticker)
    {
        List<TimeFrameUnit> timeFrameUnits = await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker)
            .Select(p => p.TimeFrame)
            .Distinct()
            .ToListAsync();

        return timeFrameUnits
            .Select(unit => new TimeFrame { Unit = unit })
            .OrderBy(tf => tf.ToMinutes())
            .ToList();
    }

    public async Task<List<string>> GetAllTickersAsync()
    {
        return await _context.HistoricalPrices
            .Select(p => p.Ticker)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<Dictionary<string, DataSummaryResult>> GetDataSummariesAsync(
        IEnumerable<string> tickers,
        TimeFrame timeFrame)
    {
        var tickerList = tickers.ToList();
        var result = new Dictionary<string, DataSummaryResult>();

        foreach (string ticker in tickerList)
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

        // Report final progress
        progress?.Report(new BulkSaveProgress(
            totalTickers,
            completedTickers,
            string.Empty,
            totalRecordsSaved));
    }

    public async Task<int> DeleteTickerDataAsync(string ticker, TimeFrame? timeFrame = null)
    {
        IQueryable<HistoricalPrice> query = _context.HistoricalPrices
            .Where(p => p.Ticker == ticker);

        if (timeFrame is not null)
        {
            query = query.Where(p => p.TimeFrame == timeFrame.Unit);
        }

        List<HistoricalPrice> recordsToDelete = await query.ToListAsync();
        int count = recordsToDelete.Count;

        if (count > 0)
        {
            _context.HistoricalPrices.RemoveRange(recordsToDelete);
            await _context.SaveChangesAsync();
        }

        return count;
    }

    public async Task<int> DeleteDateRangeAsync(
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate)
    {
        List<HistoricalPrice> recordsToDelete = await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker
                        && p.TimeFrame == timeFrame.Unit
                        && p.DateTime >= startDate
                        && p.DateTime <= endDate)
            .ToListAsync();

        int count = recordsToDelete.Count;

        if (count > 0)
        {
            _context.HistoricalPrices.RemoveRange(recordsToDelete);
            await _context.SaveChangesAsync();
        }

        return count;
    }

    public async Task<List<TickerSummary>> GetAllTickerSummariesAsync(TimeFrame timeFrame)
    {
        var tickerGroups = await _context.HistoricalPrices
            .Where(p => p.TimeFrame == timeFrame.Unit)
            .GroupBy(p => p.Ticker)
            .Select(g => new
            {
                Ticker = g.Key,
                ISIN = g.FirstOrDefault()!.ISIN,
                RecordCount = g.Count(),
                OldestDate = g.Min(p => p.DateTime),
                LatestDate = g.Max(p => p.DateTime)
            })
            .OrderBy(t => t.Ticker)
            .ToListAsync();

        return tickerGroups
            .Select(t => new TickerSummary(
                t.Ticker,
                t.ISIN,
                t.RecordCount,
                t.OldestDate,
                t.LatestDate))
            .ToList();
    }

    public async Task<DateTime?> GetDatabaseLastModifiedAsync()
    {
        return await _context.HistoricalPrices
            .MaxAsync(p => (DateTime?)p.CreatedAt);
    }
}
