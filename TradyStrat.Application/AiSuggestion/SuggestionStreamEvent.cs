using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion;

/// <summary>
/// One event emitted by <see cref="UseCases.StreamTodaysSuggestionsUseCase"/>
/// — either a successful suggestion or a per-instrument failure. The stream
/// emits one event per held instrument; failures never block other workers.
/// </summary>
public abstract record SuggestionStreamEvent(int InstrumentId)
{
    public sealed record Ready(int InstrumentId, Suggestion Suggestion)
        : SuggestionStreamEvent(InstrumentId);

    public sealed record Failed(int InstrumentId, string Reason)
        : SuggestionStreamEvent(InstrumentId);
}
