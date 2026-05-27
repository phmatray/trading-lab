using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;
using TradingSignal.Evaluation.Metrics;

namespace TradingSignal.Adaptation;

// Spec §9.1: sweep τ ∈ {0.5, 0.55, ..., 0.9} on the adaptation window; for each
// τ, simulate "take signals with confidence ≥ τ, others → HOLD" and compute
// Sharpe net of fees. Pick the τ that maximises Sharpe. Apply that τ at test
// time. Reports the chosen τ per segment via the Label so report-time can show
// drift over segments.
public sealed partial class ThresholdOptimizer(
    IFeatureEngine featureEngine,
    ISignalGenerator signalGenerator,
    ILogger<ThresholdOptimizer>? logger = null)
    : IAdaptationStrategy
{
    private static readonly double[] Candidates =
        { 0.50, 0.55, 0.60, 0.65, 0.70, 0.75, 0.80, 0.85, 0.90 };

    private readonly ILogger<ThresholdOptimizer> _logger = logger ?? NullLogger<ThresholdOptimizer>.Instance;

    private double _selectedThreshold = 0.5;

    public double SelectedThreshold => _selectedThreshold;

    public string Label => $"+threshold(τ*={_selectedThreshold:F2})";

    public IReadOnlyDictionary<string, double> Diagnostics => new Dictionary<string, double>
    {
        ["selected_threshold"] = _selectedThreshold,
    };

    public async Task FitAsync(AdaptationContext context, CancellationToken ct)
    {
        IReadOnlyList<AdaptationSample> samples = await AdaptationDatasetBuilder
            .BuildAsync(context, featureEngine, signalGenerator, ct)
            .ConfigureAwait(false);

        _selectedThreshold = PickBestThreshold(samples, context.PeriodsPerYear);
        LogSelectedThreshold(_logger, context.Segment, _selectedThreshold, samples.Count);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Segment {Segment}: τ* = {Threshold:F2} from {N} adaptation samples")]
    private static partial void LogSelectedThreshold(ILogger logger, int segment, double threshold, int n);

    public FinalDecision Apply(RawSignal raw, FeatureSet features)
    {
        if (raw.Confidence < _selectedThreshold)
            return new FinalDecision(TradeAction.Hold, raw.Confidence);
        return new FinalDecision(raw.Action, raw.Confidence);
    }

    // Lets CompositeStrategy seed τ from a shared sweep without re-fitting.
    internal void SetThresholdForReuse(double tau) => _selectedThreshold = tau;

    internal static double PickBestThreshold(IReadOnlyList<AdaptationSample> samples, int periodsPerYear)
    {
        if (samples.Count == 0) return Candidates[0];

        double bestThreshold = Candidates[0];
        double bestSharpe = double.NegativeInfinity;

        foreach (double tau in Candidates)
        {
            double[] gated = new double[samples.Count];
            for (int i = 0; i < samples.Count; i++)
            {
                AdaptationSample s = samples[i];
                gated[i] = s.Signal.Confidence >= tau && s.Signal.Action != TradeAction.Hold
                    ? s.Outcome.RealizedReturnPct
                    : 0d;
            }

            double sharpe = ReturnMetrics.Compute(gated, periodsPerYear).AnnualizedSharpe;
            if (sharpe > bestSharpe)
            {
                bestSharpe = sharpe;
                bestThreshold = tau;
            }
        }

        return bestThreshold;
    }
}
