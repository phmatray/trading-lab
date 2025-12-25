using Shouldly;
using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.Tests.Common;

public class AggregateRootTests
{
    #region Test Helper - Concrete Aggregate for Testing

    private class TestAggregate : AggregateRoot
    {
        public override string AggregateId => "test-aggregate-1";

        protected override void Apply(DomainEvent domainEvent)
        {
            // Test aggregate doesn't need to maintain state from events
            // Just validates that Apply is called during event sourcing
        }

        public void DoSomething()
        {
            RaiseDomainEvent(new TestEvent("Something happened"));
        }

        public void DoMultipleThings()
        {
            RaiseDomainEvent(new TestEvent("First thing"));
            RaiseDomainEvent(new TestEvent("Second thing"));
            RaiseDomainEvent(new TestEvent("Third thing"));
        }
    }

    private record TestEvent(string Message) : DomainEvent;

    #endregion

    [Fact]
    public void AggregateRoot_WhenEventRaised_AddsToEventCollection()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        aggregate.DoSomething();

        // Assert
        var events = aggregate.GetDomainEvents();
        events.Count.ShouldBe(1);
        events[0].ShouldBeOfType<TestEvent>();
        ((TestEvent)events[0]).Message.ShouldBe("Something happened");
    }

    [Fact]
    public void AggregateRoot_WhenMultipleEventsRaised_MaintainsOrder()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        aggregate.DoMultipleThings();

        // Assert
        var events = aggregate.GetDomainEvents();
        events.Count.ShouldBe(3);
        ((TestEvent)events[0]).Message.ShouldBe("First thing");
        ((TestEvent)events[1]).Message.ShouldBe("Second thing");
        ((TestEvent)events[2]).Message.ShouldBe("Third thing");
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoMultipleThings();
        aggregate.GetDomainEvents().Count.ShouldBe(3);

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.GetDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public void GetDomainEvents_ReturnsReadOnlyCollection()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoSomething();

        // Act
        var events = aggregate.GetDomainEvents();

        // Assert
        events.ShouldBeAssignableTo<IReadOnlyList<DomainEvent>>();
    }
}
