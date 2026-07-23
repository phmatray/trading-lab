namespace TradingViewBlazor.Components;

public class MiniChart
    : TradingViewComponentBase<MiniChartSettings>
{
    protected override string TradingViewScriptUrl
        => "https://s3.tradingview.com/external-embedding/embed-widget-mini-symbol-overview.js";

    protected override string SerializeSettings(MiniChartSettings settings)
        => JsonSerializer.Serialize(settings, SourceGenerationContext.Default.MiniChartSettings);
}