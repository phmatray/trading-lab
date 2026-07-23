using TradyStrat.Domain.Shared.Money;

namespace TradyStrat.Domain.Portfolio;

public sealed record PortfolioSnapshot(
    IReadOnlyList<PositionRow> Positions,
    Money   CurrentValueEur,
    Money   CostBasisEur,
    Money   UnrealizedPnLEur,
    Money   RealizedPnLEur,
    decimal ProgressPct,
    // Legacy scalars retained for HeroCapital/PortfolioRail/GrowthChart consumers.
    // Populated only when there's exactly one position. Spec §13.1 — removed when
    // the dashboard view-model rewrite lands.
    decimal Shares,
    Money   AvgCostEur);
