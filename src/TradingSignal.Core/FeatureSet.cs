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
    double VolatilityPct,
    /// <summary>
    /// Trend-strength indicator (0..100). Above ~25 suggests a real directional trend;
    /// below ~20 suggests ranging/choppy market where trend-following signals tend to fail.
    /// </summary>
    double Adx14 = 0d,
    /// <summary>
    /// Current bar's volume divided by the 20-bar average volume. Above ~1.5 = breakout
    /// conviction; below ~0.6 = low-conviction drift. ~1 means typical.
    /// </summary>
    double VolumeRatio = 1d);
