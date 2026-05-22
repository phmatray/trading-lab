using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain.Suggestions.Services;
using TradyStrat.Infrastructure.AiSuggestion;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Infrastructure.Settings;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.AiSuggestion.UseCases;

public class QuerySuggestionsUseCaseTests
{
    private static readonly DateOnly Today = new(2026, 5, 18);
    private const int InstrId = 1;
    private const string Ticker = "TST";

    [Fact]
    public async Task Newest_first_with_outcome_when_evaluable()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, InstrId, Ticker);

        var oldDate = Today.AddDays(-10);
        db.Suggestions.Add(MkSuggestion(InstrId, oldDate, SuggestionAction.Acquire, 8));
        SeedExactBars(db, Ticker, oldDate, [100m, 101m, 101m, 102m, 102m, 103m]);

        db.Suggestions.Add(MkSuggestion(InstrId, Today, SuggestionAction.Hold, 5));
        SeedExactBars(db, Ticker, Today, [100m]);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = BuildUseCase(db);
        var input = new QuerySuggestionsInput(InstrId, Today.AddDays(-30), Today, Action: null, Limit: 10);
        var output = await sut.ExecuteAsync(input, TestContext.Current.CancellationToken);

        output.Items.Count.ShouldBe(2);

        output.Items[0].Date.ShouldBe(Today);
        output.Items[0].ForwardReturnPct.ShouldBeNull();
        output.Items[0].Correct.ShouldBeNull();

        output.Items[1].Date.ShouldBe(oldDate);
        output.Items[1].ForwardReturnPct.ShouldNotBeNull();
        output.Items[1].ForwardReturnPct!.Value.ShouldBe(3.0m);
        output.Items[1].Correct.ShouldNotBeNull();
        output.Items[1].Correct!.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task Limit_truncates_results()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, InstrId, Ticker);

        for (int i = 1; i <= 5; i++)
            db.Suggestions.Add(MkSuggestion(InstrId, new DateOnly(2026, 1, i), SuggestionAction.Hold, 5));

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = BuildUseCase(db);
        var input = new QuerySuggestionsInput(InstrId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), Action: null, Limit: 2);
        var output = await sut.ExecuteAsync(input, TestContext.Current.CancellationToken);

        output.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Action_filter_narrows_results()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, InstrId, Ticker);

        db.Suggestions.Add(MkSuggestion(InstrId, new DateOnly(2026, 1, 5),  SuggestionAction.Acquire, 8));
        db.Suggestions.Add(MkSuggestion(InstrId, new DateOnly(2026, 1, 10), SuggestionAction.Acquire, 7));
        db.Suggestions.Add(MkSuggestion(InstrId, new DateOnly(2026, 1, 15), SuggestionAction.Hold,    5));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = BuildUseCase(db);
        var input = new QuerySuggestionsInput(InstrId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31),
            Action: SuggestionAction.Acquire, Limit: 10);
        var output = await sut.ExecuteAsync(input, TestContext.Current.CancellationToken);

        output.Items.Count.ShouldBe(2);
        output.Items.ShouldAllBe(s => s.Action == SuggestionAction.Acquire);
    }

    private static QuerySuggestionsUseCase BuildUseCase(AppDbContext db)
    {
        var fwdCalculator = new ForwardReturnCalculator(new TestRepo<PriceBar>(db), new EfInstrumentRepository(db));
        var correctness   = new FixedThresholdCorrectness(2.0m);
        return new QuerySuggestionsUseCase(
            new EfSuggestionRepository(db),
            fwdCalculator,
            correctness,
            NullLogger<QuerySuggestionsUseCase>.Instance);
    }

    private static Suggestion MkSuggestion(int instrId, DateOnly date, SuggestionAction action, int conviction)
        => Suggestion.From(
            instrumentId: new InstrumentId(instrId),
            forDate:      date,
            action:       action,
            quantityHint: Quantity.None,
            maxPriceHint: Price.None(Currency.Eur),
            conviction:   Conviction.Of(conviction),
            rationale:    "test rationale",
            citations:    [],
            snapshot:     MarketSnapshot.Empty,
            fingerprint:  PromptFingerprint.Of("TEST", "", ""),
            thinkingText: "",
            createdAt:    DateTime.UtcNow);

    private static void SeedInstrument(AppDbContext db, int id, string ticker)
        => db.Instruments.Add(Instrument.Existing(
            id:         new InstrumentId(id),
            ticker:     ticker,
            name:       ticker,
            currency:   Currency.Eur,
            exchange:   Exchange.Of("X"),
            timezoneId: TimezoneId.Of("Etc/UTC"),
            kind:       InstrumentKind.Held,
            addedAt:    DateTime.UtcNow));

    private static void SeedExactBars(AppDbContext db, string ticker, DateOnly from, decimal[] closes)
    {
        for (int i = 0; i < closes.Length; i++)
            db.PriceBars.Add(new PriceBar
            {
                Id     = 0,
                Ticker = ticker,
                Date   = from.AddDays(i),
                Open   = closes[i],
                High   = closes[i],
                Low    = closes[i],
                Close  = closes[i],
                Volume = 1,
            });
    }
}
