using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed record PositionRow(
    InstrumentId InstrumentId,
    string       Ticker,        // resolved by caller from IInstrumentRepository
    string       Currency,      // resolved by caller (instrument's native currency code)
    Quantity     Quantity,
    Money        CostBasisEur,
    Money        MarketValueEur,
    Money        UnrealizedPnLEur,
    Money        RealizedPnLEur);
