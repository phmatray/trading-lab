namespace TradingViewBlazor.Components;

public class TickerSettings
{
    /// <summary>
    /// Default symbols in widget
    /// </summary>
    [JsonPropertyName("symbols")]
    public TickerSymbol[] Symbols { get; set; } =
    [
        new TickerSymbol { ProName = "FOREXCOM:SPXUSD", Title = "S&P 500" },
        new TickerSymbol { ProName = "FOREXCOM:NSXUSD", Title = "US 100" },
        new TickerSymbol { ProName = "FX_IDC:EURUSD", Title = "EUR to USD" },
        new TickerSymbol { ProName = "BITSTAMP:BTCUSD", Title = "Bitcoin" },
        new TickerSymbol { ProName = "BITSTAMP:ETHUSD", Title = "Ethereum" }
    ];
    
    /// <summary>
    /// Transparent background for component
    /// </summary>
    [JsonPropertyName("isTransparent")]
    public bool IsTransparent { get; set; } = false;
    
    /// <summary>
    /// Show symbol logo
    /// </summary>
    [JsonPropertyName("showSymbolLogo")]
    public bool ShowSymbolLogo { get; set; } = true;
    
    /// <summary>
    /// Sets the default theme
    /// </summary>
    [JsonPropertyName("colorTheme")]
    public TradingViewTheme ColorTheme { get; set; } = TradingViewTheme.Light;
    
    /// <summary>
    /// Sets the default locale
    /// </summary>
    [JsonPropertyName("locale")]
    public string Locale { get; set; } = "en";
    
    /// <summary>
    /// Full-size chart url
    /// </summary>
    [JsonPropertyName("largeChartUrl")]
    public string? LargeChartUrl { get; set; }
}