using Shouldly;
using TradingSignal.Core;

namespace TradingSignal.Backtest.Tests;

public sealed class PortfolioTests
{
    [Fact]
    public void Buy_Then_Sell_With_Price_Up_Realizes_Net_Profit()
    {
        // Enter at 100, exit at 110. 10 bps fee each leg = 0.001 round trip.
        // Long equity model: pos = (1.0 / 100) * (1 - 0.001) = 0.00999
        // After exit at 110: cash = 0.00999 * 110 * (1 - 0.001) = 1.09780011
        // Equity ≈ 1.0978
        Portfolio portfolio = new(feeBps: 10, enableShort: false);
        portfolio.Execute(TradeAction.Buy, 100m, 100m);
        portfolio.Execute(TradeAction.Sell, 110m, 110m);

        portfolio.Cash.ShouldBeInRange(1.097d, 1.099d);
        portfolio.Position.ShouldBe(0d);
        portfolio.TradeCount.ShouldBe(2);
    }

    [Fact]
    public void Hold_While_Flat_Is_A_No_Op()
    {
        Portfolio portfolio = new(feeBps: 10, enableShort: false);
        portfolio.Execute(TradeAction.Hold, 100m, 100m);

        portfolio.Cash.ShouldBe(1d);
        portfolio.Position.ShouldBe(0d);
        portfolio.TradeCount.ShouldBe(0);
    }

    [Fact]
    public void Sell_While_Flat_Without_Short_Is_A_No_Op()
    {
        Portfolio portfolio = new(feeBps: 10, enableShort: false);
        portfolio.Execute(TradeAction.Sell, 100m, 100m);

        portfolio.Cash.ShouldBe(1d);
        portfolio.Position.ShouldBe(0d);
        portfolio.TradeCount.ShouldBe(0);
    }

    [Fact]
    public void Buy_While_Already_Long_Is_A_No_Op()
    {
        Portfolio portfolio = new(feeBps: 10, enableShort: false);
        portfolio.Execute(TradeAction.Buy, 100m, 100m);
        int tradesAfterFirstBuy = portfolio.TradeCount;
        double positionAfterFirstBuy = portfolio.Position;

        portfolio.Execute(TradeAction.Buy, 105m, 105m);

        portfolio.TradeCount.ShouldBe(tradesAfterFirstBuy);
        portfolio.Position.ShouldBe(positionAfterFirstBuy);
    }

    [Fact]
    public void Per_Bar_Returns_Tracked_Even_On_Hold()
    {
        Portfolio portfolio = new(feeBps: 10, enableShort: false);
        portfolio.Execute(TradeAction.Buy, 100m, 100m);
        portfolio.Execute(TradeAction.Hold, 100m, 105m);  // price moved up by 5%
        portfolio.Execute(TradeAction.Hold, 100m, 110m);

        portfolio.PerBarReturns.Count.ShouldBe(3);
        portfolio.EquityCurve.Count.ShouldBe(3);
        portfolio.EquityCurve[^1].ShouldBeGreaterThan(portfolio.EquityCurve[0]);
    }

    [Fact]
    public void Equity_Curve_Falls_When_Long_And_Price_Drops()
    {
        Portfolio portfolio = new(feeBps: 10, enableShort: false);
        portfolio.Execute(TradeAction.Buy, 100m, 100m);
        portfolio.Execute(TradeAction.Hold, 100m, 90m);

        portfolio.EquityCurve[^1].ShouldBeLessThan(1d);
    }

    [Fact]
    public void Short_Profits_When_Price_Falls()
    {
        Portfolio portfolio = new(feeBps: 0, enableShort: true);
        portfolio.Execute(TradeAction.Sell, 100m, 100m);   // open short at 100
        double equityAtShort = portfolio.EquityCurve[^1];
        portfolio.Execute(TradeAction.Hold, 100m, 90m);   // price fell 10%

        portfolio.EquityCurve[^1].ShouldBeGreaterThan(equityAtShort);
    }
}
