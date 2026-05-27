using TradingSignal.Adaptation.MetaModel;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;

namespace TradingSignal.Adaptation;

// Composition order (spec §9.3): raw → threshold filter → meta-model gate.
// Each layer can be measured independently (NullAdaptation, ThresholdOptimizer,
// CompositeStrategy) so the marginal value of each is visible on out-of-sample data.
public sealed class CompositeStrategy(
    IFeatureEngine featureEngine,
    ISignalGenerator signalGenerator) : IAdaptationStrategy
{
    private readonly ThresholdOptimizer _threshold = new(featureEngine, signalGenerator);
    private readonly MetaModelStrategy _metaModel = new(featureEngine, signalGenerator);

    public string Label => $"+threshold+meta(τ*={_threshold.SelectedThreshold:F2})";

    public double SelectedThreshold => _threshold.SelectedThreshold;
    public double LastTrainAccuracy => _metaModel.LastTrainAccuracy;

    public IReadOnlyDictionary<string, double> Diagnostics => new Dictionary<string, double>
    {
        ["selected_threshold"] = _threshold.SelectedThreshold,
        ["meta_train_accuracy"] = _metaModel.LastTrainAccuracy,
    };

    public async Task FitAsync(AdaptationContext context, CancellationToken ct)
    {
        // Build the adaptation dataset once, reuse for both layers. The signal
        // generator's response cache makes a second BuildAsync free anyway, but
        // this avoids the redundant outcome computation.
        IReadOnlyList<AdaptationSample> samples = await AdaptationDatasetBuilder
            .BuildAsync(context, featureEngine, signalGenerator, ct)
            .ConfigureAwait(false);

        double tau = ThresholdOptimizer.PickBestThreshold(samples, context.PeriodsPerYear);
        _threshold.SetThresholdForReuse(tau);
        _metaModel.TrainOnSamples(samples);
    }

    public FinalDecision Apply(RawSignal raw, FeatureSet features)
    {
        FinalDecision gated = _threshold.Apply(raw, features);
        if (gated.Action == TradeAction.Hold) return gated;
        return _metaModel.Apply(raw, features);
    }
}
