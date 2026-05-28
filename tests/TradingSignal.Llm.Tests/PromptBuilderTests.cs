using Shouldly;
using TradingSignal.Core;
using TradingSignal.Llm.Prompts;

namespace TradingSignal.Llm.Tests;

public sealed class PromptBuilderTests
{
    private static FeatureSet Make(decimal close, double ema20, double ema50) => new(
        AsOfUtc: new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
        Symbol: "BTCUSDT",
        Close: close,
        Rsi14: 50, MacdLine: 0, MacdSignal: 0, MacdHistogram: 0,
        Ema20: ema20, Ema50: ema50, Atr14: 100,
        Return1: 0, Return5: 0, VolatilityPct: 0);

    // --- EmaCross — directly addresses the audit's "EMA20 > EMA50" misordering bug ---

    [Theory]
    [InlineData(100.0, 90.0, "bullish (EMA20 > EMA50)")]
    [InlineData(75807.0, 76166.0, "bearish (EMA20 < EMA50)")]  // exact numbers from contradictory prediction aa13fe81
    [InlineData(50.0, 50.0, "neutral (EMA20 == EMA50)")]
    public void EmaCross_Categorizes_By_Numeric_Comparison(double ema20, double ema50, string expected)
    {
        PromptBuilder.EmaCross(Make(close: 100m, ema20, ema50)).ShouldBe(expected);
    }

    // --- PriceVsEmas — covers the four quadrants ---

    [Theory]
    [InlineData(110.0, 100.0, 90.0, "price above both")]
    [InlineData(80.0, 100.0, 90.0, "price below both")]
    [InlineData(95.0, 90.0, 100.0, "price above EMA20, below EMA50")]
    [InlineData(95.0, 100.0, 90.0, "price below EMA20, above EMA50")]
    public void PriceVsEmas_Categorizes_All_Four_Quadrants(
        double close, double ema20, double ema50, string expected)
    {
        PromptBuilder.PriceVsEmas(Make((decimal)close, ema20, ema50)).ShouldBe(expected);
    }

    // --- Integration: the derived lines actually appear in the built user message ---

    [Fact]
    public void BuildUserMessage_Includes_Derived_EmaCross_And_PriceVsEmas_Lines()
    {
        FeatureSet f = Make(close: 76_805m, ema20: 75_807, ema50: 76_166); // contradictory aa13fe81 setup
        string message = PromptBuilder.BuildUserMessage(f, Array.Empty<FewShotCase>(), maxFewShot: 0);

        message.ShouldContain("ema_cross: bearish (EMA20 < EMA50)");
        message.ShouldContain("price_vs_emas: price above both");
    }
}
