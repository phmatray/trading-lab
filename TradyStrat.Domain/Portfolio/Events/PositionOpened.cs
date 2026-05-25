using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio.Events;

public sealed record PositionOpened(
    PositionId   PositionId,
    InstrumentId InstrumentId,
    DateTime     OccurredAt) : DomainEvent(OccurredAt);
