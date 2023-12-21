namespace TradingViewBlazor.Components;

public class AdvancedRealTimeChartSettings
{
    [JsonPropertyName("width")]
    public string Width { get; set; } = "100%";
    
    [JsonPropertyName("height")]
    public string Height { get; set; } = "610";
    
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = "NASDAQ:MSFT";
    
    [JsonPropertyName("interval")]
    public string Interval { get; set; } = "D";
    
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = "Etc/UTC";
    
    [JsonPropertyName("theme")]
    public TradingViewTheme Theme { get; set; } = TradingViewTheme.Light;
    
    [JsonPropertyName("style")]
    public string Style { get; set; } = "1";
    
    [JsonPropertyName("locale")]
    public string Locale { get; set; } = "en";
    
    [JsonPropertyName("enable_publishing")]
    public bool EnablePublishing { get; set; } = false;
    
    [JsonPropertyName("withdateranges")]
    public bool WithDateRanges { get; set; } = false;
    
    [JsonPropertyName("hide_side_toolbar")]
    public bool HideSideToolbar { get; set; } = false;
    
    [JsonPropertyName("allow_symbol_change")]
    public bool AllowSymbolChange { get; set; } = true;
}