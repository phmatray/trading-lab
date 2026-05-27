namespace TradingSignal.Core;

public sealed record Candle(
    DateTime OpenTimeUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume);
