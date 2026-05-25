using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.Settings;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.PriceFeed;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain.Suggestions.Services;

namespace TradyStrat.Application.AiSuggestion.UseCases;

/// <summary>
/// Re-runs the AI prompt against historical snapshots over a date range and
/// scores the resulting suggestions. Application-resident so unit tests can
/// exercise the loop with stub clients without bringing in the Spectre CLI.
/// Spec §7.
/// </summary>
public sealed class ReplaySuggestionsUseCase(
    IAiSnapshotService snapshots,
    IAiClient ai,
    IPriceBarReadRepository bars,
    IInstrumentRepository instruments,
    ISuggestionRepository suggestionRepo,
    ICorrectnessRule correctness,
    IDomainEventDispatcher dispatcher,
    IClock clock,
    ILogger<ReplaySuggestionsUseCase> log)
    : UseCaseBase<ReplaySuggestionsInput, ReplayReport>(log)
{
    private const int ForwardBars = 5;

    protected override async Task<ReplayReport> ExecuteCore(ReplaySuggestionsInput input, CancellationToken ct)
    {
        var instrument = await instruments.GetAsync(new InstrumentId(input.InstrumentId), ct)
            ?? throw new InvalidOperationException($"Instrument id {input.InstrumentId} not found.");
        var ticker = instrument.Ticker;
        var iid = new InstrumentId(input.InstrumentId);

        var rows = new List<ReplayedSuggestion>();
        var versions = new HashSet<string>(StringComparer.Ordinal);

        for (var date = input.Since; date <= input.Until; date = date.AddDays(1))
        {
            // Only act on dates that have a price bar — skips weekends/holidays.
            var firstBar = await bars.ListSinceAsync(ticker, date, ct);
            if (firstBar.Count == 0 || firstBar[0].Date != date) continue;

            AiSnapshot snapshot;
            try { snapshot = await snapshots.CreateAsync(input.InstrumentId, date, ct); }
            catch { continue; }   // skip dates the snapshot service can't build

            var response = await ai.AskAsync(snapshot, ct);
            var suggestion = SuggestionBuilder.FromAiResponse(response, snapshot, clock.UtcNow());

            // Score against the next 5 stored bars.
            var window = firstBar.Take(ForwardBars + 1).ToArray();
            bool isComplete = window.Length >= ForwardBars + 1;
            decimal fwdReturnPct = 0m;
            bool wasCorrect = false;
            if (isComplete)
            {
                var closeAt  = window[0].Close;
                var closeFwd = window[ForwardBars].Close;
                fwdReturnPct = closeAt == 0m ? 0m : (closeFwd - closeAt) / closeAt * 100m;
                wasCorrect   = correctness.Evaluate(suggestion.Action, fwdReturnPct);
            }

            var versionHash = snapshot.PromptVersionHash;
            versions.Add(versionHash);
            rows.Add(new ReplayedSuggestion(
                ForDate: date,
                Action: suggestion.Action,
                Conviction: suggestion.Conviction.Value,
                FwdReturnPct: fwdReturnPct,
                WasCorrect: wasCorrect,
                IsForwardWindowComplete: isComplete,
                PromptVersionHash: versionHash));

            if (input.Persist)
            {
                var existing = await suggestionRepo.GetForAsync(iid, date, ct);
                if (existing is not null && !input.Force)
                    throw new InvalidOperationException(
                        $"Suggestion already exists for instrument {input.InstrumentId} on {date}. Pass --force to replace.");
                if (existing is not null) await suggestionRepo.RemoveAsync(existing, ct);
                var events = await suggestionRepo.AddAsync(suggestion, ct);
                await dispatcher.DispatchAsync(events, ct);
            }
        }

        var scored = rows.Where(r => r.IsForwardWindowComplete).ToArray();
        var perAction = scored
            .GroupBy(r => r.Action)
            .ToDictionary(g => g.Key, g => new ActionAggregate(
                Count:           g.Count(),
                HitRatePct:      (decimal)g.Count(r => r.WasCorrect) / g.Count() * 100m,
                AvgFwdReturnPct: g.Average(r => r.FwdReturnPct),
                AvgConviction:   g.Average(r => (decimal)r.Conviction)));

        var overall = new ActionAggregate(
            Count:           scored.Length,
            HitRatePct:      scored.Length == 0 ? 0m : (decimal)scored.Count(r => r.WasCorrect) / scored.Length * 100m,
            AvgFwdReturnPct: scored.Length == 0 ? 0m : scored.Average(r => r.FwdReturnPct),
            AvgConviction:   scored.Length == 0 ? 0m : scored.Average(r => (decimal)r.Conviction));

        var weightSum   = scored.Sum(r => r.Conviction);
        var weightedSum = scored.Sum(r => r.Conviction * (r.WasCorrect ? 1m : 0m));
        var convictionWeightedScore = weightSum == 0 ? 0m : weightedSum / weightSum;

        return new ReplayReport(
            InstrumentId:                input.InstrumentId,
            Since:                       input.Since,
            Until:                       input.Until,
            Rows:                        rows,
            PerAction:                   perAction,
            Overall:                     overall,
            ConvictionWeightedScore:     convictionWeightedScore,
            DistinctPromptVersionHashes: versions.ToList());
    }
}
