using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class StronglyTypedIdsTests
{
    [Fact]
    public void New_returns_zero_sentinel()
    {
        InstrumentId.New().Value.ShouldBe(0);
        TradeId.New().Value.ShouldBe(0);
        PositionId.New().Value.ShouldBe(0);
    }

    [Fact]
    public void Singleton_ids_have_expected_value()
    {
        PortfolioId.Singleton.Value.ShouldBe(1);
        GoalId.Singleton.Value.ShouldBe(1);
    }

    [Fact]
    public void Distinct_id_types_do_not_unify()
    {
        var i = new InstrumentId(7);
        var t = new TradeId(7);
        i.Value.ShouldBe(t.Value);
    }

    [Fact]
    public void Equality_is_structural()
    {
        new InstrumentId(5).ShouldBe(new InstrumentId(5));
        new InstrumentId(5).ShouldNotBe(new InstrumentId(6));
    }
}
