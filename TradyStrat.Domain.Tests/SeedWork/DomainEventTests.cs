using Shouldly;
using TradyStrat.Domain.SeedWork;
using Xunit;

namespace TradyStrat.Domain.Tests.SeedWork;

public class DomainEventTests
{
    private sealed record FooHappened(int X, DateTime OccurredAt) : DomainEvent(OccurredAt);

    [Fact]
    public void EventId_is_assigned_a_fresh_guid()
    {
        var e1 = new FooHappened(1, DateTime.UtcNow);
        var e2 = new FooHappened(1, DateTime.UtcNow);
        e1.EventId.ShouldNotBe(Guid.Empty);
        e1.EventId.ShouldNotBe(e2.EventId);
    }

    [Fact]
    public void OccurredAt_round_trips()
    {
        var at = new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);
        var e = new FooHappened(1, at);
        e.OccurredAt.ShouldBe(at);
    }

    [Fact]
    public void OccurredAt_is_accessible_through_IDomainEvent_interface()
    {
        var at = new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);
        IDomainEvent e = new FooHappened(99, at);
        e.OccurredAt.ShouldBe(at);
    }
}
