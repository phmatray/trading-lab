export function initTradingViewComponent(reference, settings) {
  const script = document.createElement("script");
  script.src = "https://s3.tradingview.com/external-embedding/embed-widget-mini-symbol-overview.js";
  script.type = "text/javascript";
  script.async = true;
  script.innerHTML = JSON.stringify({
    symbol: settings.symbol,
    width: settings.width,
    height: settings.height,
    locale: settings.locale,
    dateRange: settings.dateRange,
    colorTheme: settings.colorTheme,
    isTransparent: settings.isTransparent,
    autosize: settings.autosize,
    largeChartUrl: settings.largeChartUrl
  });

  reference.appendChild(script);
}
