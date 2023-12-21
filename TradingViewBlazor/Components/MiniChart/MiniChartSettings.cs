namespace TradingViewBlazor.Components;

public class MiniChartSettings
{
    public string Symbol { get; set; } = "FX:EURUSD";
    public string Width { get; set; } = "350";
    public string Height { get; set; } = "220";
    public string Locale { get; set; } = "en";
    public string DateRange { get; set; } = "12M";
    public string ColorTheme { get; set; } = "light";
    public bool IsTransparent { get; set; } = false;
    public bool Autosize { get; set; } = false;
    public string LargeChartUrl { get; set; } = "";
}