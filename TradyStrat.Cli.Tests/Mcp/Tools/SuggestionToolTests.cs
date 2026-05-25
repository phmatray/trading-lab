using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Cli.Mcp.Tools;
using TradyStrat.Domain;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Shared.Market;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Infrastructure.Settings;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Tools;

public sealed class SuggestionToolTests
{
    private static readonly DateOnly FixedToday = new(2026, 5, 18);
    private const string FocusTicker = "CON3.L";

    // ─── Fake use case ───────────────────────────────────────────────────────────

    private sealed class FakeSuggestionUseCase(QuerySuggestionsOutput response)
        : IUseCase<QuerySuggestionsInput, QuerySuggestionsOutput>
    {
        public QuerySuggestionsInput? LastInput { get; private set; }

        public Task<QuerySuggestionsOutput> ExecuteAsync(QuerySuggestionsInput input, CancellationToken ct)
        {
            LastInput = input;
            // Filter the response items by action if specified, to simulate real use case behaviour.
            var items = input.Action is null
                ? response.Items
                : response.Items.Where(i => i.Action == input.Action).ToList();
            // Respect limit.
            var limited = items.Take(input.Limit).ToList();
            return Task.FromResult(new QuerySuggestionsOutput(limited));
        }
    }

    // ─── Fixture helpers ──────────────────────────────────────────────────────

    private static Instrument MakeInstrument(int id, string ticker) => Instrument.Existing(
        id:         new InstrumentId(id),
        ticker:     ticker,
        name:       ticker,
        currency:   Currency.Gbp,
        exchange:   Exchange.Of("LSE"),
        timezoneId: TimezoneId.Of("Europe/London"),
        kind:       InstrumentKind.Held,
        addedAt:    DateTime.UtcNow);

    private static QueriedSuggestion MakeSuggestion(SuggestionAction action, DateOnly date)
        => new(
            Date: date,
            Action: action,
            Conviction: 7,
            Reasoning: "test",
            EnvelopeHash: null,
            PromptVersionHash: null,
            ForwardReturnPct: null,
            Correct: null);

    private static async Task<(SuggestionTool tool, FakeSuggestionUseCase fakeUseCase)> BuildAsync(
        string focus,
        string[] knownTickers,
        QuerySuggestionsOutput? response = null,
        CancellationToken ct = default)
    {
        var db = InMemoryDb.Create();
        for (var i = 0; i < knownTickers.Length; i++)
            db.Instruments.Add(MakeInstrument(i + 1, knownTickers[i]));
        await db.SaveChangesAsync(ct);

        var repo = new EfInstrumentRepository(db);
        var listInstruments = new ListInstrumentsUseCase(repo, NullLogger<ListInstrumentsUseCase>.Instance);
        var guards = new Guards(listInstruments);

        var defaultResponse = response ?? new QuerySuggestionsOutput([]);
        var fakeUseCase = new FakeSuggestionUseCase(defaultResponse);
        var clock = new FakeClock(FixedToday.ToDateTime(TimeOnly.MinValue));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Tickers:Focus"] = focus })
            .Build();

        var tool = new SuggestionTool(fakeUseCase, guards, clock, config);
        return (tool, fakeUseCase);
    }

    // ─── Tests ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_suggestions_newest_first_with_default_range()
    {
        var ct = TestContext.Current.CancellationToken;

        var items = new[]
        {
            MakeSuggestion(SuggestionAction.Acquire, new DateOnly(2026, 5, 15)),
            MakeSuggestion(SuggestionAction.Hold,    new DateOnly(2026, 5, 10)),
            MakeSuggestion(SuggestionAction.Trim,    new DateOnly(2026, 5,  5)),
        };
        var response = new QuerySuggestionsOutput(items);

        var (tool, useCase) = await BuildAsync(FocusTicker, [FocusTicker], response, ct);

        var result = await tool.QuerySuggestions(ct: ct);

        result.ShouldNotBeNull();
        result.Instrument.ShouldBe(FocusTicker);
        result.Count.ShouldBe(3);
        result.Items.Count.ShouldBe(3);

        // Default range: 90 days back from today (2026-05-18) to 2026-05-18
        useCase.LastInput.ShouldNotBeNull();
        useCase.LastInput!.To.ShouldBe(FixedToday);
        useCase.LastInput.From.ShouldBe(new DateOnly(2026, 2, 17)); // 90 days before
    }

    [Fact]
    public async Task Action_filter_narrows_results()
    {
        var ct = TestContext.Current.CancellationToken;

        var items = new[]
        {
            MakeSuggestion(SuggestionAction.Acquire, new DateOnly(2026, 5, 15)),
            MakeSuggestion(SuggestionAction.Hold,    new DateOnly(2026, 5, 10)),
            MakeSuggestion(SuggestionAction.Acquire, new DateOnly(2026, 5,  5)),
        };
        var response = new QuerySuggestionsOutput(items);

        var (tool, _) = await BuildAsync(FocusTicker, [FocusTicker], response, ct);

        var result = await tool.QuerySuggestions(action: "Acquire", ct: ct);

        result.Count.ShouldBe(2);
        result.Items.ShouldAllBe(s => s.Action == "Acquire");
    }

    [Fact]
    public async Task Limit_zero_throws_with_message_about_1_to_100()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, _) = await BuildAsync(FocusTicker, [FocusTicker], ct: ct);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => tool.QuerySuggestions(limit: 0, ct: ct));

        ex.Message.ShouldContain("1 and 100");
        ex.Message.ShouldContain("0");
    }

    [Fact]
    public async Task Limit_above_100_throws_with_message_about_1_to_100()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, _) = await BuildAsync(FocusTicker, [FocusTicker], ct: ct);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => tool.QuerySuggestions(limit: 101, ct: ct));

        ex.Message.ShouldContain("1 and 100");
        ex.Message.ShouldContain("101");
    }

    [Fact]
    public async Task Action_bogus_throws_with_list_of_valid_actions()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, _) = await BuildAsync(FocusTicker, [FocusTicker], ct: ct);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => tool.QuerySuggestions(action: "Buy", ct: ct));

        ex.Message.ShouldContain("Buy");
        // Should list at least one valid action name
        ex.Message.ShouldContain("Acquire");
    }

    [Fact]
    public async Task Unknown_instrument_throws_via_Guards()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, _) = await BuildAsync(FocusTicker, [FocusTicker], ct: ct);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => tool.QuerySuggestions(instrument: "NOPE", ct: ct));

        ex.Message.ShouldContain("NOPE");
        ex.Message.ShouldContain("list_instruments");
    }
}
