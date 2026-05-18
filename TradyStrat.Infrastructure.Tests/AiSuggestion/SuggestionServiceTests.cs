using TradyStrat.Infrastructure.AiSuggestion;
using TradyStrat.Infrastructure.Exceptions;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Application.Settings.Config;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.AiSuggestion;

public class SuggestionServiceTests
{
    private static AiSnapshot SampleSnapshot() => new(
        Today: new DateOnly(2026, 5, 6),
        InstrumentId: 1,
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
            NullLogger<SuggestionService>.Instance, new StubSettingsReader("claude-opus-4-7", 1500));

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
            clock, NullLogger<SuggestionService>.Instance,
            new StubSettingsReader("claude-opus-4-7", 1500));

        await Should.ThrowAsync<AnthropicCallFailedException>(() =>
            svc.AskAsync(SampleSnapshot(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Sets_ModelId_and_MaxOutputTokens_from_settings_reader()
    {
        var clock = new FakeClock(new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc));
        var fake = new FakeChatClient(_ => Task.CompletedTask);   // never invokes the tool

        var svc = new SuggestionService(fake, clock, NullLogger<SuggestionService>.Instance,
            new StubSettingsReader("claude-test-model", 4242));

        await Should.ThrowAsync<AnthropicCallFailedException>(() =>   // tool not invoked → expected throw
            svc.AskAsync(SampleSnapshot(), TestContext.Current.CancellationToken));

        fake.LastOptions.ShouldNotBeNull();
        fake.LastOptions!.ModelId.ShouldBe("claude-test-model");
        fake.LastOptions.MaxOutputTokens.ShouldBe(4242);
    }

    private sealed class StubSettingsReader(string model, int maxTokens) : ISettingsReader
    {
        public Task<AnthropicSettings> AnthropicAsync(CancellationToken ct)
            => Task.FromResult(new AnthropicSettings(model, maxTokens, 8192));
        public Task<PolymarketSettings> PolymarketAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<string> FocusTickerAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<DateTime?> LastUpdatedAsync(IEnumerable<string> keys, CancellationToken ct) => throw new NotSupportedException();
    }
}
