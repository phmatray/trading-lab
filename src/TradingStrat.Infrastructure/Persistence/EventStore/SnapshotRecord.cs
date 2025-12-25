namespace TradingStrat.Infrastructure.Persistence.EventStore;

/// <summary>
/// Entity Framework model for storing aggregate snapshots.
/// Snapshots optimize event sourcing by storing aggregate state at specific versions,
/// reducing the number of events that need to be replayed during aggregate loading.
/// </summary>
public class SnapshotRecord
{
    /// <summary>
    /// The unique identifier for the aggregate.
    /// Primary key for the snapshots table.
    /// </summary>
    public required string AggregateId { get; init; }

    /// <summary>
    /// The fully-qualified type name of the aggregate.
    /// Used for deserialization to the correct aggregate type.
    /// Example: "TradingStrat.Domain.Entities.Portfolio"
    /// </summary>
    public required string AggregateType { get; init; }

    /// <summary>
    /// The version of the aggregate when the snapshot was taken.
    /// Events after this version still need to be replayed.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// The serialized aggregate state in JSON format.
    /// Contains the complete aggregate state at the snapshot version.
    /// </summary>
    public required string SnapshotData { get; init; }

    /// <summary>
    /// The UTC timestamp when the snapshot was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }
}
