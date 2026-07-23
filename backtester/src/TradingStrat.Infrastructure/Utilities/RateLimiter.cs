using System.Collections.Concurrent;

namespace TradingStrat.Infrastructure.Utilities;

/// <summary>
/// Rate limiter using sliding window algorithm to enforce API call limits.
/// Thread-safe implementation for concurrent API requests.
/// </summary>
public class RateLimiter
{
    private readonly int _maxCallsPerWindow;
    private readonly TimeSpan _windowDuration;
    private readonly ConcurrentQueue<DateTime> _callTimestamps = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Creates a new rate limiter with specified limits.
    /// </summary>
    /// <param name="maxCallsPerWindow">Maximum number of calls allowed within the time window</param>
    /// <param name="windowDuration">Duration of the sliding window</param>
    public RateLimiter(int maxCallsPerWindow, TimeSpan windowDuration)
    {
        if (maxCallsPerWindow <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCallsPerWindow), "Must be greater than 0");
        }

        if (windowDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(windowDuration), "Must be greater than zero");
        }

        _maxCallsPerWindow = maxCallsPerWindow;
        _windowDuration = windowDuration;
    }

    /// <summary>
    /// Waits until a slot becomes available within the rate limit, then proceeds.
    /// Uses sliding window algorithm to track API calls.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to abort waiting</param>
    public async Task WaitForSlotAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Remove expired timestamps (outside the sliding window)
            DateTime windowStart = DateTime.UtcNow - _windowDuration;
            while (_callTimestamps.TryPeek(out DateTime oldestTimestamp) && oldestTimestamp < windowStart)
            {
                _callTimestamps.TryDequeue(out _);
            }

            // If we've hit the rate limit, wait until oldest call expires
            while (_callTimestamps.Count >= _maxCallsPerWindow)
            {
                if (_callTimestamps.TryPeek(out DateTime oldestTimestamp))
                {
                    TimeSpan waitTime = (oldestTimestamp + _windowDuration) - DateTime.UtcNow;
                    if (waitTime > TimeSpan.Zero)
                    {
                        // Add small buffer to avoid edge cases
                        await Task.Delay(waitTime + TimeSpan.FromMilliseconds(100), cancellationToken);
                    }

                    // Remove expired timestamp
                    _callTimestamps.TryDequeue(out _);
                }
                else
                {
                    break;
                }
            }

            // Record this API call
            _callTimestamps.Enqueue(DateTime.UtcNow);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets the current number of API calls in the sliding window.
    /// </summary>
    public int GetCurrentCallCount()
    {
        DateTime windowStart = DateTime.UtcNow - _windowDuration;
        return _callTimestamps.Count(ts => ts >= windowStart);
    }

    /// <summary>
    /// Resets the rate limiter by clearing all tracked timestamps.
    /// </summary>
    public void Reset()
    {
        while (_callTimestamps.TryDequeue(out _))
        {
            // Clear queue
        }
    }
}
