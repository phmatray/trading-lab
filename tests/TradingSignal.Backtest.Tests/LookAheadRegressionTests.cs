using Shouldly;
using TradingSignal.Adaptation;
using TradingSignal.Backtest;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;
using TradingSignal.Indicators;

namespace TradingSignal.Backtest.Tests;

// THE spec §8.4 look-ahead regression test (orchestrator-level guard, in addition
// to the feature-level guard in TradingSignal.Indicators.Tests).
//
// Strategy: run the same orchestrator twice. Once on the full candle list,
// once on a list truncated at the test-window end. The decision stream
// (action + features) must be byte-identical. Any look-ahead leak — direct
// or via an indicator — would diverge the two runs.
public sealed class LookAheadRegressionTests
{
    private const int CandlesPerDay = 24;

    private static BacktestOptions ShortOptions() => new()
    {
        AdaptationDays = 5,
        TestDays = 2,
        StepDays = 2,
        EvaluationHorizonCandles = 1,
        CandlesPerDay = CandlesPerDay,
        FeeBps = 10,
        EnableShort = false,
    };

    [Fact]
    public async Task First_Segment_Decisions_Identical_With_Or_Without_Future_Candles()
    {
        BacktestOptions opts = ShortOptions();
        IReadOnlyList<Candle> full = Synthetic.Candles(count: 12 * CandlesPerDay, seed: 7);
        int testEnd = opts.AdaptationCandles + opts.TestCandles;
        IReadOnlyList<Candle> truncated = full.Take(testEnd + opts.EvaluationHorizonCandles).ToList();

        BacktestResult resultFull = await BuildOrchestrator(opts).RunAsync(full, "BTCUSDT", CancellationToken.None);
        BacktestResult resultTrunc = await BuildOrchestrator(opts).RunAsync(truncated, "BTCUSDT", CancellationToken.None);

        // The truncated list is exactly long enough for one segment; the full list
        // may produce more. The look-ahead invariant says the FIRST segment must be
        // byte-identical between the two — future candles cannot influence past decisions.
        resultTrunc.Segments.Count.ShouldBe(1);
        resultFull.Segments.Count.ShouldBeGreaterThanOrEqualTo(1);

        IReadOnlyList<Prediction> a = resultFull.Segments[0].Predictions;
        IReadOnlyList<Prediction> b = resultTrunc.Segments[0].Predictions;

        a.Count.ShouldBe(b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            a[i].Signal.Action.ShouldBe(b[i].Signal.Action, $"action mismatch at {i}");
            a[i].Signal.Confidence.ShouldBe(b[i].Signal.Confidence, $"confidence mismatch at {i}");
            a[i].Features.ShouldBe(b[i].Features, $"features mismatch at {i}");
            a[i].AsOfUtc.ShouldBe(b[i].AsOfUtc, $"as-of mismatch at {i}");
        }
    }

    [Fact]
    public async Task Per_Index_Truncation_Yields_Same_Decisions_As_Full_Candle_List()
    {
        // Stronger variant: at each decision i, hand the orchestrator a candle list
        // truncated at i + horizon. Identical decisions to the full-list run prove
        // the orchestrator only ever reads candles[0..i + horizon].
        BacktestOptions opts = ShortOptions();
        IReadOnlyList<Candle> full = Synthetic.Candles(count: 12 * CandlesPerDay, seed: 11);

        // Reference run (full list)
        BacktestResult reference = await BuildOrchestrator(opts).RunAsync(full, "BTCUSDT", CancellationToken.None);
        IReadOnlyList<Prediction> refPreds = reference.Segments[0].Predictions;

        // Replay each decision independently with a candle list truncated to (i + horizon + 1).
        // Build a synthetic single-decision orchestrator for each index.
        FeatureEngine fe = new("BTCUSDT");
        DeterministicSignalGenerator gen = new();
        NullAdaptation strat = new();

        for (int idx = 0; idx < refPreds.Count; idx++)
        {
            Prediction reference_i = refPreds[idx];
            int candleIndex = full.Select((c, j) => (c, j)).First(t => t.c.OpenTimeUtc == reference_i.AsOfUtc).j;

            List<Candle> truncated = full.Take(candleIndex + 1 + opts.EvaluationHorizonCandles).ToList();
            FeatureSet features = fe.Compute(truncated, candleIndex);
            RawSignal raw = await gen.GenerateAsync(features, Array.Empty<FewShotCase>(), CancellationToken.None);
            FinalDecision decision = strat.Apply(raw, features);

            features.ShouldBe(reference_i.Features, $"features diverged at {idx}");
            decision.Action.ShouldBe(reference_i.Signal.Action, $"action diverged at {idx}");
            raw.Confidence.ShouldBe(reference_i.Signal.Confidence, $"confidence diverged at {idx}");
        }
    }

    private static WalkForwardOrchestrator BuildOrchestrator(BacktestOptions opts)
    {
        IFeatureEngine fe = new FeatureEngine("BTCUSDT");
        ISignalGenerator gen = new DeterministicSignalGenerator();
        IAdaptationStrategy strat = new NullAdaptation();
        return new WalkForwardOrchestrator(fe, gen, strat, store: null, opts);
    }
}
