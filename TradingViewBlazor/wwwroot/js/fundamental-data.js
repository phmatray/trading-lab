export function initTradingViewComponent(reference, settings) {
  const script = document.createElement("script");
  script.src = "https://s3.tradingview.com/external-embedding/embed-widget-financials.js"
  script.type = "text/javascript";
  script.async = true;
  script.innerHTML = JSON.stringify({
    isTransparent: settings.isTransparent,
    largeChartUrl: settings.largeChartUrl,
    displayMode: settings.displayMode,
    width: settings.width,
    height: settings.height,
    colorTheme: settings.colorTheme,
    symbol: settings.symbol,
    locale: settings.locale
  });

  reference.appendChild(script);
}