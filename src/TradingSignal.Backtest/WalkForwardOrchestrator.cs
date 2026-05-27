using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TradingSignal.Adaptation;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;
using TradingSignal.Evaluation;

namespace TradingSignal.Backtest;

public sealed partial class WalkForwardOrchestrator(
    IFeatureEngine featureEngine,
    ISignalGenerator signalGenerator,
    IAdaptationStrategy adaptation,
    IPredictionStore? store,
    BacktestOptions options,
    ILogger<WalkForwardOrchestrator>? logger = null)
{
    private readonly ILogger<WalkForwardOrchestrator> _logger = logger ?? NullLogger<WalkForwardOrchestrator>.Instance;
    private static readonly IReadOnlyList<FewShotCase> EmptyMemory = Array.Empty<FewShotCase>();

    public async Task<BacktestResult> RunAsync(
        IReadOnlyList<Candle> candles,
        string symbol,
        CancellationToken ct)
    {
        if (candles.Count == 0) throw new ArgumentException("candles is empty", nameof(candles));

        List<SegmentResult> segments = new();
        int cursor = 0;
        int segmentIndex = 0;

        while (true)
        {
            int adaptStart = cursor;
            int testStart = adaptStart + options.AdaptationCandles;
            int testEnd = testStart + options.TestCandles;

            // Need one extra candle past the test window for outcome computation (entry at i+1, exit at i+H).
            int requiredCount = testEnd + options.EvaluationHorizonCandles;
            if (requiredCount > candles.Count) break;
            if (testStart >= candles.Count || testEnd > candles.Count) break;

            SegmentResult result = await RunSegmentAsync(
                segmentIndex, candles, symbol, adaptStart, testStart, testEnd, ct).ConfigureAwait(false);
            segments.Add(result);

            cursor += options.StepCandles;
            segmentIndex++;
        }

        LogWalkForwardComplete(_logger, segments.Count, segments.Sum(s => s.PredictionCount));

        return new BacktestResult(symbol, adaptation.Label, segments);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Walk-forward complete: {Segments} segments, {Predictions} predictions")]
    private static partial void LogWalkForwardComplete(ILogger logger, int segments, int predictions);

    private async Task<SegmentResult> RunSegmentAsync(
        int segment,
        IReadOnlyList<Candle> candles,
        string symbol,
        int adaptStart,
        int testStart,
        int testEnd,
        CancellationToken ct)
    {
        AdaptationContext context = new(
            Segment: segment,
            Candles: candles,
            AdaptStartIndex: adaptStart,
            TestStartIndex: testStart,
            Symbol: symbol,
            FeeBps: options.FeeBps,
            EvaluationHorizonCandles: options.EvaluationHorizonCandles,
            PeriodsPerYear: options.PeriodsPerYear);

        await adaptation.FitAsync(context, ct).ConfigureAwait(false);
        // Snapshot diagnostics immediately after fit — strategy state mutates on the
        // next segment's fit, so the per-segment record needs a copy now.
        IReadOnlyDictionary<string, double> diagnostics = adaptation.Diagnostics
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        Portfolio portfolio = new(options.FeeBps, options.EnableShort);
        List<Prediction> predictions = new();
        List<Outcome> outcomes = new();

        int firstDecisionIndex = Math.Max(testStart, featureEngine.WarmupPeriods);

        for (int i = firstDecisionIndex; i < testEnd; i++)
        {
            // We need candles[i+1] for execution AND candles[i+H] for outcome. The loop
            // bound + the requiredCount check at the top guarantee both exist.
            FeatureSet features = featureEngine.Compute(candles, i);
            RawSignal raw = await signalGenerator.GenerateAsync(features, EmptyMemory, ct).ConfigureAwait(false);
            FinalDecision decision = adaptation.Apply(raw, features);

            decimal executionPrice = candles[i + 1].Open;
            decimal markPrice = candles[i + 1].Close;
            portfolio.Execute(decision.Action, executionPrice, markPrice);

            RawSignal recordedSignal = new(decision.Action, raw.Confidence, raw.Reason);
            Prediction prediction = new(
                Id: Guid.NewGuid(),
                AsOfUtc: features.AsOfUtc,
                Symbol: symbol,
                Features: features,
                Signal: recordedSignal,
                WalkForwardSegment: segment);
            Outcome outcome = OutcomeComputer.Compute(
                prediction, candles, i, options.EvaluationHorizonCandles, options.FeeBps);

            predictions.Add(prediction);
            outcomes.Add(outcome);

            if (store is not null)
            {
                await store.SavePredictionAsync(prediction, ct).ConfigureAwait(false);
                await store.SaveOutcomeAsync(outcome, ct).ConfigureAwait(false);
            }
        }

        return new SegmentResult(
            Segment: segment,
            StrategyLabel: adaptation.Label,
            TestStartUtc: candles[testStart].OpenTimeUtc,
            TestEndUtc: candles[Math.Min(testEnd, candles.Count) - 1].OpenTimeUtc,
            PredictionCount: predictions.Count,
            TradeCount: portfolio.TradeCount,
            PerBarReturns: portfolio.PerBarReturns,
            EquityCurve: portfolio.EquityCurve,
            Predictions: predictions,
            Outcomes: outcomes,
            Diagnostics: diagnostics);
    }
}
