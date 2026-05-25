using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Infrastructure.SeedWork;

public sealed class DomainEventDispatcher(IServiceProvider sp, ILogger<DomainEventDispatcher> log)
    : IDomainEventDispatcher
{
    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct)
    {
        _ = log;
        foreach (var evt in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(evt.GetType());
            var handlers = sp.GetServices(handlerType);
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
            foreach (var h in handlers)
            {
                if (h is null) continue;
                var task = (Task?)method.Invoke(h, [evt, ct]) ?? Task.CompletedTask;
                await task;
            }
        }
    }
}
