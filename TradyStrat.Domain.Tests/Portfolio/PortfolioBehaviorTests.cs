using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Portfolio.Events;
using TradyStrat.Domain.Shared;
using Xunit;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Domain.Tests.Portfolio;

public class PortfolioBehaviorTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Existing_creates_a_singleton_portfolio()
    {
        var p = PortfolioAr.Existing(PortfolioId.Singleton);
        p.Id.ShouldBe(PortfolioId.Singleton);
        p.Positions.ShouldBeEmpty();
    }

    [Fact]
    public void RecordTrade_creates_new_position_first_time()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        portfolio.RecordTrade(
            new InstrumentId(7),
            new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        // A PositionOpened event should have been raised for the new position.
        portfolio.DomainEvents.OfType<PositionOpened>()
            .Any(e => e.InstrumentId == new InstrumentId(7))
            .ShouldBeTrue();
        portfolio.Positions.Count.ShouldBe(1);
        portfolio.Positions[0].InstrumentId.ShouldBe(new InstrumentId(7));
    }

    [Fact]
    public void RecordTrade_reuses_existing_position()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        portfolio.RecordTrade(new InstrumentId(7), new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);

        // Drain events from first trade so we can isolate the second trade's events.
        portfolio.DequeueDomainEvents();

        portfolio.RecordTrade(new InstrumentId(7), new DateOnly(2026, 1, 5), TradeSide.Buy,
            Quantity.Of(5m), Price.Of(Money.Of(6m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);

        // No PositionOpened for the second trade on the same instrument.
        portfolio.DomainEvents.OfType<PositionOpened>().ShouldBeEmpty();
        portfolio.Positions.Count.ShouldBe(1);
        portfolio.Positions[0].Trades.Count.ShouldBe(2);
    }

    [Fact]
    public void DeleteTrade_replays_remaining_trades_in_that_position()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        var r1 = portfolio.RecordTrade(new InstrumentId(7), new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);
        portfolio.RecordTrade(new InstrumentId(7), new DateOnly(2026, 1, 5), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(5m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);
        portfolio.RecordTrade(new InstrumentId(7), new DateOnly(2026, 1, 8), TradeSide.Sell,
            Quantity.Of(5m), Price.Of(Money.Of(6m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);

        // After delete of the first buy, replay: only [Buy@5, Sell@6] of qty 5 each.
        // remaining lot @ 5; sell of 5 @ 6 realizes 5*(6-5)=5.
        portfolio.DeleteTrade(r1.TradeId, _now);

        var pos = portfolio.Positions[0];
        pos.Trades.Count.ShouldBe(2);
        pos.TotalQuantity.ShouldBe(Quantity.Of(5m));
        pos.RealizedPnL.ShouldBe(Money.Of(5m, Currency.Eur));
    }

    [Fact]
    public void DeleteTrade_unknown_id_throws()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        Should.Throw<TradeValidationException>(() =>
            portfolio.DeleteTrade(new TradeId(999), _now));
    }

    [Fact]
    public void ImportTrades_atomic_rolls_back_on_failure()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        var good = new TradeDraft(
            new InstrumentId(7), new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "");
        // This sell would exceed open lots — should fail mid-batch.
        var bad = new TradeDraft(
            new InstrumentId(7), new DateOnly(2026, 1, 2), TradeSide.Sell,
            Quantity.Of(20m), Price.Of(Money.Of(5m, Currency.Eur)),
            Money.Zero(Currency.Eur), "");

        Should.Throw<TradeValidationException>(() =>
            portfolio.ImportTrades([good, bad], _now));

        // Atomic: nothing applied.
        portfolio.Positions.ShouldBeEmpty();
    }

    [Fact]
    public void ImportTrades_failure_clears_pending_domain_events()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        var good = new TradeDraft(
            new InstrumentId(7), new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "");
        var bad = new TradeDraft(
            new InstrumentId(7), new DateOnly(2026, 1, 2), TradeSide.Sell,
            Quantity.Of(20m), Price.Of(Money.Of(5m, Currency.Eur)),
            Money.Zero(Currency.Eur), "");

        Should.Throw<TradeValidationException>(() =>
            portfolio.ImportTrades([good, bad], _now));

        // A partially-applied batch must leave no orphan events. Guards the
        // ClearDomainEvents() call in Portfolio.ImportTrades's catch block.
        portfolio.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void TradeIds_are_unique_across_positions()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        portfolio.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);
        portfolio.RecordTrade(new InstrumentId(2), new DateOnly(2026, 1, 2), TradeSide.Buy,
            Quantity.Of(5m), Price.Of(Money.Of(8m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);
        portfolio.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 3), TradeSide.Buy,
            Quantity.Of(3m), Price.Of(Money.Of(5m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);

        var allIds = portfolio.Positions.SelectMany(p => p.Trades).Select(t => t.Id.Value).ToList();
        allIds.ShouldBe([1, 2, 3], ignoreOrder: true);
    }

    [Fact]
    public void Empty_with_clock_raises_PortfolioCreated()
    {
        var p = PortfolioAr.Empty(PortfolioId.Singleton, _now);

        var evt = p.DomainEvents.OfType<PortfolioCreated>().ShouldHaveSingleItem();
        evt.PortfolioId.ShouldBe(PortfolioId.Singleton);
        evt.OccurredAt.ShouldBe(_now);
    }

    [Fact]
    public void RecordTrade_raises_TradeRecorded()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);

        var result = portfolio.RecordTrade(
            new InstrumentId(7),
            new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        var evt = portfolio.DomainEvents.OfType<TradeRecorded>().ShouldHaveSingleItem();
        evt.TradeId.ShouldBe(result.TradeId);
        evt.PositionId.ShouldBe(result.PositionId);
        evt.OccurredAt.ShouldBe(_now);
    }

    [Fact]
    public void DeleteTrade_raises_TradeDeleted_with_the_deleted_TradeId()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        var r1 = portfolio.RecordTrade(
            new InstrumentId(7),
            new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);
        portfolio.DequeueDomainEvents();   // discard PositionOpened + first TradeRecorded

        var result = portfolio.DeleteTrade(r1.TradeId, _now);

        var evt = portfolio.DomainEvents.OfType<TradeDeleted>().ShouldHaveSingleItem();
        evt.TradeId.ShouldBe(r1.TradeId);
        evt.PositionId.ShouldBe(r1.PositionId);
        evt.OccurredAt.ShouldBe(_now);
        result.TradeId.ShouldBe(r1.TradeId);   // event returned synchronously matches
    }
}
