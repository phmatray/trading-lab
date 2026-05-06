using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.UseCases.Settings;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Fx;             // TestRepo<T>
using TradyStrat.Tests.Specifications; // InMemoryDb
using TradyStrat.Tests.Time;           // FakeClock
using Xunit;

namespace TradyStrat.Tests.UseCases.Settings;

public class UpdateGoalUseCaseTests
{
    [Fact]
    public async Task Inserts_default_goal_when_none_exists_then_updates()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));

        var uc = new UpdateGoalUseCase(new TestRepo<GoalConfig>(db), clock,
            NullLogger<UpdateGoalUseCase>.Instance);

        var goal = await uc.ExecuteAsync(new UpdateGoalInput(
            TargetEur: 500_000m, TargetDate: new(2030,1,1)), ct);

        goal.TargetEur.ShouldBe(500_000m);
        (await db.Goals.SingleAsync(ct)).TargetDate.ShouldBe(new DateOnly(2030,1,1));
    }
}
