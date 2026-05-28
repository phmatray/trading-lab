using System.Net;
using System.Text.Json;
using Shouldly;
using TradingSignal.Core;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Prompts;
using TradingSignal.Llm.Strategies;
using TradingSignal.Llm.Tests.Fakes;

namespace TradingSignal.Llm.Tests;

public sealed class ReasoningCallStrategyTests
{
    private const string Sys = "system";
    private const string User = "user";

    private static LmStudioOptions Options() => new()
    {
        ModelId = "qwen/qwen3.6-35b-a3b",
        MaxOutputTokens = 2048,
        TimeoutSeconds = 120,
        ReasoningEffort = "medium",
    };

    private static (ReasoningCallStrategy sut, FakeHttpMessageHandler handler) Build()
    {
        FakeHttpMessageHandler handler = new();
        HttpClient http = new(handler) { BaseAddress = new Uri("http://localhost:1234/v1/") };
        return (new ReasoningCallStrategy(http, Options()), handler);
    }

    private static object OkBody(string content, string? reasoning = null)
        => new
        {
            choices = new[]
            {
                new
                {
                    finish_reason = "stop",
                    message = new { role = "assistant", content, reasoning_content = reasoning },
                },
            },
        };

    [Fact]
    public void SystemPrompt_Returns_Reasoning_Constant()
    {
        var (sut, _) = Build();
        sut.SystemPrompt.ShouldBe(PromptBuilder.SystemPromptReasoning);
    }

    [Fact]
    public async Task Parses_Content_And_Captures_Reasoning_Content()
    {
        var (sut, handler) = Build();
        handler.EnqueueJson(OkBody(
            content: """{"action":"BUY","confidence":0.7,"reason":"trend up"}""",
            reasoning: "step 1... step 2..."));

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Buy);
        outcome.Signal.Confidence.ShouldBe(0.7);
        outcome.Signal.Reason.ShouldBe("trend up");
        outcome.ReasoningContent.ShouldBe("step 1... step 2...");
    }

    [Fact]
    public async Task Parses_Fenced_Json_Inside_Prose_Content()
    {
        var (sut, handler) = Build();
        string content = "Reasoning summary.\n\n```json\n{\"action\":\"SELL\",\"confidence\":0.6,\"reason\":\"rsi extreme\"}\n```\n";
        handler.EnqueueJson(OkBody(content: content, reasoning: "longer trace"));

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Sell);
        outcome.ReasoningContent.ShouldBe("longer trace");
    }

    [Fact]
    public async Task Retries_With_Stricter_Reminder_When_First_Parse_Fails()
    {
        var (sut, handler) = Build();
        handler.EnqueueJson(OkBody(content: "garbage no json here", reasoning: "first trace"));
        handler.EnqueueJson(OkBody(
            content: """{"action":"HOLD","confidence":0.4,"reason":"unsure"}""",
            reasoning: "second trace"));

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Hold);
        outcome.ReasoningContent.ShouldBe("second trace");
        handler.ReceivedRequests.Count.ShouldBe(2);

        // Second request body has the stricter reminder appended as a user message.
        JsonElement secondMessages = handler.ReceivedBodies[1].GetProperty("messages");
        secondMessages.GetArrayLength().ShouldBe(3); // system, user, reminder
        secondMessages[2].GetProperty("role").GetString().ShouldBe("user");
        (secondMessages[2].GetProperty("content").GetString() ?? string.Empty).ShouldContain("ONLY");
    }

    [Fact]
    public async Task Empty_Content_With_Non_Empty_Reasoning_Returns_ParseFailure_With_Trace_Preserved()
    {
        var (sut, handler) = Build();
        handler.EnqueueJson(OkBody(content: "", reasoning: "thought but did not answer"));
        handler.EnqueueJson(OkBody(content: "", reasoning: "still no answer"));

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Hold);
        outcome.Signal.Reason.ShouldBe("parse_failure");
        outcome.ReasoningContent.ShouldBe("still no answer");
    }

    [Fact]
    public async Task Http_500_Degrades_To_ParseFailure_Without_Throwing()
    {
        var (sut, handler) = Build();
        handler.EnqueueRawJson("server error", HttpStatusCode.InternalServerError);
        handler.EnqueueRawJson("server error", HttpStatusCode.InternalServerError);

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Hold);
        outcome.Signal.Reason.ShouldBe("parse_failure");
    }

    [Fact]
    public async Task Transport_Exception_Degrades_To_ParseFailure()
    {
        var (sut, handler) = Build();
        handler.EnqueueException(new HttpRequestException("connection refused"));
        handler.EnqueueException(new HttpRequestException("again"));

        LlmCallOutcome outcome = await sut.GenerateAsync(Sys, User, CancellationToken.None);

        outcome.Signal.Action.ShouldBe(TradeAction.Hold);
        outcome.Signal.Reason.ShouldBe("parse_failure");
    }

    [Fact]
    public async Task Caller_Cancellation_Propagates()
    {
        var (sut, handler) = Build();
        handler.EnqueueException(new OperationCanceledException());
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.GenerateAsync(Sys, User, cts.Token));
    }

    [Fact]
    public async Task Request_Body_Has_Expected_Shape()
    {
        var (sut, handler) = Build();
        handler.EnqueueJson(OkBody(
            content: """{"action":"BUY","confidence":0.5,"reason":"x"}""",
            reasoning: null));

        await sut.GenerateAsync(Sys, User, CancellationToken.None);

        JsonElement body = handler.ReceivedBodies[0];
        body.GetProperty("model").GetString().ShouldBe("qwen/qwen3.6-35b-a3b");
        body.GetProperty("max_tokens").GetInt32().ShouldBe(2048);
        body.GetProperty("reasoning_effort").GetString().ShouldBe("medium");
        body.TryGetProperty("response_format", out _).ShouldBeFalse();
        body.GetProperty("messages").GetArrayLength().ShouldBe(2);
    }
}
