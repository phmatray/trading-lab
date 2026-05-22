namespace TradyStrat.Domain;

[System.Obsolete("Replaced by TradyStrat.Domain.Portfolio.Lot in Phase 2 — removed with PortfolioService.")]
public sealed record Lot(DateOnly OpenedOn, decimal Quantity, decimal UnitCostEur)
{
    public decimal CostBasisEur => Quantity * UnitCostEur;
}
