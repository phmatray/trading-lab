namespace TradingViewBlazor.Components;

public class Ticker
    : TradingViewComponentBase<TickerSettings>
{
    protected override string TradingViewScriptUrl
        => "https://s3.tradingview.com/external-embedding/embed-widget-tickers.js";
}