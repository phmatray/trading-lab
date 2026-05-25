using Shouldly;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared.Money;
using Xunit;

namespace TradyStrat.Domain.Tests.Portfolio;

public class TradeTests
{
    private static Trade Buy(decimal qty, decimal price, decimal fees = 0m) =>
        Trade.Create(
            executedOn: new DateOnly(2026, 1, 1),
            side: TradeSide.Buy,
            quantity: Quantity.Of(qty),
            pricePerShare: Price.Of(Money.Of(price, Currency.Eur)),
            fees: Money.Of(fees, Currency.Eur),
            note: "",
            now: new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Create_assigns_zero_id_sentinel()
    {
        var t = Buy(10m, 4m);
        t.Id.ShouldBe(TradeId.New());
    }

    [Fact]
    public void Create_rejects_zero_quantity()
    {
        Should.Throw<TradeValidationException>(() =>
            Trade.Create(
                executedOn: new DateOnly(2026, 1, 1),
                side: TradeSide.Buy,
                quantity: Quantity.Of(0m),
                pricePerShare: Price.Of(Money.Of(4m, Currency.Eur)),
                fees: Money.Zero(Currency.Eur),
                note: "",
                now: DateTime.UtcNow));
    }

    [Fact]
    public void Create_rejects_unspecified_quantity()
    {
        Should.Throw<TradeValidationException>(() =>
            Trade.Create(
                executedOn: new DateOnly(2026, 1, 1),
                side: TradeSide.Buy,
                quantity: Quantity.None,
                pricePerShare: Price.Of(Money.Of(4m, Currency.Eur)),
                fees: Money.Zero(Currency.Eur),
                note: "",
                now: DateTime.UtcNow));
    }

    [Fact]
    public void Create_rejects_empty_price()
    {
        Should.Throw<TradeValidationException>(() =>
            Trade.Create(
                executedOn: new DateOnly(2026, 1, 1),
                side: TradeSide.Buy,
                quantity: Quantity.Of(10m),
                pricePerShare: Price.None(Currency.Eur),
                fees: Money.Zero(Currency.Eur),
                note: "",
                now: DateTime.UtcNow));
    }

    [Fact]
    public void IsBuy_reflects_side()
    {
        Buy(10m, 4m).IsBuy.ShouldBeTrue();
        var sell = Trade.Create(
            executedOn: new DateOnly(2026, 1, 1),
            side: TradeSide.Sell,
            quantity: Quantity.Of(10m),
            pricePerShare: Price.Of(Money.Of(4m, Currency.Eur)),
            fees: Money.Zero(Currency.Eur),
            note: "",
            now: DateTime.UtcNow);
        sell.IsBuy.ShouldBeFalse();
    }
}
