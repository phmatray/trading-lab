using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed record IndicatorReading(
    string Ticker,
    decimal Price,
    BollingerReading Bollinger,
    Percentage Rsi,
    decimal Sma50,
    decimal Sma200,
    IchimokuReading Ichimoku,
    Zone Zone,
    IReadOnlyList<string> Reasons);
