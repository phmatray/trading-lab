using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Instruments.Events;

public sealed record InstrumentRenamed(
    InstrumentId InstrumentId,
    string       OldName,
    string       NewName,
    DateTime     OccurredAt) : DomainEvent(OccurredAt);
