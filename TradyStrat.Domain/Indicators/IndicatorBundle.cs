using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed record IndicatorBundle(
    BollingerReading Bollinger,
    Percentage Rsi,
    decimal Sma50,
    decimal Sma200,
    IchimokuReading Ichimoku)
{
    public static readonly IndicatorBundle Empty = new(
        BollingerReading.Empty,
        Percentage.Empty,
        0m,
        0m,
        IchimokuReading.Empty);
}
