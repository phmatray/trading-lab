using TradyStrat.Application.Goals;

namespace TradyStrat.Application.AiSuggestion.Snapshot.Sections;

public sealed class GoalSection(IGoalRepository goalRepo) : ISnapshotSectionProvider
{
    public int Order => 10;

    public async Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        builder.Goal = await goalRepo.GetAsync(ct);
    }
}
