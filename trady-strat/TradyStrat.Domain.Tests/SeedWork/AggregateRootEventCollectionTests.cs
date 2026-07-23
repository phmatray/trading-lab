using Shouldly;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.SeedWork;
using Xunit;

namespace TradyStrat.Domain.Tests.SeedWork;

public class AggregateRootEventCollectionTests
{
    private sealed record FooHappened(DateTime OccurredAt) : DomainEvent(OccurredAt);

    private sealed class TestAr : AggregateRoot<InstrumentId>
    {
        public TestAr(InstrumentId id) : base(id) { }
        public void Do(DateTime at) => Raise(new FooHappened(at));
    }

    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Raise_appends_to_DomainEvents()
    {
        var ar = new TestAr(new InstrumentId(1));
        ar.Do(_now);
        ar.DomainEvents.Count.ShouldBe(1);
        ar.DomainEvents[0].ShouldBeOfType<FooHappened>();
    }

    [Fact]
    public void DequeueDomainEvents_returns_snapshot_and_clears()
    {
        var ar = new TestAr(new InstrumentId(1));
        ar.Do(_now);
        ar.Do(_now);

        var drained = ar.DequeueDomainEvents();
        drained.Count.ShouldBe(2);
        ar.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Dequeue_twice_yields_empty_on_second_call()
    {
        var ar = new TestAr(new InstrumentId(1));
        ar.Do(_now);
        ar.DequeueDomainEvents();
        ar.DequeueDomainEvents().ShouldBeEmpty();
    }
}
