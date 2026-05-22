using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class ExchangeTests
{
    [Fact]
    public void Of_trims_and_preserves_casing()
    {
        Exchange.Of("  LSE  ").Code.ShouldBe("LSE");
        Exchange.Of("NYSEArca").Code.ShouldBe("NYSEArca");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Of_rejects_empty(string? value)
    {
        Should.Throw<ArgumentException>(() => Exchange.Of(value!));
    }

    [Fact]
    public void Equality_is_structural_and_case_sensitive()
    {
        Exchange.Of("LSE").ShouldBe(Exchange.Of("LSE"));
        Exchange.Of("LSE").ShouldNotBe(Exchange.Of("lse"));
    }
}
