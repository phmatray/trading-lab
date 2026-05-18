using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed record QuerySuggestionsInput(
    int InstrumentId,
    DateOnly From,
    DateOnly To,
    SuggestionAction? Action,
    int Limit);
