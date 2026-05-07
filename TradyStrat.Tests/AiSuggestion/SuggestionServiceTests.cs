using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Tests.Common.Time;
using Xunit;

namespace TradyStrat.Tests.AiSuggestion;

public class SuggestionServiceTests
{
    private static AiSnapshot SampleSnapshot() => new(
        Today: new DateOnly(2026, 5, 6),
        Goal: GoalConfig.Default(DateTime.UtcNow),
        Portfolio: new([], 0, 0, 0, 0, 0, 0, 0),
        Tickers: [],
        RecentTrades: [],
        UsdPerEur: 1.08m,
        Markets: [],
        PromptHash: "hashvalue");

    [Fact]
    public async Task Captures_tool_invocation_and_returns_typed_Suggestion()
    {
        var clock = new FakeClock(new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc));

        async Task Invoke(IList<AIFunction> tools)
        {
            var t = tools.Single();

            // Build args as snake_case (matches AIFunctionFactory's parameter naming).
            var argsJson = JsonSerializer.SerializeToElement(new
            {
                action = "Acquire",
                quantity_hint = 8m,
                max_price_hint = 4.85m,
                conviction = 4,
                rationale = "Below lower band; RSI rising.",
                citations = new[]
                {
                    new { claim = "x", indicator = "Bollinger", ticker = "CON3.DE", value = "below" }
                },
                market_citations = Array.Empty<object>()
            });

            // Build AIFunctionArguments from the JsonElement properties.
            var fnArgs = new AIFunctionArguments();
            foreach (var prop in argsJson.EnumerateObject())
                fnArgs.Add(prop.Name, prop.Value);

            await t.InvokeAsync(fnArgs, TestContext.Current.CancellationToken);
        }

        var svc = new SuggestionService(new FakeChatClient(Invoke), clock,
            NullLogger<SuggestionService>.Instance);

        var sug = await svc.AskAsync(SampleSnapshot(), TestContext.Current.CancellationToken);

        sug.Action.ShouldBe(SuggestionAction.Acquire);
        sug.QuantityHint.ShouldBe(8m);
        sug.Conviction.ShouldBe(4);
        sug.Rationale.ShouldContain("Below lower band");
        sug.PromptHash.ShouldBe("hashvalue");
    }

    [Fact]
    public async Task Throws_AnthropicCallFailedException_when_tool_not_invoked()
    {
        var clock = new FakeClock(new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc));
        var svc = new SuggestionService(
            new FakeChatClient(_ => Task.CompletedTask),
            clock, NullLogger<SuggestionService>.Instance);

        await Should.ThrowAsync<AnthropicCallFailedException>(() =>
            svc.AskAsync(SampleSnapshot(), TestContext.Current.CancellationToken));
    }
}
