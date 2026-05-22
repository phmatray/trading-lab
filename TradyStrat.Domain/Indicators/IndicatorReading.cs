namespace TradyStrat.Domain;

public sealed record IndicatorReading(
    string Ticker,
    decimal Price,
    BollingerReading? Bollinger,
    decimal? Rsi,
    decimal? Sma50,
    decimal? Sma200,
    IchimokuReading? Ichimoku,
    Zone Zone,
    IReadOnlyList<string> Reasons);
