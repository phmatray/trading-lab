namespace TradyStrat.Shared.Domain;

public sealed record IndicatorBundle(
    BollingerReading? Bollinger,
    decimal? Rsi,
    decimal? Sma50,
    decimal? Sma200,
    IchimokuReading? Ichimoku);
