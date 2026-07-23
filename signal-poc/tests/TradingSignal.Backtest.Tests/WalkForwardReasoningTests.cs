using Shouldly;
using TradingSignal.Adaptation;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;
using TradingSignal.Indicators;

namespace TradingSignal.Backtest.Tests;

// Regression: WalkForwardOrchestrator.cs:107 once reconstructed RawSignal from
// the LLM output but passed only 3 args (Action, Confidence, Reason), silently
// dropping the new Reasoning field on its way to the predictions store. Caught
// only by an end-to-end run against a real reasoning model that returned non-
// null traces — every unit test against the strategy / generator / store in
// isolation continued to pass. These tests close that loop.
public sealed class WalkForwardReasoningTests
{
    private const int CandlesPerDay = 24;

    private static BacktestOptions Options() => new()
    {
        AdaptationDays = 5,
        TestDays = 2,
        StepDays = 2,
        EvaluationHorizonCandles = 1,
        CandlesPerDay = CandlesPerDay,
    };

    [Fact]
    public async Task Reasoning_From_Signal_Generator_Is_Preserved_On_Stored_Prediction()
    {
        ReasoningSignalGenerator generator = new(reasoning: "the trace");
        WalkForwardOrchestrator orchestrator = new(
            new FeatureEngine("BTCUSDT"), generator, new NullAdaptation(), store: null, Options());
        IReadOnlyList<Candle> candles = Synthetic.Candles(16 * CandlesPerDay);

        BacktestResult result = await orchestrator.RunAsync(candles, "BTCUSDT", CancellationToken.None);

        result.Segments.ShouldNotBeEmpty();
        foreach (SegmentResult segment in result.Segments)
        {
            segment.Predictions.ShouldNotBeEmpty();
            foreach (Prediction prediction in segment.Predictions)
            {
                prediction.Signal.Reasoning.ShouldBe("the trace");
            }
        }
    }

    [Fact]
    public async Task Null_Reasoning_From_Generator_Remains_Null_On_Stored_Prediction()
    {
        ReasoningSignalGenerator generator = new(reasoning: null);
        WalkForwardOrchestrator orchestrator = new(
            new FeatureEngine("BTCUSDT"), generator, new NullAdaptation(), store: null, Options());
        IReadOnlyList<Candle> candles = Synthetic.Candles(16 * CandlesPerDay);

        BacktestResult result = await orchestrator.RunAsync(candles, "BTCUSDT", CancellationToken.None);

        result.Segments.SelectMany(s => s.Predictions).ShouldAllBe(p => p.Signal.Reasoning == null);
    }

    [Fact]
    public async Task Per_Prediction_Reasoning_Is_Preserved_In_Order()
    {
        // Generator stamps a unique reasoning per call so we can verify the
        // orchestrator doesn't lose, swap, or smear traces across predictions.
        SequentialReasoningSignalGenerator generator = new();
        WalkForwardOrchestrator orchestrator = new(
            new FeatureEngine("BTCUSDT"), generator, new NullAdaptation(), store: null, Options());
        IReadOnlyList<Candle> candles = Synthetic.Candles(16 * CandlesPerDay);

        BacktestResult result = await orchestrator.RunAsync(candles, "BTCUSDT", CancellationToken.None);

        List<Prediction> allPredictions = result.Segments.SelectMany(s => s.Predictions).ToList();
        allPredictions.ShouldNotBeEmpty();
        for (int i = 0; i < allPredictions.Count; i++)
        {
            allPredictions[i].Signal.Reasoning.ShouldBe($"trace-{i}");
        }
    }

    private sealed class ReasoningSignalGenerator(string? reasoning) : ISignalGenerator
    {
        public Task<RawSignal> GenerateAsync(
            FeatureSet features, IReadOnlyList<FewShotCase> memory, CancellationToken ct)
            => Task.FromResult(new RawSignal(TradeAction.Hold, 0.5, "r", reasoning));
    }

    private sealed class SequentialReasoningSignalGenerator : ISignalGenerator
    {
        private int _counter;

        public Task<RawSignal> GenerateAsync(
            FeatureSet features, IReadOnlyList<FewShotCase> memory, CancellationToken ct)
        {
            int n = _counter++;
            return Task.FromResult(new RawSignal(TradeAction.Hold, 0.5, "r", $"trace-{n}"));
        }
    }
}
