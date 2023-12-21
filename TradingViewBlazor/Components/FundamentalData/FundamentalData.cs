namespace TradingViewBlazor.Components;

public class FundamentalData
    : TradingViewComponentBase<FundamentalDataSettings>
{
    protected override string TradingViewScriptUrl
        => "https://s3.tradingview.com/external-embedding/embed-widget-financials.js";
}