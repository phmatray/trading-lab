namespace TradyStrat.Common.Domain;

public sealed record PortfolioSnapshot(
    decimal Shares,
    decimal AvgCostEur,
    decimal CurrentValueEur,
    decimal UnrealizedPnLEur,
    decimal RealizedPnLEur,
    decimal ProgressPct);
