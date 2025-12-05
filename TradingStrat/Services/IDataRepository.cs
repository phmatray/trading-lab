using TradingStrat.Models;

namespace TradingStrat.Services;

public interface IDataRepository
{
    Task SaveHistoricalDataAsync(string ticker, string? isin, IEnumerable<HistoricalDataPoint> dataPoints);
    Task<DateTime?> GetLatestDataDateAsync(string ticker);
    Task<List<HistoricalPrice>> GetHistoricalDataAsync(string ticker);
    Task<DataSummary> GetDataSummaryAsync(string ticker);
}

public record DataSummary(
    string Ticker,
    string? ISIN,
    int TotalRecords,
    int NewRecords,
    DateTime? OldestDate,
    DateTime? LatestDate,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? LatestClose);
