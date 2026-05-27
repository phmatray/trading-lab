using Shouldly;
using TradingSignal.Core;
using TradingSignal.Llm.Parsing;

namespace TradingSignal.Llm.Tests;

public sealed class SignalResponseParserTests
{
    [Fact]
    public void Parses_Clean_Json()
    {
        string raw = """{"action":"BUY","confidence":0.78,"reason":"RSI oversold"}""";

        SignalResponseParser.TryParse(raw, out RawSignal? s).ShouldBeTrue();
        s.Action.ShouldBe(TradeAction.Buy);
        s.Confidence.ShouldBe(0.78);
        s.Reason.ShouldBe("RSI oversold");
    }

    [Fact]
    public void Strips_Markdown_Fences()
    {
        string raw = """
            ```json
            {"action":"SELL","confidence":0.6,"reason":"bearish"}
            ```
            """;

        SignalResponseParser.TryParse(raw, out RawSignal? s).ShouldBeTrue();
        s.Action.ShouldBe(TradeAction.Sell);
        s.Confidence.ShouldBe(0.6);
    }

    [Fact]
    public void Tolerates_Leading_And_Trailing_Prose()
    {
        string raw = "Sure, here is my answer: {\"action\":\"HOLD\",\"confidence\":0.4,\"reason\":\"flat\"} hope that helps!";

        SignalResponseParser.TryParse(raw, out RawSignal? s).ShouldBeTrue();
        s.Action.ShouldBe(TradeAction.Hold);
    }

    [Theory]
    [InlineData("buy", TradeAction.Buy)]
    [InlineData("Buy", TradeAction.Buy)]
    [InlineData("BUY", TradeAction.Buy)]
    [InlineData("sell", TradeAction.Sell)]
    [InlineData("hold", TradeAction.Hold)]
    public void Accepts_Mixed_Case_Action(string actionText, TradeAction expected)
    {
        string raw = $"{{\"action\":\"{actionText}\",\"confidence\":0.5,\"reason\":\"x\"}}";

        SignalResponseParser.TryParse(raw, out RawSignal? s).ShouldBeTrue();
        s.Action.ShouldBe(expected);
    }

    [Fact]
    public void Clamps_Confidence_Above_1()
    {
        string raw = """{"action":"BUY","confidence":1.7,"reason":"x"}""";

        SignalResponseParser.TryParse(raw, out RawSignal? s).ShouldBeTrue();
        s.Confidence.ShouldBe(1.0);
    }

    [Fact]
    public void Clamps_Confidence_Below_0()
    {
        string raw = """{"action":"BUY","confidence":-0.5,"reason":"x"}""";

        SignalResponseParser.TryParse(raw, out RawSignal? s).ShouldBeTrue();
        s.Confidence.ShouldBe(0.0);
    }

    [Fact]
    public void Fails_On_Empty_String()
    {
        SignalResponseParser.TryParse(string.Empty, out RawSignal? s).ShouldBeFalse();
        s.Action.ShouldBe(TradeAction.Hold);
        s.Reason.ShouldBe("parse_failure");
    }

    [Fact]
    public void Fails_On_Garbage()
    {
        SignalResponseParser.TryParse("this is not json at all", out RawSignal? s).ShouldBeFalse();
        s.Action.ShouldBe(TradeAction.Hold);
    }

    [Fact]
    public void Fails_On_Malformed_Json()
    {
        SignalResponseParser.TryParse("{\"action\": \"BUY\", \"confidence\":", out RawSignal? s).ShouldBeFalse();
        s.Action.ShouldBe(TradeAction.Hold);
    }

    [Fact]
    public void Fails_On_Invalid_Action_Value()
    {
        string raw = """{"action":"LONG","confidence":0.7,"reason":"x"}""";

        SignalResponseParser.TryParse(raw, out RawSignal? s).ShouldBeFalse();
        s.Action.ShouldBe(TradeAction.Hold);
    }

    [Fact]
    public void Fails_When_Confidence_Missing()
    {
        string raw = """{"action":"BUY","reason":"x"}""";

        SignalResponseParser.TryParse(raw, out RawSignal? s).ShouldBeFalse();
    }

    [Fact]
    public void Handles_Missing_Reason_With_Empty_String()
    {
        string raw = """{"action":"BUY","confidence":0.5}""";

        // Reason is treated leniently — empty string is acceptable.
        SignalResponseParser.TryParse(raw, out RawSignal? s).ShouldBeTrue();
        s.Reason.ShouldBe(string.Empty);
    }
}
