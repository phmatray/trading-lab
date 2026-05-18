namespace TradyStrat.Domain;

public sealed record Lot(DateOnly OpenedOn, decimal Quantity, decimal UnitCostEur)
{
    public decimal CostBasisEur => Quantity * UnitCostEur;
}
