using TradingSignal.Core;
using TradingSignal.Core.Abstractions;
using TradingSignal.Evaluation;

namespace TradingSignal.Adaptation;

internal sealed record AdaptationSample(
    int CandleIndex,
    FeatureSet Features,
    RawSignal Signal,
    Outcome Outcome);

internal static class AdaptationDatasetBuilder
{
    // Generates the adaptation-window dataset that both the threshold optimizer
    // and the meta-model train on. LLM calls hit the response cache so subsequent
    // strategy fits within the same segment are effectively free.
    public static async Task<IReadOnlyList<AdaptationSample>> BuildAsync(
        AdaptationContext context,
        IFeatureEngine featureEngine,
        ISignalGenerator signalGenerator,
        CancellationToken ct)
    {
        IReadOnlyList<Candle> candles = context.Candles;
        int adaptStart = Math.Max(context.AdaptStartIndex, featureEngine.WarmupPeriods);

        // Outcomes need candle[i + horizon]; cap to one bar before TestStartIndex so the
        // adaptation window stays disjoint from the test window.
        int adaptEnd = Math.Min(
            context.TestStartIndex,
            candles.Count - context.EvaluationHorizonCandles);

        List<AdaptationSample> samples = new();
        for (int i = adaptStart; i < adaptEnd; i++)
        {
            FeatureSet features = featureEngine.Compute(candles, i);
            RawSignal signal = await signalGenerator.GenerateAsync(features, Array.Empty<FewShotCase>(), ct).ConfigureAwait(false);
            Prediction temp = new(
                Id: Guid.Empty,
                AsOfUtc: features.AsOfUtc,
                Symbol: context.Symbol,
                Features: features,
                Signal: signal,
                WalkForwardSegment: context.Segment);
            Outcome outcome = OutcomeComputer.Compute(
                temp, candles, i, context.EvaluationHorizonCandles, context.FeeBps);
            samples.Add(new AdaptationSample(i, features, signal, outcome));
        }
        return samples;
    }
}
