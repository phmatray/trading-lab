using Shouldly;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Portfolio;

public class LotTests
{
    [Fact]
    public void CostBasis_is_quantity_times_unit_cost()
    {
        var lot = new global::TradyStrat.Domain.Portfolio.Lot(
            openedOn: new DateOnly(2026, 1, 1),
            quantity: Quantity.Of(10m),
            unitCost: Money.Of(4.20m, Currency.Eur));

        lot.CostBasis.ShouldBe(Money.Of(42.00m, Currency.Eur));
    }

    [Fact]
    public void WithQuantity_returns_new_lot_with_updated_quantity()
    {
        var lot = new global::TradyStrat.Domain.Portfolio.Lot(
            openedOn: new DateOnly(2026, 1, 1),
            quantity: Quantity.Of(10m),
            unitCost: Money.Of(4m, Currency.Eur));

        var trimmed = lot.WithQuantity(Quantity.Of(7m));

        trimmed.Quantity.ShouldBe(Quantity.Of(7m));
        trimmed.UnitCost.ShouldBe(Money.Of(4m, Currency.Eur));
        trimmed.OpenedOn.ShouldBe(lot.OpenedOn);
    }
}
