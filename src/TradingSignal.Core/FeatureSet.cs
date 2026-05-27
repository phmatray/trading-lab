namespace TradingSignal.Core;

// Snapshot of indicator state at a single decision point.
// Invariant: contains only information observable at AsOfUtc — no future data.
public sealed record FeatureSet(
    DateTime AsOfUtc,
    string Symbol,
    decimal Close,
    double Rsi14,
    double MacdLine,
    double MacdSignal,
    double MacdHistogram,
    double Ema20,
    double Ema50,
    double Atr14,
    double Return1,
    double Return5,
    double VolatilityPct);
