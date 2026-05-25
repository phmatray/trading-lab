using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared.Money;
using Xunit;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Domain.Tests.Portfolio;

public class PortfolioInvariantsTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RecordTrade_with_zero_quantity_throws_at_factory()
    {
        var p = PortfolioAr.Existing(PortfolioId.Singleton);
        Should.Throw<TradeValidationException>(() =>
            p.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 1), TradeSide.Buy,
                Quantity.Of(0m), Price.Of(Money.Of(4m, Currency.Eur)),
                Money.Zero(Currency.Eur), "", _now));
    }

    [Fact]
    public void RecordTrade_with_None_quantity_throws()
    {
        var p = PortfolioAr.Existing(PortfolioId.Singleton);
        Should.Throw<TradeValidationException>(() =>
            p.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 1), TradeSide.Buy,
                Quantity.None, Price.Of(Money.Of(4m, Currency.Eur)),
                Money.Zero(Currency.Eur), "", _now));
    }

    [Fact]
    public void RecordTrade_with_empty_price_throws()
    {
        var p = PortfolioAr.Existing(PortfolioId.Singleton);
        Should.Throw<TradeValidationException>(() =>
            p.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 1), TradeSide.Buy,
                Quantity.Of(10m), Price.None(Currency.Eur),
                Money.Zero(Currency.Eur), "", _now));
    }

    [Fact]
    public void Sell_exceeding_open_lots_throws_per_position()
    {
        var p = PortfolioAr.Existing(PortfolioId.Singleton);
        p.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(5m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        Should.Throw<TradeValidationException>(() =>
            p.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 5), TradeSide.Sell,
                Quantity.Of(10m), Price.Of(Money.Of(5m, Currency.Eur)),
                Money.Zero(Currency.Eur), "", _now));
    }

    [Fact]
    public void DeleteTrade_unknown_id_throws()
    {
        var p = PortfolioAr.Existing(PortfolioId.Singleton);
        Should.Throw<TradeValidationException>(() => p.DeleteTrade(new TradeId(999), _now));
    }
}
