using Ardalis.Specification;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Common.UseCases;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Time;
using TradyStrat.Features.AiSuggestion.Specifications;

namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed class GetTodaysSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotFactory snapshotFactory,
    IAiClient ai,
    IClock clock,
    IConfiguration config,
    ILogger<GetTodaysSuggestionUseCase> log)
    : UseCaseBase<Unit, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(Unit _, CancellationToken ct)
    {
        var focusTicker = config["Tickers:Focus"]
            ?? throw new InvalidOperationException("Tickers:Focus is not configured.");
        var today = clock.TodayInExchangeTzFor(focusTicker);

        // Fast path: row already there, no gate needed.
        var existing = await repo.FirstOrDefaultAsync(new SuggestionForDateSpec(today), ct);
        if (existing is not null) return existing;

        // Slow path: serialize concurrent writers via the shared gate, then
        // re-check inside the gate (a peer may have inserted while we waited).
        await SuggestionGate.Instance.WaitAsync(ct);
        try
        {
            existing = await repo.FirstOrDefaultAsync(new SuggestionForDateSpec(today), ct);
            if (existing is not null) return existing;

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
