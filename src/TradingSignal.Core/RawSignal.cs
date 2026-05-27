namespace TradingSignal.Core;

public sealed record RawSignal(
    TradeAction Action,
    double Confidence,
    string Reason);
