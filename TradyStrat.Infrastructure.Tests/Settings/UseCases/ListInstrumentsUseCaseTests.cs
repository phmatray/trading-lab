using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Settings;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Settings.UseCases;

public class ListInstrumentsUseCaseTests
{
    // Ordinal string sort: 'I' (0x49) < 'N' (0x4E), so "COIN" precedes "CON3.L".
    private static readonly string[] ExpectedTickersSorted = ["BTC-USD", "COIN", "CON3.L"];

    [Fact]
    public async Task Returns_instruments_sorted_by_ticker()
    {
        await using var db = InMemoryDb.Create();

        db.Instruments.Add(Make("CON3.L"));
        db.Instruments.Add(Make("BTC-USD"));
        db.Instruments.Add(Make("COIN"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sut = new ListInstrumentsUseCase(
            new EfInstrumentRepository(db),
            NullLogger<ListInstrumentsUseCase>.Instance);

        var list = await sut.ExecuteAsync(Unit.Value, TestContext.Current.CancellationToken);

        list.Select(i => i.Ticker).ShouldBe(ExpectedTickersSorted);
    }

    private static Instrument Make(string ticker)
        => Instrument.Existing(
            id:         new InstrumentId(0),
            ticker:     ticker,
            name:       ticker,
            currency:   Currency.Usd,
            exchange:   Exchange.Of("NMS"),
            timezoneId: TimezoneId.Of("America/New_York"),
            kind:       InstrumentKind.Held,
            addedAt:    DateTime.UtcNow);
}
