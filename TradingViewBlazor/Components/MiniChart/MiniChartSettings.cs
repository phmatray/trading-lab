namespace TradingViewBlazor.Components;

public class MiniChartSettings
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = "FX:EURUSD";
    
    [JsonPropertyName("width")]
    public string Width { get; set; } = "350";
    
    [JsonPropertyName("height")]
    public string Height { get; set; } = "220";
    
    [JsonPropertyName("locale")]
    public string Locale { get; set; } = "en";
    
    [JsonPropertyName("dateRange")]
    public string DateRange { get; set; } = "12M";
    
    [JsonPropertyName("colorTheme")]
    public TradingViewTheme ColorTheme { get; set; } = TradingViewTheme.Light;
    
    [JsonPropertyName("isTransparent")]
    public bool IsTransparent { get; set; } = false;
    
    [JsonPropertyName("largeChartUrl")]
    public string LargeChartUrl { get; set; } = "";
}