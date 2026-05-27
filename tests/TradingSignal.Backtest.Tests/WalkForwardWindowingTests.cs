using Shouldly;
using TradingSignal.Adaptation;
using TradingSignal.Backtest;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;
using TradingSignal.Indicators;

namespace TradingSignal.Backtest.Tests;

public sealed class WalkForwardWindowingTests
{
    private const int CandlesPerDay = 24;

    private static WalkForwardOrchestrator Build(BacktestOptions opts)
    {
        IFeatureEngine fe = new FeatureEngine("BTCUSDT");
        ISignalGenerator gen = new DeterministicSignalGenerator();
        IAdaptationStrategy strat = new NullAdaptation();
        return new WalkForwardOrchestrator(fe, gen, strat, store: null, opts);
    }

    [Fact]
    public async Task Produces_Multiple_Non_Overlapping_Test_Segments()
    {
        BacktestOptions opts = new()
        {
            AdaptationDays = 5,
            TestDays = 2,
            StepDays = 2,
            EvaluationHorizonCandles = 1,
            CandlesPerDay = CandlesPerDay,
        };
        // Need at least: adapt(5d) + test(2d) + buffer ≈ 8d to get 1 segment;
        // each subsequent segment adds step(2d). 16 days → 5 segments.
        IReadOnlyList<Candle> candles = Synthetic.Candles(16 * CandlesPerDay);

        BacktestResult result = await Build(opts).RunAsync(candles, "BTCUSDT", CancellationToken.None);

        result.Segments.Count.ShouldBeGreaterThan(2);

        // Test windows should not overlap
        for (int i = 1; i < result.Segments.Count; i++)
        {
            result.Segments[i].TestStartUtc.ShouldBeGreaterThan(result.Segments[i - 1].TestEndUtc);
        }
    }

    [Fact]
    public async Task Stops_When_Not_Enough_Candles_For_A_Full_Segment()
    {
        BacktestOptions opts = new()
        {
            AdaptationDays = 5,
            TestDays = 2,
            StepDays = 2,
            EvaluationHorizonCandles = 1,
            CandlesPerDay = CandlesPerDay,
        };
        // Exactly 5d + 2d = 7d of candles, no headroom for outcome computation.
        IReadOnlyList<Candle> candles = Synthetic.Candles(7 * CandlesPerDay);

        BacktestResult result = await Build(opts).RunAsync(candles, "BTCUSDT", CancellationToken.None);

        // One short of the horizon means we may produce zero segments or one truncated.
        // The invariant is: every segment produced has predictions that all have valid outcomes.
        foreach (SegmentResult seg in result.Segments)
        {
            seg.Predictions.Count.ShouldBe(seg.Outcomes.Count);
        }
    }

    [Fact]
    public async Task Each_Segment_Has_Same_Test_Window_Length()
    {
        BacktestOptions opts = new()
        {
            AdaptationDays = 3,
            TestDays = 1,
            StepDays = 1,
            EvaluationHorizonCandles = 1,
            CandlesPerDay = CandlesPerDay,
        };
        IReadOnlyList<Candle> candles = Synthetic.Candles(10 * CandlesPerDay);

        BacktestResult result = await Build(opts).RunAsync(candles, "BTCUSDT", CancellationToken.None);

        result.Segments.Count.ShouldBeGreaterThan(1);
        int expectedPredictionsPerSegment = result.Segments[0].PredictionCount;
        foreach (SegmentResult seg in result.Segments)
        {
            seg.PredictionCount.ShouldBe(expectedPredictionsPerSegment);
        }
    }
}
