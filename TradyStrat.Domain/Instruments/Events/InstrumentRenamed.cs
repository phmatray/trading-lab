using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Instruments.Events;

public sealed record InstrumentRenamed(
    InstrumentId InstrumentId,
    string       OldName,
    string       NewName,
    DateTime     OccurredAt) : DomainEvent(OccurredAt);
