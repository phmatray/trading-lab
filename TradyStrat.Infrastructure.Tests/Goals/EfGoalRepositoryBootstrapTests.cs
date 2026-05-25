using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Domain.Goals.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Goals;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Goals;

public class EfGoalRepositoryBootstrapTests
{
    private sealed class GoalCreatedSpy : IDomainEventHandler<GoalCreated>
    {
        public List<GoalCreated> Received { get; } = new();
        public Task HandleAsync(GoalCreated evt, CancellationToken ct)
        { Received.Add(evt); return Task.CompletedTask; }
    }

    private sealed class DispatchingDispatcher(GoalCreatedSpy spy) : IDomainEventDispatcher
    {
        public async Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct)
        {
            foreach (var e in events)
                if (e is GoalCreated gc) await spy.HandleAsync(gc, ct);
        }
    }

    [Fact]
    public async Task First_GetAsync_persists_default_goal_and_dispatches_GoalCreated()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));
        var spy = new GoalCreatedSpy();
        var repo = new EfGoalRepository(db, clock, new DispatchingDispatcher(spy));

        var goal = await repo.GetAsync(ct);

        goal.Id.ShouldBe(GoalId.Singleton);
        goal.Target.Amount.ShouldBe(1_000_000m);
        spy.Received.ShouldHaveSingleItem()
            .GoalId.ShouldBe(GoalId.Singleton);
        spy.Received[0].Target.ShouldBe(goal.Target);
        spy.Received[0].OccurredAt.ShouldBe(clock.UtcNow());
        goal.DomainEvents.ShouldBeEmpty();   // drained
        (await db.Goals.SingleAsync(ct)).Id.ShouldBe(GoalId.Singleton);
    }

    [Fact]
    public async Task Second_GetAsync_returns_existing_goal_without_dispatching()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));
        var spy = new GoalCreatedSpy();
        var repo = new EfGoalRepository(db, clock, new DispatchingDispatcher(spy));

        _ = await repo.GetAsync(ct);   // bootstrap
        spy.Received.Clear();          // reset
        _ = await repo.GetAsync(ct);   // second call

        spy.Received.ShouldBeEmpty();
    }
}
