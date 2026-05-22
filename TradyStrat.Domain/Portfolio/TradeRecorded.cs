using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed record TradeRecorded(
    TradeId    TradeId,
    PositionId PositionId,
    bool       CreatedPosition,
    Money      RealizedDelta);
