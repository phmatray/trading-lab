using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed record TradeDraft(
    InstrumentId InstrumentId,
    DateOnly     ExecutedOn,
    TradeSide    Side,
    Quantity     Quantity,
    Price        PricePerShare,
    Money        Fees,
    string       Note);
