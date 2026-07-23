using Shouldly;
using TradyStrat.Domain.Shared.Money;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared.Money;

public class CurrencyPairTests
{
    [Fact]
    public void Of_assigns_base_and_quote()
    {
        var pair = CurrencyPair.Of(Currency.Eur, Currency.Usd);
        pair.Base.ShouldBe(Currency.Eur);
        pair.Quote.ShouldBe(Currency.Usd);
    }

    [Fact]
    public void ToString_formats_as_BASE_slash_QUOTE()
    {
        CurrencyPair.Of(Currency.Eur, Currency.Usd).ToString().ShouldBe("EUR/USD");
    }

    [Fact]
    public void Equality_is_structural()
    {
        CurrencyPair.Of(Currency.Eur, Currency.Usd)
            .ShouldBe(CurrencyPair.Of(Currency.Eur, Currency.Usd));
        CurrencyPair.Of(Currency.Eur, Currency.Usd)
            .ShouldNotBe(CurrencyPair.Of(Currency.Usd, Currency.Eur));
    }

    [Fact]
    public void Of_rejects_same_base_and_quote()
    {
        Should.Throw<ArgumentException>(() => CurrencyPair.Of(Currency.Eur, Currency.Eur));
    }
}
