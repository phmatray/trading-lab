using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Domain.Tests.Portfolio;

public class PortfolioGrowthSeriesTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Empty_portfolio_returns_empty_series()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        var series = portfolio.GrowthSeries(
            new Dictionary<InstrumentId, IReadOnlyList<PriceBar>>());
        series.ShouldBeEmpty();
    }

    [Fact]
    public void Prepends_synthetic_zero_day_then_accumulates_per_bar()
    {
        var portfolio = PortfolioAr.Existing(PortfolioId.Singleton);
        var iid = new InstrumentId(1);
        portfolio.RecordTrade(iid, new DateOnly(2026, 1, 2), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        var bars = new Dictionary<InstrumentId, IReadOnlyList<PriceBar>>
        {
            [iid] = new List<PriceBar>
            {
                new() { Id = 0, Ticker = "X", Date = new DateOnly(2026, 1, 2),
                    Open = 4m, High = 5m, Low = 4m, Close = 5m, Volume = 0 },
                new() { Id = 0, Ticker = "X", Date = new DateOnly(2026, 1, 3),
                    Open = 5m, High = 6m, Low = 5m, Close = 6m, Volume = 0 },
            }
        };

        var series = portfolio.GrowthSeries(bars);

        series.Count.ShouldBe(3);
        series[0].Date.ShouldBe(new DateOnly(2026, 1, 1)); // synthetic
        series[0].Value.Amount.ShouldBe(0m);
        series[1].Date.ShouldBe(new DateOnly(2026, 1, 2));
        series[1].Value.Amount.ShouldBe(50m);   // 10 shares × 5
        series[2].Date.ShouldBe(new DateOnly(2026, 1, 3));
        series[2].Value.Amount.ShouldBe(60m);   // 10 shares × 6
    }
}
