using TradingSignal.Core;

namespace TradingSignal.Adaptation;

// Drops signals below a fixed confidence threshold to HOLD. Project 7 will add
// a per-segment optimizer that picks the threshold from the adaptation window.
public sealed class FixedThresholdGate(double threshold) : IAdaptationStrategy
{
    public double Threshold => threshold;

    public string Label => $"threshold>={threshold:F2}";

    public IReadOnlyDictionary<string, double> Diagnostics => new Dictionary<string, double>
    {
        ["selected_threshold"] = threshold,
    };

    public Task FitAsync(AdaptationContext context, CancellationToken ct) => Task.CompletedTask;

    public FinalDecision Apply(RawSignal raw, FeatureSet features)
    {
        if (raw.Confidence < threshold) return new FinalDecision(TradeAction.Hold, raw.Confidence);
        return new FinalDecision(raw.Action, raw.Confidence);
    }
}
