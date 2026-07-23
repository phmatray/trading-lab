namespace TradingViewBlazor.Components;

public class StockHeatmap
    : TradingViewComponentBase<StockHeatmapSettings>
{
    protected override string TradingViewScriptUrl
        => "https://s3.tradingview.com/external-embedding/embed-widget-stock-heatmap.js";

    protected override string SerializeSettings(StockHeatmapSettings settings)
        => JsonSerializer.Serialize(settings, SourceGenerationContext.Default.StockHeatmapSettings);
}