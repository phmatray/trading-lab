using TradingViewBlazor.Models;

namespace TradingViewBlazor.Components.TechnicalAnalysis;

public class TechnicalAnalysisSettings
{
    public string Interval { get; set; } = "1m";
    public int Width { get; set; } = 425;
    public bool IsTransparent { get; set; } = false;
    public int Height { get; set; } = 450;
    public string Symbol { get; set; } = "NASDAQ:AAPL";
    public bool ShowIntervalTabs { get; set; } = true;
    public string DisplayMode { get; set; } = "single";
    public string Locale { get; set; } = "en";
    public TradingViewTheme ColorTheme { get; set; } = TradingViewTheme.Light;
}