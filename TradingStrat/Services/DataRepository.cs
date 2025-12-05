using Microsoft.EntityFrameworkCore;
using TradingStrat.Data;
using TradingStrat.Models;

namespace TradingStrat.Services;

public class DataRepository : IDataRepository
{
    private readonly TradingContext _context;

    public DataRepository(TradingContext context)
    {
        _context = context;
    }

    public async Task SaveHistoricalDataAsync(string ticker, string? isin, IEnumerable<HistoricalDataPoint> dataPoints)
    {
        var dataPointsList = dataPoints.ToList();
        if (!dataPointsList.Any())
        {
            return;
        }

        var historicalPrices = dataPointsList.Select(dataPoint => new HistoricalPrice
        {
            Ticker = ticker,
            ISIN = isin,
            DateTime = dataPoint.DateTime,
            Open = dataPoint.Open,
            High = dataPoint.High,
            Low = dataPoint.Low,
            Close = dataPoint.Close,
            AdjustedClose = dataPoint.AdjustedClose,
            Volume = dataPoint.Volume,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        // Get existing dates to filter out duplicates
        var dates = historicalPrices.Select(p => p.DateTime).ToList();
        var existingDates = await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker && dates.Contains(p.DateTime))
            .Select(p => p.DateTime)
            .ToListAsync();

        // Only add new records
        var newRecords = historicalPrices
            .Where(p => !existingDates.Contains(p.DateTime))
            .ToList();

        if (newRecords.Any())
        {
            await _context.HistoricalPrices.AddRangeAsync(newRecords);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<DateTime?> GetLatestDataDateAsync(string ticker)
    {
        return await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker)
            .MaxAsync(p => (DateTime?)p.DateTime);
    }

    public async Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker)
    {
        return await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker)
            .OrderBy(p => p.DateTime)
            .ToListAsync();
    }

    public async Task<DataSummary> GetDataSummaryAsync(string ticker)
    {
        var data = await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker)
            .ToListAsync();

        if (!data.Any())
        {
            return new DataSummary(ticker, null, 0, 0, null, null, null, null, null);
        }

        var isin = data.FirstOrDefault()?.ISIN;
        var totalRecords = data.Count;
        var oldestDate = data.Min(p => p.DateTime);
        var latestDate = data.Max(p => p.DateTime);
        var minPrice = data.Where(p => p.Low.HasValue).Min(p => p.Low);
        var maxPrice = data.Where(p => p.High.HasValue).Max(p => p.High);
        var latestClose = data
            .OrderByDescending(p => p.DateTime)
            .FirstOrDefault()?.Close;

        return new DataSummary(
            ticker,
            isin,
            totalRecords,
            0, // Will be updated when we know new records count
            oldestDate,
            latestDate,
            minPrice,
            maxPrice,
            latestClose);
    }
}
