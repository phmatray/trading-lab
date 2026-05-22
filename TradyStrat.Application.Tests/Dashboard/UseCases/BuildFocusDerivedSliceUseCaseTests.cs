using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.AiSuggestion.CallDiff;
using TradyStrat.Application.Dashboard;
using TradyStrat.Application.Dashboard.UseCases;
using TradyStrat.Application.Indicators;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Infrastructure.AiSuggestion;
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

    [Fact]
    public async Task No_prior_returns_CallDiff_None_and_populated_others()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, InstrId, Ticker);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var focus = MkSuggestion(InstrId, Today, SuggestionAction.Hold);

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
        var priorRow = MkSuggestion(InstrId, yesterday, SuggestionAction.Acquire);
        db.Suggestions.Add(priorRow);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var focus = MkSuggestion(InstrId, Today, SuggestionAction.Hold);

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
            new Citation("x", "RSI", Ticker, ""),
            new Citation("y", "RSI", Ticker, ""),               // duplicate — dedup expected
            new Citation("z", "Bollinger", Ticker, ""),
            new Citation("?", "NotAnIndicator", Ticker, ""),    // unparseable — skipped
        };
        var focus = MkSuggestion(InstrId, Today, SuggestionAction.Hold, citations);

        var engine = new RecordingIndicatorEngine();
        var sut = new BuildFocusDerivedSliceUseCase(
            new EfSuggestionRepository(db),
            engine);

        var slice = await sut.BuildAsync(focus, Today, TestContext.Current.CancellationToken);

        engine.Calls.Count.ShouldBe(2);   // RSI once + Bollinger once
        slice.IndicatorHistories.Count.ShouldBe(2);
    }

    private static BuildFocusDerivedSliceUseCase BuildSut(AppDbContext db)
        => new(
            new EfSuggestionRepository(db),
            new StubIndicatorEngine());

    private static void SeedInstrument(AppDbContext db, int id, string ticker)
        => db.Instruments.Add(Instrument.Existing(
            id:         new InstrumentId(id),
            ticker:     ticker,
            name:       ticker,
            currency:   Currency.Usd,
            exchange:   Exchange.Of("TST"),
            timezoneId: TimezoneId.Of("Etc/UTC"),
            kind:       InstrumentKind.Held,
            addedAt:    DateTime.UtcNow));

    private static Suggestion MkSuggestion(
        int instrumentId,
        DateOnly forDate,
        SuggestionAction action,
        IReadOnlyList<Citation>? citations = null)
        => Suggestion.From(
            instrumentId: new InstrumentId(instrumentId),
            forDate:      forDate,
            action:       action,
            quantityHint: Quantity.None,
            maxPriceHint: Price.None(Currency.Eur),
            conviction:   Conviction.Of(5),
            rationale:    "t",
            citations:    citations ?? [],
            snapshot:     MarketSnapshot.Empty,
            fingerprint:  PromptFingerprint.Of("h", "", ""),
            thinkingText: "",
            createdAt:    DateTime.UtcNow);

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
