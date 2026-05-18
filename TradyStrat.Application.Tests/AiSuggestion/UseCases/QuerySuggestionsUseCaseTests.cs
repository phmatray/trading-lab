using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.AiSuggestion.UseCases;

public class QuerySuggestionsUseCaseTests
{
    private static readonly DateOnly Today = new(2026, 5, 18);
    private const int InstrId = 1;
    private const string Ticker = "TST";

    // ──────────────────────────────────────────────────────────────────────────
    // Test 1: newest-first ordering, forward-return evaluable for old, null for today
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Newest_first_with_outcome_when_evaluable()
    {
        await using var db = InMemoryDb.Create();
        SeedInstrument(db, InstrId, Ticker);

        // Suggestion ~10 days ago — 6 bars available after that date, window complete
        var oldDate = Today.AddDays(-10);
        db.Suggestions.Add(MkSuggestion(InstrId, oldDate, SuggestionAction.Acquire, 8));
        SeedExactBars(db, Ticker, oldDate, [100m, 101m, 101m, 102m, 102m, 103m]);

        // Suggestion for today — only 1 bar (today), window incomplete
        db.Suggestions.Add(MkSuggestion(InstrId, Today, SuggestionAction.Hold, 5));
        SeedExactBars(db, Ticker, Today, [100m]);

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = BuildUseCase(db);
        var input = new QuerySuggestionsInput(InstrId, Today.AddDays(-30), Today, Action: null, Limit: 10);
        var output = await sut.ExecuteAsync(input, TestContext.Current.CancellationToken);

        output.Items.Count.ShouldBe(2);

        // Newest-first: Today first, then old
        output.Items[0].Date.ShouldBe(Today);
        output.Items[0].ForwardReturnPct.ShouldBeNull();
        output.Items[0].Correct.ShouldBeNull();

        output.Items[1].Date.ShouldBe(oldDate);
        output.Items[1].ForwardReturnPct.ShouldNotBeNull();
        output.Items[1].ForwardReturnPct!.Value.ShouldBe(3.0m);
        output.Items[1].Correct.ShouldNotBeNull();
        output.Items[1].Correct!.Value.ShouldBeTrue(); // Acquire + +3% > 2% threshold
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 2: Limit truncates results
    // ──────────────────────────────────────────────────────────────────────────

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

    // ──────────────────────────────────────────────────────────────────────────
    // Test 3: Action filter narrows results
    // ──────────────────────────────────────────────────────────────────────────

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

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static QuerySuggestionsUseCase BuildUseCase(AppDbContext db)
    {
        var fwdCalculator = new ForwardReturnCalculator(new TestRepo<PriceBar>(db), new TestRepo<Instrument>(db));
        var correctness   = new FixedThresholdCorrectness(2.0m);
        return new QuerySuggestionsUseCase(
            new TestRepo<Suggestion>(db),
            fwdCalculator,
            correctness,
            NullLogger<QuerySuggestionsUseCase>.Instance);
    }

    private static Suggestion MkSuggestion(int instrId, DateOnly date, SuggestionAction action, int conviction)
        => new()
        {
            Id            = 0,
            InstrumentId  = instrId,
            ForDate       = date,
            Action        = action,
            Conviction    = conviction,
            Rationale     = "test rationale",
            CitationsJson = "[]",
            PromptHash    = "TEST",
            CreatedAt     = DateTime.UtcNow,
        };

    private static void SeedInstrument(AppDbContext db, int id, string ticker)
        => db.Instruments.Add(new Instrument
        {
            Id         = id,
            Ticker     = ticker,
            Name       = ticker,
            Currency   = "EUR",
            Exchange   = "X",
            TimezoneId = "Etc/UTC",
            Kind       = InstrumentKind.Held,
            AddedAt    = DateTime.UtcNow,
        });

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
