namespace TradingViewBlazor.Components;

public class FundamentalDataSettings
{
    public bool IsTransparent { get; set; } = false;
    public string LargeChartUrl { get; set; } = "";
    public string DisplayMode { get; set; } = "regular";
    public string Width { get; set; } = "480";
    public string Height { get; set; } = "830";
    public TradingViewTheme ColorTheme { get; set; } = TradingViewTheme.Light;
    public string Symbol { get; set; } = "NASDAQ:AAPL";
    public string Locale { get; set; } = "en";
}