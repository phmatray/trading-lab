namespace TradingViewBlazor.Components;

public class Ticker
    : TradingViewComponentBase<TickerSettings>
{
    protected override string JavaScriptFileName
        => "./_content/TradingViewBlazor/js/ticker.js";
}