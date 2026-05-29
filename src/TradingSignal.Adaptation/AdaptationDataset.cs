using TradingSignal.Core;
using TradingSignal.Core.Abstractions;
using TradingSignal.Evaluation;

namespace TradingSignal.Adaptation;

internal sealed record AdaptationSample(
    int CandleIndex,
    FeatureSet Features,
    RawSignal Signal,
    Outcome Outcome);

/// <summary>
/// Builds a causal, symmetric few-shot memory shared by the walk-forward test loop
/// and the adaptation-window dataset builder. A <see cref="FewShotCase"/> for bar
/// <c>j</c> may only be surfaced once its outcome is fully observable — i.e. once a
/// later decision index <c>i</c> satisfies <c>j + horizon &lt;= i</c>. This guarantees
/// no look-ahead: the example fed when predicting bar <c>i</c> never depends on a
/// candle at or after <c>i</c>.
///
/// <para>The "best action in hindsight" for bar <c>j</c> is derived from the gross
/// return of entering at <c>candles[j+1].Open</c> and exiting at
/// <c>candles[j+horizon].Close</c>, compared against the round-trip fee:
/// <c>&gt; +fee =&gt; BUY</c>, <c>&lt; -fee =&gt; SELL</c>, else <c>HOLD</c>.</para>
/// </summary>
public sealed class RollingFewShotMemory
{
    private readonly IReadOnlyList<Candle> _candles;
    private readonly IFeatureEngine _featureEngine;
    private readonly int _horizon;
    private readonly double _feeRoundTrip;
    private readonly int _maxFewShot;

    private readonly List<FewShotCase> _cases = new();
    private int _nextCandleToConsider;

    public RollingFewShotMemory(
        IReadOnlyList<Candle> candles,
        IFeatureEngine featureEngine,
        int horizonCandles,
        double feeBps,
        int maxFewShot)
    {
        _candles = candles;
        _featureEngine = featureEngine;
        _horizon = horizonCandles;
        _feeRoundTrip = 2d * feeBps / 10_000d;
        _maxFewShot = maxFewShot;
        _nextCandleToConsider = featureEngine.WarmupPeriods;
    }

    /// <summary>
    /// Returns the most recent (up to MaxFewShot) cases whose outcome is known at
    /// decision index <paramref name="decisionIndex"/>. Empty when MaxFewShot &lt;= 0.
    /// </summary>
    public IReadOnlyList<FewShotCase> MemoryFor(int decisionIndex)
    {
        if (_maxFewShot <= 0) return Array.Empty<FewShotCase>();

        // Materialize every bar j whose outcome is now observable: j + horizon <= decisionIndex.
        // Outcome of j needs candles[j+1] and candles[j+horizon]; both exist because
        // j + horizon <= decisionIndex and the caller already reads candles[decisionIndex].
        while (_nextCandleToConsider + _horizon <= decisionIndex
               && _nextCandleToConsider + _horizon < _candles.Count)
        {
            int j = _nextCandleToConsider;
            FeatureSet features = _featureEngine.Compute(_candles, j);
            decimal entry = _candles[j + 1].Open;
            decimal exit = _candles[j + _horizon].Close;
            double grossReturn = entry == 0m ? 0d : (double)((exit - entry) / entry);

            TradeAction best =
                grossReturn > _feeRoundTrip ? TradeAction.Buy :
                grossReturn < -_feeRoundTrip ? TradeAction.Sell :
                TradeAction.Hold;

            _cases.Add(new FewShotCase(features, best, grossReturn));
            _nextCandleToConsider++;
        }

        int take = Math.Min(_maxFewShot, _cases.Count);
        if (take == 0) return Array.Empty<FewShotCase>();
        return _cases.GetRange(_cases.Count - take, take);
    }
}

/// <summary>
/// Ambient few-shot cap shared between the walk-forward orchestrator (which owns the
/// <see cref="TradingSignal.Backtest"/>-side test loop and lives in another assembly) and
/// the adaptation strategies, which call <c>AdaptationDatasetBuilder.BuildAsync</c>
/// internally without threading the cap through their own APIs. The orchestrator sets
/// <see cref="MaxFewShot"/> immediately before invoking <c>FitAsync</c> so train (adaptation
/// window) and test (orchestrator loop) run on the SAME signal distribution. Backed by an
/// <see cref="AsyncLocal{T}"/> so concurrent runs do not bleed into one another. Default 0
/// =&gt; empty memory (parity with the old behaviour).
/// </summary>
public static class FewShotMemorySettings
{
    private static readonly AsyncLocal<int> MaxFewShotSlot = new();

    public static int MaxFewShot
    {
        get => MaxFewShotSlot.Value;
        set => MaxFewShotSlot.Value = value;
    }
}

internal static class AdaptationDatasetBuilder
{
    // Generates the adaptation-window dataset that both the threshold optimizer
    // and the meta-model train on. LLM calls hit the response cache so subsequent
    // strategy fits within the same segment are effectively free.
    //
    // Few-shot memory is built with the SAME rolling, causal scheme used by the
    // orchestrator's test loop (see RollingFewShotMemory) so threshold/meta models
    // are fit on the distribution they are later applied to.
    public static async Task<IReadOnlyList<AdaptationSample>> BuildAsync(
        AdaptationContext context,
        IFeatureEngine featureEngine,
        ISignalGenerator signalGenerator,
        CancellationToken ct,
        int maxFewShot = -1)
    {
        int effectiveMaxFewShot = maxFewShot >= 0 ? maxFewShot : FewShotMemorySettings.MaxFewShot;

        IReadOnlyList<Candle> candles = context.Candles;
        int adaptStart = Math.Max(context.AdaptStartIndex, featureEngine.WarmupPeriods);

        // Outcomes need candle[i + horizon]; cap to one bar before TestStartIndex so the
        // adaptation window stays disjoint from the test window.
        int adaptEnd = Math.Min(
            context.TestStartIndex,
            candles.Count - context.EvaluationHorizonCandles);

        RollingFewShotMemory memory = new(
            candles, featureEngine, context.EvaluationHorizonCandles, context.FeeBps, effectiveMaxFewShot);

        List<AdaptationSample> samples = new();
        for (int i = adaptStart; i < adaptEnd; i++)
        {
            FeatureSet features = featureEngine.Compute(candles, i);
            IReadOnlyList<FewShotCase> fewShot = memory.MemoryFor(i);
            RawSignal signal = await signalGenerator.GenerateAsync(features, fewShot, ct).ConfigureAwait(false);
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
