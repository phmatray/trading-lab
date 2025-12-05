namespace TradingStrat.Models;

public record EquityPoint(
    DateTime DateTime,
    decimal Equity,
    int Position
);
