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
    }
}
