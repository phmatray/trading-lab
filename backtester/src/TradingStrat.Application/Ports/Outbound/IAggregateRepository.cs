using TradingStrat.Domain.Common;
using TradingStrat.Domain.Exceptions;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Port for loading and saving event-sourced aggregates.
/// Rebuilds aggregates from event streams and optionally uses snapshots for performance.
/// </summary>
/// <typeparam name="T">The aggregate root type, must implement IEventSourcedAggregate.</typeparam>
public interface IAggregateRepository<T> where T : IEventSourcedAggregate
{
    /// <summary>
    /// Loads an aggregate from the event store by its unique identifier.
    /// Rebuilds the aggregate state by replaying all events in chronological order.
    /// Optionally uses snapshots to improve performance for aggregates with many events.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <returns>
    /// The reconstructed aggregate with its full event history applied,
    /// or null if no events exist for the given ID.
    /// </returns>
    Task<T?> LoadAsync(string aggregateId);

    /// <summary>
    /// Saves an aggregate by appending its uncommitted domain events to the event store.
    /// After successful persistence, clears the uncommitted events from the aggregate.
    /// Optionally creates a snapshot if the aggregate has accumulated enough events.
    /// </summary>
    /// <param name="aggregate">The aggregate to save.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ConcurrencyException">
    /// Thrown when another process has modified the aggregate since it was loaded.
    /// </exception>
    Task SaveAsync(T aggregate);

    /// <summary>
    /// Checks if an aggregate exists in the event store.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <returns>True if events exist for the aggregate, false otherwise.</returns>
    Task<bool> ExistsAsync(string aggregateId);

    /// <summary>
    /// Gets the current version number of an aggregate without loading it.
    /// Useful for optimistic concurrency checks.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <returns>The current version number, or 0 if the aggregate doesn't exist.</returns>
    Task<int> GetVersionAsync(string aggregateId);
}
