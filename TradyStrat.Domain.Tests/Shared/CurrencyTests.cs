using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class CurrencyTests
{
    [Fact]
    public void Parse_normalizes_to_uppercase()
    {
        Currency.Parse("usd").Code.ShouldBe("USD");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData(null)]
    public void Parse_rejects_invalid_inputs(string? input)
    {
        Should.Throw<ArgumentException>(() => Currency.Parse(input!));
    }

    [Fact]
    public void Static_accessors_exist()
    {
        Currency.Eur.Code.ShouldBe("EUR");
        Currency.Usd.Code.ShouldBe("USD");
        Currency.Gbp.Code.ShouldBe("GBP");
    }

    [Fact]
    public void Equality_is_structural()
    {
        Currency.Parse("USD").ShouldBe(Currency.Parse("usd"));
        Currency.Eur.ShouldNotBe(Currency.Usd);
    }
}
