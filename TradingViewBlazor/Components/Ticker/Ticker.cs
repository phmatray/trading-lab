namespace TradingViewBlazor.Components;

public class Ticker
    : TradingViewComponentBase<TickerSettings>
{
    protected override string TradingViewScriptUrl
        => "https://s3.tradingview.com/external-embedding/embed-widget-tickers.js";
    
    protected override string SerializeSettings(TickerSettings settings)
        => JsonSerializer.Serialize(settings, SourceGenerationContext.Default.TickerSettings);
}