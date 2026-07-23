namespace TradingStrat.Domain.Common;

/// <summary>
/// Marker interface for aggregates that support event sourcing.
/// Event-sourced aggregates can be rebuilt from their domain event history.
/// </summary>
public interface IEventSourcedAggregate : IAggregateRoot
{
    /// <summary>
    /// Unique identifier for the aggregate stream in the event store.
    /// </summary>
    string AggregateId { get; }

    /// <summary>
    /// Current version of the aggregate, representing the number of events applied.
    /// Used for optimistic concurrency control.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Rebuilds the aggregate state by replaying domain events in chronological order.
    /// Events are applied without being added to the uncommitted events collection.
    /// </summary>
    /// <param name="events">Historical domain events to replay, must be in chronological order.</param>
    void LoadFromHistory(IEnumerable<DomainEvent> events);
}
