using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Infrastructure.Persistence.EfCore;

namespace TradingStrat.Infrastructure.Persistence.EventStore;

/// <summary>
/// EF Core implementation of the snapshot store.
/// Persists aggregate snapshots to optimize event sourcing performance by reducing event replay.
/// </summary>
public class SnapshotStore : ISnapshotStore
{
    private readonly TradingContext _context;
    private readonly JsonSerializerOptions _jsonOptions;

    public SnapshotStore(TradingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        // Configure JSON serialization for snapshots
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Include all fields to properly serialize aggregate state
            IncludeFields = true
        };
    }

    /// <inheritdoc />
    public async Task SaveSnapshotAsync<T>(T aggregate) where T : IEventSourcedAggregate
    {
        if (aggregate is null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        string aggregateId = aggregate.AggregateId;
        string aggregateType = typeof(T).AssemblyQualifiedName
            ?? throw new InvalidOperationException($"Cannot get type name for aggregate {typeof(T)}");

        // Serialize the aggregate state
        string snapshotData = JsonSerializer.Serialize(aggregate, typeof(T), _jsonOptions);

        // Check if snapshot already exists
        SnapshotRecord? existingSnapshot = await _context.Snapshots
            .FirstOrDefaultAsync(s => s.AggregateId == aggregateId);

        if (existingSnapshot is not null)
        {
            // Update existing snapshot
            _context.Snapshots.Remove(existingSnapshot);
        }

        // Create new snapshot record
        var snapshotRecord = new SnapshotRecord
        {
            AggregateId = aggregateId,
            AggregateType = aggregateType,
            Version = aggregate.Version,
            SnapshotData = snapshotData,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Snapshots.AddAsync(snapshotRecord);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<AggregateSnapshot<T>?> GetSnapshotAsync<T>(string aggregateId) where T : IEventSourcedAggregate
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate ID cannot be null or whitespace", nameof(aggregateId));
        }

        SnapshotRecord? snapshotRecord = await _context.Snapshots
            .FirstOrDefaultAsync(s => s.AggregateId == aggregateId);

        if (snapshotRecord is null)
        {
            return null;
        }

        // Deserialize the aggregate
        Type? aggregateType = Type.GetType(snapshotRecord.AggregateType);
        if (aggregateType is null)
        {
            throw new InvalidOperationException(
                $"Cannot find aggregate type '{snapshotRecord.AggregateType}' for snapshot '{aggregateId}'");
        }

        object? deserializedAggregate = JsonSerializer.Deserialize(
            snapshotRecord.SnapshotData,
            aggregateType,
            _jsonOptions);

        if (deserializedAggregate is not T aggregate)
        {
            throw new InvalidOperationException(
                $"Deserialized aggregate is not of type {typeof(T).Name}: {snapshotRecord.AggregateType}");
        }

        return new AggregateSnapshot<T>
        {
            Aggregate = aggregate,
            Version = snapshotRecord.Version,
            CreatedAt = snapshotRecord.CreatedAt
        };
    }

    /// <inheritdoc />
    public async Task DeleteSnapshotsAsync(string aggregateId)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate ID cannot be null or whitespace", nameof(aggregateId));
        }

        List<SnapshotRecord> snapshots = await _context.Snapshots
            .Where(s => s.AggregateId == aggregateId)
            .ToListAsync();

        if (snapshots.Any())
        {
            _context.Snapshots.RemoveRange(snapshots);
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<bool> SnapshotExistsAsync(string aggregateId)
    {
        if (string.IsNullOrWhiteSpace(aggregateId))
        {
            throw new ArgumentException("Aggregate ID cannot be null or whitespace", nameof(aggregateId));
        }

        return await _context.Snapshots
            .AnyAsync(s => s.AggregateId == aggregateId);
    }
}
