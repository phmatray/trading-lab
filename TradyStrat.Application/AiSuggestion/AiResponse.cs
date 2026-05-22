using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion;

/// <summary>
/// Anthropic-agnostic AI response shape used by IAiClient implementations.
/// The use case is responsible for converting this DTO into a Suggestion AR
/// via SuggestionBuilder.FromAiResponse — keeps the AI adapter free of domain
/// invariants and factory wiring.
/// </summary>
public sealed record AiResponse(
    SuggestionAction Action,
    decimal? QuantityHint,
    decimal? MaxPriceHint,
    int Conviction,
    string Rationale,
    IReadOnlyList<Citation> Citations,
    MarketSnapshot Snapshot,
    string ThinkingText);
