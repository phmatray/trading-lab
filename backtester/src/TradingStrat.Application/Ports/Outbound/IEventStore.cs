using TradingStrat.Domain.Common;
using TradingStrat.Domain.Exceptions;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Port for persisting and retrieving domain events in an append-only event store.
/// Supports optimistic concurrency control through version tracking.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends new events to an aggregate's event stream.
    /// Uses optimistic concurrency control - fails if expected version doesn't match current version.
    /// </summary>
    /// <param name="streamId">The unique identifier for the event stream (typically aggregate ID).</param>
    /// <param name="events">The domain events to append, in chronological order.</param>
    /// <param name="expectedVersion">The expected current version of the stream (for concurrency control).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ConcurrencyException">Thrown when expectedVersion doesn't match the current stream version.</exception>
    Task AppendEventsAsync(string streamId, IEnumerable<DomainEvent> events, int expectedVersion);

    /// <summary>
    /// Retrieves all events for an aggregate stream, optionally starting from a specific version.
    /// Events are returned in chronological order.
    /// </summary>
    /// <param name="streamId">The unique identifier for the event stream.</param>
    /// <param name="fromVersion">Optional version number to start reading from (inclusive). Defaults to 0 (all events).</param>
    /// <returns>A list of domain events in chronological order.</returns>
    Task<List<DomainEvent>> GetEventsAsync(string streamId, int fromVersion = 0);

    /// <summary>
    /// Gets the current version number for an event stream.
    /// Returns 0 for streams that don't exist yet.
    /// </summary>
    /// <param name="streamId">The unique identifier for the event stream.</param>
    /// <returns>The current version number of the stream.</returns>
    Task<int> GetStreamVersionAsync(string streamId);

    /// <summary>
    /// Checks if an event stream exists for the given stream ID.
    /// </summary>
    /// <param name="streamId">The unique identifier for the event stream.</param>
    /// <returns>True if the stream exists, false otherwise.</returns>
    Task<bool> StreamExistsAsync(string streamId);
}
