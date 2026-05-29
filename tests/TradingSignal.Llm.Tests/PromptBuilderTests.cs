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

    [Fact]
    public void BuildUserMessage_Includes_Adx14_And_VolumeRatio_Lines()
    {
        FeatureSet f = Make(close: 100m, ema20: 99, ema50: 98) with { Adx14 = 27.5, VolumeRatio = 1.875 };
        string message = PromptBuilder.BuildUserMessage(f, Array.Empty<FewShotCase>(), maxFewShot: 0);

        message.ShouldContain("adx14: 27.50");
        message.ShouldContain("volume_ratio: 1.875");
    }

    // --- System prompt: the new hard-precedence rules and fee/horizon context must be present ---

    [Fact]
    public void SystemPromptReasoning_Includes_Ema_Cross_Precedence_Rule()
    {
        // The bearish-cross BUY gate. Phrase the assertion to be tolerant of soft line
        // breaks in the prompt body.
        string prompt = PromptBuilder.SystemPromptReasoning;
        prompt.ShouldContain("ema_cross` is bearish");
        prompt.ShouldContain("do NOT emit BUY");
        prompt.ShouldContain("rsi14` < 30");
    }

    [Fact]
    public void SystemPromptReasoning_Includes_Adx_Ranging_Rule()
    {
        PromptBuilder.SystemPromptReasoning.ShouldContain("adx14` < 20");
        PromptBuilder.SystemPromptReasoning.ShouldContain("cap any BUY/SELL confidence at 0.55");
    }

    [Fact]
    public void SystemPromptReasoning_Includes_Volume_Conviction_Rule()
    {
        PromptBuilder.SystemPromptReasoning.ShouldContain("volume_ratio` < 0.7");
    }

    [Fact]
    public void SystemPromptReasoning_Includes_Fee_Horizon_Context()
    {
        PromptBuilder.SystemPromptReasoning.ShouldContain("20 bps");
        PromptBuilder.SystemPromptReasoning.ShouldContain("0.4%");
    }

    [Fact]
    public void SystemPromptReasoning_Includes_Bearish_Cross_Plus_Overbought_Sell_Trigger()
    {
        // Catches the T20 missed-SELL case: bearish cross + RSI>70 + high volume should
        // be a SELL candidate, not HOLD by default. Line-break-tolerant assertions.
        string prompt = PromptBuilder.SystemPromptReasoning;
        prompt.ShouldContain("Rule 3");
        prompt.ShouldContain("`rsi14` > 70");
        prompt.ShouldContain("`volume_ratio` > 1.0");
        prompt.ShouldContain("consider SELL");
    }

    [Fact]
    public void SystemPromptReasoning_Caps_Hold_Confidence_At_0_70()
    {
        PromptBuilder.SystemPromptReasoning.ShouldContain("Cap HOLD confidence at 0.70");
    }

    [Fact]
    public void SystemPromptReasoning_Asks_For_Concise_Rule_Section()
    {
        // Trace-compression directive to keep the model from burning the 4096-token budget
        // on verbose rule restatement.
        PromptBuilder.SystemPromptReasoning.ShouldContain("under 500 words");
    }

    [Fact]
    public void SystemPromptInstruct_Is_Unchanged_By_New_Rules()
    {
        // The instruct path (Gemma / Qwen2.5-instruct) should not see the new precedence
        // rules — those are deliberately reasoning-strategy only, where the model has the
        // tokens to apply them.
        PromptBuilder.SystemPromptInstruct.ShouldNotContain("ema_cross");
        PromptBuilder.SystemPromptInstruct.ShouldNotContain("adx14");
        PromptBuilder.SystemPromptInstruct.ShouldNotContain("volume_ratio");
    }
}
