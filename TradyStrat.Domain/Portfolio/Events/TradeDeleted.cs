using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio.Events;

public sealed record TradeDeleted(
    PositionId PositionId,
    Money      RealizedDelta,
    DateTime   OccurredAt) : DomainEvent(OccurredAt);
