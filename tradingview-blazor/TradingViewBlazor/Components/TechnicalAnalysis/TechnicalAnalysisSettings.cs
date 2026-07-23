namespace TradingViewBlazor.Components;

public class TechnicalAnalysisSettings
{
    [JsonPropertyName("interval")]
    public string Interval { get; set; } = "1m";
    
    [JsonPropertyName("width")]
    public string Width { get; set; } = "425";
    
    [JsonPropertyName("height")]
    public string Height { get; set; } = "450";
    
    [JsonPropertyName("isTransparent")]
    public bool IsTransparent { get; set; } = false;
    
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = "NASDAQ:AAPL";
    
    [JsonPropertyName("showIntervalTabs")]
    public bool ShowIntervalTabs { get; set; } = true;
    
    [JsonPropertyName("displayMode")]
    public string DisplayMode { get; set; } = "single";
    
    [JsonPropertyName("locale")]
    public string Locale { get; set; } = "en";
    
    [JsonPropertyName("colorTheme")]
    public TradingViewTheme ColorTheme { get; set; } = TradingViewTheme.Light;
}