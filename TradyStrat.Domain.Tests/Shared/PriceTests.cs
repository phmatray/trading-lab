using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class PriceTests
{
    [Fact]
    public void Price_times_Quantity_returns_Money()
    {
        var p = Price.Of(Money.Of(4m, Currency.Eur));
        (p * Quantity.Of(10m)).ShouldBe(Money.Of(40m, Currency.Eur));
    }

    [Fact]
    public void Price_times_None_quantity_returns_None_money()
    {
        var p = Price.Of(Money.Of(4m, Currency.Eur));
        (p * Quantity.None).IsEmpty.ShouldBeTrue();
        (p * Quantity.None).Currency.ShouldBe(Currency.Eur);
    }

    [Fact]
    public void None_price_propagates()
    {
        var p = Price.None(Currency.Eur);
        (p * Quantity.Of(10m)).IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Price_minus_Price_returns_Money_delta()
    {
        var hi = Price.Of(Money.Of(7m, Currency.Eur));
        var lo = Price.Of(Money.Of(5m, Currency.Eur));
        (hi - lo).ShouldBe(Money.Of(2m, Currency.Eur));
    }
}
