namespace TradyStrat.Common.Domain;

public sealed record PositionRow(
    int InstrumentId,
    string Ticker,
    string Currency,
    decimal Quantity,
    decimal CostBasisEur,
    decimal MarketValueEur,
    decimal UnrealizedPnLEur,
    decimal RealizedPnLEur);
