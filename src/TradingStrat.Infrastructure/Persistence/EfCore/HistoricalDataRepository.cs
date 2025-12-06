using Microsoft.EntityFrameworkCore;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Infrastructure.Persistence.EfCore;

public class HistoricalDataRepository : IHistoricalDataPort
{
    private readonly TradingContext _context;

    public HistoricalDataRepository(TradingContext context)
    {
        _context = context;
    }

    public async Task SaveHistoricalDataAsync(string ticker, string? isin, IEnumerable<HistoricalPrice> data)
    {
        var dataList = data.ToList();
        if (!dataList.Any())
        {
            return;
        }

        // Update ticker and ISIN for all entries
        foreach (HistoricalPrice price in dataList)
        {
            price.Ticker = ticker;
            price.ISIN = isin;
            price.CreatedAt = DateTime.UtcNow;
        }

        // Get existing dates to filter out duplicates
        var dates = dataList.Select(p => p.DateTime).ToList();
        List<DateTime> existingDates = await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker && dates.Contains(p.DateTime))
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

    public async Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker, DateTime start, DateTime end)
    {
        return await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker && p.DateTime >= start && p.DateTime <= end)
            .OrderBy(p => p.DateTime)
            .ToListAsync();
    }

    public async Task<DataSummaryResult> GetDataSummaryAsync(string ticker)
    {
        List<HistoricalPrice> data = await _context.HistoricalPrices
            .Where(p => p.Ticker == ticker)
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
}
