using Ardalis.Specification;
using TradyStrat.Application.Abstractions;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;
using TradyStrat.Specifications.Suggestions;

namespace TradyStrat.Application.UseCases.AiSuggestion;

public sealed class GetTodaysSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotFactory snapshotFactory,
    IAiClient ai,
    IClock clock,
    ILogger<GetTodaysSuggestionUseCase> log)
    : UseCaseBase<Unit, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(Unit _, CancellationToken ct)
    {
        var today = clock.TodayInExchangeTzFor("CON3.L");
        var existing = await repo.FirstOrDefaultAsync(new SuggestionForDateSpec(today), ct);
        if (existing is not null) return existing;

        var snap  = await snapshotFactory.CreateAsync(today, ct);
        var fresh = await ai.AskAsync(snap, ct);
        await repo.AddAsync(fresh, ct);
        return fresh;
    }
}
