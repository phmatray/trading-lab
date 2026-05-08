namespace TradyStrat.Features.AiSuggestion.UseCases;

public sealed record BackfillSuggestionsInput(DateOnly Date, int InstrumentId);
