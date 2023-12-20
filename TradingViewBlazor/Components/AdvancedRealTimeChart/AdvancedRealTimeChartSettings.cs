using TradingViewBlazor.Models;

namespace TradingViewBlazor.Components.AdvancedRealTimeChart;

public class AdvancedRealTimeChartSettings
{
    public string Width { get; set; } = "100%";
    public string Height { get; set; } = "610";
    public bool Autosize { get; set; } = true;
    public string Symbol { get; set; } = "NASDAQ:MSFT";
    public string Interval { get; set; } = "D";
    public TimeZoneInfo Timezone { get; set; } = TimeZoneInfo.Utc;
    public TradingViewTheme Theme { get; set; } = TradingViewTheme.Dark;
    public string Style { get; set; } = "1";
    public string Locale { get; set; } = "en";
    public bool EnablePublishing { get; set; } = false;
    public bool WithDateRanges { get; set; } = false;
    public bool HideSideToolbar { get; set; } = false;
    public bool AllowSymbolChange { get; set; } = true;
}