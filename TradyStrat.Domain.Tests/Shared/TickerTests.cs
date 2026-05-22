using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class TickerTests
{
    [Fact]
    public void Of_accepts_well_formed_yahoo_symbol()
    {
        Ticker.Of("CON3.L").Value.ShouldBe("CON3.L");
        Ticker.Of("BTC-USD").Value.ShouldBe("BTC-USD");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("AB C")]
    [InlineData(null)]
    public void Of_rejects_invalid(string? input)
    {
        Should.Throw<ArgumentException>(() => Ticker.Of(input!));
    }

    [Fact]
    public void Equality_is_structural()
    {
        Ticker.Of("CON3.L").ShouldBe(Ticker.Of("CON3.L"));
        Ticker.Of("CON3.L").ShouldNotBe(Ticker.Of("COIN"));
    }
}
