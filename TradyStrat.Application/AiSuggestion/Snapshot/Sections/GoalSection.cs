using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Snapshot.Sections;

public sealed class GoalSection(
    IReadRepositoryBase<GoalConfig> goalRepo,
    IClock clock) : ISnapshotSectionProvider
{
    public int Order => 10;

    public async Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        builder.Goal = await goalRepo.GetByIdAsync(1, ct)
            ?? GoalConfig.Default(clock.UtcNow());
    }
}
