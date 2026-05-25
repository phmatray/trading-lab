using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.Settings;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared.Market;
using TradyStrat.Domain.Suggestions;
using DomainSuggestionGate = TradyStrat.Domain.Suggestions.Services.SuggestionGate;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed class GetTodaysSuggestionUseCase(
    ISuggestionRepository suggestions,
    IAiSnapshotService snapshotService,
    IAiClient ai,
    IClock clock,
    IInstrumentRepository instruments,
    IDomainEventDispatcher dispatcher,
    ILogger<GetTodaysSuggestionUseCase> log)
    : UseCaseBase<GetTodaysSuggestionInput, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(
        GetTodaysSuggestionInput input, CancellationToken ct)
    {
        var iid = new InstrumentId(input.InstrumentId);
        var instrument = await instruments.GetAsync(iid, ct)
            ?? throw new InstrumentNotFoundException(
                $"Instrument id {input.InstrumentId} not registered.");

        var today = clock.TodayInExchangeTzFor(instrument.Ticker);

        // Fast path: row already there, no gate needed.
        var existing = await suggestions.GetForAsync(iid, today, ct);
        if (existing is not null) return existing;

        // Slow path: serialize concurrent writers via the shared plumbing gate,
        // then re-check inside the gate (a peer may have inserted while we
        // waited). The gate is process-wide; inter-request races (two browser
        // tabs, refresh during a re-run) are its primary value.
        var gate = SuggestionGatePlumbing.For(today, input.InstrumentId);
        await gate.WaitAsync(ct);
        try
        {
            var snap = await snapshotService.CreateAsync(input.InstrumentId, today, ct);
            var candidateFp = PromptFingerprint.Of(
                snap.PromptHash, snap.EnvelopeHash, snap.PromptVersionHash);

            // Re-fetch after acquiring the gate; bind to the domain decision.
            existing = await suggestions.GetForAsync(iid, today, ct);
            var decision = DomainSuggestionGate.Decide(existing, candidateFp);
            if (decision is GateDecision.Reuse reuse) return reuse.Existing;

            var response = await ai.AskAsync(snap, ct);
            var fresh = SuggestionBuilder.FromAiResponse(response, snap, clock.UtcNow());
            var events = await suggestions.AddAsync(fresh, ct);
            await dispatcher.DispatchAsync(events, ct);
            return fresh;
        }
        finally
        {
            gate.Release();
        }
    }
}
