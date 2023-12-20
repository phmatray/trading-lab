export function initTradingViewComponent(reference, settings) {
  const script = document.createElement("script");
  script.src = "https://s3.tradingview.com/external-embedding/embed-widget-technical-analysis.js";
  script.type = "text/javascript";
  script.async = true;
  script.innerHTML = JSON.stringify({
    "interval": settings.interval,
    "width": settings.width,
    "isTransparent": settings.isTransparent,
    "height": settings.height,
    "symbol": settings.symbol,
    "showIntervalTabs": settings.showIntervalTabs,
    "displayMode": settings.displayMode,
    "locale": settings.locale,
    "colorTheme": settings.colorTheme
  });
  
  reference.appendChild(script);
}