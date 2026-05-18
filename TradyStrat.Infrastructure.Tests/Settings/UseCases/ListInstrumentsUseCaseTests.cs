using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Application.UseCases;
using TradyStrat.Infrastructure.Data;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.TestKit;          // TestRepo<T>
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings.UseCases;

public class ListInstrumentsUseCaseTests
{
    // Ordinal string sort: 'I' (0x49) < 'N' (0x4E), so "COIN" precedes "CON3.L".
    private static readonly string[] ExpectedTickersSorted = ["BTC-USD", "COIN", "CON3.L"];

    [Fact]
    public async Task Returns_instruments_sorted_by_ticker()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        db.Instruments.Add(Make("CON3.L"));
        db.Instruments.Add(Make("BTC-USD"));
        db.Instruments.Add(Make("COIN"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new ListInstrumentsUseCase(
            new TestRepo<Instrument>(db),
            NullLogger<ListInstrumentsUseCase>.Instance);

        var list = await sut.ExecuteAsync(Unit.Value, TestContext.Current.CancellationToken);

        list.Select(i => i.Ticker).ShouldBe(ExpectedTickersSorted);
    }

    private static Instrument Make(string ticker) => new()
    {
        Id = 0, Ticker = ticker, Name = ticker, Currency = "USD",
        Exchange = "NMS", TimezoneId = "America/New_York",
        Kind = InstrumentKind.Held, AddedAt = DateTime.UtcNow,
    };
}
