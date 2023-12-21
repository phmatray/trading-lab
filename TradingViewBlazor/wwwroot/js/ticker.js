export function initTradingViewComponent(reference, settings) {
  const script = document.createElement("script");
  script.src = "https://s3.tradingview.com/external-embedding/embed-widget-tickers.js";
  script.type = "text/javascript";
  script.async = true;
  script.innerHTML = JSON.stringify({
    "symbols": settings.symbols,
    "isTransparent": settings.isTransparent,
    "showSymbolLogo": settings.showSymbolLogo,
    "colorTheme": settings.colorTheme.toString(),
    "locale": settings.locale
  });

  reference.appendChild(script);
}