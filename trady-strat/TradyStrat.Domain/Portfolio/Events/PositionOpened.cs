using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Portfolio.Events;

public sealed record PositionOpened(
    PositionId   PositionId,
    InstrumentId InstrumentId,
    DateTime     OccurredAt) : DomainEvent(OccurredAt);
