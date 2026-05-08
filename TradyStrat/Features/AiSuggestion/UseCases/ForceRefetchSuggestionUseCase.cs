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
    IConfiguration config,
    ILogger<ForceRefetchSuggestionUseCase> log)
    : UseCaseBase<Unit, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(Unit _, CancellationToken ct)
    {
        var focusTicker = config["Tickers:Focus"]
            ?? throw new InvalidOperationException("Tickers:Focus is not configured.");
        var today = clock.TodayInExchangeTzFor(focusTicker);

        // Acquire the shared gate so the delete+insert pair runs as a single
        // critical section against any concurrent GetTodaysSuggestion or
        // ForceRefetch — otherwise two concurrent rerun clicks both pass
        // the existence check, then both try to INSERT and the second hits
        // the UQ(ForDate) constraint.
        await SuggestionGate.Instance.WaitAsync(ct);
        try
        {
            var existing = await repo.FirstOrDefaultAsync(new SuggestionForDateSpec(today), ct);
            if (existing is not null) await repo.DeleteAsync(existing, ct);

            var snap  = await snapshotFactory.CreateAsync(today, ct);
            var fresh = await ai.AskAsync(snap, ct);
            await repo.AddAsync(fresh, ct);
            return fresh;
        }
        finally
        {
            SuggestionGate.Instance.Release();
        }
    }
}
