using TradyStrat.Domain.Suggestions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Application.Dashboard;
using TradyStrat.Application.Dashboard.UseCases;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.Dashboard.UseCases;

public class BuildFocusDerivedSliceUseCaseTests
{
    private static readonly DateOnly Today = new(2026, 5, 21);
    private const int InstrId = 1;
    private const string Ticker = "TST";

    // Matches Suggestion.CitationOpts so Suggestion.Citations round-trips snake_case JSON.
    private static readonly JsonSerializerOptions CitationOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    [Fact]
    public async Task No_prior_returns_CallDiff_None_and_populated_others()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, InstrId, Ticker);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var focus = MkSuggestion(InstrId, Today, citations: Array.Empty<Citation>(), marketJson: null);

        var sut = BuildSut(db);
        var slice = await sut.BuildAsync(focus, Today, TestContext.Current.CancellationToken);

        slice.CallDiff.ShouldBe(CallDiff.None);
        slice.IndicatorHistories.ShouldBeEmpty();
        slice.MarketSnapshot.ShouldBe(MarketSnapshot.Empty);
    }

    [Fact]
    public async Task With_prior_builds_CallDiff_diff()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, InstrId, Ticker);

        var yesterday = Today.AddDays(-1);
        var priorRow = MkSuggestion(InstrId, yesterday) with { Action = SuggestionAction.Acquire };
        db.Suggestions.Add(priorRow);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var focus = MkSuggestion(InstrId, Today) with { Action = SuggestionAction.Hold };

        var sut = BuildSut(db);
        var slice = await sut.BuildAsync(focus, Today, TestContext.Current.CancellationToken);

        slice.CallDiff.ShouldNotBe(CallDiff.None);
        slice.CallDiff.ActionChanged.ShouldBeTrue();
        slice.CallDiff.PriorAction.ShouldBe(SuggestionAction.Acquire);
    }

    [Fact]
    public async Task Histories_built_only_for_cited_indicators_and_deduped()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, InstrId, Ticker);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var citations = new[]
        {
            new Citation(Claim: "x", Indicator: "RSI", Ticker: Ticker, Value: ""),
            new Citation(Claim: "y", Indicator: "RSI", Ticker: Ticker, Value: ""),     // duplicate — dedup expected
            new Citation(Claim: "z", Indicator: "Bollinger", Ticker: Ticker, Value: ""),
            new Citation(Claim: "?", Indicator: "NotAnIndicator", Ticker: Ticker, Value: ""), // unparseable — skipped
        };
        var focus = MkSuggestion(InstrId, Today, citations: citations);

        var engine = new RecordingIndicatorEngine();
        var sut = new BuildFocusDerivedSliceUseCase(
            new TestRepo<Suggestion>(db),
            engine,
            NullLogger<BuildFocusDerivedSliceUseCase>.Instance);

        var slice = await sut.BuildAsync(focus, Today, TestContext.Current.CancellationToken);

        engine.Calls.Count.ShouldBe(2);   // RSI once + Bollinger once
        slice.IndicatorHistories.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Malformed_market_json_returns_Empty_and_does_not_throw()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, InstrId, Ticker);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var focus = MkSuggestion(InstrId, Today, marketJson: "{not valid json");

        var sut = BuildSut(db);
        var slice = await sut.BuildAsync(focus, Today, TestContext.Current.CancellationToken);

        slice.MarketSnapshot.ShouldBe(MarketSnapshot.Empty);
    }

    // ---------- Helpers ----------

    private static BuildFocusDerivedSliceUseCase BuildSut(AppDbContext db)
        => new(
            new TestRepo<Suggestion>(db),
            new StubIndicatorEngine(),
            NullLogger<BuildFocusDerivedSliceUseCase>.Instance);

    private static void SeedInstrument(AppDbContext db, int id, string ticker)
        => db.Instruments.Add(new Instrument
        {
            Id         = id,
            Ticker     = ticker,
            Name       = ticker,
            Currency   = "USD",
            Exchange   = "TST",
            TimezoneId = "Etc/UTC",
            Kind       = InstrumentKind.Held,
            AddedAt    = DateTime.UtcNow,
        });

    private static Suggestion MkSuggestion(
        int instrumentId,
        DateOnly forDate,
        IReadOnlyList<Citation>? citations = null,
        string? marketJson = null)
    {
        var json = citations is { Count: > 0 }
            ? JsonSerializer.Serialize(citations, CitationOpts)
            : "[]";
        return new Suggestion
        {
            Id = 0,
            InstrumentId = instrumentId,
            ForDate = forDate,
            Action = SuggestionAction.Hold,
            Conviction = 5,
            Rationale = "t",
            CitationsJson = json,
            MarketSnapshotJson = marketJson,
            PromptHash = "h",
            CreatedAt = DateTime.UtcNow,
        };
    }

    private sealed class StubIndicatorEngine : IIndicatorEngine
    {
        public Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct)
            => throw new NotSupportedException("ComputeFor not used in this test.");

        public Task<IndicatorReading> ComputeFor(string ticker, DateOnly asOf, CancellationToken ct)
            => throw new NotSupportedException("ComputeFor not used in this test.");

        public Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int lastN, CancellationToken ct)
            => Task.FromResult(IndicatorSeries.Empty);

        public Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int lastN, DateOnly asOf, CancellationToken ct)
            => Task.FromResult(IndicatorSeries.Empty);
    }

    private sealed class RecordingIndicatorEngine : IIndicatorEngine
    {
        public List<(string Ticker, IndicatorKind Kind)> Calls { get; } = [];

        public Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<IndicatorReading> ComputeFor(string ticker, DateOnly asOf, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int lastN, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int lastN, DateOnly asOf, CancellationToken ct)
        {
            Calls.Add((ticker, kind));
            return Task.FromResult(IndicatorSeries.Empty);
        }
    }
}
