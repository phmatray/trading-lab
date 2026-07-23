namespace TradingViewBlazor.Components;

public class CryptoCoinsHeatmap
    : TradingViewComponentBase<CryptoCoinsHeatmapSettings>
{
    protected override string TradingViewScriptUrl
        => "https://s3.tradingview.com/external-embedding/embed-widget-crypto-coins-heatmap.js";

    protected override string SerializeSettings(CryptoCoinsHeatmapSettings settings)
        => JsonSerializer.Serialize(settings, SourceGenerationContext.Default.CryptoCoinsHeatmapSettings);
}