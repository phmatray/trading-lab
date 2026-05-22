using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion;

/// <summary>
/// Maps an AI response + the snapshot that elicited it onto a Suggestion AR
/// via the Suggestion.From factory. Shared by every use case that persists a
/// fresh AI suggestion (GetTodays, ForceRefetch, Backfill, Replay) so the
/// VO/factory wiring lives in one place.
/// </summary>
internal static class SuggestionBuilder
{
    public static Suggestion FromAiResponse(
        AiResponse response,
        AiSnapshot snapshot,
        DateTime createdAt)
    {
        var fingerprint = PromptFingerprint.Of(
            snapshot.PromptHash, snapshot.EnvelopeHash, snapshot.PromptVersionHash);

        var quantity = response.QuantityHint is { } q ? Quantity.Of(q) : Quantity.None;
        var price    = response.MaxPriceHint is { } p
                          ? Price.Of(Money.Of(p, Currency.Eur))
                          : Price.None(Currency.Eur);

        return Suggestion.From(
            instrumentId: new InstrumentId(snapshot.InstrumentId),
            forDate:      snapshot.Today,
            action:       response.Action,
            quantityHint: quantity,
            maxPriceHint: price,
            conviction:   Conviction.Of(response.Conviction),
            rationale:    response.Rationale,
            citations:    response.Citations,
            snapshot:     response.Snapshot,
            fingerprint:  fingerprint,
            thinkingText: response.ThinkingText,
            createdAt:    createdAt);
    }
}
