namespace TradyStrat.Application.AiSuggestion.Snapshot;

/// <summary>
/// Composite orchestrator. Runs each registered <see cref="ISnapshotSectionProvider"/>
/// in Order ascending against a single <see cref="SnapshotBuilder"/>, then seals the
/// builder into an immutable <see cref="AiSnapshot"/>.
/// </summary>
public sealed class AiSnapshotService(
    IEnumerable<ISnapshotSectionProvider> sections) : IAiSnapshotService
{
    private readonly ISnapshotSectionProvider[] _ordered = sections.OrderBy(s => s.Order).ToArray();

    public async Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        var b = new SnapshotBuilder();
        foreach (var section in _ordered)
            await section.ContributeAsync(b, instrumentId, asOf, ct);
        return b.Build(asOf, instrumentId);
    }
}
