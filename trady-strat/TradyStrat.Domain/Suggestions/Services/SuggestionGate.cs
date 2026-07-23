namespace TradyStrat.Domain.Suggestions.Services;

/// <summary>
/// Pure domain decision: given an optional existing Suggestion for
/// (instrumentId, date) and a candidate PromptFingerprint, decide whether
/// to reuse the existing row or fetch a fresh AI suggestion.
///
/// Reuse iff: existing != null AND existing.Fingerprint matches the candidate.
/// The per-(date, instrumentId) concurrency plumbing is separate
/// (Application/AiSuggestion/SuggestionGatePlumbing).
/// </summary>
public static class SuggestionGate
{
    public static GateDecision Decide(Suggestion? existing, PromptFingerprint candidateFingerprint)
    {
        if (existing is null) return new GateDecision.Fetch();
        if (existing.Fingerprint == candidateFingerprint) return new GateDecision.Reuse(existing);
        return new GateDecision.Fetch();
    }
}
