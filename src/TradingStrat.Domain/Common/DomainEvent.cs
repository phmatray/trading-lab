namespace TradingStrat.Domain.Common;

/// <summary>
/// Base class for all domain events.
/// Domain events represent something that happened in the domain that domain experts care about.
/// They are immutable and capture the state at the time the event occurred.
/// </summary>
public abstract record DomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The UTC timestamp when this event occurred.
    /// </summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
