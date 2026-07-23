namespace TradingViewBlazor.Components;

public class FundamentalDataSettings
{
    [JsonPropertyName("isTransparent")]
    public bool IsTransparent { get; set; } = false;
    
    [JsonPropertyName("largeChartUrl")]
    public string LargeChartUrl { get; set; } = "";
    
    [JsonPropertyName("displayMode")]
    public string DisplayMode { get; set; } = "regular";
    
    [JsonPropertyName("width")]
    public string Width { get; set; } = "480";
    
    [JsonPropertyName("height")]
    public string Height { get; set; } = "830";
    
    [JsonPropertyName("colorTheme")]
    public TradingViewTheme ColorTheme { get; set; } = TradingViewTheme.Light;
    
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = "NASDAQ:AAPL";
    
    [JsonPropertyName("locale")]
    public string Locale { get; set; } = "en";
}