namespace TradingStrat.Models;

public class HistoricalPrice
{
    public int Id { get; set; }
    public required string Ticker { get; set; }
    public string? ISIN { get; set; }
    public DateTime DateTime { get; set; }
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal? Close { get; set; }
    public decimal? AdjustedClose { get; set; }
    public long? Volume { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
