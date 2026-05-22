using Ardalis.Specification;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using DomainSuggestionGate = TradyStrat.Domain.Suggestions.Services.SuggestionGate;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed class GetTodaysSuggestionUseCase(
    ISuggestionRepository suggestions,
    IAiSnapshotService snapshotService,
    IAiClient ai,
    IClock clock,
    IReadRepositoryBase<Instrument> instruments,
    ILogger<GetTodaysSuggestionUseCase> log)
    : UseCaseBase<GetTodaysSuggestionInput, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(
        GetTodaysSuggestionInput input, CancellationToken ct)
    {
        var instrument = await instruments.GetByIdAsync(input.InstrumentId, ct)
            ?? throw new InstrumentNotFoundException(
                $"Instrument id {input.InstrumentId} not registered.");

        var today = clock.TodayInExchangeTzFor(instrument.Ticker);
        var iid = new InstrumentId(instrument.Id);

        // Fast path: row already there, no gate needed.
        var existing = await suggestions.GetForAsync(iid, today, ct);
        if (existing is not null) return existing;

        // Slow path: serialize concurrent writers via the shared plumbing gate,
        // then re-check inside the gate (a peer may have inserted while we
        // waited). The gate is process-wide; inter-request races (two browser
        // tabs, refresh during a re-run) are its primary value.
        var gate = SuggestionGatePlumbing.For(today, instrument.Id);
        await gate.WaitAsync(ct);
        try
        {
            var snap = await snapshotService.CreateAsync(instrument.Id, today, ct);
            var candidateFp = PromptFingerprint.Of(
                snap.PromptHash, snap.EnvelopeHash, snap.PromptVersionHash);

            // Re-fetch after acquiring the gate; bind to the domain decision.
            existing = await suggestions.GetForAsync(iid, today, ct);
            var decision = DomainSuggestionGate.Decide(existing, candidateFp);
            if (decision is GateDecision.Reuse reuse) return reuse.Existing;

            var response = await ai.AskAsync(snap, ct);
            var fresh = SuggestionBuilder.FromAiResponse(response, snap, clock.UtcNow());
            await suggestions.AddAsync(fresh, ct);
            return fresh;
        }
        finally
        {
            gate.Release();
        }
    }
}
