using Shouldly;
using TradingSignal.Core;
using TradingSignal.Evaluation;

namespace TradingSignal.Evaluation.Tests;

public sealed class OutcomeComputerTests
{
    private static Candle C(int idx, decimal open, decimal close) =>
        new(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(idx),
            open, Math.Max(open, close) + 1m, Math.Min(open, close) - 1m, close, 1m);

    private static Prediction PredAt(int idx, TradeAction action) => new(
        Id: Guid.NewGuid(),
        AsOfUtc: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(idx),
        Symbol: "BTCUSDT",
        Features: new FeatureSet(
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(idx),
            "BTCUSDT", 0m, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        Signal: new RawSignal(action, 0.7, "test"),
        WalkForwardSegment: 0);

    [Fact]
    public void Buy_Correct_When_Price_Rises_Net_Of_Fees()
    {
        // Entry 100 → exit 105 = 5% gross. 10 bps round-trip = 20 bps = 0.002 net.
        var candles = new[] { C(0, 100m, 100m), C(1, 100m, 105m) };
        var p = PredAt(0, TradeAction.Buy);

        var o = OutcomeComputer.Compute(p, candles, decisionIndex: 0, horizonCandles: 1, feeBps: 10);

        o.EntryPrice.ShouldBe(100m);
        o.ExitPrice.ShouldBe(105m);
        o.RealizedReturnPct.ShouldBe(0.05 - 0.002, 1e-9);
        o.DirectionCorrect.ShouldBeTrue();
    }

    [Fact]
    public void Sell_Correct_When_Price_Falls()
    {
        var candles = new[] { C(0, 100m, 100m), C(1, 100m, 95m) };
        var p = PredAt(0, TradeAction.Sell);

        var o = OutcomeComputer.Compute(p, candles, decisionIndex: 0, horizonCandles: 1, feeBps: 10);

        o.RealizedReturnPct.ShouldBe(0.05 - 0.002, 1e-9); // short return positive
        o.DirectionCorrect.ShouldBeTrue();
    }

    [Fact]
    public void Hold_Correct_When_Move_Smaller_Than_Round_Trip_Fee()
    {
        // 0.05% move with 10 bps round-trip (0.2%) → not worth trading, HOLD correct
        var candles = new[] { C(0, 100m, 100m), C(1, 100m, 100.05m) };
        var p = PredAt(0, TradeAction.Hold);

        var o = OutcomeComputer.Compute(p, candles, decisionIndex: 0, horizonCandles: 1, feeBps: 10);

        o.RealizedReturnPct.ShouldBe(0d);
        o.DirectionCorrect.ShouldBeTrue();
    }

    [Fact]
    public void Hold_Incorrect_When_Move_Exceeds_Round_Trip()
    {
        // 5% move dwarfs 0.2% round-trip → HOLD missed opportunity
        var candles = new[] { C(0, 100m, 100m), C(1, 100m, 105m) };
        var p = PredAt(0, TradeAction.Hold);

        var o = OutcomeComputer.Compute(p, candles, decisionIndex: 0, horizonCandles: 1, feeBps: 10);

        o.DirectionCorrect.ShouldBeFalse();
    }

    [Fact]
    public void Throws_When_Decision_Index_Has_No_Future_Candle()
    {
        var candles = new[] { C(0, 100m, 100m) };
        var p = PredAt(0, TradeAction.Buy);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            OutcomeComputer.Compute(p, candles, decisionIndex: 0, horizonCandles: 1, feeBps: 10));
    }

    [Fact]
    public void Buy_Loses_When_Price_Falls()
    {
        var candles = new[] { C(0, 100m, 100m), C(1, 100m, 95m) };
        var p = PredAt(0, TradeAction.Buy);

        var o = OutcomeComputer.Compute(p, candles, decisionIndex: 0, horizonCandles: 1, feeBps: 10);

        o.RealizedReturnPct.ShouldBe(-0.05 - 0.002, 1e-9);
        o.DirectionCorrect.ShouldBeFalse();
    }
}
