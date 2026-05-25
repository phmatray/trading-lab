using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Domain.Tests.Portfolio;

public class PortfolioSnapshotTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Instrument Inst(int id, string ticker, string currency)
        => Instrument.Existing(
            id:         new InstrumentId(id),
            ticker:     ticker,
            name:       ticker,
            currency:   Currency.Parse(currency),
            exchange:   Exchange.Of("X"),
            timezoneId: TimezoneId.Of("UTC"),
            kind:       InstrumentKind.Held,
            addedAt:    DateTime.UtcNow);

    [Fact]
    public void Empty_portfolio_snapshot_is_zero()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        var instruments = new Dictionary<InstrumentId, Instrument>();
        var prices      = new Dictionary<InstrumentId, Price>();

        var snap = portfolio.Snapshot(instruments, prices, Money.Of(1_000_000m, Currency.Eur));

        snap.Positions.Count.ShouldBe(0);
        snap.Shares.ShouldBe(0m);
        snap.CurrentValueEur.ShouldBe(Money.Zero(Currency.Eur));
        snap.UnrealizedPnLEur.ShouldBe(Money.Zero(Currency.Eur));
        snap.RealizedPnLEur.ShouldBe(Money.Zero(Currency.Eur));
        snap.ProgressPct.ShouldBe(0m);
    }

    [Fact]
    public void Single_buy_snapshot_includes_fees_in_avg_cost()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        var iid = new InstrumentId(1);
        portfolio.RecordTrade(iid, new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Of(2m, Currency.Eur), "", _now);

        var instruments = new Dictionary<InstrumentId, Instrument> {
            [iid] = Inst(1, "CON3.L", "USD"),
        };
        var prices = new Dictionary<InstrumentId, Price> {
            [iid] = Price.Of(Money.Of(5m, Currency.Eur)),
        };

        var snap = portfolio.Snapshot(instruments, prices, Money.Of(1_000_000m, Currency.Eur));

        snap.Shares.ShouldBe(10m);
        // Avg cost = (10*4 + 2)/10 = 4.20
        snap.AvgCostEur.ShouldBe(Money.Of(4.20m, Currency.Eur));
        snap.CurrentValueEur.ShouldBe(Money.Of(50m, Currency.Eur));
        snap.UnrealizedPnLEur.ShouldBe(Money.Of(8m, Currency.Eur));
    }

    [Fact]
    public void Multi_position_snapshot_sums_across_positions()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        var a = new InstrumentId(1); var b = new InstrumentId(2);
        portfolio.RecordTrade(a, new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);
        portfolio.RecordTrade(b, new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(5m), Price.Of(Money.Of(10m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        var instruments = new Dictionary<InstrumentId, Instrument> {
            [a] = Inst(1, "AAA", "USD"),
            [b] = Inst(2, "BBB", "USD"),
        };
        var prices = new Dictionary<InstrumentId, Price> {
            [a] = Price.Of(Money.Of(5m, Currency.Eur)),
            [b] = Price.Of(Money.Of(12m, Currency.Eur)),
        };

        var snap = portfolio.Snapshot(instruments, prices, Money.Of(1_000m, Currency.Eur));

        snap.Positions.Count.ShouldBe(2);
        snap.CurrentValueEur.ShouldBe(Money.Of(110m, Currency.Eur));   // 50 + 60
        snap.CostBasisEur.ShouldBe(Money.Of(90m, Currency.Eur));       // 40 + 50
        snap.UnrealizedPnLEur.ShouldBe(Money.Of(20m, Currency.Eur));
        // Multi-position: legacy scalars are zero.
        snap.Shares.ShouldBe(0m);
        snap.AvgCostEur.ShouldBe(Money.Zero(Currency.Eur));
    }

    [Fact]
    public void Snapshot_progress_pct_uses_goal_target()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        var iid = new InstrumentId(1);
        portfolio.RecordTrade(iid, new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(100m), Price.Of(Money.Of(10m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        var instruments = new Dictionary<InstrumentId, Instrument> { [iid] = Inst(1, "X", "EUR") };
        var prices      = new Dictionary<InstrumentId, Price>      { [iid] = Price.Of(Money.Of(20m, Currency.Eur)) };

        var snap = portfolio.Snapshot(instruments, prices, Money.Of(10_000m, Currency.Eur));

        // value = 100 * 20 = 2000; goal = 10000; pct = 20.
        snap.ProgressPct.ShouldBe(20m);
    }

    [Fact]
    public void SnapshotAsOf_excludes_trades_after_date()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        var iid = new InstrumentId(1);
        portfolio.RecordTrade(iid, new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);
        portfolio.RecordTrade(iid, new DateOnly(2026, 2, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(5m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        var instruments = new Dictionary<InstrumentId, Instrument> { [iid] = Inst(1, "X", "EUR") };
        var prices      = new Dictionary<InstrumentId, Price>      { [iid] = Price.Of(Money.Of(6m, Currency.Eur)) };

        // As of mid-January, only the first buy is included.
        var snap = portfolio.SnapshotAsOf(
            new DateOnly(2026, 1, 15), instruments, prices, Money.Of(1_000m, Currency.Eur));

        snap.Shares.ShouldBe(10m);
        snap.CurrentValueEur.ShouldBe(Money.Of(60m, Currency.Eur));
    }
}
