namespace TradingStrat.Services;

public interface IYahooFinanceService
{
    Task<IReadOnlyList<HistoricalDataPoint>> GetHistoricalDataAsync(
        string ticker,
        DateTime startDate,
        DateTime endDate);
}

public record HistoricalDataPoint(
    DateTime DateTime,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal AdjustedClose,
    long Volume);
