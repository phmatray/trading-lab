namespace TradingStrat.Domain.Common;

/// <summary>
/// Abstract base class for aggregate roots with event sourcing support.
/// Provides infrastructure for collecting domain events, version tracking, and event replay.
/// </summary>
public abstract class AggregateRoot : IEventSourcedAggregate
{
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// Unique identifier for the aggregate in the event store.
    /// Derived classes should override to provide the actual ID.
    /// </summary>
    public abstract string AggregateId { get; }

    /// <summary>
    /// Current version of the aggregate, representing the number of events applied.
    /// Incremented with each event applied during LoadFromHistory or command execution.
    /// </summary>
    public int Version { get; protected set; }

    /// <summary>
    /// Raises a domain event and applies it to the aggregate state.
    /// This method is called when executing commands (creating new events).
    /// </summary>
    /// <param name="domainEvent">The domain event to raise and apply.</param>
    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        ApplyEvent(domainEvent, isNew: true);
    }

    /// <summary>
    /// Applies an event to the aggregate state and optionally adds it to uncommitted events.
    /// </summary>
    /// <param name="domainEvent">The domain event to apply.</param>
    /// <param name="isNew">True if this is a new event being raised, false if replaying from history.</param>
    private void ApplyEvent(DomainEvent domainEvent, bool isNew)
    {
        // Invoke the specific Apply method for this event type
        Apply(domainEvent);

        if (isNew)
        {
            _domainEvents.Add(domainEvent);
        }

        Version++;
    }

    /// <summary>
    /// Rebuilds the aggregate state by replaying domain events in chronological order.
    /// Events are applied without being added to the uncommitted events collection.
    /// </summary>
    /// <param name="events">Historical domain events to replay, must be in chronological order.</param>
    public void LoadFromHistory(IEnumerable<DomainEvent> events)
    {
        foreach (DomainEvent @event in events)
        {
            ApplyEvent(@event, isNew: false);
        }
    }

    /// <summary>
    /// Applies a domain event to the aggregate state.
    /// Derived classes must implement this method to handle specific event types.
    /// Uses pattern matching to route events to specific Apply(EventType) methods.
    /// </summary>
    /// <param name="domainEvent">The domain event to apply.</param>
    protected abstract void Apply(DomainEvent domainEvent);

    /// <summary>
    /// Gets all domain events that have been raised by this aggregate since last clear.
    /// Returns a read-only collection to prevent external modification.
    /// </summary>
    /// <returns>A read-only list of uncommitted domain events.</returns>
    public IReadOnlyList<DomainEvent> GetDomainEvents()
        => _domainEvents.AsReadOnly();

    /// <summary>
    /// Clears all uncommitted domain events from the aggregate.
    /// Should be called after events have been persisted to the event store.
    /// </summary>
    public void ClearDomainEvents()
        => _domainEvents.Clear();
}
