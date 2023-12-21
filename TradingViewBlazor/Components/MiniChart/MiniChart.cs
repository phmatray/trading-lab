namespace TradingViewBlazor.Components;

public class MiniChart
    : TradingViewComponentBase<MiniChartSettings>
{
    protected override string TradingViewScriptUrl
        => "https://s3.tradingview.com/external-embedding/embed-widget-mini-symbol-overview.js";
}