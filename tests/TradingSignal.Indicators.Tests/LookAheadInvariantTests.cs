using Shouldly;

namespace TradingSignal.Indicators.Tests;

// THE feature-level look-ahead regression test (spec §5).
// If FeatureEngine ever consults candles past upToIndex (directly or by passing
// the unsliced list to Skender), this test catches it: the computed features
// must depend ONLY on candles[0..upToIndex].
public sealed class LookAheadInvariantTests
{
    [Fact]
    public void Compute_At_Index_Is_Identical_Whether_Future_Candles_Are_Present_Or_Truncated()
    {
        var engine = new FeatureEngine("BTCUSDT");
        var all = SyntheticCandles.Generate(count: 300);

        var checkedCount = 0;
        for (var i = engine.WarmupPeriods; i < all.Count; i++)
        {
            var truncated = all.Take(i + 1).ToList();
            var withFuture = all; // entire list including indices > i

            var fromTruncated = engine.Compute(truncated, i);
            var fromFull = engine.Compute(withFuture, i);

            fromFull.ShouldBe(fromTruncated, $"feature mismatch at index {i}");
            checkedCount++;
        }

        checkedCount.ShouldBeGreaterThan(200);
    }
}
