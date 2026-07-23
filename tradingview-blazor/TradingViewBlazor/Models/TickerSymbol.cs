namespace TradingViewBlazor.Models;

public class TickerSymbol
{
    /// <summary>
    /// Ticker name
    /// </summary>
    [JsonPropertyName("proName")]
    public required string ProName { get; set; }
    
    /// <summary>
    /// Ticker title
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }
}