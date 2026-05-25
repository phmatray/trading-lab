using Shouldly;
using TradyStrat.Domain.Shared.Money;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared.Money;

public class QuantityTests
{
    [Fact]
    public void Of_accepts_non_negative()
    {
        Quantity.Of(0m).Value.ShouldBe(0m);
        Quantity.Of(10.5m).Value.ShouldBe(10.5m);
    }

    [Fact]
    public void Of_rejects_negative()
    {
        Should.Throw<ArgumentException>(() => Quantity.Of(-1m));
    }

    [Fact]
    public void None_is_distinct_from_Zero()
    {
        Quantity.None.ShouldNotBe(Quantity.Of(0m));
        Quantity.None.IsSpecified.ShouldBeFalse();
        Quantity.Of(0m).IsSpecified.ShouldBeTrue();
    }

    [Fact]
    public void Add_propagates_specified()
    {
        (Quantity.Of(2m) + Quantity.Of(3m)).ShouldBe(Quantity.Of(5m));
        (Quantity.Of(2m) + Quantity.None).IsSpecified.ShouldBeFalse();
    }

    [Fact]
    public void Subtract_throws_on_negative_result()
    {
        Should.Throw<ArgumentException>(() => Quantity.Of(3m) - Quantity.Of(5m));
    }
}
