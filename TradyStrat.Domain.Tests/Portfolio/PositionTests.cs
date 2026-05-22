using Shouldly;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Portfolio;

public class PositionTests
{
    [Fact]
    public void New_position_for_instrument_is_empty()
    {
        var p = Position.OpenFor(new InstrumentId(7));

        p.InstrumentId.ShouldBe(new InstrumentId(7));
        p.OpenLots.ShouldBeEmpty();
        p.Trades.ShouldBeEmpty();
        p.TotalQuantity.ShouldBe(Quantity.Zero);
        p.CostBasis.ShouldBe(Money.Zero(Currency.Eur));
        p.RealizedPnL.ShouldBe(Money.Zero(Currency.Eur));
    }

    [Fact]
    public void Id_starts_at_sentinel_until_persisted()
    {
        Position.OpenFor(new InstrumentId(1)).Id.ShouldBe(PositionId.New());
    }
}
