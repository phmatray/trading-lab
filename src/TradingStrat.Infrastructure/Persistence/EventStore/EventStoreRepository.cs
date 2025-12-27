using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Exceptions;
using TradingStrat.Infrastructure.Persistence.EfCore;

namespace TradingStrat.Infrastructure.Persistence.EventStore;

/// <summary>
/// EF Core implementation of the event store.
/// Persists domain events to a relational database with optimistic concurrency control.
/// </summary>
public class EventStoreRepository : IEventStore
{
    private readonly TradingContext _context;
    private readonly JsonSerializerOptions _jsonOptions;

    public EventStoreRepository(TradingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        // Configure JSON serialization for domain events
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async Task AppendEventsAsync(string streamId, IEnumerable<DomainEvent> events, int expectedVersion)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or whitespace", nameof(streamId));
        }

        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        var eventsList = events.ToList();
        if (!eventsList.Any())
        {
            return; // Nothing to append
        }

        // Optimistic concurrency check
        int currentVersion = await GetStreamVersionAsync(streamId);

        if (currentVersion != expectedVersion)
        {
            throw new ConcurrencyException(streamId, expectedVersion, currentVersion);
        }

        // Append events with sequential version numbers
        int nextVersion = currentVersion;
        foreach (DomainEvent domainEvent in eventsList)
        {
            nextVersion++;

            var eventRecord = new EventRecord
            {
                StreamId = streamId,
                Version = nextVersion,
                EventType = domainEvent.GetType().AssemblyQualifiedName
                    ?? throw new InvalidOperationException($"Cannot get type name for event {domainEvent.GetType()}"),
                EventData = JsonSerializer.Serialize((object)domainEvent, _jsonOptions),
                Timestamp = domainEvent.OccurredAt
            };

            await _context.Events.AddAsync(eventRecord);
        }

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<DomainEvent>> GetEventsAsync(string streamId, int fromVersion = 0)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or whitespace", nameof(streamId));
        }

        List<EventRecord> eventRecords = await _context.Events
            .Where(e => e.StreamId == streamId && e.Version > fromVersion)
            .OrderBy(e => e.Version)
            .ToListAsync();

        var domainEvents = new List<DomainEvent>();

        foreach (EventRecord record in eventRecords)
        {
            // Deserialize event using the stored type information
            Type? eventType = Type.GetType(record.EventType);
            if (eventType == null)
            {
                throw new InvalidOperationException(
                    $"Cannot find event type '{record.EventType}' for stream '{streamId}' version {record.Version}");
            }

            object? deserializedEvent = JsonSerializer.Deserialize(record.EventData, eventType, _jsonOptions);
            if (deserializedEvent is not DomainEvent domainEvent)
            {
                throw new InvalidOperationException(
                    $"Deserialized event is not a DomainEvent: {record.EventType}");
            }

            domainEvents.Add(domainEvent);
        }

        return domainEvents;
    }

    /// <inheritdoc />
    public async Task<int> GetStreamVersionAsync(string streamId)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or whitespace", nameof(streamId));
        }

        // Get the maximum version number for this stream (0 if no events exist)
        int? maxVersion = await _context.Events
            .Where(e => e.StreamId == streamId)
            .MaxAsync(e => (int?)e.Version);

        return maxVersion ?? 0;
    }

    /// <inheritdoc />
    public async Task<bool> StreamExistsAsync(string streamId)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            throw new ArgumentException("Stream ID cannot be null or whitespace", nameof(streamId));
        }

        return await _context.Events
            .AnyAsync(e => e.StreamId == streamId);
    }
}
