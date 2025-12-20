// TradingView Widget JavaScript Interop

window.TradingView = window.TradingView || {};

window.TradingView.loadChart = function (widgetId, ticker, theme) {
    // Wait for TradingView library to be available
    if (typeof TradingView === 'undefined' || !TradingView.widget) {
        console.error('TradingView library not loaded');
        return;
    }

    try {
        new TradingView.widget({
            "width": "100%",
            "height": "100%",
            "symbol": ticker,
            "interval": "D",
            "timezone": "Etc/UTC",
            "theme": theme || "light",
            "style": "1",
            "locale": "en",
            "toolbar_bg": "#f1f3f6",
            "enable_publishing": false,
            "allow_symbol_change": true,
            "container_id": `tradingview_chart_${widgetId}`,
            "studies": [
                "MASimple@tv-basicstudies",
                "RSI@tv-basicstudies"
            ]
        });
    } catch (error) {
        console.error('Error loading TradingView widget:', error);
    }
};

window.TradingView.loadSymbolInfo = function (widgetId, ticker) {
    // Wait for TradingView library to be available
    if (typeof TradingView === 'undefined' || !TradingView.widget) {
        console.error('TradingView library not loaded');
        return;
    }

    try {
        new TradingView.MiniChart({
            "width": "100%",
            "height": "100%",
            "symbol": ticker,
            "interval": "D",
            "timezone": "Etc/UTC",
            "theme": "light",
            "style": "1",
            "locale": "en",
            "toolbar_bg": "#f1f3f6",
            "enable_publishing": false,
            "container_id": `tradingview_mini_${widgetId}`
        });
    } catch (error) {
        console.error('Error loading TradingView symbol info:', error);
    }
};
