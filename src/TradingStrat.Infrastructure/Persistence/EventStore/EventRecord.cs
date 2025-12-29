using System.ComponentModel.DataAnnotations;

namespace TradingStrat.Infrastructure.Persistence.EventStore;

/// <summary>
/// Entity Framework model for storing domain events in the event store.
/// Each record represents a single domain event in an aggregate's event stream.
/// </summary>
public class EventRecord
{
    /// <summary>
    /// The unique identifier for the event stream (typically the aggregate ID).
    /// Combined with Version forms the composite primary key.
    /// </summary>
    public required string StreamId { get; init; }

    /// <summary>
    /// The sequential version number of this event within the stream.
    /// Starts at 1 and increments for each new event.
    /// Used for optimistic concurrency control and event ordering.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// The fully-qualified type name of the domain event.
    /// Used for deserialization to the correct event type.
    /// Example: "TradingStrat.Domain.Events.PortfolioCreatedEvent"
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// The serialized domain event data in JSON format.
    /// Contains all event properties needed for aggregate reconstruction.
    /// Maximum size: 1MB to prevent excessive memory usage.
    /// </summary>
    [MaxLength(1_000_000)]
    public required string EventData { get; init; }

    /// <summary>
    /// The UTC timestamp when the event occurred.
    /// Captured from the domain event's OccurredAt property.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
