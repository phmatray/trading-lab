using Shouldly;
using TradyStrat.Domain.Suggestions;
using Xunit;

namespace TradyStrat.Domain.Tests.Suggestions;

public class ConvictionTests
{
    [Fact]
    public void Of_accepts_1_through_10()
    {
        Conviction.Of(1).Value.ShouldBe(1);
        Conviction.Of(5).Value.ShouldBe(5);
        Conviction.Of(10).Value.ShouldBe(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void Of_rejects_out_of_range(int value)
    {
        Should.Throw<ArgumentException>(() => Conviction.Of(value));
    }

    [Fact]
    public void Equality_is_structural()
    {
        Conviction.Of(5).ShouldBe(Conviction.Of(5));
        Conviction.Of(5).ShouldNotBe(Conviction.Of(6));
    }
}
