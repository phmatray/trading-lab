namespace TradingViewBlazor.Components;

public class AdvancedRealTimeChart
    : TradingViewComponentBase<AdvancedRealTimeChartSettings>
{
    protected override string TradingViewScriptUrl
        => "https://s3.tradingview.com/external-embedding/embed-widget-advanced-chart.js";

    protected override string SerializeSettings(AdvancedRealTimeChartSettings settings)
        => JsonSerializer.Serialize(settings, SourceGenerationContext.Default.AdvancedRealTimeChartSettings);
}