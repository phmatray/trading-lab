using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings;
using TradyStrat.Application.Trades.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Portfolio.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Infrastructure.Portfolio;
using TradyStrat.Infrastructure.Settings;
using TradyStrat.Infrastructure.SeedWork;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Application.Tests.Trades;

public class LogTradeUseCaseDispatchTests
{
    private sealed class TradeRecordedSpy : IDomainEventHandler<TradeRecorded>
    {
        public List<TradeRecorded> Received { get; } = new();
        public Task HandleAsync(TradeRecorded evt, CancellationToken ct)
        { Received.Add(evt); return Task.CompletedTask; }
    }

    [Fact]
    public async Task LogTrade_persists_and_dispatches_TradeRecorded_and_drains_AR()
    {
        await using var db = InMemoryDb.Create();

        // Seed instrument (Existing → no events).
        // Use EUR so price and fees share the same currency (fees are always EUR in LogTradeUseCase).
        db.Instruments.Add(Instrument.Existing(
            id:          new InstrumentId(42),
            ticker:      "ADYEN",
            name:        "Adyen",
            currency:    Currency.Eur,
            exchange:    Exchange.Of("AMS"),
            timezoneId:  TimezoneId.Of("Europe/Amsterdam"),
            kind:        InstrumentKind.Held,
            addedAt:     new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var spy = new TradeRecordedSpy();
        var sc = new ServiceCollection();
        sc.AddSingleton<AppDbContext>(db);
        sc.AddScoped<IPortfolioRepository, EfPortfolioRepository>();
        sc.AddScoped<IInstrumentRepository, EfInstrumentRepository>();
        sc.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc)));
        sc.AddSingleton<IDomainEventHandler<TradeRecorded>>(spy);
        sc.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        sc.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        var sp = sc.BuildServiceProvider();

        var uc = new LogTradeUseCase(
            sp.GetRequiredService<IPortfolioRepository>(),
            sp.GetRequiredService<IInstrumentRepository>(),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<IDomainEventDispatcher>(),
            NullLogger<LogTradeUseCase>.Instance);

        var result = await uc.ExecuteAsync(new LogTradeInput(
            InstrumentId:   42,
            ExecutedOn:     new DateOnly(2026, 5, 25),
            Side:           TradeSide.Buy,
            Quantity:       5m,
            PricePerShare:  100m,
            FeesEur:        0m,
            Note:           "t1"),
            TestContext.Current.CancellationToken);

        // Persisted
        var portfolio = await sp.GetRequiredService<IPortfolioRepository>().GetAsync(TestContext.Current.CancellationToken);
        portfolio.Positions.ShouldHaveSingleItem().Trades.ShouldHaveSingleItem();

        // Dispatched
        spy.Received.ShouldHaveSingleItem().TradeId.ShouldBe(result.TradeId);

        // Drained
        portfolio.DomainEvents.ShouldBeEmpty();
    }
}
