using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Application.UseCases;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.TestKit.Time;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.AiSuggestion.UseCases;

public class GetAllTodaysSuggestionsUseCaseTests
{
    private static readonly string[] ExpectedTickersInOrder = ["AAA", "BBB"];

    private static async Task<(GetAllTodaysSuggestionsUseCase sut, AppDbContext db, RecordingFactory factory)>
        BuildSutAsync(
            IDictionary<int, Exception>? throwForInstrumentId = null,
            CancellationToken ct = default)
    {
        var db = InMemoryDb.Create();

        // Two Held + one Watchlist.
        db.Instruments.AddRange(
            new Instrument { Id = 0, Ticker = "AAA", Name = "A", Currency = "USD",
                Exchange = "X", TimezoneId = "UTC", Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow },
            new Instrument { Id = 0, Ticker = "BBB", Name = "B", Currency = "USD",
                Exchange = "X", TimezoneId = "UTC", Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow },
            new Instrument { Id = 0, Ticker = "WATCH", Name = "W", Currency = "USD",
                Exchange = "X", TimezoneId = "UTC", Kind = InstrumentKind.Watchlist, AddedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var clock = new FakeClock(new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc));
        var idToTicker = await db.Instruments
            .ToDictionaryAsync(i => i.Id, i => i.Ticker, ct);
        var factory = new RecordingFactory(throwForInstrumentId, idToTicker);

        var single = new GetTodaysSuggestionUseCase(
            new TestRepo<Suggestion>(db), factory, factory, clock,
            new TestRepo<Instrument>(db),
            NullLogger<GetTodaysSuggestionUseCase>.Instance);

        var list = new ListInstrumentsUseCase(
            new TestRepo<Instrument>(db),
            NullLogger<ListInstrumentsUseCase>.Instance);

        var sut = new GetAllTodaysSuggestionsUseCase(
            single, list,
            NullLogger<GetAllTodaysSuggestionsUseCase>.Instance);

        return (sut, db, factory);
    }

    [Fact]
    public async Task Returns_one_Suggestion_per_Held_instrument()
    {
        var ct = TestContext.Current.CancellationToken;
        var (sut, _, factory) = await BuildSutAsync(ct: ct);

        var results = await sut.ExecuteAsync(Unit.Value, ct);

        results.Count.ShouldBe(2);
        results.Select(r => factory.TickerOf(r.InstrumentId)).ShouldBe(ExpectedTickersInOrder);
    }

    [Fact]
    public async Task Watchlist_instruments_are_excluded()
    {
        var ct = TestContext.Current.CancellationToken;
        var (sut, db, _) = await BuildSutAsync(ct: ct);

        var results = await sut.ExecuteAsync(Unit.Value, ct);

        var watchId = (await db.Instruments.SingleAsync(i => i.Ticker == "WATCH", ct)).Id;
        results.Select(r => r.InstrumentId).ShouldNotContain(watchId);
    }

    [Fact]
    public async Task One_failing_ticker_does_not_block_the_others()
    {
        var ct = TestContext.Current.CancellationToken;
        var failures = new Dictionary<int, Exception> { /* will populate after seed */ };
        var (sut0, db, _) = await BuildSutAsync(ct: ct);
        var aaaId = (await db.Instruments.SingleAsync(i => i.Ticker == "AAA", ct)).Id;
        failures[aaaId] = new PriceFeedUnavailableException("simulated");

        // Re-build with the failure map populated against the actual id.
        var (sut, _, _) = await BuildSutAsync(throwForInstrumentId: failures, ct: ct);

        var results = await sut.ExecuteAsync(Unit.Value, ct);

        // Only BBB succeeds; AAA's PriceFeedUnavailableException is swallowed.
        results.Count.ShouldBe(1);
    }

    private sealed class RecordingFactory : IAiSnapshotService, IAiClient
    {
        private readonly IDictionary<int, Exception>? _throws;
        private readonly IReadOnlyDictionary<int, string> _idToTicker;
        public List<int> CalledFor { get; } = new();
        public string TickerOf(int id) => _idToTicker.TryGetValue(id, out var t) ? t : "?";

        public RecordingFactory(
            IDictionary<int, Exception>? throws,
            IReadOnlyDictionary<int, string> idToTicker)
        {
            _throws = throws;
            _idToTicker = idToTicker;
        }

        public Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct)
        {
            if (_throws is not null && _throws.TryGetValue(instrumentId, out var ex)) throw ex;
            CalledFor.Add(instrumentId);
            return Task.FromResult(new AiSnapshot(
                asOf, instrumentId,
                GoalConfig.Default(DateTime.UtcNow),
                new PortfolioSnapshot([], 0, 0, 0, 0, 0, 0, 0),
                [], [], 1m, [], "h"));
        }

        public Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
        {
            return Task.FromResult(new Suggestion {
                Id = 0, InstrumentId = snapshot.InstrumentId,
                ForDate = snapshot.Today, Action = SuggestionAction.Hold,
                Conviction = 3, Rationale = "ok", CitationsJson = "[]",
                PromptHash = "h", CreatedAt = DateTime.UtcNow });
        }
    }
}
