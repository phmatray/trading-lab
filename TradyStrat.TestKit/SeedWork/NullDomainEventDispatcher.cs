using TradyStrat.Domain.SeedWork;

namespace TradyStrat.TestKit.SeedWork;

/// <summary>
/// No-op dispatcher for use in tests that do not need to observe domain events.
/// </summary>
public sealed class NullDomainEventDispatcher : IDomainEventDispatcher
{
    public static readonly NullDomainEventDispatcher Instance = new();

    public Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct)
        => Task.CompletedTask;
}
