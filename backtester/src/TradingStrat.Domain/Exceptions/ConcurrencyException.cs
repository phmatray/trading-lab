namespace TradingStrat.Domain.Exceptions;

/// <summary>
/// Exception thrown when an optimistic concurrency conflict is detected during event persistence.
/// This occurs when two processes attempt to modify the same aggregate simultaneously.
/// </summary>
public class ConcurrencyException : DomainException
{
    /// <summary>
    /// Gets the stream ID where the concurrency conflict occurred.
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Gets the expected version number.
    /// </summary>
    public int ExpectedVersion { get; }

    /// <summary>
    /// Gets the actual current version number in the event store.
    /// </summary>
    public int ActualVersion { get; }

    /// <summary>
    /// Initializes a new instance of the ConcurrencyException class.
    /// </summary>
    /// <param name="streamId">The stream ID where the conflict occurred.</param>
    /// <param name="expectedVersion">The expected version number.</param>
    /// <param name="actualVersion">The actual current version number.</param>
    public ConcurrencyException(string streamId, int expectedVersion, int actualVersion)
        : base($"Concurrency conflict for stream '{streamId}': expected version {expectedVersion}, but current version is {actualVersion}")
    {
        StreamId = streamId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}
