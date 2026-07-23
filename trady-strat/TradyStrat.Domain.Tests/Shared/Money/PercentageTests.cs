using Shouldly;
using TradyStrat.Domain.Shared.Money;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared.Money;

public class PercentageTests
{
    [Fact]
    public void Of_preserves_value()
    {
        Percentage.Of(42.5m).Value.ShouldBe(42.5m);
    }

    [Fact]
    public void Empty_is_zero_value_with_IsEmpty_true()
    {
        Percentage.Empty.Value.ShouldBe(0m);
        Percentage.Empty.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Of_is_not_IsEmpty()
    {
        Percentage.Of(0m).IsEmpty.ShouldBeFalse();   // zero is a valid reading
        Percentage.Of(50m).IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void Equality_is_structural()
    {
        Percentage.Of(50m).ShouldBe(Percentage.Of(50m));
        Percentage.Empty.ShouldBe(Percentage.Empty);
        Percentage.Of(0m).ShouldNotBe(Percentage.Empty); // zero-specified ≠ Empty
    }
}
