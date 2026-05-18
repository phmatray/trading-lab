namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed record BackfillSuggestionsInput(DateOnly Date, int InstrumentId);
