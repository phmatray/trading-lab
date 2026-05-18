namespace TradyStrat.Application.AiSuggestion.Snapshot;

/// <summary>
/// Composite member: contributes one slice of an <see cref="AiSnapshot"/> to the
/// <see cref="SnapshotBuilder"/>. Lower <see cref="Order"/> runs first, so later
/// sections can depend on earlier sections' contributions.
/// </summary>
public interface ISnapshotSectionProvider
{
    int Order { get; }

    Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct);
}
