namespace TradyStrat.Domain;

public sealed record PortfolioSnapshot(
    IReadOnlyList<PositionRow> Positions,
    decimal CurrentValueEur,    // sum of Positions.MarketValueEur
    decimal CostBasisEur,       // sum of Positions.CostBasisEur
    decimal UnrealizedPnLEur,   // CurrentValueEur - CostBasisEur
    decimal RealizedPnLEur,     // sum of per-ticker realized
    decimal ProgressPct,        // CurrentValueEur / GoalEur * 100
    // Legacy scalars retained for callers (HeroCapital, PortfolioRail, GrowthChart)
    // until the dashboard view-model rewrite (Task 14) lands. Populated only when
    // the snapshot has exactly one position; zero otherwise.
    decimal Shares,
    decimal AvgCostEur);
