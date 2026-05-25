using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Portfolio;

public class PositionTests
{
    [Fact]
    public void New_position_for_instrument_is_empty()
    {
        var p = Position.OpenFor(new InstrumentId(7));

        p.InstrumentId.ShouldBe(new InstrumentId(7));
        p.OpenLots.ShouldBeEmpty();
        p.Trades.ShouldBeEmpty();
        p.TotalQuantity.ShouldBe(Quantity.Zero);
        p.CostBasis.ShouldBe(Money.Zero(Currency.Eur));
        p.RealizedPnL.ShouldBe(Money.Zero(Currency.Eur));
    }

    [Fact]
    public void Id_starts_at_sentinel_until_persisted()
    {
        Position.OpenFor(new InstrumentId(1)).Id.ShouldBe(PositionId.New());
    }

    private static Trade Buy(int day, decimal qty, decimal price, decimal fees = 0m) =>
        Trade.Create(
            executedOn: new DateOnly(2026, 1, day),
            side: TradeSide.Buy,
            quantity: Quantity.Of(qty),
            pricePerShare: Price.Of(Money.Of(price, Currency.Eur)),
            fees: Money.Of(fees, Currency.Eur),
            note: "",
            now: new DateTime(2026, 1, day, 12, 0, 0, DateTimeKind.Utc));

    private static Trade Sell(int day, decimal qty, decimal price, decimal fees = 0m) =>
        Trade.Create(
            executedOn: new DateOnly(2026, 1, day),
            side: TradeSide.Sell,
            quantity: Quantity.Of(qty),
            pricePerShare: Price.Of(Money.Of(price, Currency.Eur)),
            fees: Money.Of(fees, Currency.Eur),
            note: "",
            now: new DateTime(2026, 1, day, 12, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Single_buy_folds_fees_into_cost_basis()
    {
        var p = Position.OpenFor(new InstrumentId(1));
        p.Record(Buy(1, qty: 10m, price: 4.00m, fees: 2.00m));

        // unit cost = (10*4 + 2) / 10 = 4.20
        p.TotalQuantity.ShouldBe(Quantity.Of(10m));
        p.CostBasis.ShouldBe(Money.Of(42.00m, Currency.Eur));
        p.RealizedPnL.ShouldBe(Money.Zero(Currency.Eur));
    }

    [Fact]
    public void FIFO_sell_realizes_oldest_lot_first()
    {
        var p = Position.OpenFor(new InstrumentId(1));
        p.Record(Buy(1, 10m, 4.00m));    // lot @ 4.00
        p.Record(Buy(5, 10m, 5.00m));    // lot @ 5.00
        p.Record(Sell(8, 5m, 6.00m));    // sell 5 → realize 5*(6-4) = 10

        p.TotalQuantity.ShouldBe(Quantity.Of(15m));
        p.RealizedPnL.ShouldBe(Money.Of(10m, Currency.Eur));
    }

    [Fact]
    public void Sell_spanning_multiple_lots_realizes_each()
    {
        var p = Position.OpenFor(new InstrumentId(1));
        p.Record(Buy(1, 10m, 4.00m));
        p.Record(Buy(5, 10m, 5.00m));
        // sell 15 @ 7 → realize 10*(7-4) + 5*(7-5) = 30 + 10 = 40
        p.Record(Sell(8, 15m, 7.00m));

        p.TotalQuantity.ShouldBe(Quantity.Of(5m));
        p.RealizedPnL.ShouldBe(Money.Of(40m, Currency.Eur));
    }

    [Fact]
    public void Sell_allocates_fees_pro_rata_across_realization()
    {
        var p = Position.OpenFor(new InstrumentId(1));
        p.Record(Buy(1, 10m, 4.00m));
        // sell 10 @ 6, fees 3 → realize 10*(6-4) - 3 = 17
        p.Record(Sell(8, 10m, 6.00m, fees: 3m));

        p.RealizedPnL.ShouldBe(Money.Of(17m, Currency.Eur));
    }

    [Fact]
    public void Oversell_throws_TradeValidationException()
    {
        var p = Position.OpenFor(new InstrumentId(1));
        p.Record(Buy(1, 10m, 4.00m));
        Should.Throw<TradeValidationException>(() => p.Record(Sell(8, 11m, 5.00m)));
    }
}
