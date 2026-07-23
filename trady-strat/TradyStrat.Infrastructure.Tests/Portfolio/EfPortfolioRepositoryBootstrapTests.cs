using Microsoft.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Portfolio.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Infrastructure.Portfolio;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Portfolio;

public class EfPortfolioRepositoryBootstrapTests
{
    private sealed class PortfolioCreatedSpy : IDomainEventHandler<PortfolioCreated>
    {
        public List<PortfolioCreated> Received { get; } = new();
        public Task HandleAsync(PortfolioCreated evt, CancellationToken ct)
        { Received.Add(evt); return Task.CompletedTask; }
    }

    private sealed class DispatchingDispatcher(PortfolioCreatedSpy spy) : IDomainEventDispatcher
    {
        public async Task DispatchAsync(IReadOnlyList<IDomainEvent> events, CancellationToken ct)
        {
            foreach (var e in events)
                if (e is PortfolioCreated pc) await spy.HandleAsync(pc, ct);
        }
    }

    [Fact]
    public async Task First_GetAsync_persists_singleton_and_dispatches_PortfolioCreated()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));
        var spy = new PortfolioCreatedSpy();
        var repo = new EfPortfolioRepository(db, clock, new DispatchingDispatcher(spy));

        var portfolio = await repo.GetAsync(ct);

        portfolio.Id.ShouldBe(PortfolioId.Singleton);
        spy.Received.ShouldHaveSingleItem()
            .PortfolioId.ShouldBe(PortfolioId.Singleton);
        spy.Received[0].OccurredAt.ShouldBe(clock.UtcNow());
        portfolio.DomainEvents.ShouldBeEmpty();   // drained
        (await db.Portfolios.SingleAsync(ct)).Id.ShouldBe(PortfolioId.Singleton);
    }

    [Fact]
    public async Task Second_GetAsync_returns_existing_singleton_without_dispatching()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var clock = new FakeClock(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc));
        var spy = new PortfolioCreatedSpy();
        var repo = new EfPortfolioRepository(db, clock, new DispatchingDispatcher(spy));

        _ = await repo.GetAsync(ct);   // bootstrap
        spy.Received.Clear();          // reset
        _ = await repo.GetAsync(ct);   // second call

        spy.Received.ShouldBeEmpty();
    }
}
