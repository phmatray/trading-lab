using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Infrastructure.SeedWork;

public sealed class DomainEventDispatcher(IServiceProvider sp, ILogger<DomainEventDispatcher> log)
    : IDomainEventDispatcher
{
    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct)
    {
        if (events.Count == 0) return;
        DispatcherLog.Dispatching(log, events.Count);

        foreach (var evt in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(evt.GetType());
            var handlers = sp.GetServices(handlerType);
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
            foreach (var h in handlers)
            {
                if (h is null) continue;
                try
                {
                    var task = (Task?)method.Invoke(h, [evt, ct]) ?? Task.CompletedTask;
                    await task;
                }
                catch (TargetInvocationException tie) when (tie.InnerException is not null)
                {
                    // Reflection wraps synchronous throws inside the handler.
                    // Log + rethrow the actual handler exception with its
                    // original stack trace preserved.
                    var actual = tie.InnerException;
                    DispatcherLog.HandlerFailed(log, actual, evt.GetType().Name, h.GetType().Name, evt.EventId);
                    ExceptionDispatchInfo.Capture(actual).Throw();
                }
                catch (Exception ex)
                {
                    DispatcherLog.HandlerFailed(log, ex, evt.GetType().Name, h.GetType().Name, evt.EventId);
                    throw;
                }
            }
        }
    }
}

internal static partial class DispatcherLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Dispatching {EventCount} domain event(s)")]
    public static partial void Dispatching(ILogger logger, int eventCount);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Domain event handler {HandlerType} failed for event {EventType} ({EventId})")]
    public static partial void HandlerFailed(
        ILogger logger, Exception ex, string eventType, string handlerType, Guid eventId);
}
