namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed record ReplaySuggestionsInput(
    int InstrumentId,
    DateOnly Since,
    DateOnly Until,
    bool Persist,
    bool Force);
