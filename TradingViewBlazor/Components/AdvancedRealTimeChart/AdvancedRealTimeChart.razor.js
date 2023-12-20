export function initAdvancedRealTimeChart(reference, settings) {
  const script = document.createElement("script");
  script.src = "https://s3.tradingview.com/external-embedding/embed-widget-advanced-chart.js";
  script.type = "text/javascript";
  script.async = true;
  script.innerHTML = JSON.stringify({
    width: settings.width,
    height: settings.height,
    autosize: settings.autosize,
    symbol: settings.symbol,
    interval: settings.interval,
    timezone: settings.timezone,
    theme: settings.theme,
    "style": "1",
    locale: settings.locale,
    enablePublishing: settings.enablePublishing,
    withdateranges: settings.withdateranges,
    hide_side_toolbar: settings.hide_side_toolbar,
    allowSymbolChange: settings.allowSymbolChange,
    "support_host": "https://www.tradingview.com"
  });
  
  reference.appendChild(script);
}