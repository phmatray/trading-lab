using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;
using TradyStrat.TestKit;
using TradyStrat.TestKit.AiSuggestion;
using TradyStrat.TestKit.Settings;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Application.Tests.AiSuggestion.UseCases;

public class StreamTodaysSuggestionsUseCaseTests
{
    private static readonly DateOnly Today = new(2026, 5, 21);
    private static readonly DateTime TodayUtc = new(2026, 5, 21, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Empty_input_yields_no_events()
    {
        var sut = BuildSut(out _);

        var events = new List<SuggestionStreamEvent>();
        await foreach (var ev in sut.StreamAsync(Array.Empty<int>(), TestContext.Current.CancellationToken))
            events.Add(ev);

        events.ShouldBeEmpty();
    }

    [Fact]
    public async Task Single_instrument_yields_one_Ready_event()
    {
        var sut = BuildSut(out var fake, maxParallel: 3, instrumentIds: [7]);
        fake.ConfigureFor(7, MkSuggestion(7));

        var events = new List<SuggestionStreamEvent>();
        await foreach (var ev in sut.StreamAsync([7], TestContext.Current.CancellationToken))
            events.Add(ev);

        events.Count.ShouldBe(1);
        events[0].ShouldBeOfType<SuggestionStreamEvent.Ready>().InstrumentId.ShouldBe(7);
    }

    [Fact]
    public async Task Yields_in_completion_order_not_input_order()
    {
        var sut = BuildSut(out var fake, maxParallel: 3, instrumentIds: [1, 2, 3]);
        fake.ConfigureFor(1, MkSuggestion(1), TimeSpan.FromMilliseconds(120));
        fake.ConfigureFor(2, MkSuggestion(2), TimeSpan.FromMilliseconds(20));
        fake.ConfigureFor(3, MkSuggestion(3), TimeSpan.FromMilliseconds(60));

        var order = new List<int>();
        await foreach (var ev in sut.StreamAsync([1, 2, 3], TestContext.Current.CancellationToken))
            order.Add(ev.InstrumentId);

        order.ShouldBe([2, 3, 1]);
    }

    [Fact]
    public async Task One_failed_instrument_does_not_block_others()
    {
        var sut = BuildSut(out var fake, maxParallel: 3, instrumentIds: [10, 20, 30]);
        fake.ConfigureFor(10, MkSuggestion(10));
        fake.ConfigureFailureFor(20, new InvalidOperationException("boom"));
        fake.ConfigureFor(30, MkSuggestion(30));

        var events = new List<SuggestionStreamEvent>();
        await foreach (var ev in sut.StreamAsync([10, 20, 30], TestContext.Current.CancellationToken))
            events.Add(ev);

        events.Count.ShouldBe(3);
        events.OfType<SuggestionStreamEvent.Ready>()
            .Select(e => e.InstrumentId)
            .OrderBy(x => x)
            .ShouldBe([10, 30]);
        var fail = events.OfType<SuggestionStreamEvent.Failed>().ShouldHaveSingleItem();
        fail.InstrumentId.ShouldBe(20);
        fail.Reason.ShouldContain("boom");
    }

    [Fact]
    public async Task Respects_max_parallel_cap()
    {
        var ids = new[] { 1, 2, 3, 4, 5 };
        var sut = BuildSut(out var fake, maxParallel: 2, instrumentIds: ids);
        foreach (var id in ids)
            fake.ConfigureFor(id, MkSuggestion(id), TimeSpan.FromMilliseconds(30));

        await foreach (var _ in sut.StreamAsync(ids, TestContext.Current.CancellationToken)) { }

        fake.MaxObservedConcurrency.ShouldBeLessThanOrEqualTo(2);
        fake.MaxObservedConcurrency.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Cancellation_stops_new_starts()
    {
        var ids = new[] { 1, 2, 3 };
        var sut = BuildSut(out var fake, maxParallel: 1, instrumentIds: ids);
        foreach (var id in ids)
            fake.ConfigureFor(id, MkSuggestion(id), TimeSpan.FromMilliseconds(150));

        using var cts = new CancellationTokenSource();
        var received = new List<int>();

        var testCt = TestContext.Current.CancellationToken;
        var enumerate = Task.Run(async () =>
        {
            try
            {
                await foreach (var ev in sut.StreamAsync(ids, cts.Token))
                    received.Add(ev.InstrumentId);
            }
            catch (OperationCanceledException) { /* expected */ }
        }, testCt);

        await Task.Delay(40, testCt);   // let the first worker get started
        cts.Cancel();
        await enumerate;

        received.Count.ShouldBeLessThan(ids.Length);
    }

    [Fact]
    public async Task Duplicate_ids_yield_one_event_each_input_position()
    {
        var sut = BuildSut(out var fake, maxParallel: 3, instrumentIds: [9]);
        fake.ConfigureFor(9, MkSuggestion(9));

        // Pass the same id twice. The stream is dumb: each input element gets a
        // worker. This test documents that contract so callers don't expect dedup.
        var events = new List<SuggestionStreamEvent>();
        await foreach (var ev in sut.StreamAsync([9, 9], TestContext.Current.CancellationToken))
            events.Add(ev);

        events.Count.ShouldBe(2);
        events.ShouldAllBe(e => e.InstrumentId == 9);
    }

    // ---------- Helpers ----------

    /// <summary>Builds the SUT with a fresh in-memory DB and a fake AI client. Seeds one instrument per id supplied.</summary>
    private static StreamTodaysSuggestionsUseCase BuildSut(
        out FakeAiClient fake,
        int maxParallel = 3,
        params int[] instrumentIds)
    {
        fake = new FakeAiClient();
        var db = InMemoryDb.Create();
        foreach (var id in instrumentIds)
        {
            db.Instruments.Add(new Instrument
            {
                Id = id,
                Ticker = $"T{id}",
                Name = $"Test {id}",
                Currency = "USD",
                Exchange = "TST",
                TimezoneId = "UTC",
                Kind = InstrumentKind.Held,
                AddedAt = TodayUtc,
            });
        }
        db.SaveChanges();

        var instrumentRepo = new TestRepo<Instrument>(db);
        var suggestionRepo = new TestRepo<Suggestion>(db);

        // Per-instrument snapshot factory: returns a snapshot whose InstrumentId
        // matches the id being asked for, so FakeAiClient can dispatch correctly.
        var snapshot = new PerInstrumentSnapshotFactory(Today);
        var clock = new FakeClock(TodayUtc);

        var getOne = new GetTodaysSuggestionUseCase(
            suggestionRepo,
            snapshot,
            fake,
            clock,
            instrumentRepo,
            NullLogger<GetTodaysSuggestionUseCase>.Instance);

        var anthropic = new AnthropicSettings("test", 1500, 8192, maxParallel);
        var reader = new FakeSettingsReader(anthropic: anthropic);

        return new StreamTodaysSuggestionsUseCase(
            getOne,
            reader,
            NullLogger<StreamTodaysSuggestionsUseCase>.Instance);
    }

    /// <summary>Snapshot factory that echoes the requested InstrumentId. Other fields are stubbed.</summary>
    private sealed class PerInstrumentSnapshotFactory(DateOnly today) : IAiSnapshotService
    {
        public Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct)
            => Task.FromResult(new AiSnapshot(
                today, instrumentId,
                GoalConfig.Default(DateTime.UtcNow),
                new PortfolioSnapshot([], 0, 0, 0, 0, 0, 0, 0),
                [], [], null, [], [],
                "env", "pv", "ph"));
    }

    private static Suggestion MkSuggestion(int instrumentId, SuggestionAction action = SuggestionAction.Hold, int conviction = 5)
        => new()
        {
            Id = 0,
            InstrumentId = instrumentId,
            ForDate = Today,
            Action = action,
            Conviction = conviction,
            Rationale = "test",
            CitationsJson = "[]",
            PromptHash = "h",
            CreatedAt = TodayUtc,
        };
}
