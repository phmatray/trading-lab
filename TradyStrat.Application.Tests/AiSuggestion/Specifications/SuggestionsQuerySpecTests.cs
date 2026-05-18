using Shouldly;
using TradyStrat.Application.AiSuggestion.Specifications;
using TradyStrat.Domain;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.AiSuggestion.Specifications;

public class SuggestionsQuerySpecTests
{
    private static readonly DateOnly From = new(2026, 1, 1);
    private static readonly DateOnly To   = new(2026, 1, 31);
    private const int InstrId  = 1;
    private const int OtherId  = 2;

    [Fact]
    public async Task Returns_suggestions_newest_first()
    {
        await using var db = InMemoryDb.Create();

        MkSuggestion(db, InstrId, new DateOnly(2026, 1, 5),  SuggestionAction.Hold);
        MkSuggestion(db, InstrId, new DateOnly(2026, 1, 20), SuggestionAction.Acquire);
        MkSuggestion(db, InstrId, new DateOnly(2026, 1, 10), SuggestionAction.Trim);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo    = new TestRepo<Suggestion>(db);
        var spec    = new SuggestionsQuerySpec(InstrId, From, To, action: null, limit: 10);
        var results = await repo.ListAsync(spec, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(3);
        results.Select(s => s.ForDate).ShouldBeInOrder(SortDirection.Descending);
    }

    [Fact]
    public async Task Filters_by_action()
    {
        await using var db = InMemoryDb.Create();

        MkSuggestion(db, InstrId, new DateOnly(2026, 1, 5),  SuggestionAction.Hold);
        MkSuggestion(db, InstrId, new DateOnly(2026, 1, 10), SuggestionAction.Acquire);
        MkSuggestion(db, InstrId, new DateOnly(2026, 1, 15), SuggestionAction.Acquire);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo    = new TestRepo<Suggestion>(db);
        var spec    = new SuggestionsQuerySpec(InstrId, From, To, action: SuggestionAction.Acquire, limit: 10);
        var results = await repo.ListAsync(spec, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(2);
        results.ShouldAllBe(s => s.Action == SuggestionAction.Acquire);
    }

    [Fact]
    public async Task Limit_is_honored()
    {
        await using var db = InMemoryDb.Create();

        for (int i = 1; i <= 5; i++)
            MkSuggestion(db, InstrId, new DateOnly(2026, 1, i), SuggestionAction.Hold);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repo    = new TestRepo<Suggestion>(db);
        var spec    = new SuggestionsQuerySpec(InstrId, From, To, action: null, limit: 3);
        var results = await repo.ListAsync(spec, TestContext.Current.CancellationToken);

        results.Count.ShouldBe(3);
    }

    private static void MkSuggestion(TradyStrat.Infrastructure.Data.AppDbContext db, int instrId, DateOnly date, SuggestionAction action)
        => db.Suggestions.Add(new Suggestion
        {
            Id           = 0,
            InstrumentId = instrId,
            ForDate      = date,
            Action       = action,
            Conviction   = 7,
            Rationale    = "test",
            CitationsJson = "[]",
            PromptHash   = "TEST",
            CreatedAt    = DateTime.UtcNow,
        });
}
