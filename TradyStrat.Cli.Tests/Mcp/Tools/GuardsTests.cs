using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Cli.Mcp.Tools;
using TradyStrat.Domain;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Settings;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Tools;

public sealed class GuardsTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static Instrument MakeInstrument(int id, string ticker)
        => Instrument.Existing(
            id:         new InstrumentId(id),
            ticker:     ticker,
            name:       ticker,
            currency:   Currency.Usd,
            exchange:   Exchange.Of("TEST"),
            timezoneId: TimezoneId.Of("America/New_York"),
            kind:       InstrumentKind.Watchlist,
            addedAt:    DateTime.UtcNow);

    private static async Task<Guards> BuildGuardsAsync(params Instrument[] instruments)
    {
        var db = InMemoryDb.Create();
        db.Instruments.AddRange(instruments);
        await db.SaveChangesAsync();

        var repo = new EfInstrumentRepository(db);
        var useCase = new ListInstrumentsUseCase(repo, NullLogger<ListInstrumentsUseCase>.Instance);
        return new Guards(useCase);
    }

    private static readonly Instrument[] ThreeInstruments =
    [
        MakeInstrument(1, "CON3.L"),
        MakeInstrument(2, "COIN"),
        MakeInstrument(3, "BTC-USD"),
    ];

    // ---------------------------------------------------------------------------
    // ResolveInstrumentOrThrow
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Known_ticker_resolves_to_its_Instrument()
    {
        var guards = await BuildGuardsAsync(ThreeInstruments);

        var result = await guards.ResolveInstrumentOrThrow("CON3.L", CancellationToken.None);

        result.Ticker.ShouldBe("CON3.L");
    }

    [Fact]
    public async Task Unknown_ticker_throws_ArgumentException_with_actionable_message()
    {
        var guards = await BuildGuardsAsync(ThreeInstruments);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => guards.ResolveInstrumentOrThrow("XYZ", CancellationToken.None));

        ex.Message.ShouldContain("XYZ");
        ex.Message.ShouldContain("list_instruments");
    }

    // ---------------------------------------------------------------------------
    // ResolveDateRange
    // ---------------------------------------------------------------------------

    [Fact]
    public void ResolveDateRange_null_null_uses_defaults()
    {
        var today = new DateOnly(2026, 5, 18);

        var (from, to) = Guards.ResolveDateRange(null, null, defaultBack: 90, clockToday: today);

        to.ShouldBe(new DateOnly(2026, 5, 18));
        from.ShouldBe(new DateOnly(2026, 2, 17)); // 90 days before 2026-05-18
    }

    [Fact]
    public void ResolveDateRange_parses_ISO_strings()
    {
        var today = new DateOnly(2026, 5, 18);

        var (from, to) = Guards.ResolveDateRange("2026-05-01", "2026-05-10", defaultBack: 90, clockToday: today);

        from.ShouldBe(new DateOnly(2026, 5, 1));
        to.ShouldBe(new DateOnly(2026, 5, 10));
    }

    [Fact]
    public void ResolveDateRange_throws_on_inverted_range()
    {
        var today = new DateOnly(2026, 5, 18);

        var ex = Should.Throw<ArgumentException>(
            () => Guards.ResolveDateRange("2026-05-10", "2026-05-01", defaultBack: 90, clockToday: today));

        ex.Message.ShouldContain("from");
        ex.Message.ShouldContain("2026-05-10");
        ex.Message.ShouldContain("2026-05-01");
    }

    [Fact]
    public void ResolveDateRange_throws_on_bad_date_format()
    {
        var today = new DateOnly(2026, 5, 18);

        var ex = Should.Throw<ArgumentException>(
            () => Guards.ResolveDateRange("tomorrow", null, defaultBack: 90, clockToday: today));

        ex.Message.ShouldContain("tomorrow");
        ex.Message.ShouldContain("YYYY-MM-DD");
    }
}
