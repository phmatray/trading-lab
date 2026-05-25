using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Trades.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Portfolio;
using TradyStrat.Infrastructure.Settings;
using TradyStrat.TestKit.SeedWork;
using TradyStrat.TestKit.Settings;
using TradyStrat.TestKit.Specifications;
using TradyStrat.TestKit.Time;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Trades;

public class ImportTradesCsvUseCaseTests
{
    [Fact]
    public async Task Rows_without_ticker_column_route_to_focus_instrument()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        db.Instruments.Add(Existing("CON3.L"));
        await db.SaveChangesAsync(ct);

        const string csv = """
            date,side,qty,price,fees
            2026-05-01,buy,10,100,1
            """;

        var sut = Build(db);
        var result = await sut.ExecuteAsync(new ImportTradesCsvInput(csv), ct);

        result.RowsImported.ShouldBe(1);
        var positions = await db.Set<Position>().ToListAsync(ct);
        var focus = await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct);
        positions.Count.ShouldBe(1);
        positions[0].InstrumentId.ShouldBe(focus.Id);
    }

    [Fact]
    public async Task Rows_with_ticker_column_route_to_matching_instrument()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        db.Instruments.Add(Existing("CON3.L"));
        db.Instruments.Add(Existing("COIN"));
        await db.SaveChangesAsync(ct);

        const string csv = """
            date,side,qty,price,fees,ticker
            2026-05-01,buy,10,100,1,CON3.L
            2026-05-02,buy,5,200,2,COIN
            """;

        var sut = Build(db);
        var result = await sut.ExecuteAsync(new ImportTradesCsvInput(csv), ct);

        result.RowsImported.ShouldBe(2);
        var positions = await db.Set<Position>().ToListAsync(ct);
        var con3 = await db.Instruments.SingleAsync(i => i.Ticker == "CON3.L", ct);
        var coin = await db.Instruments.SingleAsync(i => i.Ticker == "COIN", ct);
        positions.Count.ShouldBe(2);
        positions.Select(p => p.InstrumentId).ShouldBe([con3.Id, coin.Id], ignoreOrder: true);
    }

    [Fact]
    public async Task Unknown_ticker_throws_CsvImportException()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = InMemoryDb.Create();
        db.Instruments.Add(Existing("CON3.L"));
        await db.SaveChangesAsync(ct);

        const string csv = """
            date,side,qty,price,fees,ticker
            2026-05-01,buy,10,100,1,UNKNOWN
            """;

        var sut = Build(db);
        var ex = await Should.ThrowAsync<CsvImportException>(
            () => sut.ExecuteAsync(new ImportTradesCsvInput(csv), ct));

        ex.Message.ShouldContain("UNKNOWN");
    }

    private static ImportTradesCsvUseCase Build(Infrastructure.Data.AppDbContext db)
    {
        var clock = new FakeClock(DateTime.UtcNow);
        return new(
            new EfPortfolioRepository(db, clock, NullDomainEventDispatcher.Instance),
            new EfInstrumentRepository(db),
            clock,
            new FakeFocusTickerRepository("CON3.L"),
            NullDomainEventDispatcher.Instance,
            NullLogger<ImportTradesCsvUseCase>.Instance);
    }

    private static Instrument Existing(string ticker)
        => Instrument.Existing(
            id:         new InstrumentId(0),
            ticker:     ticker,
            name:       ticker,
            currency:   Currency.Eur,
            exchange:   Exchange.Of("X"),
            timezoneId: TimezoneId.Of("Etc/UTC"),
            kind:       InstrumentKind.Held,
            addedAt:    DateTime.UtcNow);
}
