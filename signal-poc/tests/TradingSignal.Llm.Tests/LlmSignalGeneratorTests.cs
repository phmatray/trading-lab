using Shouldly;
using TradingSignal.Core;
using TradingSignal.Llm.Tests.Fakes;

namespace TradingSignal.Llm.Tests;

public sealed class LlmSignalGeneratorTests
{
    private static FeatureSet Features(string symbol = "BTCUSDT") => new(
        AsOfUtc: new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc),
        Symbol: symbol,
        Close: 65_000m,
        Rsi14: 55, MacdLine: 12, MacdSignal: 10, MacdHistogram: 2,
        Ema20: 64_500, Ema50: 64_000, Atr14: 800,
        Return1: 0.002, Return5: 0.01, VolatilityPct: 1.4);

    private static LmStudioOptions Options() => new()
    {
        ModelId = "test-model",
        MaxFewShot = 0,
        MaxOutputTokens = 128,
        ReasoningEffort = "medium",
    };

    [Fact]
    public async Task Returns_Cached_Signal_Without_Calling_Strategy()
    {
        FakeLlmCallStrategy strategy = new();
        strategy.EnqueueSignal(TradeAction.Buy, 0.71, "primer");
        InMemoryLlmCache cache = new();
        LlmSignalGenerator sut = new(strategy, Options(), cache);

        RawSignal primed = await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);
        strategy.CallCount.ShouldBe(1);
        cache.Store.Count.ShouldBe(1);

        RawSignal second = await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);
        strategy.CallCount.ShouldBe(1);
        second.ShouldBe(primed);
    }

    [Fact]
    public async Task Different_Features_Produce_Different_Cache_Keys()
    {
        FakeLlmCallStrategy strategy = new();
        strategy.EnqueueSignal(TradeAction.Buy, 0.5, "a");
        strategy.EnqueueSignal(TradeAction.Sell, 0.5, "b");

        InMemoryLlmCache cache = new();
        LlmSignalGenerator sut = new(strategy, Options(), cache);

        await sut.GenerateAsync(Features("BTCUSDT"), Array.Empty<FewShotCase>(), CancellationToken.None);
        await sut.GenerateAsync(Features("ETHUSDT"), Array.Empty<FewShotCase>(), CancellationToken.None);

        cache.Store.Count.ShouldBe(2);
        strategy.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Propagates_Reasoning_Into_RawSignal_And_Cache()
    {
        FakeLlmCallStrategy strategy = new();
        strategy.EnqueueSignal(TradeAction.Buy, 0.6, "short", reasoning: "long thinking trace");
        InMemoryLlmCache cache = new();
        LlmSignalGenerator sut = new(strategy, Options(), cache);

        RawSignal signal = await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);

        signal.Reasoning.ShouldBe("long thinking trace");
        cache.Store.Values.Single().Reasoning.ShouldBe("long thinking trace");
    }

    [Fact]
    public async Task System_Prompt_Of_Strategy_Affects_Cache_Key()
    {
        FakeLlmCallStrategy strategyA = new() { SystemPrompt = "prompt A" };
        FakeLlmCallStrategy strategyB = new() { SystemPrompt = "prompt B" };
        strategyA.EnqueueSignal(TradeAction.Buy, 0.5, "x");
        strategyB.EnqueueSignal(TradeAction.Sell, 0.5, "y");

        InMemoryLlmCache cache = new();
        LlmSignalGenerator a = new(strategyA, Options(), cache);
        LlmSignalGenerator b = new(strategyB, Options(), cache);

        await a.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);
        await b.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);

        cache.Store.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Reasoning_Effort_Affects_Cache_Key()
    {
        FakeLlmCallStrategy strategy = new();
        strategy.EnqueueSignal(TradeAction.Buy, 0.5, "x");
        strategy.EnqueueSignal(TradeAction.Sell, 0.5, "y");

        InMemoryLlmCache cache = new();
        LlmSignalGenerator a = new(strategy, new LmStudioOptions { ModelId = "m", MaxFewShot = 0, ReasoningEffort = "low" }, cache);
        LlmSignalGenerator b = new(strategy, new LmStudioOptions { ModelId = "m", MaxFewShot = 0, ReasoningEffort = "high" }, cache);

        await a.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);
        await b.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);

        cache.Store.Count.ShouldBe(2);
    }
}
