using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Cli.Mcp.Tools;
using TradyStrat.Domain;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Tools;

public sealed class ReplayToolTests
{
    private static readonly DateOnly FixedToday = new(2026, 5, 18);
    private const string Ticker = "CON3.L";

    // ─── Fake use case ───────────────────────────────────────────────────────────

    private sealed class FakeReplayUseCase(ReplayReport response)
        : IUseCase<ReplaySuggestionsInput, ReplayReport>
    {
        public ReplaySuggestionsInput? LastInput { get; private set; }

        public Task<ReplayReport> ExecuteAsync(ReplaySuggestionsInput input, CancellationToken ct)
        {
            LastInput = input;
            return Task.FromResult(response);
        }
    }

    // ─── Fixture helpers ──────────────────────────────────────────────────────

    private static Instrument MakeInstrument(int id, string ticker) => new()
    {
        Id = id,
        Ticker = ticker,
        Name = ticker,
        Currency = "GBP",
        Exchange = "LSE",
        TimezoneId = "Europe/London",
        Kind = InstrumentKind.Held,
        AddedAt = DateTime.UtcNow,
    };

    private static ReplayReport MakeEmptyReport(int instrumentId, DateOnly since, DateOnly until)
        => new(
            InstrumentId: instrumentId,
            Since: since,
            Until: until,
            Rows: [],
            PerAction: new Dictionary<SuggestionAction, ActionAggregate>(),
            Overall: new ActionAggregate(0, 0m, 0m, 0m),
            ConvictionWeightedScore: 0m,
            DistinctPromptVersionHashes: []);

    private static async Task<(ReplayTool tool, FakeReplayUseCase fakeUseCase)> BuildAsync(
        string[] knownTickers,
        ReplayReport? response = null,
        CancellationToken ct = default)
    {
        var db = InMemoryDb.Create();
        for (var i = 0; i < knownTickers.Length; i++)
            db.Instruments.Add(MakeInstrument(i + 1, knownTickers[i]));
        await db.SaveChangesAsync(ct);

        var repo = new TestRepo<Instrument>(db);
        var listInstruments = new ListInstrumentsUseCase(repo, NullLogger<ListInstrumentsUseCase>.Instance);
        var guards = new Guards(listInstruments);

        var since = new DateOnly(2026, 1, 1);
        var until = new DateOnly(2026, 5, 1);
        var defaultResponse = response ?? MakeEmptyReport(1, since, until);
        var fakeUseCase = new FakeReplayUseCase(defaultResponse);
        var clock = new FakeClock(FixedToday.ToDateTime(TimeOnly.MinValue));

        var tool = new ReplayTool(fakeUseCase, guards, clock);
        return (tool, fakeUseCase);
    }

    // ─── Tests ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Returns_replay_report_for_explicit_range()
    {
        var ct = TestContext.Current.CancellationToken;

        var since = new DateOnly(2026, 1, 1);
        var until = new DateOnly(2026, 5, 1);
        var expectedReport = MakeEmptyReport(instrumentId: 1, since, until);

        var (tool, _) = await BuildAsync([Ticker], expectedReport, ct);

        var result = await tool.GetReplayReport(
            instrument: Ticker,
            from: "2026-01-01",
            to: "2026-05-01",
            ct: ct);

        result.ShouldNotBeNull();
        result.InstrumentId.ShouldBe(1);
        result.Since.ShouldBe(since);
        result.Until.ShouldBe(until);
    }

    [Fact]
    public async Task Persist_is_always_false_and_force_is_always_false()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, useCase) = await BuildAsync([Ticker], ct: ct);

        await tool.GetReplayReport(
            instrument: Ticker,
            from: "2026-01-01",
            to: "2026-05-01",
            ct: ct);

        useCase.LastInput.ShouldNotBeNull();
        useCase.LastInput!.Persist.ShouldBeFalse();
        useCase.LastInput.Force.ShouldBeFalse();
    }

    [Fact]
    public async Task Unknown_instrument_throws_via_Guards()
    {
        var ct = TestContext.Current.CancellationToken;
        var (tool, _) = await BuildAsync([Ticker], ct: ct);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => tool.GetReplayReport(
                instrument: "NOPE",
                from: "2026-01-01",
                to: "2026-05-01",
                ct: ct));

        ex.Message.ShouldContain("NOPE");
        ex.Message.ShouldContain("list_instruments");
    }
}
