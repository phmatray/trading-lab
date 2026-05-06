using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Abstractions;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;
using TradyStrat.Specifications.Suggestions;

namespace TradyStrat.Application.UseCases.AiSuggestion;

public sealed class ForceRefetchSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotBuilder snapshotBuilder,
    IAiClient ai,
    IClock clock,
    ILogger<ForceRefetchSuggestionUseCase> log)
    : UseCaseBase<Unit, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(Unit _, CancellationToken ct)
    {
        var today = clock.TodayInExchangeTzFor("CON3.DE");
        var existing = await repo.FirstOrDefaultAsync(new SuggestionForDateSpec(today), ct);
        if (existing is not null) await repo.DeleteAsync(existing, ct);

        var snap  = await snapshotBuilder.BuildAsync(ct);
        var fresh = await ai.AskAsync(snap, ct);
        await repo.AddAsync(fresh, ct);
        return fresh;
    }
}
