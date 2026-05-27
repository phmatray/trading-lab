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

    private static LmStudioOptions Options() => new() { ModelId = "test-model", MaxFewShot = 0, MaxOutputTokens = 128 };

    [Fact]
    public async Task Returns_Cached_Signal_Without_Calling_Llm()
    {
        FakeChatClient chat = new();
        InMemoryLlmCache cache = new();
        LlmSignalGenerator sut = new(chat, Options(), cache);

        // Prime cache
        string first = """{"action":"BUY","confidence":0.71,"reason":"primer"}""";
        chat.EnqueueText(first);
        RawSignal primed = await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);
        chat.CallCount.ShouldBe(1);
        cache.Store.Count.ShouldBe(1);

        // Second call same features → cache hit
        RawSignal second = await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);
        chat.CallCount.ShouldBe(1); // unchanged
        second.ShouldBe(primed);
    }

    [Fact]
    public async Task Parses_Successful_Response()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("""{"action":"SELL","confidence":0.62,"reason":"overbought"}""");
        LlmSignalGenerator sut = new(chat, Options(), new InMemoryLlmCache());

        RawSignal signal = await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);

        signal.Action.ShouldBe(TradeAction.Sell);
        signal.Confidence.ShouldBe(0.62);
        chat.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Retries_Once_When_First_Response_Is_Garbage()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("definitely not json");
        chat.EnqueueText("""{"action":"BUY","confidence":0.55,"reason":"retry win"}""");
        LlmSignalGenerator sut = new(chat, Options(), new InMemoryLlmCache());

        RawSignal signal = await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);

        signal.Action.ShouldBe(TradeAction.Buy);
        signal.Reason.ShouldBe("retry win");
        chat.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Degrades_To_Hold_When_Both_Attempts_Fail()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("garbage one");
        chat.EnqueueText("garbage two");
        InMemoryLlmCache cache = new();
        LlmSignalGenerator sut = new(chat, Options(), cache);

        RawSignal signal = await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);

        signal.Action.ShouldBe(TradeAction.Hold);
        signal.Confidence.ShouldBe(0d);
        signal.Reason.ShouldBe("parse_failure");
        chat.CallCount.ShouldBe(2);
        cache.Store.Count.ShouldBe(1); // even the failure result is cached so we don't retry forever
    }

    [Fact]
    public async Task Degrades_To_Hold_When_Llm_Throws_On_Both_Attempts()
    {
        FakeChatClient chat = new();
        chat.EnqueueError(new HttpRequestException("boom"));
        chat.EnqueueError(new HttpRequestException("boom again"));
        LlmSignalGenerator sut = new(chat, Options(), new InMemoryLlmCache());

        RawSignal signal = await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);

        signal.Action.ShouldBe(TradeAction.Hold);
        signal.Reason.ShouldBe("parse_failure");
    }

    [Fact]
    public async Task Different_Features_Produce_Different_Cache_Keys()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("""{"action":"BUY","confidence":0.5,"reason":"a"}""");
        chat.EnqueueText("""{"action":"SELL","confidence":0.5,"reason":"b"}""");

        InMemoryLlmCache cache = new();
        LlmSignalGenerator sut = new(chat, Options(), cache);

        await sut.GenerateAsync(Features("BTCUSDT"), Array.Empty<FewShotCase>(), CancellationToken.None);
        await sut.GenerateAsync(Features("ETHUSDT"), Array.Empty<FewShotCase>(), CancellationToken.None);

        cache.Store.Count.ShouldBe(2);
        chat.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Structured_Output_Used_On_First_Attempt_Only()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("garbage");
        chat.EnqueueText("""{"action":"HOLD","confidence":0.3,"reason":"calm"}""");
        LlmSignalGenerator sut = new(chat, Options(), new InMemoryLlmCache());

        await sut.GenerateAsync(Features(), Array.Empty<FewShotCase>(), CancellationToken.None);

        chat.ReceivedOptions.Count.ShouldBe(2);
        chat.ReceivedOptions[0]!.ResponseFormat.ShouldNotBeNull(); // schema-constrained
        chat.ReceivedOptions[1]!.ResponseFormat.ShouldBeNull();    // fallback
    }
}
