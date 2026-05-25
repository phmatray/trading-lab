using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Suggestions.Events;

public sealed record SuggestionCreated(
    SuggestionId     SuggestionId,
    InstrumentId     InstrumentId,
    DateOnly         ForDate,
    SuggestionAction Action,
    DateTime         OccurredAt) : DomainEvent(OccurredAt);
