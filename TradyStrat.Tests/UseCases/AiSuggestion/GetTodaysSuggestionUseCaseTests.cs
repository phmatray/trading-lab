using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Abstractions;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;
using Xunit;

namespace TradyStrat.Tests.UseCases.AiSuggestion;

public class GetTodaysSuggestionUseCaseTests
{
    [Fact]
    public async Task Returns_existing_row_when_today_already_present()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        db.Suggestions.Add(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "cached", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        // Stubs that should NOT be invoked when a cached row exists for today.
        var snap = new StubSnapshotBuilder(new AiSnapshot(
            new(2026,5,6), GoalConfig.Default(DateTime.UtcNow),
            new(0,0,0,0,0,0), [], [], 1.08m, "h2"));
        var ai = new StubAiClient(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Acquire,
            Conviction = 5, Rationale = "fresh", CitationsJson = "[]",
            PromptHash = "h2", CreatedAt = DateTime.UtcNow });

        var uc = new GetTodaysSuggestionUseCase(
            new TestRepo<Suggestion>(db), snap, ai,
            new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc)),
            NullLogger<GetTodaysSuggestionUseCase>.Instance);

        var s = await uc.ExecuteAsync(Unit.Value, ct);

        s.Rationale.ShouldBe("cached");
        (await db.Suggestions.CountAsync(ct)).ShouldBe(1);
    }
}
