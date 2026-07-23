namespace TradingSignal.Core.Abstractions;

/// <summary>
/// Metadata describing one backtest run. Persisted to the <c>runs</c> table so
/// predictions (which carry a <c>run_id</c>) can be traced back to the model,
/// market window, and code revision that produced them.
/// </summary>
public sealed record RunMetadata(
    string RunId,
    DateTime StartedUtc,
    string Symbol,
    string Interval,
    int HistoryDays,
    string ModelId,
    string ModelFamily,
    string? GitSha,
    string? Notes);

public interface IPredictionStore
{
    /// <summary>
    /// Persists a prediction, labelled with the owning <paramref name="runId"/> and the
    /// adaptation <paramref name="strategy"/> that produced it. Legacy rows written before
    /// these columns existed read back with empty strings.
    /// </summary>
    Task SavePredictionAsync(Prediction prediction, string runId, string strategy, CancellationToken ct);

    Task SaveOutcomeAsync(Outcome outcome, CancellationToken ct);

    /// <summary>Persists one row in the <c>runs</c> table.</summary>
    Task SaveRunAsync(RunMetadata run, CancellationToken ct);

    Task<IReadOnlyList<(Prediction Prediction, Outcome? Outcome)>> GetSegmentAsync(
        int segment,
        CancellationToken ct);
}
