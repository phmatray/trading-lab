using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Data;
using TradyStrat.TestKit.Specifications;
using Xunit;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Infrastructure.Tests.SeedWork;

public class MoneyPriceQuantityRoundTripTests
{
    private static readonly DateTime _now = new(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Trade_round_trips_through_EF_with_money_price_quantity_intact()
    {
        await using var db = InMemoryDb.Create();

        var p = PortfolioAr.Empty(PortfolioId.Singleton);
        p.RecordTrade(
            instrumentId:   new InstrumentId(42),
            executedOn:     new DateOnly(2026, 1, 15),
            side:           TradeSide.Buy,
            quantity:       Quantity.Of(7.5m),
            pricePerShare:  Price.Of(Money.Of(123.45m, Currency.Eur)),
            fees:           Money.Of(0.99m, Currency.Eur),
            note:           "rt",
            now:            _now);
        db.Portfolios.Add(p);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Reload — clear the change tracker so EF re-materialises from storage
        db.ChangeTracker.Clear();
        var reloaded = await db.Portfolios
            .Include("_positions._openLots")
            .Include("_positions._trades")
            .SingleAsync(TestContext.Current.CancellationToken);
        var trade = reloaded.Positions[0].Trades[0];

        trade.Quantity.Value.ShouldBe(7.5m);
        trade.Quantity.IsSpecified.ShouldBeTrue();
        trade.PricePerShare.PerUnit.Amount.ShouldBe(123.45m);
        trade.PricePerShare.PerUnit.Currency.ShouldBe(Currency.Eur);
        trade.PricePerShare.PerUnit.IsEmpty.ShouldBeFalse();
        trade.Fees.Amount.ShouldBe(0.99m);
        trade.Fees.Currency.ShouldBe(Currency.Eur);
        trade.Fees.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public async Task IsEmpty_and_IsSpecified_round_trip_through_EF()
    {
        // RecordTrade validates positive quantities and non-empty prices, so we can't
        // construct a Trade with None values via the AR. We CAN verify the column
        // round-trip by persisting a normal trade, clearing the change tracker,
        // and asserting the materialised entity's flag values exactly match what
        // the factories produced (IsSpecified=true on Quantity, IsEmpty=false on
        // every Money). Combined with the existing test, this gives us:
        //   - all "true" sentinel paths covered for IsSpecified
        //   - all "false" sentinel paths covered for IsEmpty (on Quantity, Price,
        //     Fees, and on the Position's RealizedPnL which starts at Zero)
        await using var db = InMemoryDb.Create();

        var p = PortfolioAr.Empty(PortfolioId.Singleton);
        p.RecordTrade(
            instrumentId:   new InstrumentId(42),
            executedOn:     new DateOnly(2026, 1, 15),
            side:           TradeSide.Buy,
            quantity:       Quantity.Of(7.5m),
            pricePerShare:  Price.Of(Money.Of(123.45m, Currency.Eur)),
            fees:           Money.Of(0.99m, Currency.Eur),
            note:           "rt",
            now:            _now);
        db.Portfolios.Add(p);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();

        var reloaded = await db.Portfolios
            .Include("_positions._openLots")
            .Include("_positions._trades")
            .SingleAsync(TestContext.Current.CancellationToken);

        reloaded.Positions.Count.ShouldBe(1);
        var position = reloaded.Positions[0];
        position.Trades.Count.ShouldBe(1);

        // RealizedPnL on a Buy-only position is Zero — Money.IsEmpty must round-trip as false.
        position.RealizedPnL.IsEmpty.ShouldBeFalse();
        position.RealizedPnL.Amount.ShouldBe(0m);

        // Every IsEmpty/IsSpecified discriminator must materialise correctly.
        var trade = position.Trades[0];
        trade.Quantity.IsSpecified.ShouldBeTrue();
        trade.PricePerShare.IsEmpty.ShouldBeFalse();
        trade.PricePerShare.PerUnit.IsEmpty.ShouldBeFalse();
        trade.Fees.IsEmpty.ShouldBeFalse();

        // Open-lot UnitCost is also a Money — its IsEmpty must round-trip too.
        position.OpenLots.Count.ShouldBe(1);
        position.OpenLots[0].UnitCost.IsEmpty.ShouldBeFalse();
        position.OpenLots[0].Quantity.IsSpecified.ShouldBeTrue();
    }
}
