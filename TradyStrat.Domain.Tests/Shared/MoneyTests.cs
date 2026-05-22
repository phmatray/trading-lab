using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class MoneyTests
{
    [Fact]
    public void Zero_is_real_value()
    {
        var z = Money.Zero(Currency.Eur);
        z.Amount.ShouldBe(0m);
        z.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void None_is_absence_sentinel()
    {
        var n = Money.None(Currency.Eur);
        n.IsEmpty.ShouldBeTrue();
        n.ShouldNotBe(Money.Zero(Currency.Eur));
    }

    [Fact]
    public void None_equality_requires_matching_currency()
    {
        Money.None(Currency.Eur).ShouldBe(Money.None(Currency.Eur));
        Money.None(Currency.Eur).ShouldNotBe(Money.None(Currency.Usd));
    }

    [Fact]
    public void Add_requires_matching_currency()
    {
        var a = Money.Of(10m, Currency.Eur);
        var b = Money.Of(5m, Currency.Eur);
        (a + b).ShouldBe(Money.Of(15m, Currency.Eur));

        Should.Throw<CurrencyMismatchException>(() => a + Money.Of(5m, Currency.Usd));
    }

    [Fact]
    public void Subtract_requires_matching_currency_and_can_go_negative()
    {
        var a = Money.Of(3m, Currency.Eur);
        var b = Money.Of(10m, Currency.Eur);
        (a - b).ShouldBe(Money.Of(-7m, Currency.Eur));
    }

    [Fact]
    public void Multiply_by_scalar_preserves_currency()
    {
        (Money.Of(4m, Currency.Eur) * 2.5m).ShouldBe(Money.Of(10m, Currency.Eur));
    }

    [Fact]
    public void Divide_by_money_returns_ratio()
    {
        (Money.Of(50m, Currency.Eur) / Money.Of(10m, Currency.Eur)).ShouldBe(5m);
        Should.Throw<CurrencyMismatchException>(() =>
            Money.Of(10m, Currency.Eur) / Money.Of(1m, Currency.Usd));
    }

    [Fact]
    public void Arithmetic_on_None_throws()
    {
        var n = Money.None(Currency.Eur);
        var v = Money.Of(5m, Currency.Eur);
        Should.Throw<InvalidOperationException>(() => n + v);
        Should.Throw<InvalidOperationException>(() => v - n);
    }
}
