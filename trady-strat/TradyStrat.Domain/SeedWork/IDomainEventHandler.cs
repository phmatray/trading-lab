#pragma warning disable CA1711 // EventHandler suffix is intentional for DDD handler contracts
namespace TradyStrat.Domain.SeedWork;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent evt, CancellationToken ct);
}
#pragma warning restore CA1711
