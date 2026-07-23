using TradyStrat.Domain.Shared.Money;

namespace TradyStrat.Domain.Portfolio;

public sealed record Lot
{
    public DateOnly OpenedOn { get; private set; }
    public Quantity Quantity { get; private set; } = Quantity.None;
    public Money    UnitCost { get; private set; } = Money.Zero(Currency.Eur);

    private Lot() { }   // EF materialization

    public Lot(DateOnly openedOn, Quantity quantity, Money unitCost)
    {
        OpenedOn = openedOn;
        Quantity = quantity;
        UnitCost = unitCost;
    }

    public Money CostBasis => UnitCost * Quantity.Value;

    public Lot WithQuantity(Quantity newQuantity) => this with { Quantity = newQuantity };
}
