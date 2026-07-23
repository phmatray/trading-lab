using Microsoft.Extensions.AI;
using Shouldly;
using TradingSignal.Core;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Prompts;
using TradingSignal.Llm.Strategies;
using TradingSignal.Llm.Tests.Fakes;

namespace TradingSignal.Llm.Tests;

public sealed class InstructCallStrategyTests
{
    private static LmStudioOptions Options() => new() { ModelId = "test-model", MaxOutputTokens = 128 };
    private const string Sys = "system";
    private const string User = "user";

    [Fact]
    public void SystemPrompt_Returns_Instruct_Constant()
    {
        FakeChatClient chat = new();
        InstructCallStrategy sut = new(chat, Options());
        sut.SystemPrompt.ShouldBe(PromptBuilder.SystemPromptInstruct);
    }

    [Fact]
    public async Task Parses_Successful_Response()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("""{"action":"SELL","confidence":0.62,"reason":"overbought"}""");
        InstructCallStrategy sut = new(chat, Options());

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Sell);
        outcome.Signal.Confidence.ShouldBe(0.62);
        outcome.ReasoningContent.ShouldBeNull();
        chat.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Retries_Once_When_First_Response_Is_Garbage()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("definitely not json");
        chat.EnqueueText("""{"action":"BUY","confidence":0.55,"reason":"retry win"}""");
        InstructCallStrategy sut = new(chat, Options());

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Buy);
        outcome.Signal.Reason.ShouldBe("retry win");
        chat.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Degrades_To_Hold_When_Both_Attempts_Fail()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("garbage one");
        chat.EnqueueText("garbage two");
        InstructCallStrategy sut = new(chat, Options());

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Hold);
        outcome.Signal.Confidence.ShouldBe(0d);
        outcome.Signal.Reason.ShouldBe("parse_failure");
        outcome.ReasoningContent.ShouldBeNull();
        chat.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Degrades_To_Hold_When_Llm_Throws_On_Both_Attempts()
    {
        FakeChatClient chat = new();
        chat.EnqueueError(new HttpRequestException("boom"));
        chat.EnqueueError(new HttpRequestException("boom again"));
        InstructCallStrategy sut = new(chat, Options());

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Hold);
        outcome.Signal.Reason.ShouldBe("parse_failure");
    }

    [Fact]
    public async Task Structured_Output_Used_On_First_Attempt_Only()
    {
        FakeChatClient chat = new();
        chat.EnqueueText("garbage");
        chat.EnqueueText("""{"action":"HOLD","confidence":0.3,"reason":"calm"}""");
        InstructCallStrategy sut = new(chat, Options());

        await sut.GenerateAsync(Sys, User, CancellationToken.None);

        chat.ReceivedOptions.Count.ShouldBe(2);
        chat.ReceivedOptions[0]!.ResponseFormat.ShouldNotBeNull();
        chat.ReceivedOptions[1]!.ResponseFormat.ShouldBeNull();
    }
}
