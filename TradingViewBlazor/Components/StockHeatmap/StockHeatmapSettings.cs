namespace TradingViewBlazor.Components;

public class StockHeatmapSettings
{
    [JsonPropertyName("exchanges")]
    public string[] Exchanges { get; set; } = ["NASDAQ"];
    
    [JsonPropertyName("dataSource")]
    public string DataSource { get; set; } = "SPX500";
    
    [JsonPropertyName("grouping")]
    public string Grouping { get; set; } = "sector";
    
    [JsonPropertyName("blockSize")]
    public string BlockSize { get; set; } = "market_cap_basic";
    
    [JsonPropertyName("blockColor")]
    public string BlockColor { get; set; } = "change";
    
    [JsonPropertyName("locale")]
    public string Locale { get; set; } = "en";
    
    [JsonPropertyName("symbolUrl")]
    public string SymbolUrl { get; set; } = "";
    
    [JsonPropertyName("colorTheme")]
    public TradingViewTheme ColorTheme { get; set; } = TradingViewTheme.Light;
    
    [JsonPropertyName("hasTopBar")]
    public bool HasTopBar { get; set; } = false;
    
    [JsonPropertyName("isDataSetEnabled")]
    public bool IsDataSetEnabled { get; set; } = false;
    
    [JsonPropertyName("isZoomEnabled")]
    public bool IsZoomEnabled { get; set; } = true;
    
    [JsonPropertyName("hasSymbolTooltip")]
    public bool HasSymbolTooltip { get; set; } = true;
    
    [JsonPropertyName("width")]
    public string Width { get; set; } = "100%";
    
    [JsonPropertyName("height")]
    public string Height { get; set; } = "500";
}