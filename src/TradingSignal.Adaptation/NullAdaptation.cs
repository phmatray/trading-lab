using TradingSignal.Core;

namespace TradingSignal.Adaptation;

public sealed class NullAdaptation : IAdaptationStrategy
{
    private static readonly IReadOnlyDictionary<string, double> Empty = new Dictionary<string, double>();

    public string Label => "llm-only";

    public IReadOnlyDictionary<string, double> Diagnostics => Empty;

    public Task FitAsync(AdaptationContext context, CancellationToken ct) => Task.CompletedTask;

    public FinalDecision Apply(RawSignal raw, FeatureSet features) => new(raw.Action);
}
