namespace TradingViewBlazor.Components;

public class FundamentalData
    : TradingViewComponentBase<FundamentalDataSettings>
{
    protected override string TradingViewScriptUrl
        => "https://s3.tradingview.com/external-embedding/embed-widget-financials.js";

    protected override string SerializeSettings(FundamentalDataSettings settings)
        => JsonSerializer.Serialize(settings, SourceGenerationContext.Default.FundamentalDataSettings);
}