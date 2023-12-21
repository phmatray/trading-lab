namespace TradingViewBlazor.Components.Ticker;

public class TickerSettings
{
    public Symbol[] Symbols { get; set; } =
    [
        new Symbol { ProName = "FOREXCOM:SPXUSD", Title = "S&P 500" },
        new Symbol { ProName = "FOREXCOM:NSXUSD", Title = "US 100" },
        new Symbol { ProName = "FX_IDC:EURUSD", Title = "EUR to USD" },
        new Symbol { ProName = "BITSTAMP:BTCUSD", Title = "Bitcoin" },
        new Symbol { ProName = "BITSTAMP:ETHUSD", Title = "Ethereum" }
    ];
    public bool IsTransparent { get; set; } = false;
    public bool ShowSymbolLogo { get; set; } = true;
    public TradingViewTheme ColorTheme { get; set; } = TradingViewTheme.Light;
    public string Locale { get; set; } = "en";
}