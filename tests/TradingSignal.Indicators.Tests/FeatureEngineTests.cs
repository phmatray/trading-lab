using Shouldly;

namespace TradingSignal.Indicators.Tests;

public sealed class FeatureEngineTests
{
    [Fact]
    public void Throws_When_Below_Warmup_Floor()
    {
        var engine = new FeatureEngine("BTCUSDT");
        var candles = SyntheticCandles.Generate(100);

        Should.Throw<ArgumentException>(() => engine.Compute(candles, engine.WarmupPeriods - 1));
    }

    [Fact]
    public void Throws_On_Out_Of_Range_Index()
    {
        var engine = new FeatureEngine("BTCUSDT");
        var candles = SyntheticCandles.Generate(100);

        Should.Throw<ArgumentOutOfRangeException>(() => engine.Compute(candles, candles.Count));
        Should.Throw<ArgumentOutOfRangeException>(() => engine.Compute(candles, -1));
    }

    [Fact]
    public void Produces_Reasonable_Features_At_Warmup_Boundary()
    {
        var engine = new FeatureEngine("BTCUSDT");
        var candles = SyntheticCandles.Generate(200);

        var f = engine.Compute(candles, engine.WarmupPeriods);

        f.Symbol.ShouldBe("BTCUSDT");
        f.AsOfUtc.ShouldBe(candles[engine.WarmupPeriods].OpenTimeUtc);
        f.Close.ShouldBe(candles[engine.WarmupPeriods].Close);
        f.Rsi14.ShouldBeInRange(0d, 100d);
        f.Ema20.ShouldBeGreaterThan(0d);
        f.Ema50.ShouldBeGreaterThan(0d);
        f.Atr14.ShouldBeGreaterThanOrEqualTo(0d);
    }

    [Fact]
    public void AsOf_Reflects_The_Decision_Candle_Not_The_Last_Candle()
    {
        var engine = new FeatureEngine("BTCUSDT");
        var candles = SyntheticCandles.Generate(200);

        var i = 100;
        var f = engine.Compute(candles, i);

        f.AsOfUtc.ShouldBe(candles[i].OpenTimeUtc);
        f.Close.ShouldBe(candles[i].Close);
    }
}
