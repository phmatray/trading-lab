using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Infrastructure.SeedWork;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.SeedWork;

public class DomainEventDispatcherTests
{
    private sealed record EventA(DateTime OccurredAt) : DomainEvent(OccurredAt);
    private sealed record EventB(DateTime OccurredAt) : DomainEvent(OccurredAt);

    private sealed class HandlerA : IDomainEventHandler<EventA>
    {
        public List<EventA> Received { get; } = new();
        public Task HandleAsync(EventA evt, CancellationToken ct)
        { Received.Add(evt); return Task.CompletedTask; }
    }

    private sealed class HandlerAFails : IDomainEventHandler<EventA>
    {
        public Task HandleAsync(EventA evt, CancellationToken ct)
            => throw new InvalidOperationException("boom");
    }

    private sealed class CatchAll : IDomainEventHandler<IDomainEvent>
    {
        public List<IDomainEvent> Received { get; } = new();
        public Task HandleAsync(IDomainEvent evt, CancellationToken ct)
        { Received.Add(evt); return Task.CompletedTask; }
    }

    private static ServiceProvider BuildSp(Action<IServiceCollection> register)
    {
        var sc = new ServiceCollection();
        register(sc);
        return sc.BuildServiceProvider();
    }

    private static DomainEventDispatcher Make(IServiceProvider sp)
        => new(sp, NullLogger<DomainEventDispatcher>.Instance);

    [Fact]
    public async Task Dispatches_to_concrete_handler()
    {
        var handler = new HandlerA();
        var sp = BuildSp(sc => sc.AddSingleton<IDomainEventHandler<EventA>>(handler));
        var d = Make(sp);

        await d.DispatchAsync([new EventA(DateTime.UtcNow)], TestContext.Current.CancellationToken);

        handler.Received.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Skips_unregistered_event_type()
    {
        var sp = BuildSp(_ => { });
        var d = Make(sp);

        await Should.NotThrowAsync(() =>
            d.DispatchAsync([new EventA(DateTime.UtcNow)], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Bubbles_handler_exceptions()
    {
        var sp = BuildSp(sc => sc.AddSingleton<IDomainEventHandler<EventA>, HandlerAFails>());
        var d = Make(sp);

        // The dispatcher uses reflection (method.Invoke), which wraps thrown exceptions
        // in TargetInvocationException — assert on the base Exception type to be safe.
        await Should.ThrowAsync<Exception>(() =>
            d.DispatchAsync([new EventA(DateTime.UtcNow)], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Catch_all_handler_for_IDomainEvent_is_NOT_invoked_for_concrete_events()
    {
        var catchAll = new CatchAll();
        var sp = BuildSp(sc => sc.AddSingleton<IDomainEventHandler<IDomainEvent>>(catchAll));
        var d = Make(sp);

        await d.DispatchAsync([new EventA(DateTime.UtcNow)], TestContext.Current.CancellationToken);

        catchAll.Received.ShouldBeEmpty();
    }

    [Fact]
    public async Task Invokes_each_registered_handler_once_per_event()
    {
        var h1 = new HandlerA();
        var h2 = new HandlerA();
        var sp = BuildSp(sc =>
        {
            sc.AddSingleton<IDomainEventHandler<EventA>>(h1);
            sc.AddSingleton<IDomainEventHandler<EventA>>(h2);
        });
        var d = Make(sp);

        await d.DispatchAsync([new EventA(DateTime.UtcNow), new EventA(DateTime.UtcNow)],
            TestContext.Current.CancellationToken);

        h1.Received.Count.ShouldBe(2);
        h2.Received.Count.ShouldBe(2);
    }
}
