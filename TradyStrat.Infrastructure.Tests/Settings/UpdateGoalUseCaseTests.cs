using TradyStrat.Infrastructure.Settings.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Domain;
using TradyStrat.TestKit;             // TestRepo<T>
using TradyStrat.TestKit.Specifications; // InMemoryDb
using TradyStrat.TestKit.Time;           // FakeClock
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings.UseCases;

public class UpdateGoalUseCaseTests
{
    [Fact]
    public async Task Inserts_default_goal_when_none_exists_then_updates()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));

        var uc = new UpdateGoalUseCase(new TestRepo<GoalConfig>(db), db, clock,
            NullLogger<UpdateGoalUseCase>.Instance);

        var goal = await uc.ExecuteAsync(new UpdateGoalInput(
            TargetEur: 500_000m, TargetDate: new(2030,1,1)), ct);

        goal.TargetEur.ShouldBe(500_000m);
        (await db.Goals.SingleAsync(ct)).TargetDate.ShouldBe(new DateOnly(2030,1,1));
    }

    [Fact]
    public async Task Updates_existing_goal_after_loading_via_repo()
    {
        // Regression: prior to detaching the tracked existing entity,
        // a second save threw "another instance with the same key value".
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        var repo = new TestRepo<GoalConfig>(db);
        var uc = new UpdateGoalUseCase(repo, db, clock, NullLogger<UpdateGoalUseCase>.Instance);

        await uc.ExecuteAsync(new UpdateGoalInput(1_000_000m, new(2030,1,1)), ct);
        // Force a tracked read between saves to mimic the page+circuit reading the goal.
        _ = await repo.GetByIdAsync(1, ct);

        var second = await uc.ExecuteAsync(new UpdateGoalInput(2_500_000m, new(2031,6,30)), ct);

        second.TargetEur.ShouldBe(2_500_000m);
        var row = await db.Goals.AsNoTracking().SingleAsync(ct);
        row.TargetEur.ShouldBe(2_500_000m);
        row.TargetDate.ShouldBe(new DateOnly(2031,6,30));
    }
}
