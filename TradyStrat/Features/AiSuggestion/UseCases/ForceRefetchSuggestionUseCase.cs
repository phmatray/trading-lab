using Ardalis.Specification;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Common.UseCases;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Time;
using TradyStrat.Features.AiSuggestion.Specifications;

namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed class ForceRefetchSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotFactory snapshotFactory,
    IAiClient ai,
    IClock clock,
    ILogger<ForceRefetchSuggestionUseCase> log)
    : UseCaseBase<Unit, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(Unit _, CancellationToken ct)
    {
        var today = clock.TodayInExchangeTzFor("CON3.L");
        var existing = await repo.FirstOrDefaultAsync(new SuggestionForDateSpec(today), ct);
        if (existing is not null) await repo.DeleteAsync(existing, ct);

        var snap  = await snapshotFactory.CreateAsync(today, ct);
        var fresh = await ai.AskAsync(snap, ct);
        await repo.AddAsync(fresh, ct);
        return fresh;
    }
}
