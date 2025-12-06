namespace TradingStrat.Domain.Entities;

public record EquityPoint(
    DateTime DateTime,
    decimal Equity,
    int Position
);
