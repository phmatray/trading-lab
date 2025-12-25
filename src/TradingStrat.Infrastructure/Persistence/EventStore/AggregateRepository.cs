using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;

namespace TradingStrat.Infrastructure.Persistence.EventStore;

/// <summary>
/// Repository for loading and saving event-sourced aggregates.
/// Implements snapshot optimization: loads from snapshot (if exists) + events since snapshot.
/// Automatically creates snapshots every 50 events for performance.
/// </summary>
/// <typeparam name="T">The aggregate root type, must implement IEventSourcedAggregate.</typeparam>
public class AggregateRepository<T> : IAggregateRepository<T> where T : IEventSourcedAggregate, new()
{
    private readonly IEventStore _eventStore;
    private readonly ISnapshotStore _snapshotStore;
    private const int SnapshotInterval = 50; // Create snapshot every 50 events

    public AggregateRepository(IEventStore eventStore, ISnapshotStore snapshotStore)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
    }

    /// <inheritdoc />
    public async Task<T?> LoadAsync(string aggregateId)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate ID cannot be null or whitespace", nameof(aggregateId));
        }

        // Try to load from snapshot first (performance optimization)
        AggregateSnapshot<T>? snapshot = await _snapshotStore.GetSnapshotAsync<T>(aggregateId);

        int fromVersion = 0;
        T aggregate;

        if (snapshot != null)
        {
            // Start from snapshot
            aggregate = snapshot.Aggregate;
            fromVersion = snapshot.Version;
        }
        else
        {
            // Create new aggregate instance
            aggregate = new T();
        }

        // Load events since snapshot (or from beginning if no snapshot)
        List<DomainEvent> events = await _eventStore.GetEventsAsync(aggregateId, fromVersion);

        if (!events.Any() && snapshot == null)
        {
            // No events and no snapshot = aggregate doesn't exist
            return default;
        }

        // Replay events to rebuild aggregate state
        if (events.Any())
        {
            aggregate.LoadFromHistory(events);
        }

        // Clear any uncommitted events from reconstruction
        aggregate.ClearDomainEvents();

        return aggregate;
    }

    /// <inheritdoc />
    public async Task SaveAsync(T aggregate)
    {
        if (aggregate == null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        var uncommittedEvents = aggregate.GetDomainEvents();
        if (!uncommittedEvents.Any())
        {
            // No changes to persist
            return;
        }

        string aggregateId = aggregate.AggregateId;
        int expectedVersion = aggregate.Version - uncommittedEvents.Count;

        // Append events to event store (will throw ConcurrencyException if version mismatch)
        await _eventStore.AppendEventsAsync(aggregateId, uncommittedEvents, expectedVersion);

        // Clear uncommitted events after successful persistence
        aggregate.ClearDomainEvents();

        // Create snapshot if we've reached the snapshot interval
        if (aggregate.Version > 0 && aggregate.Version % SnapshotInterval == 0)
        {
            await _snapshotStore.SaveSnapshotAsync(aggregate);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string aggregateId)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate ID cannot be null or whitespace", nameof(aggregateId));
        }

        // Check if event stream exists
        return await _eventStore.StreamExistsAsync(aggregateId);
    }

    /// <inheritdoc />
    public async Task<int> GetVersionAsync(string aggregateId)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate ID cannot be null or whitespace", nameof(aggregateId));
        }

        return await _eventStore.GetStreamVersionAsync(aggregateId);
    }
}
