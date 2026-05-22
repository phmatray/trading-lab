using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed record TradeDeleted(
    PositionId PositionId,
    Money      RealizedDelta);
