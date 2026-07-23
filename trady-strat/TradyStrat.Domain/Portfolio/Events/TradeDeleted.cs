using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared.Money;

namespace TradyStrat.Domain.Portfolio.Events;

public sealed record TradeDeleted(
    TradeId    TradeId,
    PositionId PositionId,
    Money      RealizedDelta,
    DateTime   OccurredAt) : DomainEvent(OccurredAt);
