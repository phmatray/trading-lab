using Shouldly;
using TradyStrat.Domain.Shared.Money;
using Xunit;
using MoneyVo = global::TradyStrat.Domain.Shared.Money.Money;

namespace TradyStrat.Domain.Tests.Shared.Money;

public class PriceTests
{
    [Fact]
    public void Price_times_Quantity_returns_Money()
    {
        var p = Price.Of(MoneyVo.Of(4m, Currency.Eur));
        (p * Quantity.Of(10m)).ShouldBe(MoneyVo.Of(40m, Currency.Eur));
    }

    [Fact]
    public void Price_times_None_quantity_returns_None_money()
    {
        var p = Price.Of(MoneyVo.Of(4m, Currency.Eur));
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
        var hi = Price.Of(MoneyVo.Of(7m, Currency.Eur));
        var lo = Price.Of(MoneyVo.Of(5m, Currency.Eur));
        (hi - lo).ShouldBe(MoneyVo.Of(2m, Currency.Eur));
    }
}
