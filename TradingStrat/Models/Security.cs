namespace TradingStrat.Models;

public class Security
{
    public int Id { get; set; }
    public required string Ticker { get; set; }
    public string? ISIN { get; set; }
    public string? Name { get; set; }
    public string? SecurityType { get; set; }
    public string? Exchange { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
