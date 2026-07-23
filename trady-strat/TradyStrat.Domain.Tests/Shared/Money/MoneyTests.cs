using Shouldly;
using TradyStrat.Domain.Shared.Money;
using Xunit;
using MoneyVo = global::TradyStrat.Domain.Shared.Money.Money;

namespace TradyStrat.Domain.Tests.Shared.Money;

public class MoneyTests
{
    [Fact]
    public void Zero_is_real_value()
    {
        var z = MoneyVo.Zero(Currency.Eur);
        z.Amount.ShouldBe(0m);
        z.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void None_is_absence_sentinel()
    {
        var n = MoneyVo.None(Currency.Eur);
        n.IsEmpty.ShouldBeTrue();
        n.ShouldNotBe(MoneyVo.Zero(Currency.Eur));
    }

    [Fact]
    public void None_equality_requires_matching_currency()
    {
        MoneyVo.None(Currency.Eur).ShouldBe(MoneyVo.None(Currency.Eur));
        MoneyVo.None(Currency.Eur).ShouldNotBe(MoneyVo.None(Currency.Usd));
    }

    [Fact]
    public void Add_requires_matching_currency()
    {
        var a = MoneyVo.Of(10m, Currency.Eur);
        var b = MoneyVo.Of(5m, Currency.Eur);
        (a + b).ShouldBe(MoneyVo.Of(15m, Currency.Eur));

        Should.Throw<CurrencyMismatchException>(() => a + MoneyVo.Of(5m, Currency.Usd));
    }

    [Fact]
    public void Subtract_requires_matching_currency_and_can_go_negative()
    {
        var a = MoneyVo.Of(3m, Currency.Eur);
        var b = MoneyVo.Of(10m, Currency.Eur);
        (a - b).ShouldBe(MoneyVo.Of(-7m, Currency.Eur));
    }

    [Fact]
    public void Multiply_by_scalar_preserves_currency()
    {
        (MoneyVo.Of(4m, Currency.Eur) * 2.5m).ShouldBe(MoneyVo.Of(10m, Currency.Eur));
    }

    [Fact]
    public void Divide_by_money_returns_ratio()
    {
        (MoneyVo.Of(50m, Currency.Eur) / MoneyVo.Of(10m, Currency.Eur)).ShouldBe(5m);
        Should.Throw<CurrencyMismatchException>(() =>
            MoneyVo.Of(10m, Currency.Eur) / MoneyVo.Of(1m, Currency.Usd));
    }

    [Fact]
    public void Arithmetic_on_None_throws()
    {
        var n = MoneyVo.None(Currency.Eur);
        var v = MoneyVo.Of(5m, Currency.Eur);
        Should.Throw<InvalidOperationException>(() => n + v);
        Should.Throw<InvalidOperationException>(() => v - n);
    }
}
