using TradyStrat.Infrastructure.AiSuggestion;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Application.Settings.Config;
using TradyStrat.TestKit.AiSuggestion;          // FakeChatClient
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.AiSuggestion;

public class SuggestionServiceCitationTests
{
    private static AiSnapshot SnapshotWith(params PredictionMarket[] markets) => new(
        Today: new DateOnly(2026, 5, 6),
        InstrumentId: 1,
        Goal: GoalConfig.Default(DateTime.UtcNow),
        Portfolio: new([], 0, 0, 0, 0, 0, 0, 0),
        Tickers: [],
        RecentTrades: [],
        UsdPerEur: 1.08m,
        Markets: markets,
        PromptHash: "h");

    private static PredictionMarket M(string slug) =>
        new(slug, slug, 0.5m, new DateOnly(2026, 12, 31), 1m, []);

    [Fact]
    public async Task Drops_unknown_market_slugs_in_citations()
    {
        var clock = new FakeClock(new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc));
        var svc = new SuggestionService(
            new FakeChatClient(InvokeWithCitations([
                ("known", "claim-a"), ("unknown", "claim-b")
            ])),
            clock, NullLogger<SuggestionService>.Instance,
            new StubSettingsReader("claude-opus-4-7", 1500));

        var sug = await svc.AskAsync(SnapshotWith(M("known")), TestContext.Current.CancellationToken);

        sug.MarketSnapshotJson.ShouldNotBeNull();
        var snap = JsonSerializer.Deserialize<MarketSnapshot>(sug.MarketSnapshotJson!, JsonOpts.Strict)!;
        snap.Cited.Select(c => c.Slug).ShouldBe(["known"]);
    }

    [Fact]
    public async Task Dedupes_duplicate_market_citations_first_wins()
    {
        var clock = new FakeClock(new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc));
        var svc = new SuggestionService(
            new FakeChatClient(InvokeWithCitations([
                ("dup", "first-claim"), ("dup", "second-claim")
            ])),
            clock, NullLogger<SuggestionService>.Instance,
            new StubSettingsReader("claude-opus-4-7", 1500));

        var sug = await svc.AskAsync(SnapshotWith(M("dup")), TestContext.Current.CancellationToken);

        var snap = JsonSerializer.Deserialize<MarketSnapshot>(sug.MarketSnapshotJson!, JsonOpts.Strict)!;
        snap.Cited.Count.ShouldBe(1);
        snap.Cited[0].Claim.ShouldBe("first-claim");
    }

    [Fact]
    public async Task MarketSnapshotJson_is_NULL_when_snapshot_has_no_markets()
    {
        var clock = new FakeClock(new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc));
        var svc = new SuggestionService(
            new FakeChatClient(InvokeWithCitations([])),
            clock, NullLogger<SuggestionService>.Instance,
            new StubSettingsReader("claude-opus-4-7", 1500));

        var sug = await svc.AskAsync(SnapshotWith(/* no markets */), TestContext.Current.CancellationToken);

        sug.MarketSnapshotJson.ShouldBeNull();
    }

    private sealed class StubSettingsReader(string model, int maxTokens) : ISettingsReader
    {
        public Task<AnthropicSettings> AnthropicAsync(CancellationToken ct)
            => Task.FromResult(new AnthropicSettings(model, maxTokens));
        public Task<PolymarketSettings> PolymarketAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<string> FocusTickerAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<DateTime?> LastUpdatedAsync(IEnumerable<string> keys, CancellationToken ct) => throw new NotSupportedException();
    }

    private static Func<IList<AIFunction>, Task> InvokeWithCitations(
        IReadOnlyList<(string Slug, string Claim)> citations) =>
        async tools =>
        {
            var t = tools.Single();
            var fnArgs = new AIFunctionArguments();
            foreach (var prop in JsonSerializer.SerializeToElement(new
            {
                action = "Hold",
                quantity_hint = (decimal?)null,
                max_price_hint = (decimal?)null,
                conviction = 1,
                rationale = "test",
                citations = Array.Empty<object>(),
                market_citations = citations.Select(c => new { slug = c.Slug, claim = c.Claim }).ToArray(),
            }).EnumerateObject())
                fnArgs.Add(prop.Name, prop.Value);
            await t.InvokeAsync(fnArgs, TestContext.Current.CancellationToken);
        };
}
