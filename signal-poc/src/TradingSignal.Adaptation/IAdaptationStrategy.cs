using TradingSignal.Core;

namespace TradingSignal.Adaptation;

public sealed record FinalDecision(TradeAction Action, double? Probability = null);

public sealed record AdaptationContext(
    int Segment,
    IReadOnlyList<Candle> Candles,
    int AdaptStartIndex,
    int TestStartIndex,
    string Symbol,
    double FeeBps,
    int EvaluationHorizonCandles,
    int PeriodsPerYear);

public interface IAdaptationStrategy
{
    // Fit on the adaptation window [AdaptStartIndex, TestStartIndex). Project 6
    // uses a fixed threshold so Fit is a no-op; Project 7 (sweep + meta-model)
    // does real work here.
    Task FitAsync(AdaptationContext context, CancellationToken ct);

    FinalDecision Apply(RawSignal raw, FeatureSet features);

    string Label { get; }

    // Snapshot of fit-time diagnostics (e.g., selected τ, in-sample meta-model
    // accuracy). Orchestrator captures this per segment so reports can show how
    // adaptation parameters drift across segments. Empty for pass-through strategies.
    IReadOnlyDictionary<string, double> Diagnostics { get; }
}
