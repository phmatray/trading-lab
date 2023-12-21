using TradingViewBlazor.Models;

namespace TradingViewBlazor.Components.TechnicalAnalysis;

public class TechnicalAnalysisSettings
{
    public string Interval { get; set; } = "1m";
    public string Width { get; set; } = "425";
    public string Height { get; set; } = "450";
    public bool IsTransparent { get; set; } = false;
    public string Symbol { get; set; } = "NASDAQ:AAPL";
    public bool ShowIntervalTabs { get; set; } = true;
    public string DisplayMode { get; set; } = "single";
    public string Locale { get; set; } = "en";
    public TradingViewTheme ColorTheme { get; set; } = TradingViewTheme.Light;
}