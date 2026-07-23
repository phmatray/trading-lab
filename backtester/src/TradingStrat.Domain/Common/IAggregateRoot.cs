namespace TradingStrat.Domain.Common;

/// <summary>
/// Marker interface for aggregate roots in the domain.
/// Aggregates are clusters of domain objects that can be treated as a single unit.
/// The aggregate root is the only member of the aggregate that external objects are allowed to hold references to.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Gets all domain events that have been raised by this aggregate since the last clear.
    /// </summary>
    /// <returns>A read-only collection of domain events.</returns>
    IReadOnlyList<DomainEvent> GetDomainEvents();

    /// <summary>
    /// Clears all domain events from the aggregate.
    /// Typically called after events have been persisted or published.
    /// </summary>
    void ClearDomainEvents();
}
