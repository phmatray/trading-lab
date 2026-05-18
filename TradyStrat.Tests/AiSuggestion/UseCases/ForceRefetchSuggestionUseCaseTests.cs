using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Domain;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Common.Time;
using Xunit;

namespace TradyStrat.Tests.AiSuggestion.UseCases;

public class ForceRefetchSuggestionUseCaseTests
{
    [Fact]
    public async Task Removes_existing_then_persists_new()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        db.Instruments.Add(new Instrument {
            Id = 0, Ticker = "CON3.L", Name = "x", Currency = "USD",
            Exchange = "LSE", TimezoneId = "Europe/London",
            Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);

        var focusId = (await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct)).Id;

        db.Suggestions.Add(new Suggestion {
            Id = 0, InstrumentId = focusId, ForDate = new(2026,5,6),
            Action = SuggestionAction.Hold, Conviction = 3, Rationale = "old",
            CitationsJson = "[]", PromptHash = "h", CreatedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        await db.SaveChangesAsync(ct);

        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        var snap = new StubSnapshotFactory(new AiSnapshot(
            new(2026,5,6), focusId, GoalConfig.Default(DateTime.UtcNow),
            new([],0,0,0,0,0,0,0), [], [], 1.08m, [], "h2"));
        var ai = new StubAiClient(new Suggestion {
            Id = 0, InstrumentId = focusId, ForDate = new(2026,5,6),
            Action = SuggestionAction.Acquire, Conviction = 5, Rationale = "fresh",
            CitationsJson = "[]", PromptHash = "h2", CreatedAt = DateTime.UtcNow });

        var uc = new ForceRefetchSuggestionUseCase(
            new TestRepo<Suggestion>(db), snap, ai, clock,
            new TestRepo<Instrument>(db),
            NullLogger<ForceRefetchSuggestionUseCase>.Instance);

        var s = await uc.ExecuteAsync(new ForceRefetchSuggestionInput(focusId), ct);

        s.Rationale.ShouldBe("fresh");
        (await db.Suggestions.CountAsync(ct)).ShouldBe(1);
        (await db.Suggestions.SingleAsync(ct)).Rationale.ShouldBe("fresh");
    }
}
