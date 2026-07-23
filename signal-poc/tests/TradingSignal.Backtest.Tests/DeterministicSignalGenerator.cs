using TradingSignal.Core;
using TradingSignal.Core.Abstractions;

namespace TradingSignal.Backtest.Tests;

// Pure function of FeatureSet. Identical features ⇒ identical signal — this is
// exactly what the look-ahead regression test relies on.
internal sealed class DeterministicSignalGenerator : ISignalGenerator
{
    public Task<RawSignal> GenerateAsync(
        FeatureSet features, IReadOnlyList<FewShotCase> memory, CancellationToken ct)
    {
        // Combine a handful of fields into a stable hash. Don't use AsOfUtc because
        // that's expected to vary (we want feature-content-only determinism).
        int hash = HashCode.Combine(
            Math.Round(features.Rsi14, 4),
            Math.Round(features.MacdHistogram, 6),
            Math.Round(features.Ema20, 4),
            Math.Round(features.Return5, 6),
            Math.Round(features.VolatilityPct, 6));

        TradeAction action = ((uint)hash % 3) switch
        {
            0 => TradeAction.Buy,
            1 => TradeAction.Sell,
            _ => TradeAction.Hold,
        };
        double confidence = ((uint)hash % 1000) / 1000d; // [0, 1)
        return Task.FromResult(new RawSignal(action, confidence, "det"));
    }
}
