using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed record Lot(DateOnly OpenedOn, Quantity Quantity, Money UnitCost)
{
    public Money CostBasis => UnitCost * Quantity.Value;

    public Lot WithQuantity(Quantity newQuantity) => this with { Quantity = newQuantity };
}
