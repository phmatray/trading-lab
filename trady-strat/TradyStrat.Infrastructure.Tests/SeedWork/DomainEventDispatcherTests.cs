using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

    /// <summary>Minimal in-memory logger for assertions; mirrors the pattern in McpLoggingFilterTests.</summary>
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public sealed record Entry(LogLevel Level, string Message, Exception? Exception);
        private readonly List<Entry> _entries = new();
        public IReadOnlyList<Entry> Entries => _entries;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _entries.Add(new Entry(logLevel, formatter(state, exception), exception));
    }

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
    public async Task Bubbles_handler_exceptions_unwrapped()
    {
        var sp = BuildSp(sc => sc.AddSingleton<IDomainEventHandler<EventA>, HandlerAFails>());
        var d = Make(sp);

        // Reflection wraps synchronous handler throws in TargetInvocationException;
        // the dispatcher unwraps them so callers see the actual handler exception.
        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            d.DispatchAsync([new EventA(DateTime.UtcNow)], TestContext.Current.CancellationToken));
        ex.Message.ShouldBe("boom");
    }

    [Fact]
    public async Task Logs_Error_when_handler_throws_with_event_metadata()
    {
        var logger = new RecordingLogger<DomainEventDispatcher>();
        var sp = BuildSp(sc => sc.AddSingleton<IDomainEventHandler<EventA>, HandlerAFails>());
        var d = new DomainEventDispatcher(sp, logger);
        var evt = new EventA(DateTime.UtcNow);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            d.DispatchAsync([evt], TestContext.Current.CancellationToken));

        var error = logger.Entries.Where(e => e.Level == LogLevel.Error).ShouldHaveSingleItem();
        error.Message.ShouldContain(nameof(EventA));
        error.Message.ShouldContain(nameof(HandlerAFails));
        error.Message.ShouldContain(evt.EventId.ToString());
        error.Exception.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task Empty_event_list_is_a_noop_and_does_not_log()
    {
        var logger = new RecordingLogger<DomainEventDispatcher>();
        var sp = BuildSp(_ => { });
        var d = new DomainEventDispatcher(sp, logger);

        await d.DispatchAsync([], TestContext.Current.CancellationToken);

        logger.Entries.ShouldBeEmpty();
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
