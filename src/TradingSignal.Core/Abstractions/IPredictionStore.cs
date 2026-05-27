namespace TradingSignal.Core.Abstractions;

public interface IPredictionStore
{
    Task SavePredictionAsync(Prediction prediction, CancellationToken ct);

    Task SaveOutcomeAsync(Outcome outcome, CancellationToken ct);

    Task<IReadOnlyList<(Prediction Prediction, Outcome? Outcome)>> GetSegmentAsync(
        int segment,
        CancellationToken ct);
}
