using TradingStrat.Domain.Common;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Port for persisting and retrieving aggregate snapshots.
/// Snapshots optimize event sourcing performance by storing aggregate state at specific versions,
/// reducing the number of events that need to be replayed.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    /// Saves a snapshot of an aggregate's current state.
    /// Snapshots should be created periodically (e.g., every 50 events) to improve load performance.
    /// </summary>
    /// <typeparam name="T">The aggregate root type.</typeparam>
    /// <param name="aggregate">The aggregate to snapshot.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveSnapshotAsync<T>(T aggregate) where T : IEventSourcedAggregate;

    /// <summary>
    /// Retrieves the most recent snapshot for an aggregate.
    /// Returns null if no snapshot exists.
    /// </summary>
    /// <typeparam name="T">The aggregate root type.</typeparam>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <returns>
    /// A snapshot containing the aggregate state and version,
    /// or null if no snapshot exists for this aggregate.
    /// </returns>
    Task<AggregateSnapshot<T>?> GetSnapshotAsync<T>(string aggregateId) where T : IEventSourcedAggregate;

    /// <summary>
    /// Deletes all snapshots for an aggregate.
    /// Useful when testing or when aggregate schema changes require rebuild from events.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteSnapshotsAsync(string aggregateId);

    /// <summary>
    /// Checks if a snapshot exists for the given aggregate.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <returns>True if a snapshot exists, false otherwise.</returns>
    Task<bool> SnapshotExistsAsync(string aggregateId);
}

/// <summary>
/// Represents a snapshot of an aggregate's state at a specific version.
/// Used to optimize aggregate loading by avoiding replay of all events.
/// </summary>
/// <typeparam name="T">The aggregate root type.</typeparam>
public sealed class AggregateSnapshot<T> where T : IEventSourcedAggregate
{
    /// <summary>
    /// Gets or sets the aggregate with its state at the snapshot version.
    /// </summary>
    public required T Aggregate { get; init; }

    /// <summary>
    /// Gets or sets the version number of the aggregate when the snapshot was taken.
    /// Events after this version still need to be replayed.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Gets or sets the date and time when the snapshot was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }
}
